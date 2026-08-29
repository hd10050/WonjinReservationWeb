namespace WonjinApi.Utils;

// web-security-audit-guide.md 10장 — LIKE 이스케이프는 컨트롤러마다 각자 구현하지 말고 공용 헬퍼로
// 통일할 것(재감사 2026-08-27 발견 — AdminReservationsController·AdminAuditLogsController에 완전히
// 동일한 코드가 중복돼 있었음).
public static class LikeEscape
{
    // 🔴 검색어 길이 상한(2026-08-30 감사) — 입력 필드 길이 제한 절대원칙. 목록 검색창의 프론트
    // maxlength="200"과 동일한 값. API 직접 호출로 무제한 길이 문자열이 들어와 Escape의 3회 Replace
    // 재할당 + 거대한 ILIKE 패턴으로 과도한 페이로드가 되는 것을 백엔드에서도 막는다.
    public const int MaxSearchLength = 200;

    public static string Escape(string s) => s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    // 목록 "contains" 검색 전용 — Trim + 길이 상한(MaxSearchLength) + LIKE 메타문자 이스케이프를
    // 한 번에 처리한다. 7개 관리 목록 컨트롤러가 전부 `Escape(search.Trim())` 형태로 중복하던 것을
    // 이 메서드로 통일(10장 "공용 헬퍼 하나로" 원칙). 반환값은 `$"%{keyword}%"` 패턴에 그대로 끼운다.
    public static string EscapeContains(string raw)
    {
        var trimmed = raw.Trim();
        if (trimmed.Length > MaxSearchLength)
            trimmed = trimmed[..MaxSearchLength];
        return Escape(trimmed);
    }
}
