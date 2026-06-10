namespace Lucy.Identity.Domain.Entities;

public sealed class UserAccount
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public required string PasswordHash { get; init; }
    public AccountRole Role { get; set; } = AccountRole.Lucy;
    public AccountStatus Status { get; set; } = AccountStatus.Active;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public UserPrivacyProfile? PrivacyProfile { get; set; }
    public List<RefreshToken> RefreshTokens { get; } = [];
}
