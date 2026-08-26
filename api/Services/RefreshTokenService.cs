using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WonjinApi.Data;
using WonjinApi.Models;

namespace WonjinApi.Services;

public class RefreshTokenService(AppDbContext db) : IRefreshTokenService
{
    private const int TokenDays = 7;

    public async Task<(RefreshToken Entity, string RawToken)> CreateAsync(int userId)
    {
        var rawToken = GenerateRawToken();
        var tokenHash = HashToken(rawToken);
        var now = DateTimeOffset.UtcNow;

        var entity = new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = now.AddDays(TokenDays),
            CreatedAt = now,
        };
        db.RefreshTokens.Add(entity);

        // 재로그인 없는 계정의 토큰이 영구 잔류하지 않도록 사용자별 만료·폐기 토큰을 그때그때 정리
        await db.RefreshTokens
            .Where(r => r.UserId == userId && (r.IsRevoked || r.ExpiresAt <= now))
            .ExecuteDeleteAsync();

        await db.SaveChangesAsync();
        return (entity, rawToken);
    }

    public async Task<RefreshToken?> ValidateAsync(string rawToken)
    {
        var hash = HashToken(rawToken);
        return await db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r =>
                r.TokenHash == hash &&
                !r.IsRevoked &&
                r.ExpiresAt > DateTimeOffset.UtcNow);
    }

    public async Task RevokeAsync(string rawToken)
    {
        var hash = HashToken(rawToken);
        await db.RefreshTokens
            .Where(r => r.TokenHash == hash)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsRevoked, true));
    }

    public async Task RevokeAllForUserAsync(int userId)
    {
        await db.RefreshTokens
            .Where(r => r.UserId == userId && !r.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsRevoked, true));
    }

    // 128자 hex = 64바이트 엔트로피(7-1절)
    private static string GenerateRawToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(64));

    // DB에는 해시만 저장 — 평문 저장 금지(7-1절)
    private static string HashToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
