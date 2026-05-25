using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NorthwindCrm.Api.Models;

[Table("audit_log")]
public class AuditLogEntry
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("correlation_id")]
    public Guid CorrelationId { get; set; }

    [Column("user_prompt")]
    public string? UserPrompt { get; set; }

    [Column("tool_name")]
    public string? ToolName { get; set; }

    [Column("tool_input", TypeName = "jsonb")]
    public string? ToolInput { get; set; }

    [Column("entity_type")]
    public string? EntityType { get; set; }

    [Column("entity_id")]
    public string? EntityId { get; set; }

    [Column("action")]
    public string Action { get; set; } = null!;

    [Column("old_values", TypeName = "jsonb")]
    public string? OldValues { get; set; }

    [Column("new_values", TypeName = "jsonb")]
    public string? NewValues { get; set; }

    [Column("performed_by")]
    public string? PerformedBy { get; set; }

    [Column("performed_at")]
    public DateTime PerformedAt { get; set; }
}