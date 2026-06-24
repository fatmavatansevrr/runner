using RunningApp.Domain.Enums;

namespace RunningApp.Domain.Entities;

public class UserProfile
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty; // external auth subject
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DistanceUnit Unit { get; set; } = DistanceUnit.Km;
    public RunningBackground RunningBackground { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
