namespace WonjinApi.Models;

// 실장 마스터 데이터. users와 FK로 연결하지 않는다 — 계정과 1:1이 아니다(D8).
// DELETE 없음, is_active=false로만 비활성화(D13).
public class Consultant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = [];
}
