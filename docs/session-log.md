# WonjinReservationWeb 세션 로그

> CLAUDE.md의 "세션 요약"에서 밀려난 항목을 시간순으로 누적하는 아카이브.
> CLAUDE.md에는 최신 1건만 인라인으로 남기고, 나머지는 여기에 쌓는다. **삭제 금지.**

- **2026-08-25 (1) 요약 — 프로젝트 착수 및 설계 SSOT 작성**: 하네스 점검(루트 `.claude/settings.json`의 PostToolUse 문법검사 훅 · PreToolUse git push/production 차단 훅 · `WebProject\memory\MEMORY.md` 경로 전부 정상 로드 확인)으로 시작. 진행 중 PostToolUse 훅의 `.cs` 빌드검사가 `Split-Path`로 잡은 부모 폴더에서 `-Recurse`(하위 방향)로만 `.csproj`를 찾아, `Controllers/` 같은 하위 폴더 파일 저장 시 조용히 스킵되는 버그를 발견해 보고(push 차단 훅은 `git rev-parse --show-toplevel` 기준이라 영향 없음 — 수정 여부는 사용자 결정 대기).
  이어 성형외과 예약 시스템 설계 착수. **참고 자료 전수 검토**: `reservation-desk_1.html`(1123줄, 참고 화면 원본) · `auth-pattern-reference.md`(1983줄) · `admin-panel-pattern-reference.md`(862줄) · `seo-pattern-reference.md`(686줄) · `web-security-audit-guide.md`(227줄) · `MeiyantongWeb/CLAUDE.md`.
  **사용자 결정 4건**: ①단일 병원 전용 ②**위챗 탑재 취소**(랜딩엔 폼만) ③예약금은 실장 수동 입금 확인만 ④유입 경로는 UTM/추천코드 자동 기록 + 인플루언서 전환율은 최고 어드민 전용 메뉴로 분리.
  **산출물**: `docs/design.md`(20장 설계 SSOT) — 확정 결정 11건(D1~D11), DB 스키마 8개 테이블 + 인덱스 전수 역산, 입력 길이 3곳 일치표, 예약 상태 머신 5상태, API 명세, 화면 설계, 감사 로그(3역할 전부 대상), 유입 경로 일별 집계 설계, 보안 체크리스트, 성능 설계, 법적 리스크 `[미확인]` 4건, Phase 0~9 계획, 미결정 7건(M1~M7).
  **설계 판단 근거 기록**: (a) 화면 깜빡임 금지 원칙을 SSR 프리로드로 이행하려면 SSR 요청에 인증 쿠키가 실려야 하므로 **동일 출처 API 프록시가 선택이 아닌 전제**임을 확인 — 부수 효과로 쿠키를 `SameSite=Lax`로 좁힐 수 있고 OAuth를 안 쓰므로 프록시 우회 예외 경로 문제도 없음. (b) 실장을 별도 테이블로 만들지 않고 `users.role='Consultant'` + 운영 컬럼 2개로 처리(1:1 관계 테이블은 과설계). (c) 유입 경로를 방문당 1행이 아니라 `(날짜 × 캠페인 조합)`당 1행 UPSERT로 설계해 광고 트래픽에도 행이 폭증하지 않게 함. (d) 시술명은 translations 테이블 대신 언어별 컬럼 4개 — 조인 제거 + DB 레벨 길이 제약 확보(언어 추가 시 마이그레이션 필요는 수용). (e) 연락 희망 시간을 자유 텍스트가 아닌 4지선다로 — 고객이 중국어로 자유 입력하면 한국인 실장이 해석 못 하는 실제 문제를 차단. (f) UI 라이브러리 미도입, 네이티브 `<dialog>`·`<input type="date">`·자체 월간 그리드로 처리.
  **지시 범위 준수**: "구현하지말고 설계파일을 남겨줘" 지시에 따라 **코드 구현은 일절 하지 않음**. git 저장소가 없어 `git init` + remote 연결 + 초기 커밋/push까지 수행.
