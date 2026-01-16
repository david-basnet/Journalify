using SQLite;

public class JournalDatabase
{
    private SQLiteAsyncConnection _database;
    private string _dbPath;

    public JournalDatabase(string dbPath)
    {
        _dbPath = dbPath;
        _database = new SQLiteAsyncConnection(dbPath);
        _database.CreateTableAsync<JournalEntry>().Wait();
    }

    public string GetDatabasePath() => _dbPath;

    public async Task<int> SaveEntryAsync(JournalEntry entry, int userId)
    {
        if (string.IsNullOrWhiteSpace(entry.Content))
            throw new ArgumentException("Entry content cannot be empty.");
        
        if (string.IsNullOrWhiteSpace(entry.PrimaryMood))
            throw new ArgumentException("Primary mood is required.");

        entry.UserId = userId;
        entry.CreatedAt = DateTime.Now;
        entry.UpdatedAt = DateTime.Now;
        
        var existingEntry = await GetTodayEntryAsync(userId);
        if (existingEntry != null)
        {
            throw new InvalidOperationException("An entry already exists for today. Please update the existing entry instead.");
        }
        
        return await _database.InsertAsync(entry);
    }

    public Task<JournalEntry> GetTodayEntryAsync(int userId)
    {
        var today = DateTime.Today;
        return _database.Table<JournalEntry>()
            .Where(e => e.EntryDate == today && e.UserId == userId)
            .FirstOrDefaultAsync();
    }

    public Task<JournalEntry> GetEntryByIdAsync(int id, int userId)
    {
        return _database.Table<JournalEntry>()
            .Where(e => e.Id == id && e.UserId == userId)
            .FirstOrDefaultAsync();
    }

    public Task<List<JournalEntry>> GetEntriesByDateAsync(DateTime date, int userId)
    {
        return _database.Table<JournalEntry>()
            .Where(e => e.EntryDate == date && e.UserId == userId)
            .ToListAsync();
    }

    public Task<List<JournalEntry>> GetEntriesByDateRangeAsync(DateTime startDate, DateTime endDate, int userId)
    {
        return _database.Table<JournalEntry>()
            .Where(e => e.EntryDate >= startDate && e.EntryDate <= endDate && e.UserId == userId)
            .OrderByDescending(e => e.EntryDate)
            .ToListAsync();
    }

    public Task<List<JournalEntry>> GetAllEntriesAsync(int userId)
    {
        return _database.Table<JournalEntry>()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.EntryDate)
            .ToListAsync();
    }

    public Task<List<JournalEntry>> SearchEntriesAsync(string searchTerm, int userId)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return GetAllEntriesAsync(userId);

        return _database.Table<JournalEntry>()
            .Where(e => e.UserId == userId && 
                       ((e.Content != null && e.Content.Contains(searchTerm)) || 
                        (e.Category != null && e.Category.Contains(searchTerm))))
            .OrderByDescending(e => e.EntryDate)
            .ToListAsync();
    }

    public Task<List<JournalEntry>> GetEntriesByMoodAsync(string mood, int userId)
    {
        return _database.Table<JournalEntry>()
            .Where(e => e.UserId == userId && 
                       (e.PrimaryMood == mood || e.SecondaryMood1 == mood || e.SecondaryMood2 == mood))
            .OrderByDescending(e => e.EntryDate)
            .ToListAsync();
    }

    public Task<List<JournalEntry>> GetEntriesByTagAsync(string tag, int userId)
    {
        return _database.Table<JournalEntry>()
            .Where(e => e.UserId == userId && e.Tags != null && e.Tags.Contains(tag))
            .OrderByDescending(e => e.EntryDate)
            .ToListAsync();
    }

    public Task<int> GetEntryCountAsync(int userId)
    {
        return _database.Table<JournalEntry>()
            .Where(e => e.UserId == userId)
            .CountAsync();
    }

    public async Task<int> UpdateEntryAsync(JournalEntry entry, int userId)
    {
        if (entry.Id <= 0)
            throw new ArgumentException("Entry ID must be valid.");

        if (string.IsNullOrWhiteSpace(entry.Content))
            throw new ArgumentException("Entry content cannot be empty.");

        if (string.IsNullOrWhiteSpace(entry.PrimaryMood))
            throw new ArgumentException("Primary mood is required.");

        entry.UserId = userId;
        entry.UpdatedAt = DateTime.Now;
        return await _database.UpdateAsync(entry);
    }

    public async Task<int> DeleteEntryAsync(JournalEntry entry, int userId)
    {
        if (entry.UserId != userId)
            throw new UnauthorizedAccessException("You do not have permission to delete this entry.");
        
        return await _database.DeleteAsync(entry);
    }

    public async Task<int> DeleteEntryByIdAsync(int id, int userId)
    {
        var entry = await GetEntryByIdAsync(id, userId);
        if (entry == null)
            throw new ArgumentException("Entry not found.");
        
        return await _database.DeleteAsync(entry);
    }

    public async Task<DatabaseStats> GetStatisticsAsync(int userId)
    {
        var entries = await GetAllEntriesAsync(userId);
        var moodCounts = entries
            .GroupBy(e => e.PrimaryMood)
            .Select(g => new MoodCount { Mood = g.Key ?? "Unknown", Count = g.Count() })
            .ToList();

        return new DatabaseStats
        {
            TotalEntries = entries.Count,
            MoodDistribution = moodCounts,
            OldestEntry = entries.LastOrDefault()?.EntryDate,
            NewestEntry = entries.FirstOrDefault()?.EntryDate
        };
    }

    public async Task<StreakInfo> GetStreakInfoAsync(int userId)
    {
        var entries = await GetAllEntriesAsync(userId);
        if (entries.Count == 0)
        {
            return new StreakInfo { CurrentStreak = 0, LongestStreak = 0, MissedDays = new List<DateTime>() };
        }

        var entryDates = entries.Select(e => e.EntryDate.Date).Distinct().OrderByDescending(d => d).ToList();
        
        int currentStreak = 0;
        var today = DateTime.Today;
        var checkDate = today;
        
        while (entryDates.Contains(checkDate))
        {
            currentStreak++;
            checkDate = checkDate.AddDays(-1);
        }

        int longestStreak = 0;
        int tempStreak = 0;
        var sortedDates = entryDates.OrderBy(d => d).ToList();
        
        if (sortedDates.Count > 0)
        {
            var previousDate = sortedDates[0];
            tempStreak = 1;
            longestStreak = 1;

            for (int i = 1; i < sortedDates.Count; i++)
            {
                var currentDate = sortedDates[i];
                var daysDiff = (currentDate - previousDate).Days;

                if (daysDiff == 1)
                {
                    tempStreak++;
                    longestStreak = Math.Max(longestStreak, tempStreak);
                }
                else
                {
                    tempStreak = 1;
                }

                previousDate = currentDate;
            }
        }

        var missedDays = new List<DateTime>();
        if (entryDates.Count > 0)
        {
            var firstEntry = entryDates.Min();
            var lastEntry = entryDates.Max();
            var checkDate2 = firstEntry;
            
            while (checkDate2 <= today)
            {
                if (!entryDates.Contains(checkDate2) && checkDate2 <= today)
                {
                    missedDays.Add(checkDate2);
                }
                checkDate2 = checkDate2.AddDays(1);
            }
        }

        return new StreakInfo
        {
            CurrentStreak = currentStreak,
            LongestStreak = longestStreak,
            MissedDays = missedDays.OrderByDescending(d => d).ToList()
        };
    }
}

public class StreakInfo
{
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public List<DateTime> MissedDays { get; set; } = new();
}

public class MoodCount
{
    public string? Mood { get; set; }
    public int Count { get; set; }
}

public class DatabaseStats
{
    public int TotalEntries { get; set; }
    public List<MoodCount>? MoodDistribution { get; set; }
    public DateTime? OldestEntry { get; set; }
    public DateTime? NewestEntry { get; set; }
}
