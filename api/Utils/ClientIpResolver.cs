using System.Security.Cryptography;
using System.Text;

namespace WonjinApi.Utils;

// IP 기반 기능(rate limit·감사로그 등) 전체가 이 함수 하나만 참조해야 한다(루트 CLAUDE.md
// "IP 기반 기능 구현 원칙"). 원래 Program.cs 로컬함수와 AuditLogFilter.cs 인라인 코드로 이중
// 구현돼 있었다 — 값은 항상 동일했으나(2026-08-28 헤더명 변경 때 두 곳 다 같이 고침), 향후 3번째
// IP 기반 기능(화이트리스트 등) 추가 시 한쪽만 고치고 잊는 드리프트 위험이 있어 통합한다(2026-09-01 감사).
public static class ClientIpResolver
{
    // 프론트(Workers)가 릴레이하는 X-Wj-Client-Ip는 내부시크릿이 유효할 때만 신뢰하고, 아니면 실제
    // TCP 연결 IP로 폴백한다. 원래 이름은 CF-Connecting-IP였으나, Render(onrender.com)도 Cloudflare
    // 엣지 뒤에 있어서 그 이름의 헤더는 Render 앞단 엣지가 항상 실제 TCP 접속 값(Workers 아웃바운드
    // IP, PoP마다 달라짐)으로 재작성해버림을 실측 확인 — Cloudflare가 예약하지 않은 커스텀 이름
    // (X-Wj-Client-Ip)으로 프론트 server/api/[...].ts와 함께 맞춰뒀다.
    public static string Resolve(HttpContext context, string internalSecret)
    {
        var provided = context.Request.Headers["X-Internal-Secret"].FirstOrDefault();
        var trusted = !string.IsNullOrEmpty(internalSecret) && !string.IsNullOrEmpty(provided)
            && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(internalSecret));

        if (trusted)
        {
            var clientIp = context.Request.Headers["X-Wj-Client-Ip"].FirstOrDefault();
            if (!string.IsNullOrEmpty(clientIp)) return clientIp;
        }
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
