using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WonjinApi.Data;
using WonjinApi.Filters;
using WonjinApi.Services;

var builder = WebApplication.CreateBuilder(args);

// CORS 허용 오리진 — 동일 출처 프록시(D7)가 기본이라 브라우저가 이 서버를 직접 호출할 일은
// 없어야 하지만, CSRF Origin 검증(아래)에도 같은 목록을 재사용한다(4-3절 Cors__AllowedOrigins).
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? [];

// 🔴 보안감사(2026-08-26) 발견 — 프론트(Cloudflare Workers)만 CF-Connecting-IP 뒤에 있고
// 백엔드(Render)는 그렇지 않다. 이 헤더를 무조건 신뢰하면, 동일 출처 프록시를 건너뛰고
// Render URL을 직접 호출하며 헤더를 조작해 Rate Limit을 무제한 우회할 수 있었다
// (web-security-audit-guide.md 3-1절). 프론트·백엔드만 아는 내부시크릿(랜딩방문 기록과
// 동일한 InternalSecret 재사용)이 유효할 때만 CF-Connecting-IP를 신뢰하고, 아니면 실제
// TCP 연결 IP로 폴백한다 — 요청을 거부하지 않고 신뢰 헤더만 무시하는 방식(3-1절 권장).
var internalSecret = builder.Configuration["InternalSecret"] ?? "";

// Add services to the container.

builder.Services.AddControllers(options =>
{
    // 정지·강등을 매 요청 즉시 반영(7-3절). AuditLogFilter는 반드시 이 필터보다 뒤에 등록한다 —
    // 정지된 요청은 AccountStateFilter가 next()를 호출하지 않고 여기서 먼저 차단해 감사 로그까지 가지 않는다.
    options.Filters.Add<AccountStateFilter>();
    options.Filters.Add<AuditLogFilter>();
})
// 🔴 web-security-audit-guide.md 19장 재감사(2026-08-27) 발견 — 이 설정이 없으면 [Required]/
// [MaxLength] 등 DTO 애노테이션 위반이 컨트롤러 코드에 닿기도 전에 [ApiController]가 자동으로
// 가로채 기본 ValidationProblemDetails({type,title,status,errors,traceId})로 응답한다. 이 프로젝트의
// 모든 실패 응답은 {code:"..."} 고정 포맷이므로 이 경로만 형식이 어긋나 있었다.
.ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = _ => new BadRequestObjectResult(new { code = "VALIDATION_FAILED" });
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// 스키마 wonjin 고정 + 스네이크케이스 자동 변환 + 마이그레이션 히스토리 테이블 스키마 명시 고정.
// 미지정 시 search_path 규칙 때문에 연결마다 히스토리 테이블 위치가 달라져 마이그레이션이 매번
// 재실행되며 "relation already exists"로 재시작 루프에 빠진다(8장).
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "wonjin"))
        .UseSnakeCaseNamingConvention());

// ── 인증 — JWT Bearer, HttpOnly 쿠키(wj_at)에서 AT를 읽는다(7-1절) ──
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret 미설정");
var jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwtKey,
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero, // 기본 5분 허용 제거(7-1절)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                if (ctx.Request.Cookies.TryGetValue("wj_at", out var token))
                    ctx.Token = token;
                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
});

// ── Rate Limiter(7-2·7-5절) ──
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // 🔴 2026-08-28 추가 — 기본은 빈 바디라 프론트가 이유를 알 방법이 없었다. 이 프로젝트의 모든 실패
    // 응답은 {code:"..."} 고정 포맷(위 InvalidModelStateResponseFactory와 동일 원칙)이라 429도 맞춘다 —
    // 프론트 각 catch 블록은 이미 e.data.code를 t(`errors.${code}`)로 그대로 읽으므로 추가 분기가 필요 없다.
    options.OnRejected = (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        return new ValueTask(context.HttpContext.Response.WriteAsJsonAsync(new { code = "RATE_LIMITED" }, token));
    };

    // 이메일+IP 조합 파티션 — 단일 병원이라 직원 전원이 같은 사무실 IP를 공유하므로
    // 순수 IP 기준을 쓰면 출근 시간 동시 로그인이 서로의 한도를 갉아먹는다(7-2절).
    options.AddPolicy("auth", context =>
    {
        var email = context.Items["AuthEmail"] as string;
        var partitionKey = $"{email?.Trim().ToLowerInvariant() ?? "-"}|{GetClientIp(context, internalSecret)}";
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
    });

    // 사용자 ID 대신 RT 쿠키 해시로 파티션 — 동기 콜백에서 DB 조회 없이 즉시 얻을 수 있고,
    // 세션 단위라 사용자 ID 기준보다 더 세밀하게 격리된다. "auth" 정책 재사용은 금지(7-2절) —
    // 12분 간격 자동 갱신이 로그인 한도를 잠식해 세션이 통째로 튕기는 사고로 이어진다.
    options.AddPolicy("refresh", context =>
    {
        var raw = context.Request.Cookies["wj_rt"];
        var partitionKey = string.IsNullOrEmpty(raw)
            ? GetClientIp(context, internalSecret)
            : Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
    });

    // 공개 예약 폼(11-1·7-5절) — IP 파티션, 5분당 5회(2026-08-28 1분→5분 변경, 사용자 지시). 광고 랜딩發
    // 남용 방지가 목적이라 로그인처럼 이메일 조합이 필요 없다(계정이 없는 익명 제출이므로).
    options.AddPolicy("reservation-create", context =>
        RateLimitPartition.GetFixedWindowLimiter(GetClientIp(context, internalSecret), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0,
        }));

    // 🔴 web-security-audit-guide.md 4장 재감사(2026-08-27) 발견 — design.md 7-5절 표에 이미
    // 명시돼 있었지만 실제로는 구현이 안 돼있어 관리자 쓰기 API 전체(예약 수정·실장 배정·상태전이·
    // 상담기록·삭제·계정/실장/시술 CRUD)에 rate limit이 전혀 없었다. UseRateLimiter()가
    // UseAuthentication() 다음에 등록돼 있어(아래) 이 시점엔 context.User가 이미 채워져 있다.
    options.AddPolicy("admin-write", context =>
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anon";
        return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
    });
});

// ── 리버스 프록시(Render/Cloudflare) 뒤에서 실제 클라이언트 스킴 복원 ──
// 🔴 web-security-audit-guide.md 3장 재감사(2026-08-27) 발견 — 이전엔 XForwardedFor도 함께
// 신뢰(KnownIPNetworks/KnownProxies 둘 다 Clear = 전체 신뢰)했다. Context7(공식 문서, ASP.NET Core
// Forwarded Headers Middleware)로 확인한 결과 이 설정은 X-Forwarded-For 헤더값으로
// HttpContext.Connection.RemoteIpAddress 자체를 덮어쓴다 — 그런데 Render 백엔드는 CF-Connecting-IP와
// 마찬가지로 공개 URL로 직접 호출 가능해(H1 원인과 동일 전제), 공격자가 이 헤더를 조작하면
// GetClientIp()의 "신뢰 안 되면 RemoteIpAddress로 폴백" 안전망 자체가 무력화되어 H1 수정 이전과
// 동일하게 Rate Limit을 무제한 우회할 수 있었다(3-1절과 완전히 같은 취약점 클래스, 헤더만 다름).
// XForwardedProto는 스킴 판별(HTTPS 리다이렉트)에만 쓰이고 IP 기반 보안 결정에 관여하지 않아
// 낮은 위험이라 유지, XForwardedFor만 제거해 RemoteIpAddress를 실제 TCP 연결값 그대로 보존한다.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// ── DI 등록 ──
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IPushSender, PushSender>();
// Singleton 필수 — 연결된 SSE 구독자 목록을 프로세스 생존 기간 내내 들고 있어야 한다(Scoped/Transient면 요청마다 새로 생겨 무의미).
builder.Services.AddSingleton<IAdminEventBroadcaster, AdminEventBroadcaster>();
builder.Services.AddHostedService<RefreshTokenCleanupService>();

var app = builder.Build();

// Phase 0 완료 기준: 컨테이너에서 Asia/Seoul 타임존 조회 성공(9-2절 [미확인] 해소).
// 실패하면 컨테이너가 여기서 즉시 죽는다 — 배포 이미지에 tzdata가 없다는 뜻이므로 늦게 발견하면 안 된다.
var kst = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");
app.Logger.LogInformation("Asia/Seoul 타임존 로드 성공: UTC{Offset}", kst.BaseUtcOffset);

// 부팅 시 마이그레이션 적용 — 트래픽을 받기 전에 끝나므로 자기 자신의 쓰기를 막지 않는다(17장).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    // 🔴 커스텀 예외 처리기가 없으면 처리 안 된 예외의 응답 형태가 미정이라 이후 클라이언트의
    // {code:...} 파싱 계약이 깨진다. Development의 상세 스택트레이스 노출(재감사 2번 결함)은
    // UseDeveloperExceptionPage가 이 분기 밖(암묵적으로 Development에서만 활성)이라 여기서
    // 건드리지 않고, Production 전용으로 항상 {code:"INTERNAL_ERROR"} 형태만 반환하게 한다.
    app.UseExceptionHandler(errApp =>
    {
        errApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { code = "INTERNAL_ERROR" });
        });
    });
}

app.UseHttpsRedirection();

// 미들웨어 파이프라인 순서 반드시 준수(16장 체크리스트):
// ForwardedHeaders → 보안헤더 → CSRF Origin → CORS → [rate limit 이메일 프리리드] → Authentication → RateLimiter → Authorization
app.UseForwardedHeaders();

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    ctx.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    ctx.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
    await next();
});

// CSRF 방어 — 상태변경 요청의 Origin 검증. 인증·인가보다 반드시 앞단(16장).
// 🔴 web-security-audit-guide.md 6장 재감사(2026-08-27) 발견 — Origin 헤더가 아예 없는 요청을
// 무조건 통과시키고 있었다(가이드가 명시적으로 경고하는 패턴). 실제로 이 구멍을 타는 경로를
// 찾음: 프론트 SSR이 성능을 위해 프록시를 건너뛰고 백엔드를 직접 호출하는 지점들
// (useAuth.ts fetchMe/ssrRefreshCookie, useApi.ts fetchOnce)이 Origin도 X-Internal-Secret도
// 없이 호출하고 있었다 — 함께 수정해 이제 그 호출들도 시크릿을 보낸다. Origin 없는 요청은
// 이제 그 시크릿이 유효할 때만 통과한다(GetClientIp와 동일한 신뢰 메커니즘 재사용).
app.Use(async (ctx, next) =>
{
    var method = ctx.Request.Method;
    if (HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method))
    {
        var origin = ctx.Request.Headers.Origin.ToString();
        if (!string.IsNullOrEmpty(origin))
        {
            if (!allowedOrigins.Contains(origin))
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                await ctx.Response.WriteAsJsonAsync(new { code = "ORIGIN_NOT_ALLOWED" });
                return;
            }
        }
        else
        {
            var provided = ctx.Request.Headers["X-Internal-Secret"].FirstOrDefault();
            var trusted = !string.IsNullOrEmpty(internalSecret) && !string.IsNullOrEmpty(provided)
                && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(internalSecret));
            if (!trusted)
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                await ctx.Response.WriteAsJsonAsync(new { code = "ORIGIN_NOT_ALLOWED" });
                return;
            }
        }
    }
    await next();
});

app.UseCors("Frontend");

// "auth" rate limit 정책이 이메일+IP로 파티션하려면 로그인 요청 본문을 미리 읽어야 한다.
// PartitionedRateLimiter의 파티션 키 획득 콜백은 동기 함수라, RateLimiter 미들웨어 진입 전에
// 여기서 미리 비동기로 읽어 HttpContext.Items에 캐싱해둔다(7-2절 [미확인] 해소 — 실측 확인).
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path == "/api/auth/login" && HttpMethods.IsPost(ctx.Request.Method))
    {
        ctx.Request.EnableBuffering();
        using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        ctx.Request.Body.Position = 0;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("email", out var emailProp) && emailProp.ValueKind == JsonValueKind.String)
                ctx.Items["AuthEmail"] = emailProp.GetString();
        }
        catch
        {
            // 파싱 실패 시 이메일 없이 IP만으로 폴백(7-2절)
        }
    }
    await next();
});

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();

// 예약 확정 시 [예약 달력] 조용히 새로고침용 SSE(2026-08-27, 스파이크 테스트로 프록시 통과 확인 완료).
// 한 종류(reservation_confirmed)만 흘려보낸다 — 새 예약 접수는 별도 웹 푸시(PushSender)로 처리.
// 🔴 2026-08-30 감사 반영 — 이건 minimal API라 AccountStateFilter(MVC 액션 필터)가 적용되지 않는다.
// 정지·강등된 계정이 아직 만료 안 된 AT로 스트림을 유지하지 못하도록, 연결 수립 시점에 필터와
// 동일한 검사(IsSuspended + 토큰 Role과 DB Role 일치)를 직접 수행한다. EventSource는 연결이 끊기면
// 자동 재연결하므로 재연결마다 이 검사를 다시 통과해야 한다. 페이로드는 예약 ID뿐이라 이미
// auth-pattern-reference.md 22장의 "페이로드 최소화" 완화는 충족돼 있고, 여기에 연결시점 재검증을 더한다.
app.MapGet("/api/admin/events", async (HttpContext http, AppDbContext db, IAdminEventBroadcaster broadcaster, CancellationToken ct) =>
{
    var userIdStr = http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? http.User.FindFirstValue("sub");
    if (!int.TryParse(userIdStr, out var uid))
        return Results.Unauthorized();
    var state = await db.Users.AsNoTracking()
        .Where(u => u.Id == uid)
        .Select(u => new { u.IsSuspended, u.Role })
        .FirstOrDefaultAsync(ct);
    if (state is null || state.IsSuspended || state.Role != http.User.FindFirstValue(ClaimTypes.Role))
        return Results.Unauthorized();

    var reader = broadcaster.Subscribe(out var subscriptionId);

    async IAsyncEnumerable<string> Read([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var msg in reader.ReadAllAsync(cancellationToken))
                yield return msg;
        }
        finally
        {
            broadcaster.Unsubscribe(subscriptionId);
        }
    }

    return Results.ServerSentEvents(Read(ct), eventType: "reservation_confirmed");
}).RequireAuthorization();

app.Run();

// 프론트(Workers)가 릴레이하는 X-Wj-Client-Ip는 내부시크릿이 유효할 때만 신뢰하고, 아니면 실제
// TCP 연결 IP로 폴백한다(16장 + 보안감사 2026-08-26 H1 수정).
// 🔴 2026-08-28 재수정 — 원래 이름은 CF-Connecting-IP였으나, Render(onrender.com)도 Cloudflare
// 엣지 뒤에 있어서 이 이름의 헤더는 Render 앞단 엣지가 항상 실제 TCP 접속 값(Workers 아웃바운드
// IP, PoP마다 달라짐)으로 재작성해버림을 `/api/internal/debug-ip` 임시 진단으로 실측 확인(요청마다
// 다른 값 → 매 요청이 별도 rate-limit 버킷으로 흩어져 사실상 무제한이 됨). Cloudflare가 예약하지
// 않은 커스텀 이름(X-Wj-Client-Ip)으로 바꿔 프론트 `server/api/[...].ts`와 함께 수정.
static string GetClientIp(HttpContext context, string internalSecret)
{
    var provided = context.Request.Headers["X-Internal-Secret"].FirstOrDefault();
    var trusted = !string.IsNullOrEmpty(internalSecret) && !string.IsNullOrEmpty(provided)
        && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(internalSecret));

    if (trusted)
    {
        var clientIp = context.Request.Headers["X-Wj-Client-Ip"].FirstOrDefault();
        if (!string.IsNullOrEmpty(clientIp)) return clientIp;
    }
    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
