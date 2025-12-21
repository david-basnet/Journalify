using SQLite;

public class JournalEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public DateTime EntryDate { get; set; }
    
    public string? Content { get; set; }

    // Mood tracking
    public string? PrimaryMood { get; set; }
    public string? SecondaryMood1 { get; set; }
    public string? SecondaryMood2 { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Tags (comma-separated for simplicity)
    public string? Tags { get; set; }

    // Category
    public string? Category { get; set; }
}

// Mood categories
public static class MoodCategories
{
    // Positive moods
    public static readonly List<string> PositiveMoods = new() 
    { 
        "Happy", "Excited", "Grateful", "Hopeful", "Peaceful", "Energetic", "Inspired", "Loved"
    };

    // Neutral moods
    public static readonly List<string> NeutralMoods = new()
    {
        "Calm", "Neutral", "Curious", "Focused", "Determined", "Patient"
    };

    // Negative moods
    public static readonly List<string> NegativeMoods = new()
    {
        "Sad", "Angry", "Frustrated", "Anxious", "Tired", "Confused", "Disappointed", "Stressed"
    };

    public static string GetMoodCategory(string mood)
    {
        if (PositiveMoods.Contains(mood)) return "Positive";
        if (NeutralMoods.Contains(mood)) return "Neutral";
        if (NegativeMoods.Contains(mood)) return "Negative";
        return "Unknown";
    }
}
