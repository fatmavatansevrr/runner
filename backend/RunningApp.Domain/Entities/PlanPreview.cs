namespace RunningApp.Domain.Entities;

public class PlanPreview
{
    public Guid Id { get; set; }
    public Guid? InternalUserId { get; set; }  // FK → Users.Id
    public string? TemplateId { get; set; }
    public string RequestPayloadJson { get; set; } = string.Empty;
    public string PreviewPayloadJson { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
