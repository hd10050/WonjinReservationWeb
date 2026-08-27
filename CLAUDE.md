# WonjinReservationWeb 프로젝트
> 루트 규칙 상속: `C:\Users\jinho\Desktop\WebProject\CLAUDE.md` · **설계 SSOT: `docs/design.md`** — 설계 결정이 바뀌면 코드보다 이 문서를 먼저 고칠 것

## 개요
원진성형외과의 **외국인(중화권) 고객 예약·상담 관리 시스템**. 광고로 유입된 고객이 랜딩 폼으로 상담을 신청하면, 병원 실장이 위챗으로 연락해 상담·방문예약을 확정하고 그 과정을 관리자 패널에서 추적·감사·집계한다.
- 흐름: 광고(UTM·추천코드) → 랜딩 폼 제출 → 실장 위챗 연락 → 상담·시술 결정 → 방문예약 확정 → 내원 · 지원 언어 4개: **zh-CN(기본)** · zh-TW · en · ko
- 현재 상태: **Phase 1~9 전부 완료**(2026-08-27). Phase 1~8 main 병합+인프라 실배포(프론트 Cloudflare Workers `wonjinreservationweb.hd1005019.workers.dev` / 백엔드+DB Render `wonjinreservationweb.onrender.com`, 2026-08-26). **Phase 9**(SEO·보안감사) 로컬 검증까지 완료 — SEO·보안감사 1라운드+재감사(신규 9건 전부 수정). 이후 여러 워크트리 세션에 걸쳐 UI/UX·알림·shadcn 컴포넌트·예약 상세 페이지·성능·인증 개선 + 인플루언서 짧은 링크(`/go/{code}`)·주요 목록 화면 페이징 다수 완료(세션요약 (35)~(50), 상세는 `docs/session-log.md`). **2026-08-28 랜딩페이지 전면 재설계 완료**(main 병합) — 단일페이지(히어로+폼)였던 랜딩을 홈/카테고리 11개/시술상세 76개/`/inquiry` 상담폼 4개 라우트로 분리(세션요약 (51) 참고). **남은 건 CSP 도입 결정·웹푸시/Popover 애니메이션 실브라우저 재확인·상담기록이력 모달여부·시술 이미지 6개 누락·중국어 고민문구 출처확인(아래 TODO)·실배포 라이브 curl 검증**

## 기술 스택
| 레이어 | 기술 |
|---|---|
| 프론트 | Nuxt 4 + Vue 3 **Composition API** + Tailwind v4(`@tailwindcss/vite`) + `@nuxtjs/i18n`(`prefix_except_default`, 기본 zh-CN) |
| SEO | `@nuxtjs/sitemap` + `@nuxtjs/robots` |
| 백엔드/DB | ASP.NET Core 10 + EF Core(`EFCore.NamingConventions` 스네이크케이스) / PostgreSQL 16 (스키마 `wonjin`) |
| 알림(2026-08-27) | **WebPush(1.0.13) VAPID 웹푸시**(신규예약→관리자, 브라우저 종료상태도 수신) + **ASP.NET Core 10 SSE**(`TypedResults.ServerSentEvents`, 예약확정→[예약 달력] 조용한 새로고침, `System.Threading.Channels` 인메모리 pub-sub) |
| 인증 | 자체 JWT(AT 15분, 쿠키 `wj_at`) + RT(7일, SHA-256, 쿠키 `wj_rt`) — **소셜 로그인·회원가입 없음** |
| UI | **shadcn-vue**(`shadcn-nuxt`, style `new-york`) + `reka-ui` 프리미티브 + `class-variance-authority`(D19, 구 D11 대체) + 커스텀 `DatePicker`/`TimePicker` + `@internationalized/date`(D23) |
| 팔레트 | **Olive Garden Feast**(D20) — `#606C38`올리브(primary)·`#283618`짙은산림녹(foreground)·`#FEFDF7`크림(background, 2026-08-27 채도 낮춤)·`#DDA15E`탄(secondary)·`#BC6C25`번트오렌지(destructive), OKLCH 변환 후 shadcn CSS 변수에 적용 |
| 시각화 | **vue-chartjs**(`^5.3.x`) + **chart.js**(`^4.5.x`)(D21) — 실장 KPI·예약 통계 표+차트 병행. Canvas 기반이라 SSR 불가, `<ClientOnly>` 필수. Chart.js 요소 등록은 `plugins/chartjs.client.ts` 한 곳에 집중 |
| 언어 버전 고정 | **TypeScript 5.9.3 고정**(devDependency) — 7.x(네이티브 재작성판)는 `@vue/compiler-sfc`의 `ts.sys` 타입 해석과 비호환이라 reka-ui 기반 shadcn 컴포넌트 컴파일이 깨짐(11-7절) |
| 배포 | 프론트 Cloudflare Workers(nitro `cloudflare_module`+`frontend/wrangler.toml`) / 백엔드·DB Render — **2026-08-26 실배포 완료** |
| 로컬 | `docker compose up` — frontend:3700 / api:5200 / postgres:5435 |
| 엑셀 | **xlsx(SheetJS)** — 관리자 엑셀 일괄등록(2026-08-27). 🔴 **npm 레지스트리판(`0.18.x`)은 미패치 취약점 있음 — 반드시 `https://cdn.sheetjs.com/xlsx-<버전>/xlsx-<버전>.tgz`로 설치**(package.json 의존성 값이 URL인 것은 의도된 것) |

## 확정 설계 결정 (2026-08-25 · 상세는 `docs/design.md` 2장)
- **단일 병원 전용** — 전 테이블에 `hospital_id` 없음
- 🔴 **위챗 탑재 취소** — 랜딩엔 헤더·푸터·예약 폼만. QR·아이콘·공식계정 연동 전부 없음 (**재제안 금지**)
- **예약금은 실장 수동 입금 확인만** — PG·웹훅 없음. 통화는 **CNY(기본)/KRW 선택**, 환율 환산 안 함(통계는 통화별 분리, 합산 금지)
- **자동배포 감시 브랜치 = `main`** (2026-08-25 확정)
- **유입 경로 자동 기록**(UTM+추천코드), **인플루언서 전환율은 어드민 전용 메뉴** `/admin/referrals`로 분리. **인플루언서 배포용 짧은 링크 `/go/{code}`**(2026-08-27 신설, `influencer_links` 테이블) — `/admin/referrals` 안 토글 섹션에서 관리, 새 상위 메뉴 아님
- **회원가입 엔드포인트 없음** — 계정 생성 경로는 `POST /api/admin/users` 하나뿐
- **동일 출처 API 프록시 채택** — 화면 깜빡임 금지(SSR 프리로드)를 이행하기 위한 전제
- 🔴 **실장(`consultants`)은 계정(`users`)과 완전히 별개인 독립 테이블 — 1:1 아님, FK 연결 없음**
- 🔴 **실장·시술·계정·상담기록에 DELETE 없음** — 비활성화(`is_active=false`)·정지로만 처리
- **상담 기록은 누적**(`reservation_notes`) — 덮어쓰기 금지, 삭제 불가, 수정은 작성자·어드민만
- **중복 신청 허용**(D15) — 같은 위챗ID로 여러 번 신청해도 막지 않음(광고 랜딩 특성상 실수 중복 제출이 흔함). 예약 정리 수단은 삭제가 아니라 취소+복구(D24, 아래 D17 항목 참고)
- **예약 통계는 주(일~토) 단위**(D16) — `date_trunc('week')`는 월요일 시작이라 하루 밀어 계산
- 🔴 **실장 배정은 수동, 미배정 예약은 배정·조회·취소만 가능**(D17, 2026-08-27 삭제→취소로 정정) — 상담기록·상태전이(취소 제외)·저장 전부 400 차단. 담당 변경은 예외 없이 처리 이력 기록. **예약 삭제 기능 자체가 없다**(2026-08-27, 소프트 삭제 D15 폐지) — 담당자 유무와 무관하게 정리 수단은 취소뿐, 취소는 이력에 남고 **어드민만 복구 가능**(`POST /{id}/restore`). 취소·방문완료는 배정 전과 동일하게 전 구역 잠김(방문완료는 상담기록만 예외)
- **예약 코드 = `YYYYMMDD`+4자리 일별 리셋**(M3, 예 `202608260001`) — `reservation_code_counters` 원자적 증가로 발급
- **모든 시각은 KST 고정** — 브라우저 타임존 사용 금지. `stat_date`·예약코드 날짜도 KST
- 시술명은 언어별 컬럼 4개(`name_zh_cn` 등), **연락 희망 시각은 직접 입력**(`TimePicker`, `time` 컬럼, 한국 시간 기준) — 선택지로 바꾸지 말 것
- **시술 카테고리 마스터 + 시술 필수 소속 + 정렬순서 폐지**(D25, 2026-08-28) — `categories` 테이블(코드 UNIQUE + 언어별 이름 4개 + `is_active`) 신설, `procedures.category_id` NOT NULL(FK RESTRICT). `procedures.sort_order` 삭제 — 카테고리·시술 나열은 전부 **현재 UI 로케일 이름 오름차순**(API `locale` 파라미터). [시술·수술 관리]는 별도 메뉴 없이 **한 화면 탭 2개**(카테고리 관리 / 시술·수술 관리), 둘 다 엑셀 일괄등록(시술 엑셀은 소속을 카테고리 코드로 지정). 카테고리도 삭제 없음 — `is_active=false`만. 예약 상세 시술 선택은 **카테고리별 아코디언**(기본 접힘, 선택된 시술이 든 카테고리만 펼침, 시술 없거나 비활성인 카테고리는 숨김 — 단 이미 선택된 시술이 든 카테고리는 유지)
- **중화권 브랜드 표기 = `WonJin`**(D18, 2026-08-26) — 4개 로케일 전부 이 토큰 그대로 사용(번역 안 함). title 접미사·og:site_name·JSON-LD name 전부 통일
- **UI 라이브러리 = shadcn-vue**(D19, 2026-08-26) — 구 D11(라이브러리 미도입) 대체, Context7로 최신 설치 문서 확인 후 도입. `components.json`은 CLI 자동생성 대신 수동 작성(11-7절)
- **팔레트 = Olive Garden Feast**(D20, 2026-08-26) — 참고 화면(`reservation-desk_1.html`)의 청록색 팔레트를 대체. coolors.co/palettes/trending을 playwright-cli로 실측 검증해 이름·좋아요 수 확인된 팔레트만 채택(발명 절대 금지 — 실제 발생했던 사고). 🔴 **`--background`만 2026-08-27 사용자 피드백("탁하다")으로 채도 낮춤**(`#FEFAE0`→`#FEFDF7`, OKLCH 계산으로 같은 색조·명도만 조정 — 새 색 발명 아님) — 나머지 4색은 원본 그대로
- **병원 정식 정보 확정**(M8, 2026-08-26) — 상호 `원진성형외과의원`·사업자번호 `824-67-00414`·주소는 화면 푸터에 원문 그대로 표기(고유명사 번역 안 함). 🔴 **대표전화는 화면에 노출하지 않고 JSON-LD에만 포함**(예약 폼 유도 우선, 사용자 결정) — 상세는 design.md 12-1-1절
- **실장 KPI·예약 통계 = 표+차트 병행**(D21, 2026-08-26) — 차트는 `vue-chartjs`+`chart.js`, 색상은 새로 만들지 않고 D20 팔레트 재사용. Canvas는 SSR 불가라 `<ClientOnly>`로 감싸고(화면 깜빡임 금지 원칙은 데이터 프리로드 대상이라 위반 아님), 레이아웃 시프트 방지로 고정 높이 컨테이너 사용
- 🔴 **푸터 주소는 로케일별로 다른 문구**(D22, 2026-08-26) — "고유명사 번역 안 함" 원칙의 예외. 상호·사업자번호는 여전히 원문 고정, 주소만 ko=등록원문/zh-CN=제공된 간체/zh-TW·en=영문. JSON-LD도 영문 주소로 동기화(상세: design.md 12-1-1·D22)
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
- **랜딩 관련 원칙 2가지**: ①`/api/auth/me` 호출 금지 — 인증 초기화는 `/admin` 경로에서만, 광고 트래픽만큼 401이 백엔드로 감 ②방문 기록(`landing-visit`)은 내부 시크릿 헤더 필수 — 공개 엔드포인트로 열면 전환율 조작 가능, `await` 하지 말 것(랜딩 지연)
- **`ix_refresh_tokens_token_hash` 인덱스 필수** — 모든 세션이 12분마다 이 컬럼을 조회. 없으면 갱신마다 풀스캔이며 UI 지연이 없어 발견이 매우 늦어짐(타 프로젝트 실제 사고)
- **`landing_daily_stats`의 키 컬럼은 전부 `NOT NULL DEFAULT ''`** — PG UNIQUE는 NULL을 서로 다르게 취급(NULLS DISTINCT)해 NULL 허용 시 같은 조합이 무한 중복됨
- **언어 감지 리다이렉트가 `location.search`를 보존해야 함** — 빠뜨리면 UTM이 통째로 유실돼 유입 경로 추적이 죽음
- **`refresh`에 로그인용 rate limit 정책 재사용 금지** — 12분 간격 자동갱신이 한도를 잠식해 로그인까지 429로 막힘. 사용자 ID 파티션 전용 정책을 별도로 둘 것
- **EF Core 마이그레이션 원칙 2가지**: ①`__EFMigrationsHistory` 스키마 명시 고정 필수 — `MigrationsHistoryTable("__EFMigrationsHistory", "wonjin")`, 미지정 시 `search_path` 규칙으로 위치가 달라져 마이그레이션 재실행 → `relation already exists` 재시작 루프 ②`dotnet ef migrations add` 결과 파일은 적용 전 반드시 직접 읽을 것 — scaffolder가 새 부분/복합 인덱스를 이유로 기존 인덱스에 `DropIndex`를 자동 삽입하는 사고가 있었음
- **`AccountStateFilter`는 `[Authorize]` 요구 여부를 먼저 확인** — 안 하면 정지된 유저가 공개 API에서도 401을 맞아 익명 방문자보다 못한 상태가 됨
- 🔴 **`AuditLogFilter`의 상태코드 기록 — `next()` 직후 `HttpContext.Response.StatusCode`를 읽으면 항상 200으로 오기록된다**(design.md 14장·admin-panel-pattern-reference.md 4-2절 원문 패턴이 실제로 이 버그를 가짐, Phase 7 curl 실측으로 발견). 액션 필터의 `next()`는 액션이 `IActionResult`를 반환하는 시점까지만 감싸고, 그 결과를 실제로 응답에 쓰는 결과 실행은 액션 필터 파이프라인 바깥의 더 뒤 단계에서 일어나 — `BadRequest()` 등을 반환해도 이 시점의 `Response.StatusCode`는 여전히 기본값(200)이다. 컨트롤러 예외(`ActionExecutedContext.Exception`)는 별개로 여전히 확인해야 하지만, **정상 반환된 400/404/401 등은 `(executed.Result as Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult)?.StatusCode`로 읽어야 정확하다**(`AuditLogFilter.cs` 실제 구현 참고). 🔴 **두 번째 함정(500 동시성 실측으로 발견): 컨트롤러의 `SaveChangesAsync`가 실패(500)하면 같은 요청 스코프 `DbContext`의 ChangeTracker에 실패 엔티티가 그대로 남아, 필터가 이어서 `AuditLog`를 추가·저장하려 하면 같은 예외가 재발해 catch에 삼켜지고 감사 로그 자체가 통째로 유실된다**(20건 동시 요청 중 500 12건이 로그에 0건 기록되는 것을 실측). `AuditLog` 추가 직전 `db.ChangeTracker.Clear()` 필수(수정 후 500 12건 전부 정확히 기록되는 것 재확인)
- **관리자 로그인 진입점(푸터 저작권 링크) 숨김은 보안이 아님** — 실제 보호는 인증과 `[Authorize]`
- `package-lock.json`은 **npm@10.9.2**로 생성 (npm 11은 Cloudflare CI `EBADPLATFORM`)
- 로케일 JSON 값에 **순수 `@` 문자 금지** — vue-i18n이 linked message로 오인해 해당 로케일 컴파일이 깨짐(클라이언트 라우팅에서만 raw key 노출)
- **🔴 미배정 예약은 배정·취소·조회만 허용**(D17, 10-1절, 2026-08-27 이전엔 "소프트삭제"였으나 D24로 삭제 폐지), 나머지 전부 400 `RESERVATION_NOT_ASSIGNED` — 상담기록 추가·상태전이(취소 제외)·방문일시/시술/예약금 저장 전부 차단. 배정 검사를 상태전이 UPDATE의 WHERE에 함께 넣을 것(따로 조회하면 그 사이 배정 해제된 경우를 놓침). 담당자 변경은 예외 없이 처리 이력 기록
- **로그인 rate limit을 순수 IP로 두지 말 것** — 단일 병원이라 직원 전원이 같은 사무실 IP를 공유, IP 기준이면 출근시간 정상 로그인이 429로 막힘. 이메일+IP 조합 파티션 사용(7-2절)
- **통계 쿼리 `GroupBy(...).Select(g => new Dto(...))` 금지** — EF Core가 SQL로 못 옮겨 런타임 예외. 익명 타입으로 먼저 집계 후 메모리에서 DTO 매핑하는 2단계로(11-6절)
- **`RouteMap`은 세그먼트 개수 내림차순 정렬 필수** — `/notes`·`/status`가 상위 경로와 세그먼트 수 차이로 동시 매치됨. 정렬 없으면 상담기록 추가가 "예약 수정"으로 오분류(14-1절)
- **`detectBrowserLanguage`는 반드시 `false`** — 켜두면 카카오톡·라인 등 링크 미리보기 봇이 언어감지 리다이렉트를 따라가 엉뚱한 언어의 og:description 노출(5-2절)
- **robots `disallow`는 트레일링 슬래시 필수**(`/admin/`) — 슬래시 없이 쓰면 prefix 매칭으로 다른 경로까지 막히고 sitemap exclude에도 적용돼 동적 URL이 원인불명으로 누락(5-5절)
- **검색 입력을 반응형 쿼리에 직접 바인딩 금지** — 매 키입력마다 API 재호출됨. URL 쿼리를 `computed`로 감싸 제출 시에만 반응하게 할 것(12-4절)
- **shadcn-vue CLI 함정 2가지**(11-7절): ①컴포넌트 추가 시 `app/lib/utils.ts`가 자동생성 안 됨 — `components.json` 수동 작성 때문에 CLI가 `cn()` 헬퍼를 안 만듦, 컴포넌트 추가 전 직접 작성 필요 ②`add` 실행 전 `.nuxt/` 존재 확인 필수 — 없으면 `resolvedPaths: Required...`로 즉시 실패, `npx nuxi prepare` 먼저 실행
- 🔴 **reka-ui 기반 컴포넌트의 `interface Props extends PrimitiveProps` Vite 에러는 `/* @vue-ignore */`로 우회하지 말 것 — 정정.** 근본 원인은 `typescript@7.x`(네이티브판)와 `@vue/compiler-sfc`의 `ts.sys` 비호환. **해결은 `typescript`를 5.x로 다운그레이드하는 것**(위 기술 스택 표). `/* @vue-ignore */`를 쓰면 `as` prop의 `withDefaults` 기본값이 인스턴스에 반영 안 돼 `<button>`이 `<div>`로 렌더링되고 `type="submit"` 폼 제출이 조용히 깨진다 — 실측으로 확인된 실제 장애(11-7절, "실사용에 영향 없음"이라던 Phase 0 기록은 오판이었음)
- **shadcn CLI의 tsconfig alias 인식 실패를 `tsconfig.json` 수정으로 고치지 말 것** — Nuxt의 실제 타입 해석(`.nuxt/tsconfig.*.json` 참조)이 깨짐. `components.json`을 손으로 작성해 우회할 것(11-7절)
- **`app.vue`에 `<NuxtPage />`가 없으면 `pages/` 디렉토리를 아무리 만들어도 전부 404** — `pages/`를 처음 쓰는 순간부터 `app.vue`는 `<NuxtLayout><NuxtPage /></NuxtLayout>` 셸이어야 함. 기존 콘텐츠는 `pages/index.vue`로 이동
- 🔴 **Nuxt 동적 라우트 파일과 동일 이름의 디렉터리가 공존하면 안 됨** — `pages/procedures/[category].vue`(파일)와 `pages/procedures/[category]/`(하위 라우트를 담은 동명 디렉터리)가 같이 있으면 Nuxt가 이를 부모-자식 중첩 라우트로 자동 인식해(NUXT_E4016) 부모가 `<NuxtPage/>`를 렌더링하지 않는 한 자식 라우트가 전혀 마운트되지 않는다(랜딩 재설계 세션에서 시술 상세 페이지가 전혀 안 열리는 실제 장애로 발견, 세션요약 (51)). 형제 라우트로 두 계층을 독립시키려면 파일을 `pages/procedures/[category]/index.vue`로 옮길 것(Nuxt 공식 권장 패턴 — route 이름은 `index` 접미사가 자동 생략돼 기존 `localePath({name:...})` 참조가 그대로 유지된다). 새 동적 라우트를 하위 depth로 확장할 때마다 이 충돌 여부를 먼저 확인할 것
- 🔴 **C# record의 검증 애노테이션은 `[property: ...]`가 아니라 파라미터에 직접 붙일 것** — `[property: Required]`는 컴파일은 통과하지만 그 record를 실제로 모델 바인딩하는 첫 요청에서 500(`InvalidOperationException`)을 던짐. `[Required] string Email`처럼 타겟 지정자 없이 쓸 것(11-8절)
- **`useOpsLocale()`은 `locale.value = code` 직접 대입 금지, 반드시 `await setLocale(code)`** — 직접 대입은 lazy 로케일 메시지 로드를 트리거 안 해 `t()`가 raw key를 반환함. 호출부(로그인 페이지·admin 레이아웃)도 `await useOpsLocale()`로 SSR 완료를 기다릴 것(5-4절)
- **`refresh` rate limit 파티션은 사용자 ID가 아니라 RT 쿠키 해시로 구현**(설계 원문과 의도적 편차, 7-2절) — DB 조회 없이 동기 콜백에서 즉시 얻을 수 있어 세션 단위로 더 세밀하게 격리됨
- 🔴 **`db.Database.SqlQuery<T>(...)` 함정 2가지**: ①바로 `.SingleAsync()`를 걸지 말 것 — `INSERT...RETURNING`처럼 non-composable SQL을 서브쿼리로 감싸려다 `InvalidOperationException`(실측, 8-11절 코드카운터). `.ToListAsync()`로 먼저 구체화 후 메모리에서 `.Single()` ②다중 컬럼 매핑은 `UseSnakeCaseNamingConvention()` 영향을 그대로 받음 — 결과 타입 프로퍼티가 `WeekStart`면 SQL 별칭도 `week_start`여야 함, PascalCase 따옴표 별칭(`AS "WeekStart"`)은 "required column not present" 예외(실측, 11-4절). 스칼라(`SqlQuery<int>`)는 컬럼명 매칭이 없어 이 규칙이 안 보일 뿐, 다중 컬럼엔 항상 적용됨
- 🔴 **`[ApiController]`의 자동 검증 응답 2가지 함정**: ①`[FromQuery] DateOnly` 등 비-nullable 값 타입은 파라미터 누락 시 400이 아니라 `default`(예: `0001-01-01`)로 조용히 바인딩된다(자동 400은 문자열류에만 해당, 실측 확인 — 문서만으로 400을 단정하지 말 것) → 필수 쿼리 파라미터는 컨트롤러에서 `== default` 명시적으로 검사할 것. ②DTO에 `[Range]`·`[Required]`를 걸면 위반 시 액션 진입 전에 자동 400이 나가는데, 이 응답은 이 프로젝트 공용 `{code:"..."}`가 아니라 기본 `ValidationProblemDetails`라 프론트가 `UNKNOWN`으로 표시한다(실사용 버그로 발견, 2026-08-27) → 구체적 안내가 필요한 필드는 애노테이션 대신 컨트롤러 수동 검증+`BadRequest(new{code=...})`로 직접 반환할 것(`UpdateReservationRequest.DepositAmount` 참고)
- **Vue 템플릿에서 `{{ prefix }}<A>{{ link }}</A>{{ suffix }}` 사이에 줄바꿈을 넣으면 공백이 하나씩 끼어든다** — 한국어처럼 조사가 바로 붙어야 하는 언어에서 실제로 어색해짐(실측 확인). 4언어 문구를 한 태그에 이어붙일 땐 줄바꿈 없이 한 줄로 쓰고, 필요한 공백은 번역 문자열 자체에 포함시킬 것
- **`@nuxtjs/i18n`의 `locales[].code`가 대문자를 포함하면(`zh-TW`) URL prefix도 그 대소문자 그대로 생성된다**(`/zh-TW`) — 대소문자 URL 둘 다 정상 라우팅되어 기능 문제는 없으나, `code`를 DB `locale` 값과 일치시켜야 하므로 지금은 그대로 둠. SEO 정규화 필요해지면(Phase 9) 재검토
- **honeypot 필드가 채워진 요청은 400이 아니라 200으로 조용히 흘려보내고 DB에 저장하지 않을 것** — 실패 응답을 주면 봇이 실패 패턴을 학습해 우회를 시도할 여지를 준다(11-1절)
- 🔴 **한 요청 안에서 `ExecuteUpdateAsync`/`ExecuteDeleteAsync`를 여러 번 쓰거나 그 뒤에 별도 `SaveChangesAsync`로 로그를 남긴다면 반드시 `BeginTransactionAsync()`로 묶을 것** — 묶지 않으면 앞 단계들은 각각 즉시 커밋되고 마지막 단계만 실패해도 "응답은 500인데 일부 변경은 이미 반영된" 부분 커밋이 된다(실측 확인 — `UpdateReservation`에 존재하지 않는 `procedureId`를 보내면 스칼라 필드·자동 Confirmed 전이는 저장되고 시술 목록만 삭제된 채 로그 없이 500이 났음). `audit_logs`처럼 "실패해도 본 작업을 막으면 안 되는" 부가 기록만 트랜잭션 밖에서 별도 try/catch로 베스트에포트 처리
- 🔴 **Npgsql에 `timestamptz` 비교용 `DateTimeOffset`을 넘길 때 Offset은 반드시 0(UTC)이어야 함** — `TimeZoneInfo.ConvertTime(...)`으로 만든 KST(+09:00) 오프셋 `DateTimeOffset`을 쿼리 파라미터로 그대로 쓰면 `Cannot write DateTimeOffset with Offset=09:00:00 ... only offset 0 (UTC) is supported` 500(실측 확인). KST로 년/월만 뽑고 나면 반드시 `.ToUniversalTime()`을 거쳐서 쿼리에 넘길 것
- 🔴 **`audit_logs`는 컨트롤러에서 직접 쓰지 말 것 — 전역 `AuditLogFilter`(Phase 7) 전용**(14장, `AuditLog.cs`·`Program.cs` 주석과 동일). Phase 3에서 `SoftDelete`에만 예외적으로 `db.AuditLogs.Add(...)`를 넣었던 것이 14장 원문 위반이자 6개 쓰기 액션 중 하나만 특별 대우하는 비일관 상태였음 — **Phase 4에서 제거 완료**(`reservation_logs` 삭제 기록·204 응답은 그대로 유지, curl로 `audit_logs` 행 수 불변 확인). Phase 7 전까지는 6개 쓰기 액션 전부 audit_logs 공백으로 일관됨(의도된 임시 상태)
- **다중 role 컨트롤러에 쓰기 액션을 추가할 때는 액션 레벨 `[Authorize]`로 다시 좁힐 것(6-3절 원칙 1, Phase 4 실측)** — `AdminConsultantsController`·`AdminProceduresController`는 GET을 Consultant도 써야 해서(실장 재배정·시술선택 드롭다운) 클래스 레벨을 `Admin,HospitalManager,Consultant`로 열어뒀다. 여기에 POST/PUT을 그냥 추가하면 11-3절 "HospitalManager 이상" 요구와 달리 Consultant도 쓰기가 가능해진다 — 액션마다 `[Authorize(Roles="Admin,HospitalManager")]`를 다시 걸어야 한다. 실제 로그인으로 Admin·HospitalManager 200 / Consultant 403 / 익명 401 전부 실측 확인(Phase 5·6에서 같은 패턴의 컨트롤러를 열 때도 동일 점검 필요)
- **배포 함정 3가지**(2026-08-26 실배포로 확인): ①Render Internal/External Database URL은 `postgresql://user:pass@host/db` 형식인데 Npgsql `ConnectionStrings__DefaultConnection`은 `Host=...;Port=...;Database=...;Username=...;Password=...` 키=값 형식이 필요해 직접 변환해야 함 ②Cloudflare Workers 배포는 `nuxt.config.ts`에 `nitro.preset: 'cloudflare_module'` 필수 — 없으면 `wrangler.toml`의 `main`이 가리키는 `.output/server/index.mjs` 자체가 안 만들어짐(`[vars]`는 평문 커밋되므로 진짜 시크릿은 `npx wrangler secret put`으로 별도 등록) ③Git 연동 Builds의 `npm ci`는 `package-lock.json`이 조금만 어긋나도 즉시 실패(eslint 계열 의존 트리 변경만으로도 stale해질 수 있음) — `npx npm@10.9.2 install`로 재생성(npm 버전 고정 원칙과 동일 이유) 후 커밋 필수
- 🔴 **`CF-Connecting-IP`는 "프론트(Cloudflare Workers) 뒤"에서만 위조 불가능** — 백엔드(Render)는 Cloudflare 엣지 뒤가 아니라서, 이 헤더를 무조건 신뢰하면 동일 출처 프록시를 건너뛰고 백엔드를 직접 호출하며 헤더를 조작해 Rate Limit을 무제한 우회할 수 있다(보안감사 2026-08-26 발견). 프론트·백엔드만 아는 내부시크릿(`X-Internal-Secret`)이 유효할 때만 신뢰하고, 아니면 실제 TCP 연결 IP로 폴백(`Program.cs GetClientIp`, `AuditLogFilter`)
- **"존재확인만 하는 별도 SELECT" 뒤에 INSERT하는 패턴은 동시 쓰기와 레이스한다** — `AddNote`가 배정여부를 트랜잭션 밖 SELECT로만 확인하던 것을, 조건부 `ExecuteUpdateAsync`(행 lock 효과)로 바꿔 직렬화되게 수정(보안감사 2026-08-26, 당시 경합 대상은 소프트삭제였으나 2026-08-27 D24로 삭제가 폐지돼 지금은 취소(Cancelled)가 그 자리를 대신함 — 패턴 자체는 그대로 유효). 새 쓰기 액션을 추가할 때 이 컨트롤러의 다른 액션들(원자적 WHERE 패턴)과 다른 방식을 쓰고 있지 않은지 확인할 것. RT 1회용 로테이션 환경에서 SSR의 여러 데이터 페칭 지점이 각자 독립 refresh하면 안 되는 함정도 같은 감사에서 발견해 `useState` 요청스코프 캐시(`ssrRefreshCookie`, `useAuth.ts`)로 해결 완료(세션요약 (27) 참고)
- 🔴 **`nuxt.config.ts`의 `i18n`에 `baseUrl` 없으면 hreflang이 상대경로로 생성됨**(Phase 9 SEO 실측 발견, 2026-08-27) — "I18n baseUrl is required to generate valid SEO tag links" 경고를 무시하지 말 것. `useLocaleHead({seo:true})`가 만드는 `<link rel="alternate">`가 `href="/ko"` 같은 상대경로가 되어 SEO 표준(절대 URL 요구) 위반이었음. `baseUrl: process.env.NUXT_PUBLIC_SITE_URL`로 해결
- 🔴 **`nuxt.config.ts`의 `robots.sitemap` 필드를 직접 지정하지 말 것** — `@nuxtjs/sitemap`이 설치돼 있으면 `sitemap_index.xml`을 robots.txt에 이미 자동 등록한다(로컬 `?mockProductionEnv`로 실측 확인). 직접 `sitemap.xml`(메타리프레시 HTML일 뿐 실제 sitemap 아님)을 추가하면 robots.txt에 잘못된 sitemap이 중복 등록된다(5-5절 경고 사례)
- **로컬 dev docker 컨테이너는 소스 바인드마운트가 없음**(`docker-compose.yml`, `build:`만 지정) — 코드 수정 후 반드시 `docker compose up -d --build`로 재빌드해야 반영됨. `restart`만으로는 이미지가 그대로라 안 바뀜(2026-08-27 세션 중 여러 차례 재확인)
- **PostgreSQL 시스템 컬럼(`xmin` 등)을 EF Core `IsRowVersion()`으로 매핑하면 마이그레이션 파일엔 `AddColumn`이 생성되지만 실제 적용 시 DDL이 안 나가는 게 정상** — Npgsql이 시스템 컬럼명을 인식해 건너뛰도록 설계됨(`AssignConsultant` 동시성에 이 패턴 도입, 2026-08-27)
- 🔴 **새 SSR 직접 백엔드 호출(프록시 안 거치는 경로)엔 반드시 `X-Internal-Secret` 헤더를 실을 것** — CSRF 미들웨어가 Origin 없는 요청을 이 시크릿으로만 통과시킴(재감사 2026-08-27 강화). **새 관리자 쓰기 API 추가 시 `[EnableRateLimiting("admin-write")]`도 함께 걸 것** — RouteMap 등록과 별개라 놓치기 쉬움
- 🔴 **정정(2026-08-27): reka-ui `TimeFieldRoot`의 `hour-cycle`은 반드시 숫자 `:hour-cycle="24"`로 쓸 것 — 문자열(`hour-cycle="h23"`) 금지.** 과거 기록("문자열이 맞다")은 `aria-valuemax=23`만 보고 내린 오판이었음 — `TimeFieldInput`이 애초에 값을 렌더링하지 않던 시절이라 화면 텍스트 자체를 확인할 방법이 없었다. 이번에 화면 렌더링을 고치고 나서 자정(0시)이 "12"로 보이는 걸 발견해 `node_modules/reka-ui/src/shared/date/utils.ts`의 `normalizeHourCycle()`을 직접 읽어 확정: 이 함수는 `hourCycle === 24`/`=== 12`(숫자 엄격비교)만 인식하고, **문자열은 어떤 값을 넘겨도 전부 `undefined`로 떨어져 로케일 기본 hourCycle로 조용히 폴백된다**(ko-KR 기본은 h12라 자정이 "12"로 표시됨). 숫자로 바꾼 뒤 화면 텍스트(`textContent`, **`aria-valuenow`는 신뢰 금지** — `hourSegmentAttrs`가 `segmentValues.hour ? ... : placeholder`로 falsy-zero 버그가 있어 자정일 때 표시가 부정확함, 값 자체는 정상 반영되니 접근성에만 영향)로 재실측: 자정 "0"·13시 "13" 정확히 24시간제 확인, 부모 `v-model` 값도 `"00:00"` 정확
- 🔴 **전역 로딩/전환 상태를 직접 만들 땐 `page:start`/`page:finish`(Suspense pending/resolve) 대신 `useLoadingIndicator()`(`page:loading:*`)를 쓸 것**(2026-08-27) — 전자는 전환이 다른 전환에 가로채이면 카운터가 불균형해져 영영 안 걷힘(`RouteOverlay` 실제 재현). 후자는 `router.afterEach`의 취소·중복 실패도 별도로 커버해 프레임워크가 이 문제를 이미 해결해둠
- 🔴 **Tailwind Preflight가 전 요소 margin을 0으로 리셋해 네이티브 `<dialog>`의 중앙 정렬이 깨진다**(2026-08-27) — `showModal()`로 띄우는 `<dialog>`의 중앙 배치는 브라우저 기본 스타일시트의 `margin:auto`가 담당하는데, author 스타일시트(Preflight)가 항상 UA 기본값을 이겨 좌측 상단에 붙어버린다. `<dialog>`를 새로 쓸 때마다 `m-auto`를 함께 넣을 것.
## 절대 원칙 이행 (루트 CLAUDE.md)
- **화면 깜빡임 금지** — 데이터 페이지는 `<script setup>` 최상위 `await useApi(...)` SSR 프리로드. `onMounted`+client fetch 금지. 전환 오버레이는 `<Transition>` 금지, 항상 마운트 + `pointer-events`를 상태값에 직접 클래스 바인딩
- **입력 길이 3곳 일치** — DB `varchar(N)` / 백엔드 `[MaxLength(N)]` / 프론트 `maxlength` 항상 세트로 수정. 전체 표는 `docs/design.md` 9장
- **디자인 원칙** — 모든 input/textarea/select에 **보이는 label** 필수(검색창 포함). placeholder로 대체 금지. honeypot만 예외
- **DB 성능** — 새 쿼리마다 ①필터·정렬 컬럼 인덱스 ②목록 페이징 ③불필요 컬럼·관계 미조회 3가지 자체 점검
- **번역** — 4개 로케일 JSON의 키 집합이 항상 완전히 동일. 키 추가·삭제는 4파일 세트로 수정 후 개수 대조

## TODO
### 다음 세션 최우선
- [ ] **프론트 Content-Security-Policy 미적용 — 의도적 보류**(보안감사 재감사 2026-08-27) — X-Content-Type-Options 등 4개 헤더는 적용 완료했으나 CSP만 보류. `landing.vue`의 JSON-LD 인라인 스크립트가 예약마다 내용이 달라 정적 해시 지정이 안 통하고, nonce 방식은 Nuxt 통합이 더 큰 작업이라 섣불리 걸면 스크립트가 깨질 위험 — nonce 도입 여부 결정 필요
- [ ] 🔴 **실브라우저 최종 확인 필요 2건**(자동화 도구 환경 제약으로 자동검증 불가 — 19-2절 자동화 도구 특이사항과 동일 범주): ①**관리자 웹 푸시**(2026-08-27) — Service Worker 등록·`Notification.permission`이 원천 차단돼 파이프라인만 간접 확인(공개키·SSRF화이트리스트·구독저장·발송시도·활성계정필터), 테스트 계정 `verify-push@wonjin.local`로 `/admin`→종 아이콘→알림허용→새 예약 접수 재확인 권장 ②**Popover(DatePicker) 닫힘 애니메이션**(2026-08-27) — `document.hidden=true`에서 `opacity`/`pointer-events` 미해제 관측(`data-state`는 정상 전환), 코드는 순정 shadcn 생성 그대로라 실사용자 환경 재현 여부만 확인하면 됨
- [ ] **상담기록 "수정 이력"을 진짜 모달(오버레이)로 바꿀지 확인 필요**(세션요약 (40)) — 사용자가 "모달"로 요청했으나 코드베이스에 모달 컴포넌트가 없어 기존 토글형 Card 패턴 재사용한 인라인 패널로 구현. 기능은 동일(과거 내용 열람), 위치만 다름 — 그대로 둘지 진짜 오버레이로 바꿀지 확인 필요
- [ ] **랜딩 시술 이미지 6개 누락**(세션요약 (51)) — `eye`(asymmetrical-eye-correction, congenital-ptosis-children) · `nose`(bulbous-nose, tip-plasty) · `breast`(nipple-surgery) · `bodyline`(hip-augmentation) 6개 항목이 4개언어 설명·고민 콘텐츠는 있는데 `frontend/public/img/`에 이미지 파일이 없음. design.md 10절이 이미 이 6개를 "사진 재확인 필요"로 경고해뒀던 것과 정확히 일치 — 병원에 사진 수급 여부 확인 필요(코드 수정 대상 아님)
- [ ] **랜딩 중국어(zh-CN/zh-TW) 고민 불릿 39/96건이 설계문서 부록 원문과 다른 요약형 문구**(세션요약 (51)) — ko/en은 부록과 100% verbatim인데 zh만 완전한 문장이 아니라 키워드형으로 압축돼 들어감(예: `breast/augmentation`). 의료광고 문구라 "원문 그대로" 원칙이 특히 중요한 영역 — 출처 확인 또는 병원 검수 필요
- [ ] **랜딩 재설계는 실제 백엔드 왕복(폼 제출→DB 저장) 미검증**(세션요약 (51)) — 공유 dev DB(다른 세션이 쓸 수 있음)에 테스트 데이터를 남기지 않으려 의도적으로 보류. UTM 쿠키 왕복·제출 payload 구성 코드는 확인 완료. 다음 세션에서 격리 docker 스택으로 실제 상담 신청 1건 제출→DB 반영 확인 권장
### Phase 계획 (design.md 19장 = 상세 SSOT, 완료기준·재현 스크립트 전부 그쪽에 있음 — 이 표는 요약만)
Phase 0~8(스캐폴딩·인증·랜딩+예약폼·예약대시보드/상세/상담기록/상태머신·실장시술CRUD·예약달력·KPI+통계·계정관리+감사로그·유입경로분석) **전부 완료+전건 실측+main 병합 완료**(2026-08-26). Phase 9(SEO·보안감사·배포)는 인프라 실배포(Cloudflare Workers+Render)·보안감사 1라운드+재감사(Low 9건 전부 수정)·SEO 로컬검증까지 완료 — **CSP 도입 결정·실배포 도메인 라이브 curl 검증만 미착수**(위 TODO 참고).
## 미결정 (상세: `docs/design.md` 20장)
- [ ] **M6 랜딩 히어로·소개 콘텐츠**(4개 언어) — Phase 2는 기능 설명 최소 문구로 대체(마케팅 카피 아님), 실제 콘텐츠는 이후 결정
- [ ] **M2 도메인·Cloudflare 계정** — Phase 9(참고: 최초 어드민 계정은 **사용자가 DB에 직접 삽입**·시딩 코드 없음, 실장·시술 마스터도 관리 화면에서 직접 등록)

## 🔴 범위 외 — 재론 금지 (상세: `docs/design.md` 20-1절)
- **법적 검토**(의료광고 심의·유치 등록 등) — 2026-08-25 사용자 확인 완료. 중계·광고 플랫폼이 아니라 예약 기능만 제공하는 도구
- **비밀번호 분실 복구 / 계정 발급 시 초기 비밀번호 전달** — 프로젝트 관리자가 직접 처리, 시스템 기능으로 만들지 않음
- **실장 평균 최초응대 소요시간** — 구현하지 않음
- **시술 마스터 시딩** — 없음, [시술·수술 관리] 메뉴에서 직접 등록
- **개인정보 처리방침 법률 검토 거친 최종본·정확한 보유기간 수치 / DB 백업 정책** — 범위 외(예문은 2026-08-27 작성 완료, 20-1절)

## 참고 문서
`docs/design.md`(설계 SSOT) · `docs/session-log.md`(세션 아카이브) · `docs/reservation-desk_1.html`(참고 화면 원본) · `scripts/phase3-concurrency/`(동시성 재현 스크립트 3종) · 공유 가이드(`C:\Users\jinho\Desktop\WebProject\`): `auth-pattern-reference.md` · `admin-panel-pattern-reference.md` · `web-security-audit-guide.md` · `seo-pattern-reference.md` · `excel-bulk-upload-pattern-reference.md`

## 세션 요약 (오래된 항목은 `docs/session-log.md` 참고, (52)까지 이동 완료)
- **2026-08-28 (53) — 상담폼 연락희망 날짜 추가 + 홈 슬로건 제거 + 로그인 랜딩헤더 + 예약상세 라디오 shadcn화, main 직접 작업**: ①`reservations.preferred_contact_date`(`date` NULL — 라이브 기존행 호환, `AddPreferredContactDate` 마이그레이션) 신설, `/inquiry` 폼에 `DatePicker` 추가(신규 제출 필수, 누락 시 백엔드 400 `INVALID_CONTACT_DATE`), 예약 상세에 "연락 희망 일시: 날짜 시각" 표시(레거시 NULL이면 시각만). ②홈 슬로건 `landing.home.heroSubtitle`("정밀한 진단과…") 4개 로케일 + `index.vue` `<p>`·`useSeo.description`에서 제거. ③랜딩 헤더를 `components/LandingHeader.vue`로 추출(`@select-locale` 이벤트만 발신) → `layouts/landing.vue`와 `/admin/login`이 공유. 로그인은 `i18n:false`라 헤더 언어선택이 `setLocale`+`wj_lang` 쿠키로 동작(랜딩은 `switchLocalePath` 이동), 로그인 카드 내 기존 언어 select 제거. ④예약 상세 예약금 3상태 라디오를 raw `<input type=radio>`→shadcn `RadioGroup`/`RadioGroupItem`(신규 `components/ui/radio-group/`, reka-ui 기반). 재클릭 시 미확인으로 되돌리는 토글은 `label @click.capture.stop.prevent`로 reka 내부 핸들러보다 먼저 가로채 처리, 화살표키만 reka `@update:model-value`로. i18n 4파일 398키 동일(`preferredContactTime` 값 "시각"→"일시", `landing.form.contactDate`·`errors.INVALID_CONTACT_DATE` 추가). 격리 docker(`wonjin-cd-verify`)에서 마이그레이션·연락날짜 왕복(200/`INVALID_CONTACT_DATE`/레거시 NULL)·브라우저 실측(홈 슬로건 부재·로그인 헤더+언어전환·상담폼 날짜피커·예약상세 날짜표시·3상태 라디오 전 경로·저장 왕복) 전건 확인. `dotnet build`·`npm run build` 0에러. 공유 스택 6컨테이너 무영향. **검증 중 수정**: 3상태 라디오 초기 구현이 reka 내부 선택과 토글 로직이 경합해 재클릭 해제가 불안정 → `@click.capture.stop`로 reka 핸들러 선점하도록 변경. 상세: `docs/session-log.md`.
