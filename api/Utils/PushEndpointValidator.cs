namespace WonjinApi.Utils;

// web-push-notification-guide.md 4-1절 — IP 대역 차단(DNS 조회) 방식은 IPv4-매핑 IPv6 우회·DNS
// 리바인딩 TOCTOU·동기 DNS 호출로 인한 스레드풀 고갈까지 구조적 결함 3가지가 있어, 알려진 브라우저
// 벤더 푸시 서비스 도메인 화이트리스트로 검증한다. 구독 저장 시 + 발송 직전 양쪽에서 이 한 곳만
// 참조하게 해 화이트리스트 갱신 누락(drift)을 방지한다.
public static class PushEndpointValidator
{
    private static readonly string[] AllowedHostSuffixes =
    [
        "fcm.googleapis.com",                // Chrome / Edge / Android (Google FCM)
        "android.googleapis.com",             // 구버전 FCM
        "updates.push.services.mozilla.com",  // Firefox
        "notify.windows.com",                 // Windows/Edge (WNS)
        "web.push.apple.com",                 // Safari (macOS 16+/iOS 16.4+)
    ];

    public static bool IsSafe(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            return false;

        var host = uri.Host;
        return AllowedHostSuffixes.Any(suffix =>
            host.Equals(suffix, StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith("." + suffix, StringComparison.OrdinalIgnoreCase));
    }
}
