namespace Lucy.Identity.Domain.Entities;

public sealed class UserPrivacyProfile
{
    public Guid UserId { get; init; }
    public required string AvatarPersona { get; set; }
    public required string AnonymousDisplayName { get; set; }
    public string? PublicBio { get; set; }
    public string? LanguageLevel { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public UserAccount? User { get; init; }
}
