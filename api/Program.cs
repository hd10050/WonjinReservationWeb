using Microsoft.EntityFrameworkCore;
using WonjinApi.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
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

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
