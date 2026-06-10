using Lucy.Identity.Domain.Entities;

namespace Lucy.Identity.Domain.Contracts;

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserProfileResponse User);

public sealed record UserProfileResponse(
    Guid Id,
    string Email,
    string DisplayName,
    AccountRole Role,
    AccountStatus Status,
    string AvatarPersona,
    string AnonymousDisplayName,
    string? PublicBio,
    string? LanguageLevel,
    bool IsAnonymous,
    DateTimeOffset CreatedAt);

public sealed record PublicProfileResponse(
    Guid Id,
    AccountRole Role,
    string AvatarPersona,
    string AnonymousDisplayName,
    string? PublicBio,
    string? LanguageLevel);

public sealed record RoomIdentityResponse(
    Guid UserId,
    AccountRole Role,
    string AvatarPersona,
    string AnonymousDisplayName,
    bool IsAnonymous);
