namespace WonjinApi.Models;

// 관리자(3역할 전부) 행위 감사 로그. 전역 AuditLogFilter가 자동 기록한다(14장).
public class AuditLog
{
    public long Id { get; set; }
    public int? ActorUserId { get; set; }
    public string ActorEmail { get; set; } = string.Empty; // 계정 삭제 후에도 보존
    public string ActorRole { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? Ip { get; set; }
    public int StatusCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
