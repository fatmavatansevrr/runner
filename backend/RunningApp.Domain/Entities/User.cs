namespace RunningApp.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string ExternalAuthProvider { get; set; } = "firebase";
    public string ExternalUserId { get; set; } = string.Empty; // Firebase UID
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? PhotoUrl { get; set; }
    public bool EmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public UserProfile? Profile { get; set; }
}
