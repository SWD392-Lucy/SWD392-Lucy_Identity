using Lucy.Identity.Domain.Entities;

namespace Lucy.Identity.Domain.Contracts;

public sealed record UpdateMyProfileRequest(
    string DisplayName,
    string AvatarPersona,
    string AnonymousDisplayName,
    string? PublicBio = null,
    string? LanguageLevel = null);

public sealed record UpdateUserRoleRequest(AccountRole Role);

public sealed record UpdateUserStatusRequest(AccountStatus Status);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record TokenValidationRequest(string AccessToken);

public sealed record TokenValidationResponse(
    bool IsValid,
    Guid? UserId,
    AccountRole? Role,
    bool? IsAnonymous,
    DateTimeOffset? ExpiresAt);
