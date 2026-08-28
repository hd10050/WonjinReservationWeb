# 랜딩페이지 비주얼 리디자인 — 설계 문서 (2026-08-28)

> 관련 문서: [2026-08-27-landing-redesign-design.md](2026-08-27-landing-redesign-design.md) (정보구조·데이터 아키텍처 SSOT, 이번 문서는 그 위에 비주얼 레이어만 다룸 — IA·라우트·데이터 구조 변경 없음)

## 1. 배경 및 목적

사용자 피드백: "랜딩페이지가 너무 초라하고 투박해 딱딱하고 촌스럽다." 콘텐츠(76개 시술·11개 카테고리)는 2026-08-27 세션에서 이미 완성되어 있으나, 화면 자체가 좁은 폭(`max-w-3xl`)·작은 폰트·정적인 레이아웃이라 실제 성형외과 랜딩 특유의 임팩트가 없다.

참고 사이트 2곳을 playwright-cli로 직접 열어 실측 비교(스크린샷·컴퓨티드 스타일 확인, 추정 없음):
- `k-wonjin.co.kr` — 같은 병원(원진성형외과의원)의 국내용 기존 사이트. 풀블리드 사진 히어로·대형 세리프 타이포·스크롤 애니메이션.
- `idhospital.com` — 트렌디한 그라디언트·큰 볼드 타이포 참고용(다른 병원, 문구·구조는 참고하지 않고 색감·타이포 인상만 참고).

## 2. 진단 (실측)

| 항목 | 현재 | 레퍼런스 |
|---|---|---|
| 홈 히어로 | 이미지 0장, `<h1>` 텍스트 한 줄 | 풀블리드 100vh 사진 + 오버레이 텍스트 |
| 콘텐츠 폭 | 전 페이지 `max-w-3xl`(768px) — 1440px 화면의 53%만 사용 | 100vw 풀블리드 |
| 최대 폰트 | `text-3xl`(30px), 시스템 폰트만 | 100~140px 디스플레이, 전용 세리프 웹폰트(`Ivy`) |
| 모션 | `transition-colors` 하나뿐. 스크롤 리빌·패럴랙스 전무 | 스크롤 리빌·패럴랙스·캐러셀 |
| 카테고리 목록 | 24px 아이콘 + 텍스트 박스 11개 | 대형 사진 카드 |
| 배경색(실측) | `#FEFDF7` 웜크림 (hue 98.9°) | `k-wonjin` 본문 `#FFFFFF` 순백, 섹션 밴드 `#F8F8F8`/`#F0F0F0` |

자산 현황: `frontend/public/img/`에 72장 보유(히어로 13장 포함), **홈 화면은 1장도 사용하지 않음**. 자산 추가 수급 없이 기존 사진만으로 재구성 가능.

## 3. 범위

### 포함
- 색 토큰: `--background`/`--muted`/`--border`/`--input` 중립화 (브랜드색 5종은 무변경)
- 레이아웃 폭: `max-w-3xl` → 풀블리드 섹션 + `max-w-6xl` 콘텐츠 섹션
- 홈(`pages/index.vue`) 히어로 전면 재작성 (크로스페이드 + Ken Burns)
- 카테고리 목록 히어로(`procedures/[category]/index.vue`) 풀블리드 확대
- 시술 상세(`procedures/[category]/[procedure].vue`) 레이아웃 확대
- `LandingHeader.vue` 히어로 오버레이형 헤더(스크롤 시 전환)
- 스크롤 리빌 모션 컴포저블 신규 (`useScrollReveal`)
- 라틴 디스플레이 세리프 폰트 셀프호스팅 1종(2 weight)

### 제외 (범위 외)
- 정보구조·라우트·데이터 구조 변경 — [2026-08-27 스펙](2026-08-27-landing-redesign-design.md) 그대로
- 어드민 패널(9메뉴) 디자인 변경 — `--background` 등 전역 토큰 변경의 반사적 영향만 확인
- 이미지 자산 신규 수급·WebP 변환
- `inquiry.vue` 폼 자체의 기능·레이아웃 (헤더/푸터/배경 전역 토큰만 자동 반영)
- 애니메이션 라이브러리 도입 (GSAP·Framer Motion 등 — 아래 8절 근거)

## 4. 색 토큰 (전부 sRGB→OKLCH 직접 계산, 추정값 아님)

`frontend/app/assets/css/main.css`의 `:root` 블록만 대상. `--primary`(#606C38 올리브)·`--secondary`(#DDA15E 탄)·`--destructive`(#BC6C25 번트오렌지)·`--foreground`(#283618 짙은 산림녹)·`--accent`는 **전부 무변경**(D20 팔레트 유지, 사용자 확정).

| 토큰 | 현재 값 | 변경 후 | 근거 |
|---|---|---|---|
| `--background` | `oklch(0.994 0.008 99.8)` (`#FEFDF7`) | `oklch(1 0 0)` (`#FFFFFF`) | `k-wonjin.co.kr` 본문 배경 실측값과 동일 |
| `--muted` | `oklch(0.94 0.02 100)` | `oklch(0.9791 0 89.9)` (`#F8F8F8`) | `k-wonjin.co.kr` 섹션 밴드 실측값 |
| `--border` | `oklch(0.88 0.02 100)` | `oklch(0.9368 0.0029 264.5)` (`#E9EAEC`) | 중립 회색(크림 hue 제거) |
| `--input` | `oklch(0.88 0.02 100)` | `oklch(0.9368 0.0029 264.5)` (`#E9EAEC`) | `--border`와 동일 값 유지 |
| `--card` | `oklch(1 0 0)` | 무변경 | `Card.vue`가 `border` + `shadow-sm`을 이미 가지므로 흰 배경 위에서도 카드 구분 유지 확인(코드 확인 완료) |

🔴 **리스크**: `--background`는 전역 CSS 변수라 어드민 9개 화면(대시보드·예약상세·KPI 등)에도 동일 적용된다. 구현 후 어드민 화면 전체를 최소 1회씩 훑어 카드·배경 구분이 여전히 유지되는지 확인 필수(체크리스트 12절).

## 5. 레이아웃 골격

- **폭**: 풀블리드가 필요한 섹션(히어로, 배너)은 `w-full`, 텍스트·리스트 콘텐츠 섹션은 `max-w-3xl` → **`max-w-6xl`**(1152px)로 교체. 랜딩 전용이며 어드민 레이아웃(`layouts/admin.vue`)은 대상이 아님.
- **헤더**(`LandingHeader.vue`): 히어로 이미지 위에서는 투명 배경 + 흰 텍스트로 오버레이, 스크롤 시 흰 배경 헤더로 전환.
  - ⚠️ 이 컴포넌트는 **`/admin/login` 페이지와 공유**(12-2절, `components/LandingHeader.vue` 상단 주석 확인). prop(`overlay?: boolean` 등)으로 분기하여 로그인 페이지는 기존 고정 흰 배경 헤더 그대로 유지 — 회귀 확인 필수.

## 6. 홈 (`pages/index.vue`) 재작성

| 섹션 | 내용 |
|---|---|
| 히어로 | `min-h-[88vh]` 풀블리드. `eye-hero.jpg`/`nose-hero.jpg`/`contour-hero.jpg`/`lifting-hero.jpg` 4장 크로스페이드(5초 간격) + Ken Burns(`scale(1)→scale(1.08)`, 8초). 대형 세리프 영문 카피 + CJK 카피 등장 애니메이션 |
| 카테고리 | 아이콘 박스 11개 → 대형 사진 카드 그리드(`grid-cols-3`→`sm:grid-cols-2`→`grid-cols-1`), hover 시 이미지 줌 + 이름 슬라이드업 |
| 소개 | `--muted`(`#F8F8F8`) 배경 밴드 + 대형 타이포 |
| CTA | 문의 유도 배너 (`/inquiry` 링크) |

**이미지 무게 대응**: 크로스페이드 1번째 장만 `fetchpriority="high"`/eager, 나머지는 유휴 시점 로드(`loading="lazy"` 또는 `requestIdleCallback`). `stemcell-hero.png`(1.45MB, PNG)는 무거워서 **크로스페이드 후보에서 제외**(JPEG 히어로만 사용).

## 7. 카테고리 목록 / 시술 상세

- `procedures/[category]/index.vue`: 히어로 `min-h-80`(320px) → `min-h-[70vh]` 풀블리드, 텍스트 등장 애니메이션(fade+translateY).
- `procedures/[category]/[procedure].vue`: 2컬럼 유지, 이미지 비중 확대.
- 시술 이미지는 누끼(흰 배경 PNG)라 기존 크림 배경(`#FEFDF7`)에서 사각 경계가 도드라졌음 — 배경이 순백(`#FFFFFF`)이 되면서 자연 해소(4절 배경색 변경의 부수 효과).

## 8. 모션 — 라이브러리 추가 없음

**근거**: 이번 스코프에 필요한 모션 3종은 전부 표준 CSS/네이티브 API로 커버됨.

| 모션 | 구현 | 라이브러리 필요 여부 |
|---|---|---|
| 히어로 크로스페이드 | CSS `@keyframes` + `opacity`, `animation-delay` 순차 | 불필요 |
| Ken Burns 줌 | CSS `@keyframes` + `transform: scale()` | 불필요 |
| 스크롤 리빌 | 네이티브 `IntersectionObserver` + CSS transition | 불필요 |

GSAP·Framer Motion 등이 제공하는 스프링 물리·스크롤 스크러빙·정교한 타임라인 제어는 이번 설계(사진 크로스페이드 + 등장 페이드업)에 불필요 — 스크롤 스크러빙형 패럴랙스나 페이지 전환 애니메이션이 추후 요구되면 그때 별도 검토.

- `useScrollReveal` 컴포저블 신규(`app/composables/`): `IntersectionObserver` 1개로 `data-reveal` 요소들의 등장을 감지, `.is-visible` 클래스 토글.
- 🔴 `prefers-reduced-motion: reduce` 존중 — 해당 시 크로스페이드/Ken Burns/리빌 트랜지션 전부 즉시 완료 상태로 표시(접근성).
- 🔴 **화면 깜빡임 금지 원칙 준수**: above-the-fold(첫 히어로)는 애니메이션 대상에서 제외하고 SSR 첫 렌더에 항상 보이는 상태로 표시. 스크롤 리빌은 fold 아래 요소만 대상이며, 초기 `opacity:0`은 클래스 바인딩이라 JS 실패 시에도 CSS 폴백으로 콘텐츠가 보임(`@media (prefers-reduced-motion), (script-off)` 방식 또는 `<noscript>` 대응은 구현 단계에서 확정).

## 9. 폰트

- **CSP 제약**: `frontend/server/middleware/csp-nonce.ts`에 `font-src 'self'` — 외부 CDN 폰트(Google Fonts 링크 방식) 직접 사용 불가, 반드시 셀프호스팅.
- **선택**: 라틴 디스플레이 세리프만 셀프호스팅, CJK(zh-CN 기본 로케일 포함 4개 언어)는 시스템 폰트 스택 유지 — 서브셋 CJK 웹폰트는 수백KB~수MB로 광고 랜딩 첫 로딩 지연 유발.
- **확정 폰트**: Playfair Display (OFL 라이선스, 상업적 사용 무료).
  - `frontend/public/fonts/playfair-display-latin-700.woff2` (23.2KB) — Bold
  - `frontend/public/fonts/playfair-display-latin-900.woff2` (22.4KB) — Black
  - 출처: `fonts.gstatic.com` (Google Fonts v40), latin subset만(cyrillic·vietnamese·latin-ext 제외)
  - 다운로드 완료(2026-08-28, 이 세션). `@font-face` 선언·`font-display: swap` 적용은 구현 단계에서 진행.
- CJK는 시스템 폰트 스택(`-apple-system, "PingFang SC", "Microsoft YaHei", sans-serif` 등, 구현 단계에서 확정) + 큰 사이즈·굵기·자간으로 임팩트 확보.

## 10. 검증 계획

- `docker compose up -d --build`로 격리 재빌드 후 로컬 확인(로컬 dev 컨테이너는 바인드마운트 없음 — 프로젝트 CLAUDE.md 기존 함정).
- 4개 로케일(zh-CN·zh-TW·en·ko) 전부 홈/카테고리/상세 화면 육안 확인.
- `resize_window`로 모바일(375px)·태블릿·데스크톱 3개 뷰포트 확인 — 기존 375px 헤더 깨짐 이력(`LandingHeader.vue` 주석) 재발 여부 특히 확인.
- 어드민 9개 화면 배경·카드 구분 육안 확인(4절 리스크 대응).
- `/admin/login` 헤더가 오버레이형으로 잘못 바뀌지 않았는지 확인(5절 공유 컴포넌트 리스크 대응).
- `prefers-reduced-motion: reduce` 에뮬레이션 후 애니메이션 비활성 확인.
- Lighthouse 또는 `read_network_requests`로 홈 페이지 초기 로드 페이로드 확인(히어로 이미지 지연 로드 검증).

## 11. 기각한 대안

- **전체 팔레트 신규 교체**: 어드민 9개 화면까지 전부 재검증 필요, 사용자가 "팔레트는 그대로, 레이아웃·타이포·모션만 개선"으로 명시적 기각.
- **CJK 포함 풀 웹폰트 셀프호스팅**: 서브셋해도 수백KB, 광고 랜딩 첫 로딩 지연 — 기각.
- **애니메이션 라이브러리 도입**: 8절 근거로 기각.
- **병원 신규 촬영 홈 히어로**: 자산 수급까지 작업이 멈춤 — 기존 카테고리 히어로 4장 크로스페이드로 대체.

## 12. 미결정 (구현 단계에서 확정)

- [ ] `prefers-reduced-motion` 폴백의 정확한 CSS 패턴(미디어쿼리 vs `<noscript>`)
- [ ] CJK 시스템 폰트 스택의 정확한 폰트명 목록(OS별)
- [ ] 헤더 오버레이→고정 전환 스크롤 임계값(px 또는 vh 비율)
- [ ] 크로스페이드 히어로 4장 최종 선정(위 4개 후보 중 실제 화면비 확인 후 조정 가능)
