namespace WonjinApi.Models;

// 상담 기록. 누적만 하고 덮어쓰지 않는다(D14) — 삭제 없음, 수정은 작성자 본인·어드민만(앱 레벨에서 검증).
public class ReservationNote
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public Reservation? Reservation { get; set; }
    public string Body { get; set; } = string.Empty;
    public int? AuthorUserId { get; set; }
    public User? AuthorUser { get; set; }
    public string AuthorName { get; set; } = string.Empty; // 작성 시점 이름 스냅샷
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
