using Lucy.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lucy.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<UserPrivacyProfile> UserPrivacyProfiles => Set<UserPrivacyProfile>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("identity_users");

            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(user => user.Email).HasColumnName("email").HasMaxLength(256).IsRequired();
            entity.Property(user => user.DisplayName).HasColumnName("display_name").HasMaxLength(120).IsRequired();
            entity.Property(user => user.PasswordHash).HasColumnName("password_hash").HasMaxLength(512).IsRequired();
            entity.Property(user => user.Role).HasColumnName("role").HasConversion<int>().IsRequired();
            entity.Property(user => user.Status).HasColumnName("status").HasConversion<int>().IsRequired();
            entity.Property(user => user.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(user => user.UpdatedAt).HasColumnName("updated_at").IsRequired();

            entity.HasIndex(user => user.Email).IsUnique();
            entity.HasIndex(user => user.Role);
            entity.HasIndex(user => user.Status);

            entity.HasOne(user => user.PrivacyProfile)
                .WithOne(profile => profile.User)
                .HasForeignKey<UserPrivacyProfile>(profile => profile.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserPrivacyProfile>(entity =>
        {
            entity.ToTable("user_privacy_profiles");

            entity.HasKey(profile => profile.UserId);
            entity.Property(profile => profile.UserId).HasColumnName("user_id").ValueGeneratedNever();
            entity.Property(profile => profile.AvatarPersona).HasColumnName("avatar_persona").HasMaxLength(120).IsRequired();
            entity.Property(profile => profile.AnonymousDisplayName).HasColumnName("anonymous_display_name").HasMaxLength(120).IsRequired();
            entity.Property(profile => profile.PublicBio).HasColumnName("public_bio").HasMaxLength(500);
            entity.Property(profile => profile.LanguageLevel).HasColumnName("language_level").HasMaxLength(80);
            entity.Property(profile => profile.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(profile => profile.UpdatedAt).HasColumnName("updated_at").IsRequired();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");

            entity.HasKey(token => token.Id);
            entity.Property(token => token.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(token => token.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(token => token.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
            entity.Property(token => token.ExpiresAt).HasColumnName("expires_at").IsRequired();
            entity.Property(token => token.RevokedAt).HasColumnName("revoked_at");
            entity.Property(token => token.CreatedAt).HasColumnName("created_at").IsRequired();

            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasIndex(token => token.UserId);
            entity.HasOne(token => token.User)
                .WithMany(user => user.RefreshTokens)
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");

            entity.HasKey(log => log.Id);
            entity.Property(log => log.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(log => log.UserId).HasColumnName("user_id");
            entity.Property(log => log.Action).HasColumnName("action").HasMaxLength(120).IsRequired();
            entity.Property(log => log.IpAddress).HasColumnName("ip_address").HasMaxLength(80);
            entity.Property(log => log.UserAgent).HasColumnName("user_agent").HasMaxLength(300);
            entity.Property(log => log.CreatedAt).HasColumnName("created_at").IsRequired();

            entity.HasIndex(log => log.UserId);
            entity.HasIndex(log => log.Action);
        });
    }
}
