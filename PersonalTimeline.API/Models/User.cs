using System.Collections.Generic;

public class User
{
    public int Id { get; set; }
    public string OAuthProvider { get; set; } = string.Empty; // Fix
    public string OAuthId { get; set; } = string.Empty; // Fix
    public string Email { get; set; } = string.Empty; // Fix
    public string DisplayName { get; set; } = string.Empty; // Fix
    public string? ProfileImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<TimelineEntry> TimelineEntries { get; set; } = new List<TimelineEntry>(); // Fix
    public ICollection<ApiConnection> ApiConnections { get; set; } = new List<ApiConnection>(); // Fix
}
