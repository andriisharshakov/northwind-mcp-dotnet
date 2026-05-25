using System.Text.Json;
using NorthwindCrm.Api.Data;
using NorthwindCrm.Api.Models;

namespace NorthwindCrm.Api.Services;

public class AuditLogService
{
    private readonly NorthwindDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogService(NorthwindDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        string entityType,
        string entityId,
        string action,
        object? oldValues = null,
        object? newValues = null,
        string? toolName = null,
        object? toolInput = null)
    {
        var context = _httpContextAccessor.HttpContext;
        var correlationId = context?.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
        var userPrompt = context?.Request.Headers["X-User-Prompt"].FirstOrDefault();

        var entry = new AuditLogEntry
        {
            CorrelationId = Guid.Parse(correlationId),
            UserPrompt = userPrompt,
            ToolName = toolName,
            ToolInput = toolInput is null ? null : JsonSerializer.Serialize(toolInput),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldValues = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
            NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues),
            PerformedBy = context?.User?.Identity?.Name ?? "anonymous",
            PerformedAt = DateTime.UtcNow
        };

        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync();
    }
}