using System.Security.Cryptography;
using System.Text;
using SQLite;

public class AuthenticationService
{
    private SQLiteAsyncConnection? _database;
    private User? _currentUser;
    private const string CURRENT_USER_KEY = "current_user_id";

    private bool _isInitialized = false;
    private SemaphoreSlim _initSemaphore = new SemaphoreSlim(1, 1);

    public AuthenticationService(JournalDatabase journalDb)
    {
        if (journalDb == null)
            throw new ArgumentNullException(nameof(journalDb), "JournalDatabase cannot be null.");

        try
        {
            var dbPath = journalDb.GetDatabasePath();
            if (string.IsNullOrWhiteSpace(dbPath))
                throw new ArgumentException("Database path cannot be null or empty.", nameof(journalDb));

            _database = new SQLiteAsyncConnection(dbPath);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing AuthenticationService: {ex.Message}");
            _database = null;
        }
    }

    private async Task InitializeDatabaseAsync()
    {
        if (_isInitialized) return;
        
        if (_database == null)
        {
            System.Diagnostics.Debug.WriteLine("Database is null in InitializeDatabaseAsync");
            throw new InvalidOperationException("Database connection not initialized. Please restart the application.");
        }

        await _initSemaphore.WaitAsync();
        try
        {
            if (_isInitialized) return;
            
            try
            {
                await _database.CreateTableAsync<User>();
                _isInitialized = true;
                System.Diagnostics.Debug.WriteLine("User table created successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating User table: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
        finally
        {
            _initSemaphore.Release();
        }
    }

    private async Task EnsureInitializedAsync()
    {
        if (_database == null)
            throw new InvalidOperationException("Database connection not initialized.");

        if (!_isInitialized)
        {
            await InitializeDatabaseAsync();
        }
    }

    public User? CurrentUser => _currentUser;

    public bool IsAuthenticated => _currentUser != null;

        public async Task<bool> SignUpAsync(string username, string password, string? email = null)
    {
        await EnsureInitializedAsync();

        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            throw new ArgumentException("Username must be at least 3 characters long.");

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            throw new ArgumentException("Password must be at least 6 characters long.");

        User? existingUser = null;
        try
        {
            if (_database == null) throw new InvalidOperationException("Database connection not initialized.");
            existingUser = await _database.Table<User>()
                .Where(u => u.Username == username)
                .FirstOrDefaultAsync();
        }
        catch (SQLiteException)
        {
            throw new InvalidOperationException("Database error. Please try again later.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking existing user: {ex}");
            throw new InvalidOperationException("Database error. Please try again later.");
        }

        if (existingUser != null)
            throw new InvalidOperationException("Username already exists. Please choose a different username.");

        try
        {
            var user = new User
            {
                Username = username.Trim(),
                PasswordHash = HashPassword(password),
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                CreatedAt = DateTime.Now,
                LastLoginAt = DateTime.Now
            };

            await _database.InsertAsync(user);
            return true;
        }
        catch (SQLiteException ex)
        {
            if (ex.Message.Contains("UNIQUE constraint failed"))
            {
                throw new InvalidOperationException("Username already exists. Please choose a different username.");
            }
            throw new InvalidOperationException("Database error occurred. Please try again.");
        }
    }

        public async Task<SignInResult> SignInAsync(string username, string password)
    {
        await EnsureInitializedAsync();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return new SignInResult { Success = false, Message = "Username and password are required." };

        User? user = null;
        try
        {
            if (_database == null) throw new InvalidOperationException("Database connection not initialized.");
            user = await _database.Table<User>()
                .Where(u => u.Username == username)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during sign in: {ex}");
            return new SignInResult { Success = false, Message = "Database error occurred. Please try again." };
        }

        if (user == null)
            return new SignInResult { Success = false, Message = "Username not found." };

        if (!VerifyPassword(password, user.PasswordHash))
            return new SignInResult { Success = false, Message = "Password incorrect." };

        user.LastLoginAt = DateTime.Now;
        await _database.UpdateAsync(user);
        _currentUser = user;
        Preferences.Set(CURRENT_USER_KEY, user.Id.ToString());

        return new SignInResult { Success = true };
    }

    public async Task<bool> ResetPasswordAsync(string username, string newPassword)
    {
        await EnsureInitializedAsync();

        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.");

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            throw new ArgumentException("New password must be at least 6 characters long.");

        User? user = null;
        try
        {
            if (_database == null) throw new InvalidOperationException("Database connection not initialized.");
            user = await _database.Table<User>()
                .Where(u => u.Username == username)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error finding user for password reset: {ex}");
            throw new InvalidOperationException("Database error occurred. Please try again.");
        }

        if (user == null)
            throw new InvalidOperationException("Username not found.");

        user.PasswordHash = HashPassword(newPassword);
        await _database.UpdateAsync(user);

        return true;
    }

        public void SignOut()
    {
        _currentUser = null;
        Preferences.Remove(CURRENT_USER_KEY);
    }

        public async Task<bool> RestoreSessionAsync()
    {
        try
        {
            if (_database == null)
            {
                System.Diagnostics.Debug.WriteLine("Database not initialized in RestoreSessionAsync");
                return false;
            }

            await EnsureInitializedAsync();

            var userIdStr = Preferences.Get(CURRENT_USER_KEY, string.Empty);
            if (string.IsNullOrEmpty(userIdStr))
                return false;

            if (!int.TryParse(userIdStr, out int userId))
            {
                Preferences.Remove(CURRENT_USER_KEY);
                return false;
            }

            User? user = null;
            try
            {
                user = await _database.Table<User>()
                    .Where(u => u.Id == userId)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error querying user in RestoreSessionAsync: {ex}");
                Preferences.Remove(CURRENT_USER_KEY);
                return false;
            }

            if (user != null)
            {
                _currentUser = user;
                return true;
            }

            Preferences.Remove(CURRENT_USER_KEY);
            return false;
        }
        catch (InvalidOperationException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database not initialized: {ex.Message}");
            Preferences.Remove(CURRENT_USER_KEY);
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in RestoreSessionAsync: {ex}");
            Preferences.Remove(CURRENT_USER_KEY);
            return false;
        }
    }

        public async Task<User?> GetUserByIdAsync(int userId)
    {
        await EnsureInitializedAsync();
        
        if (_database == null)
            return null;
        
        try
        {
            return await _database.Table<User>()
                .Where(u => u.Id == userId)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting user by ID: {ex}");
            return null;
        }
    }

        private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

        private bool VerifyPassword(string password, string hash)
    {
        var passwordHash = HashPassword(password);
        return passwordHash == hash;
    }

        public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        if (_currentUser == null)
            throw new InvalidOperationException("No user is currently signed in.");

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            throw new ArgumentException("New password must be at least 6 characters long.");

        if (!VerifyPassword(currentPassword, _currentUser.PasswordHash))
            throw new ArgumentException("Current password is incorrect.");

        _currentUser.PasswordHash = HashPassword(newPassword);
        if (_database == null) throw new InvalidOperationException("Database connection not initialized.");
        await _database.UpdateAsync(_currentUser);

        return true;
    }
}

public class SignInResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

