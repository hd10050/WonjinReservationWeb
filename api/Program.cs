using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
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

    // 공개 예약 폼(11-1·7-5절) — IP 파티션, 분당 5회. 광고 랜딩發 남용 방지가 목적이라
    // 로그인처럼 이메일 조합이 필요 없다(계정이 없는 익명 제출이므로).
    options.AddPolicy("reservation-create", context =>
        RateLimitPartition.GetFixedWindowLimiter(GetClientIp(context, internalSecret), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));
});

// ── 리버스 프록시(Render/Cloudflare) 뒤에서 실제 클라이언트 IP·스킴 복원 ──
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // PaaS 컨테이너는 내부망으로만 접근 가능하므로 전체 신뢰가 안전(auth-pattern-reference.md 5장) —
    // 특정 CIDR로 좁히면 내부 로드밸런서 IP가 그 범위 밖이라 스킴 판별이 깨질 수 있다.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// ── DI 등록 ──
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
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
app.Use(async (ctx, next) =>
{
    var method = ctx.Request.Method;
    if (HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method))
    {
        var origin = ctx.Request.Headers.Origin.ToString();
        if (!string.IsNullOrEmpty(origin) && !allowedOrigins.Contains(origin))
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            await ctx.Response.WriteAsJsonAsync(new { code = "ORIGIN_NOT_ALLOWED" });
            return;
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

app.Run();

// Cloudflare가 실제 TCP 접속 정보로 직접 설정하는 CF-Connecting-IP는 "프론트(Workers) 뒤"에서만
// 위조 불가능하다. 백엔드(Render)를 직접 호출하는 경로에선 공격자가 이 헤더를 마음대로 채울 수
// 있으므로, 프론트만 아는 내부시크릿이 유효할 때만 신뢰하고 아니면 실제 TCP 연결 IP로 폴백한다
// (16장 + 보안감사 2026-08-26 H1 수정).
static string GetClientIp(HttpContext context, string internalSecret)
{
    var provided = context.Request.Headers["X-Internal-Secret"].FirstOrDefault();
    var trusted = !string.IsNullOrEmpty(internalSecret) && !string.IsNullOrEmpty(provided)
        && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(internalSecret));

    if (trusted)
    {
        var cfIp = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(cfIp)) return cfIp;
    }
    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
