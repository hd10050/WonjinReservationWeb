# WonjinReservationWeb 프로젝트
> 루트 규칙 상속: `C:\Users\jinho\Desktop\WebProject\CLAUDE.md` · **설계 SSOT: `docs/design.md`** — 설계 결정이 바뀌면 코드보다 이 문서를 먼저 고칠 것

## 개요
원진성형외과의 **외국인(중화권) 고객 예약·상담 관리 시스템**. 광고로 유입된 고객이 랜딩 폼으로 상담을 신청하면, 병원 실장이 위챗으로 연락해 상담·방문예약을 확정하고 그 과정을 관리자 패널에서 추적·감사·집계한다.
- 흐름: 광고(UTM·추천코드) → 랜딩 폼 제출 → 실장 위챗 연락 → 상담·시술 결정 → 방문예약 확정 → 내원
- 지원 언어 4개: **zh-CN(기본)** · zh-TW · en · ko
- 현재 상태: **Phase 1~6 main 병합 완료**(2026-08-26) + **Phase 7(계정 관리·감사 로그) `phase7-users-audit` 워크트리에서 구현·실측 완료, main 미병합**(2026-08-26, 병합은 사용자 지시 대기). Phase 1(인증)·Phase 2(랜딩+예약폼+유입경로)·Phase 3(예약 대시보드·상세·상담기록·상태머신·소프트삭제)·Phase 4(실장·시술 관리 CRUD)·Phase 5(예약 달력)·Phase 6(실장 KPI·예약 통계)·Phase 7(계정 CRUD + 전역 `AuditLogFilter` + `/admin/users`·`/admin/audit-logs`)까지 진행. **사이드바 네비게이션(12-3절)은 각 Phase가 개별 구현하지 않고 병합 시점에 한 번에 정리하기로 사용자 결정**(2026-08-26) — 그 전까지 신규 관리자 화면은 URL 직접 접근으로만 확인 가능. Phase 8부터는 사용자 지시 대기

## 기술 스택
| 레이어 | 기술 |
|---|---|
| 프론트 | Nuxt 4 + Vue 3 **Composition API** + Tailwind v4(`@tailwindcss/vite`) + `@nuxtjs/i18n`(`prefix_except_default`, 기본 zh-CN) |
| SEO | `@nuxtjs/sitemap` + `@nuxtjs/robots` |
| 백엔드/DB | ASP.NET Core 10 + EF Core(`EFCore.NamingConventions` 스네이크케이스) / PostgreSQL 16 (스키마 `wonjin`) |
| 인증 | 자체 JWT(AT 15분, 쿠키 `wj_at`) + RT(7일, SHA-256, 쿠키 `wj_rt`) — **소셜 로그인·회원가입 없음** |
| UI | **shadcn-vue**(`shadcn-nuxt`, style `new-york`) + `reka-ui` 프리미티브 + `class-variance-authority`(D19, 구 D11 대체) |
| 팔레트 | **Olive Garden Feast**(D20) — `#606C38`올리브(primary)·`#283618`짙은산림녹(foreground)·`#FEFAE0`크림(background)·`#DDA15E`탄(secondary)·`#BC6C25`번트오렌지(destructive), OKLCH 변환 후 shadcn CSS 변수에 적용 |
| 시각화 | **vue-chartjs**(`^5.3.x`) + **chart.js**(`^4.5.x`)(D21) — 실장 KPI·예약 통계 표+차트 병행. Canvas 기반이라 SSR 불가, `<ClientOnly>` 필수. Chart.js 요소 등록은 `plugins/chartjs.client.ts` 한 곳에 집중 |
| 언어 버전 고정 | **TypeScript 5.9.3 고정**(devDependency) — 7.x(네이티브 재작성판)는 `@vue/compiler-sfc`의 `ts.sys` 타입 해석과 비호환이라 reka-ui 기반 shadcn 컴포넌트 컴파일이 깨짐(11-7절) |
| 배포 | 프론트 Cloudflare Workers / 백엔드·DB Render |
| 로컬 | `docker compose up` — frontend:3700 / api:5200 / postgres:5435 |

## 확정 설계 결정 (2026-08-25 · 상세는 `docs/design.md` 2장)
- **단일 병원 전용** — 전 테이블에 `hospital_id` 없음
- 🔴 **위챗 탑재 취소** — 랜딩엔 헤더·푸터·예약 폼만. QR·아이콘·공식계정 연동 전부 없음 (**재제안 금지**)
- **예약금은 실장 수동 입금 확인만** — PG·웹훅 없음. 통화는 **CNY(기본)/KRW 선택**, 환율 환산 안 함(통계는 통화별 분리, 합산 금지)
- **자동배포 감시 브랜치 = `main`** (2026-08-25 확정)
- **유입 경로 자동 기록**(UTM+추천코드), **인플루언서 전환율은 어드민 전용 메뉴** `/admin/referrals`로 분리
- **회원가입 엔드포인트 없음** — 계정 생성 경로는 `POST /api/admin/users` 하나뿐
- **동일 출처 API 프록시 채택** — 화면 깜빡임 금지(SSR 프리로드)를 이행하기 위한 전제
- 🔴 **실장(`consultants`)은 계정(`users`)과 완전히 별개인 독립 테이블 — 1:1 아님, FK 연결 없음**
- 🔴 **실장·시술·계정·상담기록에 DELETE 없음** — 비활성화(`is_active=false`)·정지로만 처리
- **상담 기록은 누적**(`reservation_notes`) — 덮어쓰기 금지, 삭제 불가, 수정은 작성자·어드민만
- **중복 신청 허용 + 상담 기록 0건인 예약만 실장이 소프트 삭제**(D15) — 전역 쿼리 필터로 전 조회에서 제외
- **예약 통계는 주(일~토) 단위**(D16) — `date_trunc('week')`는 월요일 시작이라 하루 밀어 계산
- 🔴 **실장 배정은 수동, 미배정 예약은 배정·삭제·조회만 가능**(D17) — 상담기록·상태전이·저장 전부 400 차단. 담당 변경은 예외 없이 처리 이력 기록
- **예약 코드 = `YYYYMMDD`+4자리 일별 리셋**(M3, 예 `202608260001`) — `reservation_code_counters` 원자적 증가로 발급
- **모든 시각은 KST 고정** — 브라우저 타임존 사용 금지. `stat_date`·예약코드 날짜도 KST
- 시술명은 언어별 컬럼 4개(`name_zh_cn` 등), **연락 희망 시각은 `<input type="time">` 직접 입력**(`time` 컬럼, 한국 시간 기준) — 선택지로 바꾸지 말 것
- **중화권 브랜드 표기 = `WonJin`**(D18, 2026-08-26) — 4개 로케일 전부 이 토큰 그대로 사용(번역 안 함). title 접미사·og:site_name·JSON-LD name 전부 통일
- **UI 라이브러리 = shadcn-vue**(D19, 2026-08-26) — 구 D11(라이브러리 미도입) 대체, Context7로 최신 설치 문서 확인 후 도입. `components.json`은 CLI 자동생성 대신 수동 작성(11-7절)
- **팔레트 = Olive Garden Feast**(D20, 2026-08-26) — 참고 화면(`reservation-desk_1.html`)의 청록색 팔레트를 대체. coolors.co/palettes/trending을 playwright-cli로 실측 검증해 이름·좋아요 수 확인된 팔레트만 채택(발명 절대 금지 — 실제 발생했던 사고)
- **병원 정식 정보 확정**(M8, 2026-08-26) — 상호 `원진성형외과의원`·사업자번호 `824-67-00414`·주소는 화면 푸터에 원문 그대로 표기(고유명사 번역 안 함). 🔴 **대표전화는 화면에 노출하지 않고 JSON-LD에만 포함**(예약 폼 유도 우선, 사용자 결정) — 상세는 design.md 12-1-1절
- **실장 KPI·예약 통계 = 표+차트 병행**(D21, 2026-08-26) — 차트는 `vue-chartjs`+`chart.js`, 색상은 새로 만들지 않고 D20 팔레트 재사용. Canvas는 SSR 불가라 `<ClientOnly>`로 감싸고(화면 깜빡임 금지 원칙은 데이터 프리로드 대상이라 위반 아님), 레이아웃 시프트 방지로 고정 높이 컨테이너 사용

## 역할 · 메뉴 권한
| 메뉴 | Admin | HospitalManager | Consultant |
|---|:---:|:---:|:---:|
| 예약 대시보드 · 예약 상세 · 예약 달력 | ✅ | ✅ | ✅ |
| 실장 관리 · 시술/수술 관리 · 실장 KPI · 예약 통계 | ✅ | ✅ | ❌ |
| 계정 관리 · 로그(감사) · 유입 경로 분석 | ✅ | ❌ | ❌ |

## 🔴 이 프로젝트에서 특히 주의할 것
- 🔴🔴 **`middleware/admin.ts`의 `to.path.startsWith(p + '/')` 접두사 매칭에서 `/admin`(대시보드 루트) 자신은 반드시 제외할 것** — 제외 안 하면 `/admin/`로 시작하는 **모든** 하위 경로가 전부 매치돼(예: `/admin/kpi`도 `/admin/`로 시작하므로) 역할별 화이트리스트가 통째로 무력화된다. **Phase 1·3부터 있던 기존 결함**(2026-08-26 Phase 6 워크트리 실측으로 발견 — Consultant가 `/admin/kpi`·`/admin/stats`에 실제로 200 접근됨) → 워크트리에서 수정 후, **같은 결함이 main에도 그대로 남아있는 걸 확인해 main 체크아웃에서 직접도 수정**(당시 main에 이미 병합돼 있던 `/admin/consultants`·`/admin/procedures`로 재현·재검증, Consultant 302 차단 확인). `p !== '/admin' &&` 조건 추가, 6-3절 예제 코드도 갱신. **백엔드 `[Authorize]`는 정상 작동해 데이터 유출은 없었지만, 신규 관리자 페이지를 추가할 때마다 이 조건이 유지되는지 반드시 확인할 것**
- **🔴 실장 ≠ 계정** — [실장 관리]는 `consultants` 마스터 CRUD이고 [계정 관리]는 `users` CRUD. `role='Consultant'`로 실장 목록을 만들려 하지 말 것(초안에서 실제로 저지른 오설계)
- **비활성 실장 노출 규칙** — 신규 배정 드롭다운·KPI·통계에서는 제외, 과거 예약 상세·처리 이력에는 그대로 표시. 편집 드롭다운은 현재 배정된 비활성 실장을 목록에 남길 것(빼면 저장 시 담당자가 조용히 바뀜)
- **감사 로그 대상은 3역할 전부** — 일반 가이드는 `role=="Admin"`만 감사하지만, 사용자 요구는 실장·병원관리자 CRUD까지 전부 감사하는 것. Admin으로 좁히면 실장 행위 전체가 로그에서 빠짐
- **예약 코드는 카운터 원자적 증가로 발급**(8-11절) — "그날 최대값+1"은 동시 제출 시 UNIQUE 위반 500
- **라우트에 `{id:int}` 제약 필수** — 없으면 `/reservations/summary`의 `summary`가 `{id}`로 매칭돼 대시보드가 안 뜸. 고정 경로를 `{id}`보다 먼저 선언
- **raw SQL엔 전역 쿼리 필터가 안 걸림** — `deleted_at IS NULL`을 직접 써야 함. 허용된 raw SQL은 3곳뿐(15-1·11-4·8-11)
- **예약 달력은 `Confirmed`+`Visited` 둘 다** — Confirmed만 보면 내원 순간 달력에서 사라짐. 부분 인덱스 조건도 동일하게
- **랜딩에서 `/api/auth/me` 호출 금지** — 인증 초기화는 `/admin` 경로에서만. 광고 트래픽만큼 401이 백엔드로 감
- **랜딩 방문 기록은 내부 시크릿 헤더 필수** — 공개 엔드포인트로 열면 전환율 조작 가능. `await` 하지 말 것(랜딩 지연)
- **`ix_refresh_tokens_token_hash` 인덱스 필수** — 모든 세션이 12분마다 이 컬럼을 조회. 없으면 갱신마다 풀스캔이며 UI 지연이 없어 발견이 매우 늦어짐(타 프로젝트 실제 사고)
- **`landing_daily_stats`의 키 컬럼은 전부 `NOT NULL DEFAULT ''`** — PG UNIQUE는 NULL을 서로 다르게 취급(NULLS DISTINCT)해 NULL 허용 시 같은 조합이 무한 중복됨
- **언어 감지 리다이렉트가 `location.search`를 보존해야 함** — 빠뜨리면 UTM이 통째로 유실돼 유입 경로 추적이 죽음
- **`refresh`에 로그인용 rate limit 정책 재사용 금지** — 12분 간격 자동갱신이 한도를 잠식해 로그인까지 429로 막힘. 사용자 ID 파티션 전용 정책을 별도로 둘 것
- **`__EFMigrationsHistory` 스키마 명시 고정 필수** — `MigrationsHistoryTable("__EFMigrationsHistory", "wonjin")`. 미지정 시 `search_path` 규칙으로 위치가 달라져 마이그레이션 재실행 → `relation already exists` 재시작 루프
- **`dotnet ef migrations add` 결과 파일은 적용 전 반드시 직접 읽을 것** — scaffolder가 새 부분/복합 인덱스를 이유로 기존 인덱스에 `DropIndex`를 자동 삽입하는 사고가 있었음
- **`AccountStateFilter`는 `[Authorize]` 요구 여부를 먼저 확인** — 안 하면 정지된 유저가 공개 API에서도 401을 맞아 익명 방문자보다 못한 상태가 됨
- 🔴 **`AuditLogFilter`의 상태코드 기록 — `next()` 직후 `HttpContext.Response.StatusCode`를 읽으면 항상 200으로 오기록된다**(design.md 14장·admin-panel-pattern-reference.md 4-2절 원문 패턴이 실제로 이 버그를 가짐, Phase 7 curl 실측으로 발견). 액션 필터의 `next()`는 액션이 `IActionResult`를 반환하는 시점까지만 감싸고, 그 결과를 실제로 응답에 쓰는 결과 실행은 액션 필터 파이프라인 바깥의 더 뒤 단계에서 일어나 — `BadRequest()` 등을 반환해도 이 시점의 `Response.StatusCode`는 여전히 기본값(200)이다. 컨트롤러 예외(`ActionExecutedContext.Exception`)는 별개로 여전히 확인해야 하지만, **정상 반환된 400/404/401 등은 `(executed.Result as Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult)?.StatusCode`로 읽어야 정확하다**(`AuditLogFilter.cs` 실제 구현 참고). 🔴 **두 번째 함정(500 동시성 실측으로 발견): 컨트롤러의 `SaveChangesAsync`가 실패(500)하면 같은 요청 스코프 `DbContext`의 ChangeTracker에 실패 엔티티가 그대로 남아, 필터가 이어서 `AuditLog`를 추가·저장하려 하면 같은 예외가 재발해 catch에 삼켜지고 감사 로그 자체가 통째로 유실된다**(20건 동시 요청 중 500 12건이 로그에 0건 기록되는 것을 실측). `AuditLog` 추가 직전 `db.ChangeTracker.Clear()` 필수(수정 후 500 12건 전부 정확히 기록되는 것 재확인)
- **관리자 로그인 진입점(푸터 저작권 링크) 숨김은 보안이 아님** — 실제 보호는 인증과 `[Authorize]`
- `package-lock.json`은 **npm@10.9.2**로 생성 (npm 11은 Cloudflare CI `EBADPLATFORM`)
- 로케일 JSON 값에 **순수 `@` 문자 금지** — vue-i18n이 linked message로 오인해 해당 로케일 컴파일이 깨짐(클라이언트 라우팅에서만 raw key 노출)
- **🔴 미배정 예약은 배정·소프트삭제·조회만 허용, 나머지 전부 400 `RESERVATION_NOT_ASSIGNED`**(D17, 10-1절) — 상담기록 추가·상태전이·방문일시/시술/예약금 저장 전부 차단. 배정 검사를 상태전이 UPDATE의 WHERE에 함께 넣을 것(따로 조회하면 그 사이 배정 해제된 경우를 놓침). 담당자 변경은 예외 없이 처리 이력 기록
- **로그인 rate limit을 순수 IP로 두지 말 것** — 단일 병원이라 직원 전원이 같은 사무실 IP를 공유, IP 기준이면 출근시간 정상 로그인이 429로 막힘. 이메일+IP 조합 파티션 사용(7-2절)
- **통계 쿼리 `GroupBy(...).Select(g => new Dto(...))` 금지** — EF Core가 SQL로 못 옮겨 런타임 예외. 익명 타입으로 먼저 집계 후 메모리에서 DTO 매핑하는 2단계로(11-6절)
- **`RouteMap`은 세그먼트 개수 내림차순 정렬 필수** — `/notes`·`/status`가 상위 경로와 세그먼트 수 차이로 동시 매치됨. 정렬 없으면 상담기록 추가가 "예약 수정"으로 오분류(14-1절)
- **`detectBrowserLanguage`는 반드시 `false`** — 켜두면 카카오톡·라인 등 링크 미리보기 봇이 언어감지 리다이렉트를 따라가 엉뚱한 언어의 og:description 노출(5-2절)
- **robots `disallow`는 트레일링 슬래시 필수**(`/admin/`) — 슬래시 없이 쓰면 prefix 매칭으로 다른 경로까지 막히고 sitemap exclude에도 적용돼 동적 URL이 원인불명으로 누락(5-5절)
- **검색 입력을 반응형 쿼리에 직접 바인딩 금지** — 매 키입력마다 API 재호출됨. URL 쿼리를 `computed`로 감싸 제출 시에만 반응하게 할 것(12-4절)
- **shadcn-vue 컴포넌트 추가 시 `app/lib/utils.ts`가 자동생성 안 됨** — `components.json`을 수동 작성했기 때문에 CLI가 `cn()` 헬퍼를 안 만듦. 컴포넌트 추가 전 직접 작성 필요(11-7절)
- **shadcn-vue add 실행 전 `.nuxt/` 존재 확인 필수** — 없으면 CLI가 `resolvedPaths: Required...`로 즉시 실패. `npx nuxi prepare` 먼저 실행(11-7절)
- 🔴 **reka-ui 기반 컴포넌트의 `interface Props extends PrimitiveProps` Vite 에러는 `/* @vue-ignore */`로 우회하지 말 것 — 정정.** 근본 원인은 `typescript@7.x`(네이티브판)와 `@vue/compiler-sfc`의 `ts.sys` 비호환. **해결은 `typescript`를 5.x로 다운그레이드하는 것**(위 기술 스택 표). `/* @vue-ignore */`를 쓰면 `as` prop의 `withDefaults` 기본값이 인스턴스에 반영 안 돼 `<button>`이 `<div>`로 렌더링되고 `type="submit"` 폼 제출이 조용히 깨진다 — 실측으로 확인된 실제 장애(11-7절, "실사용에 영향 없음"이라던 Phase 0 기록은 오판이었음)
- **shadcn CLI의 tsconfig alias 인식 실패를 `tsconfig.json` 수정으로 고치지 말 것** — Nuxt의 실제 타입 해석(`.nuxt/tsconfig.*.json` 참조)이 깨짐. `components.json`을 손으로 작성해 우회할 것(11-7절)
- **`app.vue`에 `<NuxtPage />`가 없으면 `pages/` 디렉토리를 아무리 만들어도 전부 404** — `pages/`를 처음 쓰는 순간부터 `app.vue`는 `<NuxtLayout><NuxtPage /></NuxtLayout>` 셸이어야 함. 기존 콘텐츠는 `pages/index.vue`로 이동
- 🔴 **C# record의 검증 애노테이션은 `[property: ...]`가 아니라 파라미터에 직접 붙일 것** — `[property: Required]`는 컴파일은 통과하지만 그 record를 실제로 모델 바인딩하는 첫 요청에서 500(`InvalidOperationException`)을 던짐. `[Required] string Email`처럼 타겟 지정자 없이 쓸 것(11-8절)
- **`useOpsLocale()`은 `locale.value = code` 직접 대입 금지, 반드시 `await setLocale(code)`** — 직접 대입은 lazy 로케일 메시지 로드를 트리거 안 해 `t()`가 raw key를 반환함. 호출부(로그인 페이지·admin 레이아웃)도 `await useOpsLocale()`로 SSR 완료를 기다릴 것(5-4절)
- **`refresh` rate limit 파티션은 사용자 ID가 아니라 RT 쿠키 해시로 구현**(설계 원문과 의도적 편차, 7-2절) — DB 조회 없이 동기 콜백에서 즉시 얻을 수 있어 세션 단위로 더 세밀하게 격리됨
- 🔴 **`db.Database.SqlQuery<T>(...)`에 바로 `.SingleAsync()`를 걸지 말 것** — `INSERT...RETURNING`처럼 non-composable SQL을 서브쿼리로 감싸려다 `InvalidOperationException`을 던진다(실측 확인, 8-11절 코드카운터). `.ToListAsync()`로 먼저 그대로 구체화한 뒤 메모리에서 `.Single()`을 적용할 것
- 🔴 **`db.Database.SqlQuery<T>(...)`의 다중 컬럼 매핑은 `UseSnakeCaseNamingConvention()`의 영향을 그대로 받는다** — 결과 타입 프로퍼티가 `WeekStart`면 SQL 별칭도 `week_start`(스네이크케이스)여야 매칭된다. PascalCase 따옴표 별칭(`AS "WeekStart"`)을 쓰면 "required column 'week_start' was not present" 예외(실측 확인, 11-4절 주간 통계). 8-11절의 `SqlQuery<int>` 스칼라 예시는 컬럼명 매칭이 아니라서 이 규칙이 안 보였을 뿐, 다중 컬럼 raw SQL엔 항상 적용됨
- 🔴 **`[FromQuery] DateOnly` 등 비-nullable 값 타입은 파라미터 누락 시 400이 아니라 `default`(예: `0001-01-01`)로 조용히 바인딩된다**(`[ApiController]`의 자동 400은 문자열류에만 해당, 실측 확인 — 문서만으로 400을 단정하지 말 것). 필수 쿼리 파라미터는 컨트롤러에서 `== default` 명시적으로 검사해 400을 직접 반환할 것
- **Vue 템플릿에서 `{{ prefix }}<A>{{ link }}</A>{{ suffix }}` 사이에 줄바꿈을 넣으면 공백이 하나씩 끼어든다** — 한국어처럼 조사가 바로 붙어야 하는 언어에서 실제로 어색해짐(실측 확인). 4언어 문구를 한 태그에 이어붙일 땐 줄바꿈 없이 한 줄로 쓰고, 필요한 공백은 번역 문자열 자체에 포함시킬 것
- **`@nuxtjs/i18n`의 `locales[].code`가 대문자를 포함하면(`zh-TW`) URL prefix도 그 대소문자 그대로 생성된다**(`/zh-TW`) — 대소문자 URL 둘 다 정상 라우팅되어 기능 문제는 없으나, `code`를 DB `locale` 값과 일치시켜야 하므로 지금은 그대로 둠. SEO 정규화 필요해지면(Phase 9) 재검토
- **honeypot 필드가 채워진 요청은 400이 아니라 200으로 조용히 흘려보내고 DB에 저장하지 않을 것** — 실패 응답을 주면 봇이 실패 패턴을 학습해 우회를 시도할 여지를 준다(11-1절)
- 🔴 **한 요청 안에서 `ExecuteUpdateAsync`/`ExecuteDeleteAsync`를 여러 번 쓰거나 그 뒤에 별도 `SaveChangesAsync`로 로그를 남긴다면 반드시 `BeginTransactionAsync()`로 묶을 것** — 묶지 않으면 앞 단계들은 각각 즉시 커밋되고 마지막 단계만 실패해도 "응답은 500인데 일부 변경은 이미 반영된" 부분 커밋이 된다(실측 확인 — `UpdateReservation`에 존재하지 않는 `procedureId`를 보내면 스칼라 필드·자동 Confirmed 전이는 저장되고 시술 목록만 삭제된 채 로그 없이 500이 났음). `audit_logs`처럼 "실패해도 본 작업을 막으면 안 되는" 부가 기록만 트랜잭션 밖에서 별도 try/catch로 베스트에포트 처리
- 🔴 **Npgsql에 `timestamptz` 비교용 `DateTimeOffset`을 넘길 때 Offset은 반드시 0(UTC)이어야 함** — `TimeZoneInfo.ConvertTime(...)`으로 만든 KST(+09:00) 오프셋 `DateTimeOffset`을 쿼리 파라미터로 그대로 쓰면 `Cannot write DateTimeOffset with Offset=09:00:00 ... only offset 0 (UTC) is supported` 500(실측 확인). KST로 년/월만 뽑고 나면 반드시 `.ToUniversalTime()`을 거쳐서 쿼리에 넘길 것
- 🔴 **`audit_logs`는 컨트롤러에서 직접 쓰지 말 것 — 전역 `AuditLogFilter`(Phase 7) 전용**(14장, `AuditLog.cs`·`Program.cs` 주석과 동일). Phase 3에서 `SoftDelete`에만 예외적으로 `db.AuditLogs.Add(...)`를 넣었던 것이 14장 원문 위반이자 6개 쓰기 액션 중 하나만 특별 대우하는 비일관 상태였음 — **Phase 4에서 제거 완료**(`reservation_logs` 삭제 기록·204 응답은 그대로 유지, curl로 `audit_logs` 행 수 불변 확인). Phase 7 전까지는 6개 쓰기 액션 전부 audit_logs 공백으로 일관됨(의도된 임시 상태)
- **다중 role 컨트롤러에 쓰기 액션을 추가할 때는 액션 레벨 `[Authorize]`로 다시 좁힐 것(6-3절 원칙 1, Phase 4 실측)** — `AdminConsultantsController`·`AdminProceduresController`는 GET을 Consultant도 써야 해서(실장 재배정·시술선택 드롭다운) 클래스 레벨을 `Admin,HospitalManager,Consultant`로 열어뒀다. 여기에 POST/PUT을 그냥 추가하면 11-3절 "HospitalManager 이상" 요구와 달리 Consultant도 쓰기가 가능해진다 — 액션마다 `[Authorize(Roles="Admin,HospitalManager")]`를 다시 걸어야 한다. 실제 로그인으로 Admin·HospitalManager 200 / Consultant 403 / 익명 401 전부 실측 확인(Phase 5·6에서 같은 패턴의 컨트롤러를 열 때도 동일 점검 필요)

## 절대 원칙 이행 (루트 CLAUDE.md)
- **화면 깜빡임 금지** — 데이터 페이지는 `<script setup>` 최상위 `await useApi(...)` SSR 프리로드. `onMounted`+client fetch 금지. 전환 오버레이는 `<Transition>` 금지, 항상 마운트 + `pointer-events`를 상태값에 직접 클래스 바인딩
- **입력 길이 3곳 일치** — DB `varchar(N)` / 백엔드 `[MaxLength(N)]` / 프론트 `maxlength` 항상 세트로 수정. 전체 표는 `docs/design.md` 9장
- **디자인 원칙** — 모든 input/textarea/select에 **보이는 label** 필수(검색창 포함). placeholder로 대체 금지. honeypot만 예외
- **DB 성능** — 새 쿼리마다 ①필터·정렬 컬럼 인덱스 ②목록 페이징 ③불필요 컬럼·관계 미조회 3가지 자체 점검
- **번역** — 4개 로케일 JSON의 키 집합이 항상 완전히 동일. 키 추가·삭제는 4파일 세트로 수정 후 개수 대조

## TODO
### 다음 세션 최우선
- [ ] **테스트 데이터 처리 여부 결정** — `test-admin@wonjin.local` + **`test-manager@wonjin.local`·`test-consultant@wonjin.local`(2026-08-26 middleware 결함 검증 위해 실제로 DB에 추가함, 동일 비번 `TestPassword123!`)** + 실장·시술 테스트 데이터. 운영 데이터 아님 — 정리 여부 여전히 사용자 확인 대기(나중에 정리)
- [ ] **로컬 DB 테스트 더미 예약 정리 여부 확인** — Phase 2~4 실측 검증 중 생성된 더미 `reservations`가 로컬 dev DB에 계속 누적 중(30건+). 실서비스 데이터 아님(나중에 정리)
- [ ] **`PATCH /{id}/consultant`(실장 배정)를 `AuditLogFilter`의 RouteMap에 세분화 등록할지 결정**(Phase 7 미작업분 점검 중 발견) — `design.md` 14-1절 표 자체에 이 액션이 없어 현재는 일반 `update`/`reservation`으로 뭉뚱그려 기록됨(틀린 분류는 아니나 notes·status처럼 세분화되지 않음). 필요하면 `design.md` 14-1절에 `(["/api/admin/reservations","/consultant"], PATCH, assign, reservation)` 행 추가 후 `AuditLogFilter.RouteMap`에도 반영
### Phase 계획 — 완료기준 포함 (design.md 19장과 동일, 상세 코드는 그쪽 참고)
| # | 내용 | 완료기준 |
|---|---|---|
| 0 | ✅ 스캐폴딩 + DB 마이그레이션(2026-08-26 완료) | 컨테이너 기동+마이그레이션+인덱스 확인 + `Asia/Seoul` 타임존 조회 성공 — 전건 실측 검증 완료 |
| 1 | ✅ 인증 + `AccountStateFilter` + 동일출처 프록시(2026-08-26 완료) | 로그인~정지차단 E2E + 랜딩에서 `/api/auth/me` 미호출 확인(F5) — 전건 실측 검증 완료 |
| 2 | ✅ 랜딩 4언어 + 예약 폼 + 개인정보 처리방침 + 유입경로 수집(2026-08-26 완료) | 4언어 폼 제출→DB적재+UTM보존 + landing-visit 시크릿없이 404(F11) + 연락희망시각 `time` 저장 확인(D10) — 전건 실측 완료 |
| 3 | ✅ 예약 대시보드·상세·상담기록 누적·상태머신·소프트삭제(2026-08-26 완료, main 병합 완료) | 상태전이 동시성409 + 코드동시생성 중복없음(F4) + 삭제조건409(D15) + **미배정 400 차단**(D17). 동시성 재현 스크립트 3종(`scripts/phase3-concurrency/`) 전건 통과. **설계서 대비 최종 감사 완료, 미해결 2건은 위 TODO 참고** |
| 4 | ✅ 실장(`consultants`)·시술 관리 CRUD(2026-08-26 완료, main 병합 완료) | 4언어 탭 CRUD(시술) + 3역할 권한 매트릭스 실측(Admin·HospitalManager 200 / Consultant 403 / 익명 401) + 비활성 토글·`includeInactive` 필터 브라우저 실측(한중일 텍스트 입력 포함) + 시술 코드 UNIQUE 위반 검증(생성·수정 시 본인 제외) curl 실측. 비활성실장 배정 드롭다운 제외·과거예약 유지(D13)는 Phase 3 로직 그대로(Phase 4에서 변경 없음). **사이드바 미구현 — 병합 시점 일괄 정리 예정** |
| 5 | ✅ 예약 달력(2026-08-26 구현+실측+main 병합 완료) | 월범위 검증 + 부분인덱스 사용 확인 — EXPLAIN으로 Bitmap Index Scan 실사용 확인 |
| 6 | ✅ 실장 KPI·예약 통계(2026-08-26 `session-work`에서 구현+실측 완료, **이 세션에서 main 병합**) — 표+차트(D21)+담당실장축 | 빈구간 0 채움 확인 — **전건 실측 완료**(빈 주·무실적 실장·비활성 실장 전부 0/제외 확인, 수동 계산치와 API 응답 전부 일치) |
| 7 | ✅ 계정 관리·감사 로그(2026-08-26 `phase7-users-audit` 워크트리에서 구현+실측+미작업분 점검 **전건 완료**, **main 미병합**) | **3역할(Admin·HospitalManager·Consultant) 전부** 실제 쓰기 행위가 `audit_logs`에 정확히 기록됨을 curl+동시성 20건(200/400/500 전 상태코드 정확 기록)+브라우저 실측(JS 레벨 confirm() 오버라이드로 실제 클릭 이벤트 경로 확인 — 확인 시 PATCH 실행·화면 반영, 취소 시 PATCH 자체가 안 나가고 화면 불변까지 둘 다 확인)으로 전부 통과. RouteMap 등록 액션(notes/status/consultants create) 정확 분류 확인. 16장 체크리스트 재대조로 정지 확인 UI 누락·500 시 감사로그 유실 2건 발견·수정. **부가 발견(임의 수정 안 함)**: `PATCH /{id}/consultant`(실장 배정)가 design.md 14-1절 RouteMap 표 자체에 없어 일반 `update`로 뭉뚱그려 기록됨 — 세분화 필요 여부는 사용자 판단 필요 |
| 8 | 유입 경로 분석 | 비어드민 접근 차단 실측 |
| 9 | SEO·보안감사·배포 | 라이브 curl 검증 |

## 미결정 (상세: `docs/design.md` 20장)
- [ ] **M12 OG 공유 이미지** — 현재 로고(`favicon.png` 32×32, `logo.svg`)는 소셜 미리보기용으로 부적합(해상도 낮음·SVG 다수 플랫폼 미지원). 1200×630 권장 PNG/JPG 별도 제작 필요, Phase 9
- [ ] **M6 랜딩 히어로·소개 콘텐츠**(4개 언어) — Phase 2는 기능 설명 최소 문구로 대체(마케팅 카피 아님), 실제 콘텐츠는 이후 결정
- [ ] **M2 도메인·Cloudflare 계정** — Phase 9
> 최초 어드민 계정은 **사용자가 DB에 직접 삽입**(시딩 코드 없음). 실장·시술 마스터도 사용자가 관리 화면에서 직접 등록

## 🔴 범위 외 — 재론 금지 (상세: `docs/design.md` 20-1절)
- **법적 검토**(의료광고 심의·유치 등록 등) — 2026-08-25 사용자 확인 완료. 중계·광고 플랫폼이 아니라 예약 기능만 제공하는 도구
- **비밀번호 분실 복구 / 계정 발급 시 초기 비밀번호 전달** — 프로젝트 관리자가 직접 처리, 시스템 기능으로 만들지 않음
- **실장 평균 최초응대 소요시간** — 구현하지 않음
- **시술 마스터 시딩** — 없음, [시술·수술 관리] 메뉴에서 직접 등록
- **개인정보 처리방침 문안·보유기간 / DB 백업 정책** — 범위 외

## 참고 문서
`docs/design.md`(설계 SSOT) · `docs/session-log.md`(세션 아카이브) · `docs/reservation-desk_1.html`(참고 화면 원본) · `scripts/phase3-concurrency/`(동시성 재현 스크립트 3종)
공유 가이드(`C:\Users\jinho\Desktop\WebProject\`): `auth-pattern-reference.md` · `admin-panel-pattern-reference.md` · `web-security-audit-guide.md` · `seo-pattern-reference.md`

## 세션 요약 (오래된 항목은 `docs/session-log.md` 참고)
- **2026-08-26 (21) — Phase 7(계정 관리·감사 로그) 워크트리 구현+실측+미작업분 점검**: `phase7-users-audit` 워크트리(main 대비 격리, 병합은 미진행)에서 구현. 백엔드 — `AdminUsersController`(계정 발급/역할변경/정지, 자기자신 조작 차단 + 역할변경·정지 시 RT 전량폐기), `AdminAuditLogsController`(actorId/entityType/action/from/to/search 필터), 전역 `AuditLogFilter`(14-1절 RouteMap, `AccountStateFilter` 뒤에 등록). 프론트 — `/admin/users`·`/admin/audit-logs` 신규 페이지, 4개 로케일 키 추가(251키 동일성 node로 검증). 격리 포트(`.env`·`docker-compose.override.yml`, 둘 다 gitignored)로 별도 스택 기동해 curl 23건+브라우저 실측 1차 통과 후, **사용자 요청으로 design.md 16장 체크리스트를 전면 재대조해 미작업 2건 추가 발견·수정**: ①정지 액션에 확인 UI 없음 → `confirm()` 추가 ②`AuditLogFilter`가 500 케이스에서 감사 로그 자체를 통째로 유실(위 주의사항 상세) → `ChangeTracker.Clear()` 추가, 20건 동시요청(200×1/400×7/500×12) 전부 정확 기록 재확인. 이후 사용자가 재차 미작업분 확인을 요청해 **남은 미해결 2건도 마저 실측 완료**: ①Consultant 역할 계정으로 예약 배정+상담기록 추가를 실제 실행해 `actorRole:"Consultant"`로 정확히 감사 기록됨을 curl로 확인 ②Browser pane 미표시로 클릭 이벤트가 안 먹히는 문제를, `window.confirm` 오버라이드로 실제 Vue 컴포넌트 클릭 이벤트 경로를 관찰하는 방식으로 우회해 확인·취소 두 경로 모두 실측(취소 시 PATCH 자체가 안 나가고 화면도 안 바뀜까지 확인). 부가로 `PATCH /{id}/consultant`(실장 배정)가 design.md 14-1절 RouteMap 표에 없어 일반 `update`로 뭉뚱그려 기록되는 것도 발견(임의 수정 안 하고 보고만). 공유 가이드 `admin-panel-pattern-reference.md`도 같은 세션에서 별도로 정정 완료(4-8절 신설, 12장 기존 서술의 부정확한 부분 정정). 커밋은 이 브랜치에만, main 병합은 사용자 지시 대기.

