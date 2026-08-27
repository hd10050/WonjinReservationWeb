namespace WonjinApi.Models;

// [시술·수술 관리] > [카테고리 관리] 탭에서 등록하는 마스터 데이터(D25). 코드로 시딩하지 않는다.
// DELETE 없음 — is_active=false로만 비활성화(D13·시술과 동일).
public class Category
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameZhCn { get; set; } = string.Empty;
    public string NameZhTw { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameKo { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Procedure> Procedures { get; set; } = [];
}
