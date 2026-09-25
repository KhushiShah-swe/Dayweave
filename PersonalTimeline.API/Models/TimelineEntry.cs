using System.Text.Json;

public class TimelineEntry
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty; // Fix
    public string? Description { get; set; } // Fix
    public DateTime EventDate { get; set; }
    public string EntryType { get; set; } = string.Empty; // Fix
    public string? Category { get; set; } // Fix
    public string? ImageUrl { get; set; } // Fix
    public string? ExternalUrl { get; set; } // Fix
    public string? SourceApi { get; set; } // Fix
    public string? ExternalId { get; set; } // Fix
    public string? Metadata { get; set; } // Fix

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User User { get; set; } = null!; // Fix
}
