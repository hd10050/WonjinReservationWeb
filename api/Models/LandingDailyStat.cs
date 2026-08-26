namespace WonjinApi.Models;

// 유입 경로 일별 집계. (날짜×캠페인조합)당 1행으로 UPSERT — 방문마다 1행이 아니다(15-1절).
// stat_date는 반드시 KST 기준 날짜(9-2절) — UtcNow.Date 금지.
public class LandingDailyStat
{
    public int Id { get; set; }
    public DateOnly StatDate { get; set; }
    public string ReferralCode { get; set; } = string.Empty;
    public string UtmSource { get; set; } = string.Empty;
    public string UtmMedium { get; set; } = string.Empty;
    public string UtmCampaign { get; set; } = string.Empty;
    public int VisitCount { get; set; }
}
