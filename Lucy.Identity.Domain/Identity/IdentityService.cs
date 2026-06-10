using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using Lucy.Identity.Domain.Contracts;
using Lucy.Identity.Domain.Entities;
using Lucy.Identity.Domain.Repositories;

namespace Lucy.Identity.Domain.Identity;

public sealed partial class IdentityService
{
    private readonly IUserRepository users;
    private readonly IRefreshTokenRepository refreshTokens;
    private readonly IAuditLogRepository auditLogs;
    private readonly PasswordHasher passwordHasher;
    private readonly IAccessTokenService tokenService;

    public IdentityService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IAuditLogRepository auditLogs,
        PasswordHasher passwordHasher,
        IAccessTokenService tokenService)
    {
        this.users = users;
        this.refreshTokens = refreshTokens;
        this.auditLogs = auditLogs;
        this.passwordHasher = passwordHasher;
        this.tokenService = tokenService;
    }

    public async Task<IdentityResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var validationError = ValidateRegistration(request);
        if (validationError is not null)
        {
            return IdentityResult<AuthResponse>.Failure("validation_error", validationError);
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existingUser = await users.FindByEmailAsync(normalizedEmail, cancellationToken);
        if (existingUser is not null)
        {
            return IdentityResult<AuthResponse>.Failure("email_exists", "Email is already registered.");
        }

        var userId = Guid.NewGuid();
        var user = new UserAccount
        {
            Id = userId,
            Email = normalizedEmail,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = request.Role,
            PrivacyProfile = new UserPrivacyProfile
            {
                UserId = userId,
                AvatarPersona = NormalizeOrDefault(request.AvatarPersona, "calm-blue"),
                AnonymousDisplayName = NormalizeOrDefault(request.AnonymousDisplayName, "Lucy Learner"),
                PublicBio = NormalizeOptional(request.PublicBio),
                LanguageLevel = NormalizeOptional(request.LanguageLevel)
            }
        };

        await users.AddAsync(user, cancellationToken);
        await AddAuditAsync(user.Id, "identity.register", cancellationToken);
        return IdentityResult<AuthResponse>.Success(await CreateAuthResponseAsync(user, cancellationToken));
    }

    public async Task<IdentityResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await users.FindByEmailAsync(email, cancellationToken);
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return IdentityResult<AuthResponse>.Failure("invalid_credentials", "Email or password is incorrect.");
        }

        if (user.Status != AccountStatus.Active)
        {
            return IdentityResult<AuthResponse>.Failure("account_disabled", "Account is not active.");
        }

        await AddAuditAsync(user.Id, "identity.login", cancellationToken);
        return IdentityResult<AuthResponse>.Success(await CreateAuthResponseAsync(user, cancellationToken));
    }

    public async Task<IdentityResult<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return IdentityResult<AuthResponse>.Failure("validation_error", "Refresh token is required.");
        }

        var tokenHash = HashRefreshToken(request.RefreshToken);
        var storedToken = await refreshTokens.FindActiveByHashAsync(tokenHash, cancellationToken);
        if (storedToken is null)
        {
            return IdentityResult<AuthResponse>.Failure("invalid_refresh_token", "Refresh token is invalid or expired.");
        }

        var user = await users.FindByIdAsync(storedToken.UserId, cancellationToken);
        if (user is null || user.Status != AccountStatus.Active)
        {
            return IdentityResult<AuthResponse>.Failure("invalid_refresh_token", "Refresh token is invalid or expired.");
        }

        await refreshTokens.RevokeAsync(storedToken.Id, cancellationToken);
        await AddAuditAsync(user.Id, "identity.refresh", cancellationToken);
        return IdentityResult<AuthResponse>.Success(await CreateAuthResponseAsync(user, cancellationToken));
    }

    public async Task<bool> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return false;
        }

        var token = await refreshTokens.FindActiveByHashAsync(HashRefreshToken(request.RefreshToken), cancellationToken);
        if (token is null)
        {
            return false;
        }

        await refreshTokens.RevokeAsync(token.Id, cancellationToken);
        await AddAuditAsync(token.UserId, "identity.logout", cancellationToken);
        return true;
    }

    public async Task<UserProfileResponse?> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId, cancellationToken);
        return user is null ? null : ToProfile(user);
    }

    public async Task<IReadOnlyList<UserProfileResponse>> ListUsersAsync(CancellationToken cancellationToken)
    {
        var allUsers = await users.ListAsync(cancellationToken);
        return allUsers.Select(ToProfile).ToList();
    }

    public async Task<IdentityResult<UserProfileResponse>> UpdateMyProfileAsync(Guid userId, UpdateMyProfileRequest request, CancellationToken cancellationToken)
    {
        var validationError = ValidateProfileUpdate(request);
        if (validationError is not null)
        {
            return IdentityResult<UserProfileResponse>.Failure("validation_error", validationError);
        }

        var user = await users.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return IdentityResult<UserProfileResponse>.Failure("user_not_found", "User does not exist.");
        }

        var privacyProfile = new UserPrivacyProfile
        {
            UserId = userId,
            AvatarPersona = request.AvatarPersona.Trim(),
            AnonymousDisplayName = request.AnonymousDisplayName.Trim(),
            PublicBio = NormalizeOptional(request.PublicBio),
            LanguageLevel = NormalizeOptional(request.LanguageLevel)
        };
        var updatedUser = await users.UpdateProfileAsync(userId, request.DisplayName.Trim(), privacyProfile, cancellationToken);
        return updatedUser is null
            ? IdentityResult<UserProfileResponse>.Failure("user_not_found", "User does not exist.")
            : IdentityResult<UserProfileResponse>.Success(ToProfile(updatedUser));
    }

    public async Task<IdentityResult<UserProfileResponse>> UpdateUserRoleAsync(Guid userId, UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Role))
        {
            return IdentityResult<UserProfileResponse>.Failure("validation_error", "Role must be Lucy, Pro, or Super.");
        }

        var user = await users.UpdateRoleAsync(userId, request.Role, cancellationToken);
        return user is null
            ? IdentityResult<UserProfileResponse>.Failure("user_not_found", "User does not exist.")
            : IdentityResult<UserProfileResponse>.Success(ToProfile(user));
    }

    public async Task<IdentityResult<UserProfileResponse>> UpdateUserStatusAsync(Guid userId, UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Status))
        {
            return IdentityResult<UserProfileResponse>.Failure("validation_error", "Status must be Active, Suspended, or Deleted.");
        }

        var user = await users.UpdateStatusAsync(userId, request.Status, cancellationToken);
        return user is null
            ? IdentityResult<UserProfileResponse>.Failure("user_not_found", "User does not exist.")
            : IdentityResult<UserProfileResponse>.Success(ToProfile(user));
    }

    public async Task<PublicProfileResponse?> GetPublicProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId, cancellationToken);
        return user is null ? null : ToPublicProfile(user);
    }

    public async Task<RoomIdentityResponse?> GetRoomIdentityAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId, cancellationToken);
        return user is null ? null : ToRoomIdentity(user);
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(UserAccount user, CancellationToken cancellationToken)
    {
        var (refreshToken, refreshTokenHash, refreshTokenExpiresAt) = CreateRefreshToken();
        await refreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = refreshTokenExpiresAt
        }, cancellationToken);

        var (token, expiresAt) = tokenService.CreateToken(user);
        return new AuthResponse(token, expiresAt, refreshToken, refreshTokenExpiresAt, ToProfile(user));
    }

    private static UserProfileResponse ToProfile(UserAccount user)
    {
        return new UserProfileResponse(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role,
            user.Status,
            GetPrivacy(user).AvatarPersona,
            GetPrivacy(user).AnonymousDisplayName,
            GetPrivacy(user).PublicBio,
            GetPrivacy(user).LanguageLevel,
            user.Role == AccountRole.Lucy,
            user.CreatedAt);
    }

    private static PublicProfileResponse ToPublicProfile(UserAccount user)
    {
        var privacy = GetPrivacy(user);
        return new PublicProfileResponse(
            user.Id,
            user.Role,
            privacy.AvatarPersona,
            privacy.AnonymousDisplayName,
            privacy.PublicBio,
            privacy.LanguageLevel);
    }

    private static RoomIdentityResponse ToRoomIdentity(UserAccount user)
    {
        var privacy = GetPrivacy(user);
        return new RoomIdentityResponse(
            user.Id,
            user.Role,
            privacy.AvatarPersona,
            privacy.AnonymousDisplayName,
            user.Role == AccountRole.Lucy);
    }

    private static string? ValidateRegistration(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !EmailRegex().IsMatch(request.Email))
        {
            return "A valid email is required.";
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Trim().Length < 2)
        {
            return "Display name must contain at least 2 characters.";
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return "Password must contain at least 8 characters.";
        }

        if (!Enum.IsDefined(request.Role))
        {
            return "Role must be Lucy, Pro, or Super.";
        }

        return null;
    }

    private static string? ValidateProfileUpdate(UpdateMyProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Trim().Length < 2)
        {
            return "Display name must contain at least 2 characters.";
        }

        if (string.IsNullOrWhiteSpace(request.AvatarPersona) || request.AvatarPersona.Trim().Length < 2)
        {
            return "Avatar persona must contain at least 2 characters.";
        }

        if (string.IsNullOrWhiteSpace(request.AnonymousDisplayName) || request.AnonymousDisplayName.Trim().Length < 2)
        {
            return "Anonymous display name must contain at least 2 characters.";
        }

        return null;
    }

    private static UserPrivacyProfile GetPrivacy(UserAccount user)
    {
        return user.PrivacyProfile ?? new UserPrivacyProfile
        {
            UserId = user.Id,
            AvatarPersona = "calm-blue",
            AnonymousDisplayName = "Lucy Learner"
        };
    }

    private static (string Token, string TokenHash, DateTimeOffset ExpiresAt) CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(bytes);
        return (token, HashRefreshToken(token), DateTimeOffset.UtcNow.AddDays(30));
    }

    private static string HashRefreshToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes);
    }

    private async Task AddAuditAsync(Guid? userId, string action, CancellationToken cancellationToken)
    {
        await auditLogs.AddAsync(new AuditLog { UserId = userId, Action = action }, cancellationToken);
    }

    private static string NormalizeOrDefault(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();
}
