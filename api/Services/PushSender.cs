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

        // 4-9절 — 정지된 계정은 대상에서 제외(활성 계정만). Locale은 4-8절 문구 조립용, 조인으로 한 번에 조회(N+1 방지).
        var targets = await db.WebPushSubscriptions.AsNoTracking()
            .Where(s => s.User != null && !s.User.IsSuspended)
            .Select(s => new { s.Id, s.Endpoint, s.P256dh, s.Auth, Locale = s.User!.Locale })
            .ToListAsync();

        if (targets.Count == 0) return;

        var toRemove = new List<int>();
        foreach (var t in targets)
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

        if (toRemove.Count > 0)
            await db.WebPushSubscriptions.Where(s => toRemove.Contains(s.Id)).ExecuteDeleteAsync();
    }
}
