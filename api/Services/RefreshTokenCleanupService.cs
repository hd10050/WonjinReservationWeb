using Microsoft.EntityFrameworkCore;
using WonjinApi.Data;

namespace WonjinApi.Services;

// 사용자별 lazy 정리(RefreshTokenService.CreateAsync)만으로는 재로그인 없는 계정의 토큰이
// 영구 잔류하므로 전역 주기 정리를 둔다.
public class RefreshTokenCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<RefreshTokenCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(12);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await CleanupAsync(stoppingToken); }
            catch (Exception ex)
            {
                logger.LogError(ex, "[rt-cleanup] Refresh Token 정리 중 오류");
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        // BackgroundService는 싱글턴 — Scoped DbContext를 직접 주입할 수 없어 스코프를 수동 생성
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTimeOffset.UtcNow;
        var deleted = await db.RefreshTokens
            .Where(r => r.IsRevoked || r.ExpiresAt <= now)
            .ExecuteDeleteAsync(ct);

        if (deleted > 0)
            logger.LogInformation("[rt-cleanup] 만료·폐기 토큰 {Count}건 삭제", deleted);
    }
}
