namespace WonjinApi.Utils;

// web-push-notification-guide.md 4-8절 — 서버가 이벤트에 반응해 직접 조립하는 알림 문구는 프론트
// i18n(vue-i18n)을 거치지 않는다. 수신자(User)의 저장된 Locale로 직접 분기해야 화면 언어와 알림
// 언어가 어긋나지 않는다. 관리자가 직접 입력해 보내는 문구가 아니므로(이 기능엔 그런 발송 자체가
// 없음) 4개 로케일을 여기 하드코딩 — 프론트 locales/*.json과는 별개 관리 지점이다.
public static class NewReservationPushText
{
    public static (string Title, string Body) Build(string? locale, string customerName, string code) => locale switch
    {
        "ko" => ("새 예약 접수", $"{customerName}님 ({code})"),
        "en" => ("New reservation received", $"{customerName} ({code})"),
        "zh-TW" => ("收到新預約", $"{customerName}（{code}）"),
        _ => ("收到新预约", $"{customerName}（{code}）"), // zh-CN 기본 로케일 폴백(D9)
    };
}
