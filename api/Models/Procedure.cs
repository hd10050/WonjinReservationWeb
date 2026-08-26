namespace WonjinApi.Models;

// [시술·수술 관리] 메뉴에서 등록하는 마스터 데이터. 코드로 시딩하지 않는다.
public class Procedure
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameZhCn { get; set; } = string.Empty;
    public string NameZhTw { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameKo { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<ReservationProcedure> ReservationProcedures { get; set; } = [];
}
