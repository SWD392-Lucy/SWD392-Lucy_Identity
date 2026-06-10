using Lucy.Identity.Domain.Entities;
using Lucy.Identity.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Lucy.Identity.Infrastructure.Persistence;

public sealed class IdentityDataSeeder
{
    private readonly IdentityDbContext dbContext;
    private readonly PasswordHasher passwordHasher;
    private readonly IConfiguration configuration;

    public IdentityDataSeeder(
        IdentityDbContext dbContext,
        PasswordHasher passwordHasher,
        IConfiguration configuration)
    {
        this.dbContext = dbContext;
        this.passwordHasher = passwordHasher;
        this.configuration = configuration;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var email = configuration["IdentitySeed:Super:Email"];
        var password = configuration["IdentitySeed:Super:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (await dbContext.Users.AnyAsync(user => user.Email == normalizedEmail, cancellationToken))
        {
            return;
        }

        var userId = Guid.NewGuid();
        dbContext.Users.Add(new UserAccount
        {
            Id = userId,
            Email = normalizedEmail,
            DisplayName = configuration["IdentitySeed:Super:DisplayName"] ?? "Lucy Super Admin",
            PasswordHash = passwordHasher.Hash(password),
            Role = AccountRole.Super,
            Status = AccountStatus.Active,
            PrivacyProfile = new UserPrivacyProfile
            {
                UserId = userId,
                AvatarPersona = configuration["IdentitySeed:Super:AvatarPersona"] ?? "super-gold",
                AnonymousDisplayName = configuration["IdentitySeed:Super:AnonymousDisplayName"] ?? "Lucy Super",
                PublicBio = "Seeded local Super account"
            }
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
