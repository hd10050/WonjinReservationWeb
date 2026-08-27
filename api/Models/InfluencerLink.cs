namespace WonjinApi.Models;

// 인플루언서 짧은 링크 매핑(B안, 2026-08-27 신설, 15-2절 연장) — /go/{code}가 이 테이블을 조회해
// UTM·로케일을 채운 뒤 실제 랜딩으로 리다이렉트한다. reservations.referral_code(자유 문자열)와
// FK로 연결하지 않는다 — 매핑 없이 들어온 코드도 그대로 유입 경로 통계에 집계되어야 하기 때문(15-2절).
// DELETE 없음, is_active=false로만 비활성화(D13과 동일한 이 프로젝트의 마스터 데이터 관례).
public class InfluencerLink
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty; // /go/{code} URL 경로 세그먼트 — 생성 후 불변
    public string DisplayName { get; set; } = string.Empty;
    public string UtmSource { get; set; } = string.Empty;
    public string UtmMedium { get; set; } = "influencer";
    public string UtmCampaign { get; set; } = string.Empty;
    public string Locale { get; set; } = "zh-CN"; // zh-CN | zh-TW | en | ko — 리다이렉트 목적지 로케일
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
