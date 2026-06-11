using Lucy.Identity.Domain.Entities;

namespace Lucy.Identity.Domain.Identity;

public interface IAccessTokenService
{
    int RefreshTokenDays { get; }
    (string Token, DateTimeOffset ExpiresAt) CreateToken(UserAccount user);
}
