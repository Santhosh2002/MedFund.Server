using MedFund.Application.Common;
using MedFund.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MedFund.Infrastructure.Persistence;

public sealed class AuditWriter : IAuditWriter
{
    private readonly IApplicationDbContext db;
    private readonly ILogger<AuditWriter> logger;

    public AuditWriter(IApplicationDbContext db, ILogger<AuditWriter> logger)
    {
        this.db = db;
        this.logger = logger;
    }

    public void Add(Guid? actorUserId, string action, string entityType, string entityId, string? beforeJson = null, string? afterJson = null, string? ipAddress = null)
    {
        logger.LogInformation(
            "{Service}.{Function} received audit event. ActorUserId={ActorUserId}, Action={Action}, EntityType={EntityType}, EntityId={EntityId}, IpAddress={IpAddress}",
            nameof(AuditWriter),
            nameof(Add),
            actorUserId,
            action,
            entityType,
            entityId,
            ipAddress);

        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            BeforeJson = beforeJson,
            AfterJson = afterJson,
            IpAddress = ipAddress
        });
    }
}
