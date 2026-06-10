using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lucy.Identity.Domain.Entities;
using Lucy.Identity.Domain.Identity;
using Microsoft.Extensions.Options;

namespace Lucy.Identity.Api.Authentication;

public sealed class JwtTokenService : IAccessTokenService
{
    private readonly JwtOptions options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        this.options = options.Value;
    }

    public (string Token, DateTimeOffset ExpiresAt) CreateToken(UserAccount user)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(options.AccessTokenMinutes);
        var header = new Dictionary<string, object>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT"
        };
        var payload = new Dictionary<string, object>
        {
            ["sub"] = user.Id.ToString(),
            ["displayName"] = user.DisplayName,
            ["avatarPersona"] = user.PrivacyProfile?.AvatarPersona ?? "calm-blue",
            ["isAnonymous"] = user.Role == AccountRole.Lucy,
            ["role"] = user.Role.ToString(),
            ["iss"] = options.Issuer,
            ["aud"] = options.Audience,
            ["iat"] = now.ToUnixTimeSeconds(),
            ["exp"] = expiresAt.ToUnixTimeSeconds()
        };

        var encodedHeader = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(header));
        var encodedPayload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload));
        var unsignedToken = $"{encodedHeader}.{encodedPayload}";
        var signature = Base64UrlEncode(Sign(unsignedToken));
        return ($"{unsignedToken}.{signature}", expiresAt);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return null;
        }

        var unsignedToken = $"{parts[0]}.{parts[1]}";
        var expectedSignature = Base64UrlEncode(Sign(unsignedToken));
        if (!FixedTimeEquals(parts[2], expectedSignature))
        {
            return null;
        }

        using var payloadDocument = JsonDocument.Parse(Base64UrlDecode(parts[1]));
        var payload = payloadDocument.RootElement;

        if (!TryGetString(payload, "iss", out var issuer) || issuer != options.Issuer)
        {
            return null;
        }

        if (!TryGetString(payload, "aud", out var audience) || audience != options.Audience)
        {
            return null;
        }

        if (!payload.TryGetProperty("exp", out var expElement))
        {
            return null;
        }

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expElement.GetInt64());
        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        if (!TryGetString(payload, "sub", out var subject) ||
            !TryGetString(payload, "displayName", out var displayName) ||
            !TryGetString(payload, "avatarPersona", out var avatarPersona) ||
            !TryGetString(payload, "role", out var role))
        {
            return null;
        }

        var isAnonymous = payload.TryGetProperty("isAnonymous", out var anonymousElement) &&
            anonymousElement.ValueKind == JsonValueKind.True;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, subject),
            new Claim(ClaimTypes.Name, displayName),
            new Claim(ClaimTypes.Role, role),
            new Claim("displayName", displayName),
            new Claim("avatarPersona", avatarPersona),
            new Claim("isAnonymous", isAnonymous.ToString().ToLowerInvariant())
        };
        var identity = new ClaimsIdentity(claims, JwtAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    public (bool IsValid, Guid? UserId, AccountRole? Role, bool? IsAnonymous, DateTimeOffset? ExpiresAt) ValidateForService(string token)
    {
        var principal = ValidateToken(token);
        if (principal is null)
        {
            return (false, null, null, null, null);
        }

        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleClaim = principal.FindFirstValue(ClaimTypes.Role);
        var anonymousClaim = principal.FindFirstValue("isAnonymous");
        if (!Guid.TryParse(subject, out var userId) ||
            !Enum.TryParse<AccountRole>(roleClaim, out var role) ||
            !bool.TryParse(anonymousClaim, out var isAnonymous))
        {
            return (false, null, null, null, null);
        }

        using var payloadDocument = JsonDocument.Parse(Base64UrlDecode(token.Split('.')[1]));
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(payloadDocument.RootElement.GetProperty("exp").GetInt64());
        return (true, userId, role, isAnonymous, expiresAt);
    }

    private byte[] Sign(string value)
    {
        var key = Encoding.UTF8.GetBytes(options.SigningKey);
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(value));
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        return Convert.FromBase64String(padded);
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length &&
            CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
