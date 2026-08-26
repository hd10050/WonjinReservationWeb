namespace WonjinApi.Models;

// 예약 처리 이력(업무 타임라인). audit_logs(관리자 감사)와 목적이 달라 별도 테이블로 분리.
public class ReservationLog
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public Reservation? Reservation { get; set; }
    public string Action { get; set; } = string.Empty; // received|assigned|status_changed|note_added|deposit_confirmed|cancelled|deleted
    public string? Note { get; set; }
    public int? ActorUserId { get; set; } // 시스템 접수는 NULL
    public string ActorName { get; set; } = string.Empty; // 'SYSTEM' 또는 조작한 계정 이름
    public DateTimeOffset CreatedAt { get; set; }
}
