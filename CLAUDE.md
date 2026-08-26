# WonjinReservationWeb 프로젝트
> 루트 규칙 상속: `C:\Users\jinho\Desktop\WebProject\CLAUDE.md` · **설계 SSOT: `docs/design.md`** — 설계 결정이 바뀌면 코드보다 이 문서를 먼저 고칠 것

## 개요
원진성형외과의 **외국인(중화권) 고객 예약·상담 관리 시스템**. 광고로 유입된 고객이 랜딩 폼으로 상담을 신청하면, 병원 실장이 위챗으로 연락해 상담·방문예약을 확정하고 그 과정을 관리자 패널에서 추적·감사·집계한다.
- 흐름: 광고(UTM·추천코드) → 랜딩 폼 제출 → 실장 위챗 연락 → 상담·시술 결정 → 방문예약 확정 → 내원
- 지원 언어 4개: **zh-CN(기본)** · zh-TW · en · ko
- 현재 상태: **Phase 1(인증)·Phase 3(예약 대시보드·상세·상담기록·상태머신·소프트삭제) 구현 완료**(2026-08-26). 🔴 **Phase 3은 워크트리 `session-work` 브랜치에만 커밋된 상태 — main 병합은 사용자 지시 대기**(main에는 Phase 1까지만 있음). Phase 2(랜딩)는 별도 세션이 동시 진행 중

## 기술 스택
| 레이어 | 기술 |
|---|---|
| 프론트 | Nuxt 4 + Vue 3 **Composition API** + Tailwind v4(`@tailwindcss/vite`) + `@nuxtjs/i18n`(`prefix_except_default`, 기본 zh-CN) |
| SEO | `@nuxtjs/sitemap` + `@nuxtjs/robots` |
| 백엔드/DB | ASP.NET Core 10 + EF Core(`EFCore.NamingConventions` 스네이크케이스) / PostgreSQL 16 (스키마 `wonjin`) |
| 인증 | 자체 JWT(AT 15분, 쿠키 `wj_at`) + RT(7일, SHA-256, 쿠키 `wj_rt`) — **소셜 로그인·회원가입 없음** |
| UI | **shadcn-vue**(`shadcn-nuxt`, style `new-york`) + `reka-ui` 프리미티브 + `class-variance-authority`(D19, 구 D11 대체) |
| 팔레트 | **Olive Garden Feast**(D20) — `#606C38`올리브(primary)·`#283618`짙은산림녹(foreground)·`#FEFAE0`크림(background)·`#DDA15E`탄(secondary)·`#BC6C25`번트오렌지(destructive), OKLCH 변환 후 shadcn CSS 변수에 적용 |
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

## 역할 · 메뉴 권한
| 메뉴 | Admin | HospitalManager | Consultant |
|---|:---:|:---:|:---:|
| 예약 대시보드 · 예약 상세 · 예약 달력 | ✅ | ✅ | ✅ |
| 실장 관리 · 시술/수술 관리 · 실장 KPI · 예약 통계 | ✅ | ✅ | ❌ |
| 계정 관리 · 로그(감사) · 유입 경로 분석 | ✅ | ❌ | ❌ |

## 🔴 이 프로젝트에서 특히 주의할 것
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
- **`AuditLogFilter`의 예외 처리** — 컨트롤러 예외는 `next()`가 throw하지 않고 `ActionExecutedContext.Exception`에 담겨 반환됨. 확인 안 하면 실패한 쓰기가 200(성공)으로 오기록됨
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
- 🔴 **한 요청 안에서 `ExecuteUpdateAsync`/`ExecuteDeleteAsync`를 여러 번 쓰거나 그 뒤에 별도 `SaveChangesAsync`로 로그를 남긴다면 반드시 `BeginTransactionAsync()`로 묶을 것** — 묶지 않으면 앞 단계들은 각각 즉시 커밋되고 마지막 단계만 실패해도 "응답은 500인데 일부 변경은 이미 반영된" 부분 커밋이 된다(실측 확인 — `UpdateReservation`에 존재하지 않는 `procedureId`를 보내면 스칼라 필드·자동 Confirmed 전이는 저장되고 시술 목록만 삭제된 채 로그 없이 500이 났음). `audit_logs`처럼 "실패해도 본 작업을 막으면 안 되는" 부가 기록만 트랜잭션 밖에서 별도 try/catch로 베스트에포트 처리
- 🔴 **Npgsql에 `timestamptz` 비교용 `DateTimeOffset`을 넘길 때 Offset은 반드시 0(UTC)이어야 함** — `TimeZoneInfo.ConvertTime(...)`으로 만든 KST(+09:00) 오프셋 `DateTimeOffset`을 쿼리 파라미터로 그대로 쓰면 `Cannot write DateTimeOffset with Offset=09:00:00 ... only offset 0 (UTC) is supported` 500(실측 확인, `GetSummary` 최초 구현). KST로 년/월만 뽑고 나면 반드시 `.ToUniversalTime()`을 거쳐서 쿼리에 넘길 것 — `ConvertTimeToUtc(...)`(DateTime 반환, Kind=Utc)는 이 문제가 없음

## 절대 원칙 이행 (루트 CLAUDE.md)
- **화면 깜빡임 금지** — 데이터 페이지는 `<script setup>` 최상위 `await useApi(...)` SSR 프리로드. `onMounted`+client fetch 금지. 전환 오버레이는 `<Transition>` 금지, 항상 마운트 + `pointer-events`를 상태값에 직접 클래스 바인딩
- **입력 길이 3곳 일치** — DB `varchar(N)` / 백엔드 `[MaxLength(N)]` / 프론트 `maxlength` 항상 세트로 수정. 전체 표는 `docs/design.md` 9장
- **디자인 원칙** — 모든 input/textarea/select에 **보이는 label** 필수(검색창 포함). placeholder로 대체 금지. honeypot만 예외
- **DB 성능** — 새 쿼리마다 ①필터·정렬 컬럼 인덱스 ②목록 페이징 ③불필요 컬럼·관계 미조회 3가지 자체 점검
- **번역** — 4개 로케일 JSON의 키 집합이 항상 완전히 동일. 키 추가·삭제는 4파일 세트로 수정 후 개수 대조

## TODO
### 다음 세션 최우선
- [ ] **Phase 2(랜딩 4언어+예약 폼+유입경로 수집) 착수** — Phase 1 완료, 착수는 사용자 지시 대기
- [ ] **테스트 데이터 처리 여부 결정** — Phase 1 테스트 계정 `test-admin@wonjin.local`(비번 `TestPassword123!`)에 이어 Phase 3 E2E용으로 `test-manager@wonjin.local`·`test-consultant@wonjin.local`(동일 비번) 계정 2개 + 실장 2명(`김테스트`·`박테스트`) + 시술 1개(`test_botox`)를 DB 직접 INSERT로 추가. 전부 운영 데이터 아님 — 삭제할지 유지할지 Phase 4(실장·시술 관리 화면 완성 후) 사용자 확인 대기
- [ ] **Phase 4 착수 시 참고** — `GET /api/admin/consultants`·`GET /api/admin/procedures`(조회 전용)는 Phase 3에서 배정·시술선택 드롭다운용으로 이미 추가됨. Phase 4는 POST/PUT(등록·수정·비활성화)만 추가하면 됨
- [ ] **M11 로그인 시 locale 자동 반영 방식 결정** — 지금은 `PATCH /api/auth/me/locale` 수동 변경만 동작(design.md 20장)
- [ ] **Phase 1에서 범위상 의도적으로 안 만든 것**(design.md 19-2절 근거) — `useApi.ts`(GET SSR 래퍼, 실사용처 없어 Phase 3에서), 관리자 사이드바 전체(12-3절 10메뉴, 메뉴 화면 자체가 아직 없어 죽은 링크 방지차 최소 헤더만), 전환 차단 오버레이(13-2절, 데이터 프리로드 화면이 느는 Phase 2 이후 실효). 이 3개를 "빠뜨렸다"고 오인하지 말 것 — 각 Phase에서 자연스럽게 채워짐
### Phase 계획 — 완료기준 포함 (design.md 19장과 동일, 상세 코드는 그쪽 참고)
| # | 내용 | 완료기준 |
|---|---|---|
| 0 | ✅ 스캐폴딩 + DB 마이그레이션(2026-08-26 완료) | 컨테이너 기동+마이그레이션+인덱스 확인 + `Asia/Seoul` 타임존 조회 성공 — 전건 실측 검증 완료 |
| 1 | ✅ 인증 + `AccountStateFilter` + 동일출처 프록시(2026-08-26 완료) | 로그인~정지차단 E2E + 랜딩에서 `/api/auth/me` 미호출 확인(F5) — 전건 실측 검증 완료(design.md 19-2절) |
| 2 | 랜딩 4언어 + 예약 폼 + **개인정보 처리방침** + 유입경로 수집 | 4언어 폼 제출→DB적재+UTM보존 + landing-visit 시크릿없이 404(F11) + 연락희망시각 `time` 저장 확인(D10) |
| 3 | ✅ 예약 대시보드·상세·상담기록 누적·상태머신·소프트삭제(2026-08-26 완료, `session-work` 브랜치) | 상태전이 동시성409 + 코드동시생성 중복없음(F4) + 삭제조건409(D15) + **미배정 400 차단**(D17). 🔴 **동시성 재현 스크립트 3종**(`scripts/phase3-concurrency/`) 전건 실행·통과 완료 |
| 4 | 실장(`consultants`)·시술 관리 | 4언어 탭 CRUD + 비활성실장 배정·KPI 제외/과거예약 유지(D13) |
| 5 | 예약 달력 | 월범위 검증 + 부분인덱스 사용 확인 |
| 6 | 실장 KPI·예약 통계 | 빈구간 0 채움 확인 |
| 7 | 계정 관리·감사 로그 | 3역할 CRUD 전부 기록되는지 확인 |
| 8 | 유입 경로 분석 | 비어드민 접근 차단 실측 |
| 9 | SEO·보안감사·배포 | 라이브 curl 검증 |

## 미결정 (상세: `docs/design.md` 20장)
- [ ] **M8 병원 정식 정보**(상호·주소·대표전화·사업자번호) — 푸터·JSON-LD용, Phase 2
- [ ] **M10 로고 이미지**(favicon·사이드바·OG) — Phase 2
- [ ] **M6 랜딩 히어로·소개 콘텐츠**(4개 언어) — Phase 2 이후
- [ ] **M2 도메인·Cloudflare 계정** — Phase 9
- [ ] **M11 로그인 시 locale 자동 반영 방식**(7-2절) — `users.locale`이 `NOT NULL DEFAULT`라 "비어있을 때만 채운다" 원문 구현 불가, 별도 컬럼 추가 여부 결정 필요
> 최초 어드민 계정은 **사용자가 DB에 직접 삽입**(시딩 코드 없음). 실장·시술 마스터도 사용자가 관리 화면에서 직접 등록
> **설계 공백은 2026-08-26 전건 해소** — U2 제출완료(폼 자리 인라인 교체) / U3 error.vue 단일화 / U8 환경변수표(4-3절) / U10 RouteMap 매핑표(14-1절) / U11 i18n 키 규칙(5-6절) / U13 rate limit 통합표(7-5절) / U16 테스트 전략(19-1절)

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
- **2026-08-26 (12) — Phase 3(예약 대시보드·상세·상담기록·상태머신·소프트삭제) 구현 완료, `session-work` 브랜치(병합 대기)**: 옆 세션이 같은 main에서 분기해 Phase 2를 동시 진행 중이라 격리 워크트리(`.worktrees/session-work`, 포트 postgres 5536/api 5201/frontend 3702)에서 착수, main 병합은 사용자 지시 대기. 백엔드 `AdminReservationsController`(목록·요약·상세·저장·배정·상태전이·상담기록추가/수정·소프트삭제) 신규 + Phase 4 선행 최소 조회전용 `AdminConsultantsController`·`AdminProceduresController`(GET만, CRUD는 Phase 4) 신규. 상태머신(10장) 그대로 구현 — New→Consulting은 최초 상담기록 추가의 부수효과, →Confirmed는 visit_date+deposit_paid 동시충족의 부수효과, Visited·Cancelled만 수동 액션. D17(미배정 400차단)·D15(상담기록 있으면 소프트삭제 409)는 전부 `ExecuteUpdateAsync`의 WHERE절에 조건을 넣어 원자적으로 처리(10-1·11-2절 그대로). 프론트 대시보드(4카드+필터+페이징)·예약상세(12-5절 7개 섹션 전부) 신규 + `useApi`(GET SSR 프리로드 전용, Phase 1 TODO에서 보류됐던 것) 신규 + 13-2절 전환차단 오버레이(`RouteOverlay`, `<Transition>` 금지 패턴 그대로) 신규 도입. **실측 중 백엔드 버그 1건 발견·수정**: `GetSummary`가 KST 오프셋(+09:00) 그대로인 `DateTimeOffset`을 Npgsql에 넘겨 500(Offset은 UTC만 허용) — `.ToUniversalTime()` 추가로 해결(위 주의사항 참고). **프론트 RBAC 누락 1건 자체 발견·수정**: HospitalManager가 쓰기 버튼을 전부 볼 수 있던 것을 `canWrite` computed로 숨김(6-3절 원칙 2 — 버튼 숨김 없이 백엔드 403만 믿고 있었음). 완료기준 4개 항목(상태전이 동시성409·코드동시생성 무중복(F4)·삭제조건409(D15)·미배정 400차단(D17)) 전부 curl+브라우저(좌표클릭 자동화도구 특이사항 재확인 — 19-2절과 동일 패턴, JS 기반 상호작용으로 우회)로 실측, **동시성 재현 스크립트 3종**(`scripts/phase3-concurrency/`) 전부 작성·실행·통과(코드카운터 20/20 고유·상태전이 1성공/19충돌·소프트삭제 경쟁 무모순). 테스트 계정 2개(manager/consultant)·실장 2명·시술 1개를 Phase 1 테스트계정과 동일 패턴으로 DB 직접 INSERT(TODO 등록). i18n 99키 4파일 동일 확인(node 스크립트 직접 대조). AuditLogFilter·RouteMap(Phase 7)·예약 달력(Phase 5)은 각각 소관 Phase가 달라 범위에서 제외. **완료 선언 후 사용자 요청으로 재감사해 미완료 6건 추가 발견·전건 수정**: ①"내원 확인"에 확인UI 누락(confirm() 추가, 거부/승인 양쪽 실측) ②시술 체크박스가 비활성·선택됨 시술을 은닉(값 자체는 안 사라지지만 화면에서 확인·해제 불가 — includeInactive 조회+표시로 수정) ③대시보드 실장 필터에 "비활성 포함" 옵션 없음(8-4절 명시, 체크박스 추가) ④소프트삭제 시 `audit_logs` 미기록(11-2절 "양쪽에 모두 기록" 요구를 AuditLogFilter Phase7 보류와 혼동한 제 판단 오류 — 컨트롤러에서 직접 기록하도록 수정) ⑤고객정보 카드에 이름·예약코드 필드 누락(헤더에만 있었음) ⑥방문시각 라벨에 "(한국 시간)" 누락. **교훈**: "완료" 선언 전 설계서 필드 체크리스트를 항목 단위로 대조하지 않고 화면이 도는지만 확인해 놓침 — 다음부터 완료 선언 전 설계서의 필드 나열형 문장(예: "A/B/C 포함")을 그대로 체크리스트화해 하나씩 대조할 것. **`error.vue`(U3·12-1절) 구현 완료**: 404/500 통합, 4언어, noindex, 홈 링크 1개(clearError+로케일 유지), 관리자 경로 fatal 에러도 동일 화면 사용 확인(브라우저 실측: zh-CN/ko/en 3개 로케일 + `/admin/reservations/99999`). **전면 재감사(46개 파일 전문 Read + grep 대조로 누락 3개 추가 발견) 후 결함 7건 전부 수정 완료**: ①`UpdateReservation`/`ChangeStatus`/`AddNote`/`SoftDelete` 4곳의 비원자적 다단계 쓰기를 `BeginTransactionAsync`로 원자화 + `ProcedureIds` 존재 사전검증 추가(재현 확인 — 수정 전엔 존재하지 않는 procedureId 하나로 시술 목록이 조용히 삭제되면서 로그 없이 500이 났었음, 수정 후 DB 상태 완전 불변인 clean 400 확인) ②`Program.cs`에 Production 전용 `UseExceptionHandler` 추가(스택트레이스·DB 제약조건명 노출 차단 — Production 환경으로 일시 전환해 `{code:"INTERNAL_ERROR"}` clean 응답 실측 후 Development로 원복) ③`DepositAmount` 상한을 `numeric(12,2)`와 일치(`9999999999.99`) ④`useApi.ts`의 `useAsyncData` 키를 `key.value` 스냅샷 대신 반응형 `key` 자체로 전달 ⑤같은 파일 SSR 401 재시도 분기의 baseURL 계산을 `||`에서 나머지 코드와 동일한 삼항연산자로 통일. 동시성 스크립트 3종 전부 재실행해 회귀 없음 재확인.
