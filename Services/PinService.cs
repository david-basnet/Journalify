using System.Security.Cryptography;
using System.Text;

public class PinService
{
    private const string PIN_HASH_KEY = "journal_pin_hash";
    private const string PIN_SET_KEY = "journal_pin_set";

    public bool IsPinSet()
    {
        return Preferences.Get(PIN_SET_KEY, false);
    }

    public bool CreatePin(string pin, string confirmPin)
    {
        if (string.IsNullOrWhiteSpace(pin))
            return false;

        if (pin.Length < 4)
            return false;

        if (pin != confirmPin)
            return false;

        try
        {
            var pinHash = HashPin(pin);
            Preferences.Set(PIN_HASH_KEY, pinHash);
            Preferences.Set(PIN_SET_KEY, true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool VerifyPin(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin))
            return false;

        if (!IsPinSet())
            return false;

        try
        {
            var storedHash = Preferences.Get(PIN_HASH_KEY, string.Empty);
            if (string.IsNullOrEmpty(storedHash))
                return false;

            var inputHash = HashPin(pin);
            return inputHash == storedHash;
        }
        catch
        {
            return false;
        }
    }

    public void ResetPin()
    {
        Preferences.Remove(PIN_HASH_KEY);
        Preferences.Set(PIN_SET_KEY, false);
    }

    private string HashPin(string pin)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(pin);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}

