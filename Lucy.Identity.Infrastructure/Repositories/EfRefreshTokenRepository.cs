using Lucy.Identity.Domain.Entities;
using Lucy.Identity.Domain.Repositories;
using Lucy.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lucy.Identity.Infrastructure.Repositories;

public sealed class EfRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IdentityDbContext dbContext;

    public EfRefreshTokenRepository(IdentityDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<RefreshToken?> FindActiveByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return dbContext.RefreshTokens
            .FirstOrDefaultAsync(token =>
                token.TokenHash == tokenHash &&
                token.RevokedAt == null &&
                token.ExpiresAt > now,
                cancellationToken);
    }

    public async Task RevokeAsync(Guid id, CancellationToken cancellationToken)
    {
        var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(existing => existing.Id == id, cancellationToken);
        if (token is null || token.RevokedAt is not null)
        {
            return;
        }

        token.RevokedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
