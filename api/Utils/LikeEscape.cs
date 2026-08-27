namespace WonjinApi.Utils;

// web-security-audit-guide.md 10장 — LIKE 이스케이프는 컨트롤러마다 각자 구현하지 말고 공용 헬퍼로
// 통일할 것(재감사 2026-08-27 발견 — AdminReservationsController·AdminAuditLogsController에 완전히
// 동일한 코드가 중복돼 있었음).
public static class LikeEscape
{
    public static string Escape(string s) => s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
