using RunningApp.Domain.Enums;

namespace RunningApp.Domain.Entities;

public class UserProfile
{
    public Guid Id { get; set; }
    public Guid InternalUserId { get; set; }   // FK → Users.Id (NOT NULL)
    public DistanceUnit Unit { get; set; } = DistanceUnit.Km;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public User? User { get; set; }
}
