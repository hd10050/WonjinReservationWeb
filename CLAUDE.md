# WonjinReservationWeb 프로젝트
> 루트 규칙 상속: `C:\Users\jinho\Desktop\WebProject\CLAUDE.md` · **설계 SSOT: `docs/design.md`** — 설계 결정이 바뀌면 코드보다 이 문서를 먼저 고칠 것

## 개요
원진성형외과의 **외국인(중화권) 고객 예약·상담 관리 시스템**. 광고로 유입된 고객이 랜딩 폼으로 상담을 신청하면, 병원 실장이 위챗으로 연락해 상담·방문예약을 확정하고 그 과정을 관리자 패널에서 추적·감사·집계한다.
- 흐름: 광고(UTM·추천코드) → 랜딩 폼 제출 → 실장 위챗 연락 → 상담·시술 결정 → 방문예약 확정 → 내원
- 지원 언어 4개: **zh-CN(기본)** · zh-TW · en · ko
- 현재 상태: **설계 문서 작성 완료, 구현 착수 전** (2026-08-25). 구현은 사용자 명시 승인 후 시작

## 기술 스택
| 레이어 | 기술 |
|---|---|
| 프론트 | Nuxt 4 + Vue 3 **Composition API** + Tailwind v4(`@tailwindcss/vite`) + `@nuxtjs/i18n`(`prefix_except_default`, 기본 zh-CN) |
| SEO | `@nuxtjs/sitemap` + `@nuxtjs/robots` |
| 백엔드/DB | ASP.NET Core 10 + EF Core(`EFCore.NamingConventions` 스네이크케이스) / PostgreSQL 16 (스키마 `wonjin`) |
| 인증 | 자체 JWT(AT 15분, 쿠키 `wj_at`) + RT(7일, SHA-256, 쿠키 `wj_rt`) — **소셜 로그인·회원가입 없음** |
| UI | 라이브러리 미도입 — 모달은 네이티브 `<dialog>`, 달력은 자체 월간 그리드 |
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
- **모든 시각은 KST 고정** — 브라우저 타임존 사용 금지
- 시술명은 언어별 컬럼 4개(`name_zh_cn` 등), 연락 희망 시간은 4지선다(KST 범위 병기)

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
- **예약 코드는 시퀀스로 발급** — "그날 최대값+1"은 동시 제출 시 UNIQUE 위반 500
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

## 절대 원칙 이행 (루트 CLAUDE.md)
- **화면 깜빡임 금지** — 데이터 페이지는 `<script setup>` 최상위 `await useApi(...)` SSR 프리로드. `onMounted`+client fetch 금지. 전환 오버레이는 `<Transition>` 금지, 항상 마운트 + `pointer-events`를 상태값에 직접 클래스 바인딩
- **입력 길이 3곳 일치** — DB `varchar(N)` / 백엔드 `[MaxLength(N)]` / 프론트 `maxlength` 항상 세트로 수정. 전체 표는 `docs/design.md` 9장
- **디자인 원칙** — 모든 input/textarea/select에 **보이는 label** 필수(검색창 포함). placeholder로 대체 금지. honeypot만 예외
- **DB 성능** — 새 쿼리마다 ①필터·정렬 컬럼 인덱스 ②목록 페이징 ③불필요 컬럼·관계 미조회 3가지 자체 점검
- **번역** — 4개 로케일 JSON의 키 집합이 항상 완전히 동일. 키 추가·삭제는 4파일 세트로 수정 후 개수 대조

## TODO
### 다음 세션 최우선
- [ ] 🔴 **구현 착수 승인 대기** — 설계 문서(`docs/design.md`) 검토 후 Phase 0부터 시작할지 사용자 결정 필요
### Phase 계획 (상세: `docs/design.md` 19장)
- [ ] Phase 0 스캐폴딩 + DB 마이그레이션 / [ ] Phase 1 인증 + 동일출처 프록시 / [ ] Phase 2 랜딩 4언어 + 예약 폼
- [ ] Phase 3 예약 대시보드·상세·상태머신 / [ ] Phase 4 실장·시술 관리 / [ ] Phase 5 예약 달력
- [ ] Phase 6 KPI·통계 / [ ] Phase 7 계정 관리·감사 로그 / [ ] Phase 8 유입 경로 분석 / [ ] Phase 9 SEO·보안감사·배포

## 미결정 (상세: `docs/design.md` 20장)
- [ ] M2 실제 도메인·Cloudflare 계정 / M3 예약 코드 일련번호 리셋 규칙 / M5 실장 자동 배정 규칙(라운드로빈 vs 수동)
- [ ] M6 랜딩 디자인 콘텐츠 / M7 개인정보 보유기간(처리방침 기재값)

## 🔴 법적 검토 — 완료, 재론 금지
2026-08-25 사용자 확인 완료. 이 시스템은 중계·광고 플랫폼이 아니라 병원에 예약 기능만 제공하는 도구이며 관련 요건은 전부 통과됨. **의료광고 심의·외국인환자 유치 등록 등을 리스크로 다시 제기하지 말 것.**

## 참고 문서
`docs/design.md`(설계 SSOT) · `docs/session-log.md`(세션 아카이브) · `docs/reservation-desk_1.html`(참고 화면 원본)
공유 가이드(`C:\Users\jinho\Desktop\WebProject\`): `auth-pattern-reference.md` · `admin-panel-pattern-reference.md` · `web-security-audit-guide.md` · `seo-pattern-reference.md`

## 세션 요약 (오래된 항목은 `docs/session-log.md` 참고)
- **2026-08-25 (3) — 자체검토 결함 11건 전건 수정 + 실장 모델 정정 + 훅 버그 수정**: ①**🔴 D8 오설계 정정** — 실장을 `users.role='Consultant'`에 얹었던 것을 `consultants` 독립 테이블로 분리(계정과 1:1 아님, FK 연결 없음). 사용자 지적으로 발견 ②하드 삭제 전면 제거(D13) — 실장·시술·계정·상담기록 전부 비활성화/정지로만, 비활성 실장 노출 규칙 표로 명문화 ③상담 기록 누적화(D14, `reservation_notes` 신설) ④**타임존 정책 신설**(9-2절) — 전 시각 KST 고정, `Intl.DateTimeFormat`에 `timeZone` 명시로 하이드레이션 mismatch 차단, KST 월초를 UTC로 환산하는 집계 규칙 ⑤예약 코드 시퀀스 발급(F4) ⑥달력 `Confirmed`+`Visited`(F1) ⑦4카드 조건부 집계(F2) ⑧인증 초기화 `/admin` 한정(F5) ⑨방문 기록 fire-and-forget + 내부 시크릿 헤더 전용 404(F6·F11) ⑩유입경로 인덱스 `created_at` 선행으로 정정(F10) ⑪실장 간 예약 접근 전면 허용 명문화(F8, 이력으로 추적). 함께 루트 `.claude/settings.json`의 PostToolUse `.cs` 빌드검사 버그 수정(부모 폴더에서 하위로만 `.csproj`를 찾아 `Controllers/` 등 하위 폴더 저장 시 조용히 스킵되던 문제 → 상위 탐색 루프로 교체, 실제 동작·무한루프 방지 검증 완료. 백업: `settings.json.bak`)
- **2026-08-25 (2) — 설계 확정 3건 반영 + 자체검토**: 배포 브랜치 `main` 확정 / 예약금 통화 CNY·KRW(D12) / 법적 검토 완료 처리. design.md 전문 검토로 결함 11건(F1~F11) 발견 — 상세는 `docs/session-log.md`
