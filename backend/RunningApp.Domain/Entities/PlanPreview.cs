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

    // ── Backend Integration Phase 4E.2: catalog confirmation state ──────────
    // Both fields are nullable/additive. Null for every legacy SQL preview row.

    /// <summary>
    /// Set atomically at the moment a catalog-routed preview is successfully
    /// confirmed. FK → TrainingPlans.Id. Null until confirmed (or always null
    /// for legacy SQL previews). Used as the idempotency anchor: if non-null,
    /// the preview is already confirmed and a repeated confirm call returns
    /// the associated plan without creating a duplicate.
    /// </summary>
    public Guid? ConfirmedPlanId { get; set; }

    /// <summary>
    /// Emergency explicit-invalidation flag. Null = not invalidated (normal).
    /// True = this preview has been explicitly invalidated by an operator and
    /// must be rejected at confirmation time with a typed error, even if it is
    /// otherwise valid and unexpired. Never set automatically by catalog
    /// evolution (a new candidate version being published does not set this).
    /// False is semantically equivalent to null (allowed, treated as not
    /// invalidated) — only true is meaningful.
    /// </summary>
    public bool? IsInvalidated { get; set; }
}
