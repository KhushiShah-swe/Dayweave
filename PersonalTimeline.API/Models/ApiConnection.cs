public class ApiConnection
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ApiProvider { get; set; } = string.Empty; // Fix
    public string? AccessToken { get; set; } // Fix
    public string? RefreshToken { get; set; } // Fix
    public DateTime TokenExpiresAt { get; set; }
    public DateTime LastSyncAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public string? Settings { get; set; } // Fix

    // Navigation property
    public User User { get; set; } = null!; // Fix
}
