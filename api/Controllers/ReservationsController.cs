using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using WonjinApi.Data;
using WonjinApi.DTOs;
using WonjinApi.Models;
using WonjinApi.Services;

namespace WonjinApi.Controllers;

[ApiController]
[Route("api/reservations")]
public class ReservationsController(AppDbContext db, IServiceScopeFactory scopeFactory, ILogger<ReservationsController> logger) : ControllerBase
{
    private static readonly TimeZoneInfo Kst = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");
    private static readonly string[] SupportedLocales = ["zh-CN", "zh-TW", "en", "ko"];
    private static readonly string[] SupportedGenders = ["Female", "Male", "Other"];

    // 공개 예약 신청(11-1절). rate limit(IP 5분당 5회, Program.cs "reservation-create") +
    // honeypot + 개인정보 동의 서버 재검증. 이 프로젝트의 공개 API는 이 엔드포인트 하나뿐이다.
    [HttpPost]
    [EnableRateLimiting("reservation-create")]
    public async Task<ActionResult<ReservationCreateResponse>> Create([FromBody] ReservationCreateRequest req)
    {
        // honeypot — 사람 눈에는 안 보이는 필드가 채워졌으면 봇으로 간주해 조용히 성공 처리한다.
        // 실패 응답을 주면 봇이 실패 패턴을 학습해 우회를 시도할 여지를 준다.
        if (!string.IsNullOrEmpty(req.Honeypot))
            return Ok(new ReservationCreateResponse(string.Empty, req.WechatId));

        if (!req.PrivacyConsent)
            return BadRequest(new { code = "PRIVACY_CONSENT_REQUIRED" });

        if (!SupportedGenders.Contains(req.Gender))
            return BadRequest(new { code = "INVALID_GENDER" });

        if (!SupportedLocales.Contains(req.Locale))
            return BadRequest(new { code = "UNSUPPORTED_LOCALE" });

        if (req.BirthDate == default)
            return BadRequest(new { code = "INVALID_BIRTH_DATE" });

        var nowKst = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Kst);
        var kstDate = DateOnly.FromDateTime(nowKst.DateTime);

        // 연락 희망 날짜·시각(D10) — D26(2026-08-28): "상관없음" 체크 시 둘 다 필수 검증을 건너뛰고
        // null로 저장한다. 체크 안 했으면 이전과 동일하게 둘 다 필수.
        if (!req.ContactTimeIndifferent)
        {
            if (req.PreferredContactDate is null || req.PreferredContactDate == default)
                return BadRequest(new { code = "INVALID_CONTACT_DATE" });
            // 🔴 과거 날짜 선택 차단(2026-08-28 사용자 지시) — 프론트 DatePicker min-value는 UX일
            // 뿐, API 직접 호출 우회를 막는 실제 방어선은 서버다(D17 등 기존 패턴과 동일 원칙).
            // KST 기준 오늘(kstDate)과 비교 — 위 예약코드 발급에 쓰는 것과 동일한 값을 재사용한다.
            if (req.PreferredContactDate.Value < kstDate)
                return BadRequest(new { code = "PAST_CONTACT_DATE" });
            if (req.PreferredContactTime is null)
                return BadRequest(new { code = "INVALID_CONTACT_TIME" });
        }

        // 예약 코드 원자적 증가 발급(8-11절) — "그날 MAX(code)+1"은 동시 제출 시 UNIQUE 위반 500이 난다(F4).
        // INSERT ... ON CONFLICT ... DO UPDATE ... RETURNING 한 문장이 행 잠금 안에서 증가+반환을 함께 한다.
        // 🔴 INSERT...RETURNING은 "non-composable SQL"이라 SqlQuery에 바로 SingleAsync()를 걸면
        // EF Core가 서브쿼리로 감싸려다 InvalidOperationException을 던진다(실측 확인) — ToListAsync()로
        // 먼저 완전히 구체화(감싸지 않고 그대로 실행)한 뒤 메모리에서 Single()을 적용해야 한다.
        var seqRows = await db.Database.SqlQuery<int>($"""
            INSERT INTO wonjin.reservation_code_counters (code_date, last_seq)
            VALUES ({kstDate}, 1)
            ON CONFLICT (code_date)
            DO UPDATE SET last_seq = wonjin.reservation_code_counters.last_seq + 1
            RETURNING last_seq AS "Value"
            """).ToListAsync();
        var seq = seqRows.Single();

        var code = $"{kstDate:yyyyMMdd}{seq:D4}";
        var now = DateTimeOffset.UtcNow;

        var reservation = new Reservation
        {
            Code = code,
            Name = req.Name.Trim(),
            BirthDate = req.BirthDate,
            Gender = req.Gender,
            WechatId = req.WechatId.Trim(),
            PreferredContactDate = req.ContactTimeIndifferent ? null : req.PreferredContactDate,
            PreferredContactTime = req.ContactTimeIndifferent ? null : req.PreferredContactTime,
            Locale = req.Locale,
            UtmSource = Truncate(req.UtmSource, 100),
            UtmMedium = Truncate(req.UtmMedium, 100),
            UtmCampaign = Truncate(req.UtmCampaign, 100),
            ReferralCode = Truncate(req.ReferralCode, 50),
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Reservations.Add(reservation);
        db.ReservationLogs.Add(new ReservationLog
        {
            Reservation = reservation,
            Action = "received",
            ActorName = "SYSTEM",
            CreatedAt = now,
        });
        await db.SaveChangesAsync();

        // 새 예약 접수 알림(웹 푸시) — 성능감사(2026-08-28) 발견: await로 붙어있어 고객의 접수
        // 응답이 관리자 알림 발송(구독자 수만큼 FCM 등에 순차 HTTP 호출) 완료까지 그대로 기다렸다.
        // 발송 실패가 접수를 막으면 안 된다는 원칙(web-push-notification-guide.md 3-5절)에는 맞았지만
        // "실패"만 막았을 뿐 "지연"은 그대로 전이됐다 — 응답을 기다리지 않는 백그라운드로 분리한다.
        // 🔴 컨트롤러의 Scoped DI 스코프는 응답 반환과 함께 해제되므로, 그 밖에서 Scoped 서비스인
        // IPushSender(내부에서 AppDbContext 사용)를 쓰려면 반드시 새 스코프를 만들어야 한다
        // (RefreshTokenCleanupService가 BackgroundService에서 쓰는 것과 동일 원칙). ILogger<T>는
        // 싱글턴이라 스코프 해제와 무관하게 그대로 캡처해 써도 안전하다.
        _ = Task.Run(async () =>
        {
            using var scope = scopeFactory.CreateScope();
            try
            {
                var scopedPushSender = scope.ServiceProvider.GetRequiredService<IPushSender>();
                await scopedPushSender.SendNewReservationAlertAsync(reservation.Id, reservation.Name, code);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "새 예약 웹 푸시 발송 실패: reservationId={Id}", reservation.Id);
            }
        });

        return Ok(new ReservationCreateResponse(code, reservation.WechatId));
    }

    // 9-1절 — UTM·추천코드는 거부가 아니라 절단한다. 광고 플랫폼이 붙이는 파라미터 길이를
    // 우리가 통제할 수 없는데, 길다고 예약 신청 자체를 실패시키면 고객을 잃는다.
    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
