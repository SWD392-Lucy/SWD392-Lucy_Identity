using Lucy.Identity.Domain.Entities;
using Lucy.Identity.Domain.Repositories;
using Lucy.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lucy.Identity.Infrastructure.Repositories;

public sealed class EfUserRepository : IUserRepository
{
    private readonly IdentityDbContext dbContext;

    public EfUserRepository(IdentityDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyList<UserAccount>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Include(user => user.PrivacyProfile)
            .OrderByDescending(user => user.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<UserAccount?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Users
            .AsNoTracking()
            .Include(user => user.PrivacyProfile)
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        return dbContext.Users
            .AsNoTracking()
            .Include(user => user.PrivacyProfile)
            .FirstOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);
    }

    public async Task<UserAccount> AddAsync(UserAccount user, CancellationToken cancellationToken)
    {
        user.Email = NormalizeEmail(user.Email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<UserAccount?> UpdateProfileAsync(
        Guid id,
        string displayName,
        UserPrivacyProfile privacyProfile,
        CancellationToken cancellationToken)
    {
        var existingUser = await dbContext.Users
            .Include(existing => existing.PrivacyProfile)
            .FirstOrDefaultAsync(existing => existing.Id == id, cancellationToken);
        if (existingUser is null)
        {
            return null;
        }

        existingUser.DisplayName = displayName;
        existingUser.UpdatedAt = DateTimeOffset.UtcNow;
        if (existingUser.PrivacyProfile is null)
        {
            existingUser.PrivacyProfile = privacyProfile;
        }
        else
        {
            existingUser.PrivacyProfile.AvatarPersona = privacyProfile.AvatarPersona;
            existingUser.PrivacyProfile.AnonymousDisplayName = privacyProfile.AnonymousDisplayName;
            existingUser.PrivacyProfile.PublicBio = privacyProfile.PublicBio;
            existingUser.PrivacyProfile.LanguageLevel = privacyProfile.LanguageLevel;
            existingUser.PrivacyProfile.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return existingUser;
    }

    public async Task<UserAccount?> UpdateRoleAsync(Guid id, AccountRole role, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(existing => existing.PrivacyProfile)
            .FirstOrDefaultAsync(existing => existing.Id == id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.Role = role;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<UserAccount?> UpdateStatusAsync(Guid id, AccountStatus status, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(existing => existing.PrivacyProfile)
            .FirstOrDefaultAsync(existing => existing.Id == id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.Status = status;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
