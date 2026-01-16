using SQLite;

public class JournalEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public DateTime EntryDate { get; set; }
    
    public string? Content { get; set; }

    public string? PrimaryMood { get; set; }
    public string? SecondaryMood1 { get; set; }
    public string? SecondaryMood2 { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string? Tags { get; set; }

    public string? Category { get; set; }

    [Indexed]
    public int UserId { get; set; }
}

public static class MoodCategories
{
    public static readonly List<string> PositiveMoods = new() 
    { 
        "Happy", "Excited", "Grateful", "Hopeful", "Peaceful", "Energetic", "Inspired", "Loved"
    };

    public static readonly List<string> NeutralMoods = new()
    {
        "Calm", "Neutral", "Curious", "Focused", "Determined", "Patient"
    };

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

public static class PreBuiltTags
{
    public static readonly List<string> AllTags = new()
    {
        "Work", "Career", "Studies", "Family", "Friends", "Relationships", 
        "Health", "Fitness", "Personal Growth", "Self-care", "Hobbies", 
        "Travel", "Nature", "Finance", "Spirituality", "Birthday", "Holiday", 
        "Vacation", "Celebration", "Exercise", "Reading", "Writing", "Cooking", 
        "Meditation", "Yoga", "Music", "Shopping", "Parenting", "Projects", 
        "Planning", "Reflection"
    };

    public static readonly Dictionary<string, List<string>> TagsByCategory = new()
    {
        { "Work & Career", new List<string> { "Work", "Career", "Studies", "Projects", "Planning" } },
        { "Relationships", new List<string> { "Family", "Friends", "Relationships", "Parenting" } },
        { "Health & Wellness", new List<string> { "Health", "Fitness", "Exercise", "Meditation", "Yoga", "Self-care" } },
        { "Personal", new List<string> { "Personal Growth", "Reflection", "Spirituality" } },
        { "Activities", new List<string> { "Hobbies", "Reading", "Writing", "Cooking", "Music", "Shopping" } },
        { "Travel & Events", new List<string> { "Travel", "Nature", "Birthday", "Holiday", "Vacation", "Celebration" } },
        { "Other", new List<string> { "Finance" } }
    };
}
