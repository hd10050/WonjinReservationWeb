namespace WonjinApi.Models;

// 로그인 계정만 담는다. 실장 마스터 데이터는 Consultant에 따로 있고 이 테이블과 연결되지 않는다(D8).
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // Admin | HospitalManager | Consultant
    public string Name { get; set; } = string.Empty;
    public string Locale { get; set; } = "ko";
    public bool IsSuspended { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
