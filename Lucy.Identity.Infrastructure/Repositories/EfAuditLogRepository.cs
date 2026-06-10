using Lucy.Identity.Domain.Entities;
using Lucy.Identity.Domain.Repositories;
using Lucy.Identity.Infrastructure.Persistence;

namespace Lucy.Identity.Infrastructure.Repositories;

public sealed class EfAuditLogRepository : IAuditLogRepository
{
    private readonly IdentityDbContext dbContext;

    public EfAuditLogRepository(IdentityDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        dbContext.AuditLogs.Add(auditLog);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
