using Domain.Enums;

namespace Application.Common.Interfaces;

public interface IAuditLogger
{
    Task LogAsync(AuditAction action, string entityName, string? entityId = null,
                  string? oldValuesJson = null, string? newValuesJson = null,
                  CancellationToken ct = default);
}
