namespace WonjinApi.Models;

// 상담 기록 수정 이력. UpdateNote가 덮어쓰기 직전의 본문을 여기에 스냅샷으로 남긴다 — 수정 이력 모달에서 사용.
public class ReservationNoteRevision
{
    public int Id { get; set; }
    public int ReservationNoteId { get; set; }
    public ReservationNote? ReservationNote { get; set; }
    public string Body { get; set; } = string.Empty; // 수정 전(직전) 본문
    public int? EditedByUserId { get; set; }
    public string EditedByName { get; set; } = string.Empty;
    public DateTimeOffset EditedAt { get; set; }
}
