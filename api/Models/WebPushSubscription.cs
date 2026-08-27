namespace WonjinApi.Models;

// 새 예약 접수 알림 전용(어드민 내부 알림 — 공개 마케팅 푸시 아님). web-push-notification-guide.md의
// 기본 설계(UserId nullable, 비로그인 허용)와 달리 이 프로젝트는 로그인한 직원만 구독 가능해
// UserId를 필수로 둔다 — 비로그인 구독자 개념 자체가 없다.
public class WebPushSubscription
{
    public int Id { get; set; }
    public string Endpoint { get; set; } = string.Empty; // 브라우저 벤더 푸시 서버 URL, 기기·브라우저별 고유
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public int UserId { get; set; }
    public User? User { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
