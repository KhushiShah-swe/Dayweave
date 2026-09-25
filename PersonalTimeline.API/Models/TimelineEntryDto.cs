public class TimelineEntryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty; // Fix
    public string? Description { get; set; } // Fix
    public DateTime EventDate { get; set; }
    public string EntryType { get; set; } = string.Empty; // Fix
    public string? Category { get; set; } // Fix
    public string? ImageUrl { get; set; } // Fix
    public string? ExternalUrl { get; set; } // Fix
    public string SourceApi { get; set; } = "Manual";
}
