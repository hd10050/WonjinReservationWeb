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
- **예약금은 실장 수동 입금 확인만** — PG·웹훅 없음
- **유입 경로 자동 기록**(UTM+추천코드), **인플루언서 전환율은 어드민 전용 메뉴** `/admin/referrals`로 분리
- **회원가입 엔드포인트 없음** — 계정 생성 경로는 `POST /api/admin/users` 하나뿐
- **동일 출처 API 프록시 채택** — 화면 깜빡임 금지(SSR 프리로드)를 이행하기 위한 전제
- 실장은 별도 테이블 없이 `users.role='Consultant'` + `team`/`is_assignable` 컬럼
- 시술명은 언어별 컬럼 4개(`name_zh_cn` 등), 연락 희망 시간은 자유 텍스트 아닌 4지선다

## 역할 · 메뉴 권한
| 메뉴 | Admin | HospitalManager | Consultant |
|---|:---:|:---:|:---:|
| 예약 대시보드 · 예약 상세 · 예약 달력 | ✅ | ✅ | ✅ |
| 실장 관리 · 시술/수술 관리 · 실장 KPI · 예약 통계 | ✅ | ✅ | ❌ |
| 계정 관리 · 로그(감사) · 유입 경로 분석 | ✅ | ❌ | ❌ |

## 🔴 이 프로젝트에서 특히 주의할 것
- **감사 로그 대상은 3역할 전부** — 일반 가이드는 `role=="Admin"`만 감사하지만, 사용자 요구는 실장·병원관리자 CRUD까지 전부 감사하는 것. Admin으로 좁히면 실장 행위 전체가 로그에서 빠짐
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
- [ ] M1 자동배포 감시 브랜치(`main` vs `production`) — Phase 0 전 확인 필요
- [ ] M2 실제 도메인·Cloudflare 계정 / M3 예약 코드 일련번호 리셋 규칙 / M4 예약금 통화(CNY 고정 여부)
- [ ] M5 실장 자동 배정 규칙(라운드로빈 vs 수동) / M6 랜딩 디자인 콘텐츠 / M7 개인정보 보유기간

## 🔴 법적 확인 필요 `[미확인]` (상세: `docs/design.md` 18장)
외국인환자 유치 의료기관 등록 의무 / 의료광고 사전심의(제56조 금지광고는 이용자 수 무관 항상 적용) / 개인정보 국외 이전 고지 — **전부 미확인 상태이며 오픈 전 공식 확인 필요**

## 참고 문서
`docs/design.md`(설계 SSOT) · `docs/session-log.md`(세션 아카이브) · `docs/reservation-desk_1.html`(참고 화면 원본)
공유 가이드(`C:\Users\jinho\Desktop\WebProject\`): `auth-pattern-reference.md` · `admin-panel-pattern-reference.md` · `web-security-audit-guide.md` · `seo-pattern-reference.md`

## 세션 요약 (오래된 항목은 `docs/session-log.md` 참고)
- **2026-08-25 (1) — 프로젝트 설계**: 하네스(메모리·훅) 점검 후 프로젝트 착수. 참고 자료 4종(인증·어드민·보안·SEO 가이드) + 참고 화면 HTML 전수 검토 후 설계 SSOT(`docs/design.md`, 20장) 작성. 확정 결정 11건(D1~D11), 미결정 7건(M1~M7) 정리. 위챗 탑재는 사용자 지시로 취소, 인플루언서 전환율은 어드민 전용 메뉴로 분리. **구현은 착수하지 않음(설계 지시 범위 준수)**
