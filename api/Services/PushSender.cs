using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebPush;
using WonjinApi.Data;
using WonjinApi.Utils;

namespace WonjinApi.Services;

// 새 예약 접수 전용 자동 발송(시스템 트리거, 관리자가 문구를 입력해 보내는 방송 기능 없음 —
// web-push-notification-guide.md 3-3·3-6절의 "관리자 발송 폼"은 이 프로젝트엔 해당 없음).
public class PushSender(AppDbContext db, IConfiguration config, ILogger<PushSender> logger) : IPushSender
{
    public async Task SendNewReservationAlertAsync(int reservationId, string customerName, string code)
    {
        var publicKey = config["Push:VapidPublicKey"];
        var privateKey = config["Push:VapidPrivateKey"];
        var subject = config["Push:VapidSubject"];
        // VAPID 미설정이면 조용히 스킵 — 알림 발송 실패가 예약 접수 자체를 막으면 안 된다(가이드 3-5절 원칙).
        if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(privateKey) || string.IsNullOrEmpty(subject))
        {
            logger.LogInformation("VAPID 미설정 — 웹 푸시 발송 스킵");
            return;
        }

        var vapid = new VapidDetails(subject, publicKey, privateKey);
        var client = new WebPushClient();

        // 🔴 DB성능(2026-08-30 감사, F3) — 이전엔 구독 테이블을 전량 ToListAsync()로 읽었다(알림 발송
        // 백그라운드 경로 = 체감 지연 없이 조용히 나빠지는 경로). PK 기준 keyset 페이징(배치 200)으로
        // 바꿔, 테이블이 커져도 한 번에 메모리에 올리는 양·쿼리 스캔 범위가 유계가 되게 한다
        // (s.Id > lastId + OrderBy(s.Id)는 PK 인덱스, User 조인은 ix_web_push_subscriptions_user_id).
        // 단일 병원 규모(직원 수 × 기기 수)에선 사실상 1배치로 끝난다.
        // 4-9절 — 정지된 계정은 대상에서 제외. Locale은 4-8절 문구 조립용, 조인으로 조회(N+1 방지).
        const int batchSize = 200;
        var toRemove = new List<int>();
        var lastId = 0;
        while (true)
        {
            var batch = await db.WebPushSubscriptions.AsNoTracking()
                .Where(s => s.Id > lastId && s.User != null && !s.User.IsSuspended)
                .OrderBy(s => s.Id)
                .Take(batchSize)
                .Select(s => new { s.Id, s.Endpoint, s.P256dh, s.Auth, Locale = s.User!.Locale })
                .ToListAsync();
            if (batch.Count == 0) break;

            foreach (var t in batch)
            {
                // 4-1절 — 구독 저장 시점에도 검증하지만, 발송 직전에도 같은 화이트리스트로 재검증한다.
                if (!PushEndpointValidator.IsSafe(t.Endpoint))
                {
                    toRemove.Add(t.Id);
                    continue;
                }

                var (title, body) = NewReservationPushText.Build(t.Locale, customerName, code);
                var payload = JsonSerializer.Serialize(new { title, body, url = $"/admin/reservations/{reservationId}" });

                try
                {
                    await client.SendNotificationAsync(new PushSubscription(t.Endpoint, t.P256dh, t.Auth), payload, vapid);
                }
                catch (WebPushException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone
                    or HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    // 4-6절 — 재시도해도 절대 성공 못 하는 실패만 정리(5xx 등 일시적 오류는 남겨두고 다음 발송 때 재시도)
                    toRemove.Add(t.Id);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "웹 푸시 발송 실패: subscriptionId={Id}", t.Id);
                }
            }

            lastId = batch[^1].Id;
            if (batch.Count < batchSize) break;
        }

        if (toRemove.Count > 0)
            await db.WebPushSubscriptions.Where(s => toRemove.Contains(s.Id)).ExecuteDeleteAsync();
    }
}
