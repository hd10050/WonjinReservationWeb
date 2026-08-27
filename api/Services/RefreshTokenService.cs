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

        // 🔴 성능(2026-08-27, "로그인이 느림" 재조사) — 사용자별 만료·폐기 토큰 정리를 로그인·
        // 12분마다의 토큰갱신 요청 경로에서 매번 동기 DELETE로 수행하고 있었다. RefreshTokenCleanupService가
        // 12시간마다 전역으로 동일한 정리를 이미 수행하므로 이 인라인 정리는 중복이었고, 로그인·갱신
        // 왕복마다 DB 라운드트립 1회를 추가하고 있었다(최악의 경우도 만료 토큰이 최대 12시간
        // 더 남아있는 것뿐 — 영구 잔류는 아니라 안전하게 제거 가능).
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
