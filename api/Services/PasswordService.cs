namespace WonjinApi.Services;

public class PasswordService : IPasswordService
{
    // workFactor 12 — 7-1절 확정값
    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
