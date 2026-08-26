# WonjinReservationWeb 프로젝트
> 루트 규칙 상속: `C:\Users\jinho\Desktop\WebProject\CLAUDE.md` · **설계 SSOT: `docs/design.md`** — 설계 결정이 바뀌면 코드보다 이 문서를 먼저 고칠 것

## 개요
원진성형외과의 **외국인(중화권) 고객 예약·상담 관리 시스템**. 광고로 유입된 고객이 랜딩 폼으로 상담을 신청하면, 병원 실장이 위챗으로 연락해 상담·방문예약을 확정하고 그 과정을 관리자 패널에서 추적·감사·집계한다.
- 흐름: 광고(UTM·추천코드) → 랜딩 폼 제출 → 실장 위챗 연락 → 상담·시술 결정 → 방문예약 확정 → 내원
- 지원 언어 4개: **zh-CN(기본)** · zh-TW · en · ko
- 현재 상태: **Phase 2(랜딩+예약폼+유입경로) 구현 완료**(2026-08-26) — 4언어 브라우저 E2E + 백엔드 curl 실측(동시성·rate limit·honeypot 포함) 전건 검증. Phase 3(대시보드)부터는 사용자 지시 대기

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
- 🔴 **`db.Database.SqlQuery<T>(...)`에 바로 `.SingleAsync()`를 걸지 말 것** — `INSERT...RETURNING`처럼 non-composable SQL을 서브쿼리로 감싸려다 `InvalidOperationException`을 던진다(실측 확인, 8-11절 코드카운터). `.ToListAsync()`로 먼저 그대로 구체화한 뒤 메모리에서 `.Single()`을 적용할 것
- **Vue 템플릿에서 `{{ prefix }}<A>{{ link }}</A>{{ suffix }}` 사이에 줄바꿈을 넣으면 공백이 하나씩 끼어든다** — 한국어("본인은 개인정보처리방침 에 동의")처럼 조사가 바로 붙어야 하는 언어에서 실제로 어색해짐(실측 확인). 4언어 문구를 한 태그에 이어붙일 땐 줄바꿈 없이 한 줄로 쓰고, 언어별로 필요한 공백은 번역 문자열 자체(prefix 끝 등)에 포함시킬 것 — 중국어는 공백 없음이 정상, 한국어·영어는 필요
- **`@nuxtjs/i18n`의 `locales[].code`가 대문자를 포함하면(`zh-TW`) URL prefix도 그 대소문자 그대로 생성된다**(`/zh-TW`, design.md 5-1절이 표기한 소문자 `/zh-tw`와 다름) — 실측 결과 대문자·소문자 URL 둘 다 같은 페이지로 정상 라우팅되어 기능 문제는 없으나, 링크 자체는 항상 대문자로 생성됨. `code`를 DB `locale` 값(`zh-TW`, CHECK 제약과 동일)과 반드시 일치시켜야 하므로 지금은 그대로 둠 — SEO 정규화가 필요해지면(Phase 9) 재검토
- **honeypot 필드가 채워진 요청은 400이 아니라 200으로 조용히 흘려보내고 DB에 저장하지 않을 것** — 실패 응답을 주면 봇이 실패 패턴을 학습해 우회를 시도할 여지를 준다(11-1절)

## 절대 원칙 이행 (루트 CLAUDE.md)
- **화면 깜빡임 금지** — 데이터 페이지는 `<script setup>` 최상위 `await useApi(...)` SSR 프리로드. `onMounted`+client fetch 금지. 전환 오버레이는 `<Transition>` 금지, 항상 마운트 + `pointer-events`를 상태값에 직접 클래스 바인딩
- **입력 길이 3곳 일치** — DB `varchar(N)` / 백엔드 `[MaxLength(N)]` / 프론트 `maxlength` 항상 세트로 수정. 전체 표는 `docs/design.md` 9장
- **디자인 원칙** — 모든 input/textarea/select에 **보이는 label** 필수(검색창 포함). placeholder로 대체 금지. honeypot만 예외
- **DB 성능** — 새 쿼리마다 ①필터·정렬 컬럼 인덱스 ②목록 페이징 ③불필요 컬럼·관계 미조회 3가지 자체 점검
- **번역** — 4개 로케일 JSON의 키 집합이 항상 완전히 동일. 키 추가·삭제는 4파일 세트로 수정 후 개수 대조

## TODO
### 다음 세션 최우선
- [ ] **Phase 3(예약 대시보드+상세+상담기록+상태머신+소프트삭제) 착수** — Phase 2 완료, 착수는 사용자 지시 대기
- [ ] **테스트 계정 `test-admin@wonjin.local`(비번 `TestPassword123!`) 처리 여부 결정** — Phase 1 브라우저 E2E 검증용으로 DB에 직접 삽입, 최초 운영 어드민 계정과 별개. 삭제할지 유지할지 사용자 확인 대기
- [ ] **M11 로그인 시 locale 자동 반영 방식 결정** — 지금은 `PATCH /api/auth/me/locale` 수동 변경만 동작(design.md 20장)
- [ ] **로컬 DB 테스트 더미 예약 정리 여부 확인** — Phase 2 실측 검증(동시성 20건·rate limit·honeypot 등) 중 생성된 더미 `reservations` 약 27건이 로컬 dev DB에 남아있음. 실서비스 데이터 아님, Phase 3에서 대시보드 목록 확인 전에 정리 권장
- [ ] **Phase 1에서 범위상 의도적으로 안 만든 것**(design.md 19-2절 근거) — `useApi.ts`(GET SSR 래퍼, 실사용처 없어 Phase 3에서), 관리자 사이드바 전체(12-3절 10메뉴, 메뉴 화면 자체가 아직 없어 죽은 링크 방지차 최소 헤더만), 전환 차단 오버레이(13-2절 — Phase 2 페이지들은 SSR 데이터 페칭이 없어 전환 지연이 없으므로 여전히 불필요, 관리자 대시보드가 생기는 Phase 3에서 실효). 이 항목들을 "빠뜨렸다"고 오인하지 말 것 — 각 Phase에서 자연스럽게 채워짐
### Phase 계획 — 완료기준 포함 (design.md 19장과 동일, 상세 코드는 그쪽 참고)
| # | 내용 | 완료기준 |
|---|---|---|
| 0 | ✅ 스캐폴딩 + DB 마이그레이션(2026-08-26 완료) | 컨테이너 기동+마이그레이션+인덱스 확인 + `Asia/Seoul` 타임존 조회 성공 — 전건 실측 검증 완료 |
| 1 | ✅ 인증 + `AccountStateFilter` + 동일출처 프록시(2026-08-26 완료) | 로그인~정지차단 E2E + 랜딩에서 `/api/auth/me` 미호출 확인(F5) — 전건 실측 검증 완료(design.md 19-2절) |
| 2 | ✅ 랜딩 4언어 + 예약 폼 + **개인정보 처리방침**(틀만) + 유입경로 수집(2026-08-26 완료) | 4언어 폼 제출→DB적재+UTM보존 + landing-visit 시크릿없이 404(F11) + 연락희망시각 `time` 저장 확인(D10) — 전건 실측 완료(19-3절) |
| 3 | 예약 대시보드·상세·상담기록 누적·상태머신·소프트삭제 | 상태전이 동시성409 + 코드동시생성 중복없음(F4) + 삭제조건409(D15) + **미배정 400 차단**(D17). 🔴 **동시성 재현 스크립트 3종 필수**(코드생성·소프트삭제·상태전이 — 19-1절, curl+xargs -P) |
| 4 | 실장(`consultants`)·시술 관리 | 4언어 탭 CRUD + 비활성실장 배정·KPI 제외/과거예약 유지(D13) |
| 5 | 예약 달력 | 월범위 검증 + 부분인덱스 사용 확인 |
| 6 | 실장 KPI·예약 통계 | 빈구간 0 채움 확인 |
| 7 | 계정 관리·감사 로그 | 3역할 CRUD 전부 기록되는지 확인 |
| 8 | 유입 경로 분석 | 비어드민 접근 차단 실측 |
| 9 | SEO·보안감사·배포 | 라이브 curl 검증 |

## 미결정 (상세: `docs/design.md` 20장)
- [ ] **M8 병원 정식 정보**(상호·주소·대표전화·사업자번호) — 푸터·JSON-LD용. Phase 2 완료기준엔 없어 브랜드 토큰 `WonJin`(D18)만으로 footer 처리, 실제 JSON-LD는 Phase 9로 재확인
- [ ] **M10 로고 이미지**(favicon·사이드바·OG) — Phase 2엔 텍스트 브랜드로 대체, 실제 파일은 Phase 9
- [ ] **M6 랜딩 히어로·소개 콘텐츠**(4개 언어) — Phase 2는 기능 설명 최소 문구로 대체(마케팅 카피 아님), 실제 콘텐츠는 Phase 2 이후
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
`docs/design.md`(설계 SSOT) · `docs/session-log.md`(세션 아카이브) · `docs/reservation-desk_1.html`(참고 화면 원본)
공유 가이드(`C:\Users\jinho\Desktop\WebProject\`): `auth-pattern-reference.md` · `admin-panel-pattern-reference.md` · `web-security-audit-guide.md` · `seo-pattern-reference.md`

## 세션 요약 (오래된 항목은 `docs/session-log.md` 참고)
- **2026-08-26 (12) — Phase 2(랜딩+예약폼+유입경로) 구현 완료**: 백엔드 `ReservationsController`(공개 예약 신청, rate limit `reservation-create` IP 분당5)·`InternalController`(`landing-visit`, 시크릿 헤더 없으면 404)와 프론트 `layouts/landing.vue`+`pages/index.vue`+`pages/privacy.vue`+`app/error.vue`+5-3절 언어감지 스크립트+4로케일 JSON(39키 일치 자체대조)을 design.md 11-1·12-1·15-1장 그대로 구현. 새 마이그레이션 없음(Phase 0 스키마 재사용). 실행 중 실제 버그 4건 발견·수정: ①`InternalController`에 `using Microsoft.EntityFrameworkCore;` 누락(컴파일 실패) ②`db.Database.SqlQuery<int>().SingleAsync()`가 `INSERT...RETURNING`을 서브쿼리로 감싸려다 `InvalidOperationException`(실행 전엔 안 보이던 런타임 버그) → `ToListAsync()`+메모리 `Single()`로 수정 ③Vue 템플릿에서 동의 문구 태그 사이 줄바꿈이 언어별로 다른 공백 오류를 만듦(태그를 한 줄로 붙이고 필요한 공백은 번역 문자열 자체에 포함) ④페이지 `<title>` 미설정. **실측 전건 통과**: curl로 정상제출(한자 이름·UTM·`time` 컬럼 DB 정확 저장)·honeypot봇(200이지만 미저장)·동의없음(400)·잘못된성별(400)·landing-visit 무시크릿(404)/유시크릿(200) 확인 + **동시 예약 20건 제출(F4 재현, 서로 다른 IP로 rate limit 우회)→전부 200·코드 20개 전부 유니크** + rate limit 분당5(6번째 429) 확인. 브라우저 E2E로 4언어(zh-CN/zh-TW/en/ko) 랜딩·폼 제출→성공화면 전환(U2)→DB 저장, UTM이 언어감지 리다이렉트를 거쳐도 보존됨(`/?utm_source=...`→`/ko?utm_source=...`), 404 에러화면(U3, 로케일 유지)+홈복귀, honeypot 필드 오프스크린(`left:-9942px`)+`tabindex=-1` 전부 확인. **편차 발견**: `@nuxtjs/i18n`의 `code:'zh-TW'`가 URL prefix도 대문자로 생성(design.md 5-1절 소문자 `/zh-tw` 표기와 다름) — 대소문자 둘 다 실제로는 정상 라우팅되어 기능 문제는 없음, Phase 9 SEO 작업 시 재검토 필요. M8(병원정보)·M10(로고)은 Phase 2 완료기준에 없어 브랜드 토큰 `WonJin`만으로 대체, 실제 값은 Phase 9로 재확인
