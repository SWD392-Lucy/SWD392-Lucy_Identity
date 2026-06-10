using Lucy.Identity.Domain.Entities;

namespace Lucy.Identity.Domain.Repositories;

public interface IUserRepository
{
    Task<IReadOnlyList<UserAccount>> ListAsync(CancellationToken cancellationToken);
    Task<UserAccount?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task<UserAccount> AddAsync(UserAccount user, CancellationToken cancellationToken);
    Task<UserAccount?> UpdateProfileAsync(Guid id, string displayName, UserPrivacyProfile privacyProfile, CancellationToken cancellationToken);
    Task<UserAccount?> UpdateRoleAsync(Guid id, AccountRole role, CancellationToken cancellationToken);
    Task<UserAccount?> UpdateStatusAsync(Guid id, AccountStatus status, CancellationToken cancellationToken);
}

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken);
    Task<RefreshToken?> FindActiveByHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task RevokeAsync(Guid id, CancellationToken cancellationToken);
}

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken);
}
