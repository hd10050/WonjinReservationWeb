# WonjinReservationWeb 설계서 (SSOT)

> 원진성형외과 외국인 고객 예약·상담 관리 시스템의 **단일 진실 공급원(SSOT)**.
> 설계 결정이 바뀌면 코드보다 이 문서를 먼저 고칠 것.
> 상위 규칙 상속: `C:\Users\jinho\Desktop\WebProject\CLAUDE.md`
> 작성일: 2026-08-25 / 상태: **설계 확정 대기** (구현 착수 전)

---

## 0. 이 문서가 지키는 절대 원칙 (루트 CLAUDE.md)

이 설계는 아래 절대 원칙을 전 구간에서 강제한다. 각 원칙이 실제로 어느 절에서 이행되는지 매핑한다.

| 절대 원칙 | 이행 절 |
|---|---|
| DB 쿼리 성능 원칙 (인덱스·페이징·필요 컬럼만) | 8장(인덱스 전수), 11장(전 목록 API 페이징), 17장 |
| 화면 깜빡임 금지 (SSR 프리로드·전환 오버레이) | 4장(동일 출처 프록시), 13장 |
| 입력 필드 길이 제한 (DB·백엔드·프론트 3곳 일치) | 9장 |
| 디자인 원칙 (보이는 label 필수) | 12장 |
| 코딩 규칙 (코드 잘림 금지·영향범위 선설명) | 19장 |
| 번역 규칙 (4개 로케일 키 집합 일치, 태그·변수 원형 유지) | 5-6절 |
| 절대 원칙(반복 금지) — 불확실한 것은 `[미확인]` 표시 | 문서 전반 |

---

## 1. 개요 · 범위

**한 줄 정의**: 광고로 유입된 중화권 고객이 랜딩페이지의 폼으로 상담을 신청하면, 병원 실장이 위챗으로 연락해 상담·방문예약을 확정하고, 그 전 과정을 관리자 페이지에서 추적·감사·집계하는 시스템.

**핵심 흐름**

```
① 광고 집행 (인플루언서 / 캠페인)
        ↓  UTM · 추천코드가 붙은 링크
② 랜딩페이지 도착 (zh-CN / zh-TW / EN / KO)
        ↓  [이름 · 생년월일 · 성별 · 위챗ID · 연락 희망 시간대] 입력 + 개인정보 동의
③ 예약 신청 접수 (status = New, 유입 경로 자동 기록)
        ↓
④ 실장이 위챗으로 직접 연락 · 상담 (status = Consulting)
        ↓  상담 기록 · 시술 결정 · 방문일시 · 예약금 입금 확인 입력
⑤ 예약 확정 (status = Confirmed)
        ↓
⑥ 고객 내원 (status = Visited)  /  또는 취소 (status = Cancelled)
```

**범위에 포함**
- 4개 언어 공개 랜딩페이지 + 예약 신청 폼
- 3역할(어드민 / 병원관리자 / 병원실장) 관리자 패널 9개 메뉴
- 예약 상태 머신, 감사 로그, 통계·KPI, 유입 경로 분석

**범위에서 제외** (요구되지 않았으므로 만들지 않음 — 필요해지면 그때 추가)
- 고객 회원가입·로그인 (고객은 계정 없이 폼만 제출)
- 위챗 아이콘·QR·공식계정 연동 (**2026-08-25 사용자 지시로 취소** — 랜딩엔 폼만 둔다)
- PG 결제 연동 (예약금은 실장이 수동으로 입금 확인 체크만)
- 고객 자동 알림(메일·SMS·푸시)
- 다중 병원(멀티테넌트)

---

## 2. 확정된 설계 결정 (2026-08-25)

| # | 결정 | 근거 · 영향 |
|---|---|---|
| D1 | **단일 병원 전용** | 전 테이블에 `hospital_id` 없음. 소유자 필터·IDOR 방어 부담이 구조적으로 사라짐. 병원이 늘어나면 마이그레이션 필요(트레이드오프 수용) |
| D2 | **위챗 탑재 취소** | 랜딩엔 헤더·푸터·예약 폼만. 고객이 입력한 위챗ID로 실장이 자기 위챗 앱에서 먼저 연락한다 |
| D3 | **예약금은 수동 입금 확인만** | `deposit_amount` + `deposit_currency` + `deposit_paid` 세 컬럼. PG·웹훅·환불 로직 없음 |
| D12 | **예약금 통화는 CNY / KRW 선택, 기본값 CNY** | 실장이 실제로 받은 통화를 그대로 기록한다. **환율 환산은 하지 않는다** — 환산하려면 "언제 시점의 환율인가"를 정하고 환율 소스를 붙여야 하는데, 입금 시점과 조회 시점 환율이 달라 금액이 계속 변하는 지표가 되기 때문. 나중에 통계에 예약금 합계를 넣게 되면 **통화별로 분리 집계**하고 서로 다른 통화를 절대 합산하지 않는다 |
| D4 | **유입 경로 자동 기록** | 랜딩 진입 시 UTM·추천코드를 일별 집계로 기록, 예약 레코드에도 스냅샷 저장 |
| D5 | **인플루언서 전환율은 어드민 전용 메뉴로 분리** | `/admin/referrals` — 병원관리자·실장에게 노출 금지 |
| D6 | **고객 회원가입 없음 / 관리자 계정은 발급제** | `POST /api/auth/register` 엔드포인트 자체를 만들지 않는다. 계정 생성 경로는 어드민의 `POST /api/admin/users` 하나뿐 |
| D7 | **동일 출처 API 프록시 채택** | 화면 깜빡임 금지 원칙을 SSR 프리로드로 이행하려면 SSR 요청에 인증 쿠키가 실려야 하기 때문. 13장 참고 |
| D8 | **🔴 실장은 `consultants` 독립 테이블 — 계정(`users`)과 1:1이 아니다** | [실장 관리]에서 CRUD하는 **마스터 데이터**이며 로그인 계정과 완전히 별개다. 계정 없는 실장이 존재할 수 있고(병원관리자가 대신 배정·입력), 계정이 있다고 실장인 것도 아니다. **두 테이블 사이에 FK 연결을 두지 않는다** — `users.role='Consultant'`는 "로그인 권한 등급"일 뿐 "이 사람이 그 실장"이라는 뜻이 아니다. (2026-08-25 정정: 초안에서 이 둘을 하나로 합쳤던 것은 오설계) |
| D13 | **실장은 하드 삭제 불가 — `is_active=false` 비활성화만** | 삭제하면 그 실장이 담당했던 과거 예약의 담당자 정보와 KPI 이력이 통째로 사라진다. 비활성 실장은 **신규 배정 드롭다운·실장 KPI·예약 통계에서 제외**되지만, 이미 그 실장이 담당한 예약의 상세 화면과 처리 이력에는 이름이 그대로 남는다 |
| D14 | **상담 기록은 덮어쓰기가 아니라 누적** | `reservation_notes` 테이블에 작성자·시각과 함께 여러 건을 쌓는다. 상담이 여러 차례 오가는 업무라 단일 컬럼 덮어쓰기는 이전 내용을 잃는다. 삭제는 불가, 수정은 작성자 본인과 어드민만 |
| D9 | **시술명은 언어별 컬럼 4개** | `procedures` 테이블에 `name_zh_cn`/`name_zh_tw`/`name_en`/`name_ko`. 조인 없음 + DB 레벨 길이 제약(9장) 확보. 언어 추가 시 마이그레이션 필요(수용) |
| D10 | **연락 희망 시간은 자유 텍스트가 아니라 4지선다** | 고객이 중국어로 자유 입력하면 한국인 실장이 해석 못 하는 실제 문제가 생김. `morning`/`afternoon`/`evening`/`anytime` 코드로 저장하고 실장 화면엔 실장 언어로 표시 |
| D11 | **UI 컴포넌트 라이브러리 미도입** | Tailwind v4만 사용. 모달은 네이티브 `<dialog>`(포커스 트랩 내장), 날짜 입력은 `<input type="date">`, 예약 달력은 자체 월간 그리드. 필요해지면 그때 도입 |

---

## 3. 기술 스택

| 레이어 | 기술 | 비고 |
|---|---|---|
| 프론트 | Nuxt 4 + Vue 3 **Composition API** + Tailwind v4(`@tailwindcss/vite`) | Options API 혼용 금지 |
| 다국어 | `@nuxtjs/i18n` (v10 계열) | `prefix_except_default`, 기본 `zh-CN` |
| SEO | `@nuxtjs/sitemap` + `@nuxtjs/robots` | 5장 |
| 백엔드 | ASP.NET Core 10 + EF Core + `EFCore.NamingConventions`(스네이크케이스) | |
| DB | PostgreSQL 16, 스키마 `wonjin` | |
| 인증 | 자체 JWT(AT 15분) + Refresh Token(7일, SHA-256 해시) | 소셜 로그인 없음 |
| 배포 | 프론트 Cloudflare Workers / 백엔드·DB Render | 4장 |
| 로컬 | `docker compose up` | 포트는 4-2절 |

> ⚠️ **버전 고정 주의**: `package-lock.json`은 **npm@10.9.2**로 생성해 커밋할 것. npm 11로 만든 lockfile은 Cloudflare CI에서 `EBADPLATFORM`으로 실패한 전례가 있다.

---

## 4. 배포 · 도메인 구조

### 4-1. 동일 출처 API 프록시 (D7)

```
브라우저
   │  모든 요청이 프론트 도메인 하나로만 나감 (인증 쿠키가 항상 퍼스트파티)
   ▼
Cloudflare Workers (Nuxt/Nitro)
   ├─ 페이지 SSR
   └─ server/api/[...].ts  ──프록시──►  Render (ASP.NET Core)  ──►  PostgreSQL
```

- 프론트 브라우저용 `NUXT_PUBLIC_API_BASE`는 **빈 문자열**(동일 출처). `nuxt.config.ts`에서 반드시 `??`로 폴백할 것 — `||`를 쓰면 빈 문자열이 falsy로 걸려 API 절대 URL로 되돌아가 프록시가 무력화된다.
- SSR(서버→서버) 호출은 `NUXT_API_BASE_INTERNAL`(Render 절대 주소)를 그대로 사용.
- 프록시는 `redirect: 'manual'` 필수. 기본값(follow)이면 프록시가 3xx를 대신 소비해 외부 페이지 HTML을 우리 도메인 응답으로 돌려주고, 우리 CSP가 그 문서에 적용되어 깨진다.
- **이 프로젝트는 OAuth를 쓰지 않으므로**(D6) OAuth correlation 쿠키·프록시 우회 예외 경로 문제가 애초에 없다.

### 4-2. 로컬 개발 포트

| 서비스 | 포트 |
|---|---|
| frontend | 3700 |
| api | 5200 |
| postgres (호스트) | 5435 |

> ⚠️ Windows에서 포트 바인딩이 조용히 실패하면 `netsh interface ipv4 show excludedportrange protocol=tcp`로 Hyper-V 예약 범위부터 확인할 것. 겹치면 `docker-compose.override.yml`(gitignore 대상)에 `ports: !override`로 우회하고, 프론트 포트를 바꿨다면 백엔드 `Cors__AllowedOrigins`에도 그 포트를 추가해야 상태변경 요청이 CSRF Origin 검증에 막히지 않는다.

### 4-3. 브랜치 정책

- **자동배포 감시 브랜치는 `main`으로 확정**(2026-08-25 사용자 결정). 배포 구조를 바꿀 일이 생기면 사용자가 직접 변경한다.
- Claude는 **`main`에만 push**한다.

---

## 5. 다국어(i18n) · SEO 설계

### 5-1. 로케일 정의

| 코드 | hreflang | 표시명 | URL |
|---|---|---|---|
| `zh-CN` (기본) | `zh-CN` | 简体中文 | `/` |
| `zh-TW` | `zh-TW` | 繁體中文 | `/zh-tw/...` |
| `en` | `en-US` | English | `/en/...` |
| `ko` | `ko-KR` | 한국어 | `/ko/...` |

- `strategy: 'prefix_except_default'` — 기본 언어는 프리픽스 없음. SEO상 URL별로 언어가 고정된다.
- `useLocaleHead({ seo: true })`로 hreflang alternate + `<html lang>` 자동 생성.

### 5-2. 🔴 `detectBrowserLanguage`는 반드시 `false`

모듈 옵션을 켜두면 `/` 접속 시 `Accept-Language` 기준으로 302 리다이렉트가 발생하고, **카카오톡·라인·페이스북 등 링크 미리보기 봇이 그 리다이렉트를 따라가 엉뚱한 언어의 og:description을 노출**한다. `redirectOn: 'root'`로도 막히지 않는다.

### 5-3. 브라우저 언어 감지 — `<head>` 동기 인라인 스크립트로만

`detectBrowserLanguage`를 끈 대신, 자동 감지는 아래 방식으로만 구현한다. 크롤러는 스크립트를 실행하지 않으므로 `/`는 항상 SSR 그대로(zh-CN) 응답된다.

```js
// nuxt.config.ts — app.head.script 배열의 첫 번째 항목
(function () {
  try {
    if (location.pathname !== '/') return;               // 루트 진입일 때만 판단
    var manual = /(?:^|; )wj_lang_manual=1(?:;|$)/.test(document.cookie);
    var t;
    if (manual) {
      var m = document.cookie.match(/(?:^|; )wj_lang=([^;]*)/);
      t = m ? decodeURIComponent(m[1]) : 'zh-CN';
    } else {
      var l = navigator.language || '';
      if (l.indexOf('ko') === 0) t = 'ko';
      else if (l === 'zh-TW' || l === 'zh-HK' || l === 'zh-Hant' || l.indexOf('zh-Hant') === 0) t = 'zh-TW';
      else if (l.indexOf('zh') === 0) t = 'zh-CN';
      else t = 'en';
      document.cookie = 'wj_lang=' + t + '; expires=' + new Date(Date.now() + 31536000000).toUTCString() + '; path=/; samesite=lax';
    }
    if (t !== 'zh-CN') location.replace('/' + (t === 'zh-TW' ? 'zh-tw' : t) + location.search);
  } catch (e) {}
})();
```

**반드시 지킬 것**
- `wj_lang`(감지·선택 결과)과 `wj_lang_manual`(수동 선택 여부)을 **분리**한다. 하나로 합치면 "자동 감지"가 아니라 "최초 1회 영구 고정"이 되어, 사용자가 나중에 브라우저 언어를 바꿔도 반영되지 않는다.
- 헤더의 언어 선택 UI는 **같은 raw `document.cookie` 포맷**으로 두 쿠키를 함께 기록해야 한다. Nuxt `useCookie()`는 문자열을 JSON 인코딩(`"ko"`)해 저장하므로 위 스크립트의 파싱(`ko`)과 어긋난다.
- `location.search`를 반드시 보존한다 — **UTM 파라미터가 여기 실려 있어, 빠뜨리면 유입 경로 추적이 통째로 깨진다.**
- `/zh-tw`, `/en`, `/ko`로 이미 들어온 요청은 절대 건드리지 않는다(공유 링크·광고 링크가 사용자 의도).

### 5-4. 계정 locale

- `users.locale`에 계정별 선호 언어를 저장한다.
- **최초 로그인 시** 클라이언트가 감지한 로케일(`wj_lang`)을 로그인 요청 body에 함께 보내, 서버는 `users.locale`이 비어 있을 때만 채운다(이미 값이 있으면 사용자의 명시적 선택이므로 덮어쓰지 않는다).
- 변경 전용 엔드포인트 `PATCH /api/auth/me/locale`을 두고, 저장 성공 시 `wj_lang` 쿠키도 같은 값으로 즉시 동기화한다.
- 관리자 화면은 `definePageMeta({ i18n: false })`로 URL 프리픽스 라우팅에서 제외하되, **화면 언어 자체는 계정 locale을 따른다**(전용 컴포저블 `useOpsLocale()` 하나로 통일).

> ⚠️ `i18n: false` 화면에서 로케일 JSON을 직접 import해야 한다면 반드시 `import raw from '~/i18n/locales/ko.json?raw'` + `JSON.parse(raw)`를 쓸 것. `?raw` 없이 import하면 `@intlify/unplugin-vue-i18n`이 JSON을 vue-i18n 컴파일 AST로 변환해 SSR/클라이언트 하이드레이션 mismatch가 난다.

### 5-5. SEO 설정

```ts
sitemap: {
  hostname: process.env.NUXT_PUBLIC_SITE_URL,
  sources: ['/api/_sitemap-urls'],           // server/api/ 하위면 /api/ 프리픽스 필수
  exclude: ['/admin/**'],
},
robots: {
  groups: [{ userAgent: ['*'], allow: ['/'], disallow: ['/admin/'] }],  // 트레일링 슬래시 필수
  sitemap: `${process.env.NUXT_PUBLIC_SITE_URL}/sitemap.xml`,
},
```

- 🔴 `disallow`는 **prefix 매칭**이다. `/admin`(슬래시 없음)으로 쓰면 `/admin`으로 시작하는 다른 경로까지 함께 막히고, `@nuxtjs/sitemap`이 같은 규칙을 sitemap exclude에도 통합 적용해 **동적 URL이 원인불명으로 누락**된다.
- 관리자 전 페이지에 `noindex, nofollow` 메타를 **레이아웃 단위로** 부착한다(robots.txt disallow만으로는 외부 링크를 통한 색인을 못 막는다).
- 랜딩 JSON-LD는 `MedicalClinic`(schema.org `MedicalOrganization` 하위) + `Organization`을 `@graph`로 묶는다. **배포 후 Google Rich Results Test로 실제 인식 여부를 검증할 것** — 타입 인식 결과는 검증 전까지 `[미확인]`.
- JSON-LD 삽입 시 `innerHTML` 사용 + `JSON.stringify(...).replace(/</g, '\\u003c')` 이스케이프 필수(`children` 속성으로 넣으면 본문이 비고, `<` 미이스케이프는 저장형 XSS가 된다).
- Search Console에는 `sitemap.xml`이 아니라 **`sitemap_index.xml`**을 제출한다.

### 5-6. 번역 규칙 (절대 원칙)

- 로케일 파일은 `i18n/locales/{zh-CN,zh-TW,en,ko}.json` 4개.
- **4개 파일의 키 집합이 항상 완전히 동일해야 한다.** 키를 추가·삭제할 때는 4개 파일을 반드시 세트로 수정하고, 수정 후 키 개수를 직접 대조 확인한다.
- 번역 시 숫자·고유명사·HTML/Vue 태그·변수 플레이스홀더(`{name}` 등)는 원형 그대로 유지한다.
- 🔴 **로케일 JSON 값에 순수 `@` 문자를 넣지 말 것** — vue-i18n이 linked message(`@:key`) 트리거로 오인해 해당 로케일 컴파일 자체가 깨진다(`Invalid linked format`). SSR은 멀쩡한데 클라이언트 라우팅에서만 화면 전체가 raw key로 표시되는 형태로 나타나 원인 파악이 어렵다. 이메일 예시 등이 필요하면 다른 표현으로 대체할 것(작은따옴표 이스케이프는 이 컴파일러에서 동작하지 않음).
- 백엔드 에러 메시지는 한국어 문자열이 아니라 **에러 코드**(`{ code: "INVALID_CREDENTIALS" }`)로 반환하고, 프론트가 `t('errors.' + code)`로 번역한다.

---

## 6. 역할 · 권한(RBAC)

### 6-1. 역할 정의

| `users.role` | 한국어 명칭 | 담당 |
|---|---|---|
| `Admin` | 어드민(프로젝트 관리자) | 계정 발급, 사이트 관리, 감사 로그, 유입 경로 분석 |
| `HospitalManager` | 병원관리자 | KPI·통계 확인, 실장·시술 마스터 관리 |
| `Consultant` | 병원실장 | 예약 접수·상담 진행·방문예약 확정 |

### 6-2. 메뉴 × 역할 매트릭스

| # | 메뉴 | 경로 | Admin | HospitalManager | Consultant |
|---|---|---|:---:|:---:|:---:|
| 1 | 예약 대시보드 | `/admin` | ✅ | ✅ | ✅ |
| 2 | 예약 상세 | `/admin/reservations/[id]` | ✅ | ✅(읽기) | ✅ |
| 3 | 실장 관리 | `/admin/consultants` | ✅ | ✅ | ❌ |
| 4 | 시술·수술 관리 | `/admin/procedures` | ✅ | ✅ | ❌ |
| 5 | 예약 달력 | `/admin/calendar` | ✅ | ✅ | ✅ |
| 6 | 실장 KPI | `/admin/kpi` | ✅ | ✅ | ❌ |
| 7 | 예약 통계 | `/admin/stats` | ✅ | ✅ | ❌ |
| 8 | 계정 관리 | `/admin/users` | ✅ | ❌ | ❌ |
| 9 | 로그(감사) | `/admin/audit-logs` | ✅ | ❌ | ❌ |
| 10 | 유입 경로 분석 | `/admin/referrals` | ✅ | ❌ | ❌ |

> 실장 KPI·예약 통계는 사용자 지시에 따라 **어드민/병원관리자 전용**이다. 실장 본인에게도 자기 KPI를 보여줄지는 요구되지 않았으므로 만들지 않는다.

### 6-3. 🔴 권한 구현 시 반드시 지킬 것

1. **컨트롤러 단위로 다중 role을 열 때, 그 안의 모든 쓰기 액션(POST/PUT/PATCH/DELETE)을 하나씩 다시 점검한다.** ASP.NET Core는 액션의 `[Authorize]`가 컨트롤러의 것을 **완전히 덮어쓴다**(합쳐지지 않는다). "이 메뉴는 조회 위주니까 열어도 되겠지"라고 컨트롤러 레벨에서만 판단하면 같은 컨트롤러 안의 쓰기 액션까지 하위 역할에게 열리는 권한 상승 버그가 된다.
2. **버튼을 역할별로 숨겼다면 그 API도 액션 레벨에서 같이 잠근다.** 버튼만 숨기고 API를 안 잠그면 개발자 도구로 우회된다. 프론트 숨김은 UX, 백엔드 `[Authorize]`가 실제 방어선.
3. 프론트 가드는 `middleware/admin.ts` 하나에 **역할별 허용 경로 화이트리스트**로 구현한다.

```ts
// app/middleware/admin.ts
// ⚠️ 동일 출처 프록시(D7)라 SSR 요청에도 인증 쿠키가 실린다 → SSR 스킵(import.meta.server return) 금지.
//    스킵하면 인증 체크 자체가 무력화된 구멍이 된다.
const ALLOWED: Record<string, string[]> = {
  HospitalManager: ['/admin', '/admin/reservations', '/admin/consultants', '/admin/procedures', '/admin/calendar', '/admin/kpi', '/admin/stats'],
  Consultant:      ['/admin', '/admin/reservations', '/admin/calendar'],
}

export default defineNuxtRouteMiddleware((to) => {
  const { user } = useAuth()
  if (!user.value) return navigateTo('/admin/login')
  if (user.value.role === 'Admin') return

  const allowed = ALLOWED[user.value.role]
  if (!allowed) return navigateTo('/admin/login')
  if (allowed.some(p => to.path === p || to.path.startsWith(p + '/'))) return
  return navigateTo('/admin')
})
```

---

## 7. 인증 설계

### 7-1. 토큰 구조

| 항목 | 값 |
|---|---|
| Access Token | JWT, **15분**, HttpOnly 쿠키 `wj_at` |
| Refresh Token | 128자 hex(64바이트 엔트로피), **7일**, HttpOnly 쿠키 `wj_rt` |
| RT DB 저장값 | **SHA-256 해시** (평문 저장 금지) |
| RT Rotation | 갱신마다 신규 발급 + 구 토큰 폐기 |
| 비밀번호 해시 | BCrypt workFactor **12** |
| 쿠키 SameSite | 동일 출처 프록시이므로 **`Lax`**로 좁힌다 (`None` 불필요) |
| ClockSkew | `TimeSpan.Zero` (기본 5분 허용 제거) |

> D7(동일 출처 프록시) 덕분에 쿠키를 `SameSite=Lax`로 발급할 수 있다. 이는 크로스 도메인 구조에서 `None`을 쓸 때보다 CSRF 노출면이 구조적으로 작다. 그럼에도 상태변경 요청의 Origin 검증 미들웨어는 그대로 둔다(방어 심층화).

### 7-2. 엔드포인트

| 메서드 | 경로 | 인증 | 비고 |
|---|---|---|---|
| POST | `/api/auth/login` | 익명 | rate limit `auth`(IP 분당 10회), 정지 계정 차단, `locale` 동봉 가능 |
| POST | `/api/auth/refresh` | 쿠키 | rate limit **전용 정책**(아래 주의), 정지 계정이면 RT 폐기 + 쿠키 삭제 후 401 |
| POST | `/api/auth/logout` | 쿠키 | RT 폐기 + 쿠키 삭제 |
| GET | `/api/auth/me` | AT | `IsSuspended` 실시간 확인 |
| PATCH | `/api/auth/me/password` | AT | 8~64자, 성공 시 `RevokeAllForUserAsync` + 현재 세션만 재발급 |
| PATCH | `/api/auth/me/locale` | AT | 화이트리스트(`zh-CN`/`zh-TW`/`en`/`ko`) 검증 |

> ⚠️ **`refresh`에 로그인용 `auth` 정책을 재사용하지 말 것.** 프론트는 AT 만료(15분) 전에 12분 간격으로 백그라운드 자동 갱신을 돌린다. 이 정상 호출이 IP 분당 10회 한도를 잠식하면 같은 정책을 공유하는 로그인까지 429로 막혀 세션이 통째로 튕긴다. `refresh`는 **사용자 ID로 파티션한 전용 정책**을 별도로 둔다.

> **회원가입 엔드포인트는 존재하지 않는다**(D6). 계정 생성 경로는 `POST /api/admin/users` 하나뿐이며 `[Authorize(Roles="Admin")]`으로 잠긴다. 최초 어드민 계정은 부팅 시 환경변수 기반 시딩으로 1회 생성한다.

### 7-3. 정지·강등 즉시 반영 — 전역 필터

`Me()`/`Refresh()`에만 `IsSuspended` 체크를 두면, 계정을 정지시켜도 **다른 모든 API는 이미 발급된 AT가 만료될 때까지(최대 15분) 계속 통과한다.** 전역 `IAsyncActionFilter`로 매 요청 진입 직전에 DB를 1회 조회해 막는다.

```csharp
// Filters/AccountStateFilter.cs — 핵심 판정부
public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
{
    // ⚠️ [Authorize]를 요구하지 않는 액션은 그냥 통과시킨다.
    //    JWT Bearer는 [Authorize] 여부와 무관하게 유효한 AT 쿠키만 있으면 User를 채우므로,
    //    이 확인을 빠뜨리면 정지된 유저가 공개 랜딩 API조차 401을 맞아 익명 방문자보다 못한 상태가 된다.
    var metadata = context.ActionDescriptor.EndpointMetadata;
    var requiresAuth = metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any();
    if (!requiresAuth) { await next(); return; }

    var principal = context.HttpContext.User;
    if (principal.Identity?.IsAuthenticated != true) { await next(); return; }

    // ⚠️ "sub" 단독 조회 금지 — MapInboundClaims 기본값(true)이 "sub"를 NameIdentifier로 재매핑하므로
    //    NameIdentifier를 먼저 찾고 "sub"로 폴백하는 순서를 반드시 지킬 것.
    var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
    if (!int.TryParse(userIdStr, out var userId)) { await next(); return; }

    // PK 단일 인덱스 조회 — 밀리초 미만. 대부분의 액션이 이보다 무거운 쿼리를 이미 수행한다.
    var current = await db.Users.AsNoTracking()
        .Where(u => u.Id == userId)
        .Select(u => new { u.IsSuspended, u.Role })
        .FirstOrDefaultAsync();

    if (current is null || current.IsSuspended)
    {
        context.Result = new UnauthorizedResult();
        return;
    }

    // 강등·승격 즉시 반영 — 토큰의 Role과 DB의 Role이 다르면 401 → 프론트가 refresh로 새 Role을 받는다
    if (current.Role != principal.FindFirstValue(ClaimTypes.Role))
    {
        context.Result = new UnauthorizedResult();
        return;
    }

    await next();
}
```

- **전역 필터 등록 순서**: `AccountStateFilter`를 `AuditLogFilter`보다 **먼저** 등록한다(정지된 요청은 감사 로그까지 가지 않고 바로 차단).
- 어드민이 역할 변경·정지를 수행하면 그 즉시 `RevokeAllForUserAsync(userId)`로 RT를 전량 폐기한다.
- **자기 자신의 역할 변경·정지는 차단한다** — 안 막으면 아무도 풀 수 없는 상태가 만들어질 수 있다.

### 7-4. 프론트 규칙

- 인증이 필요한 모든 호출은 **예외 없이** `useApi`(GET, useFetch 기반) 또는 `authFetch`(POST/PUT/PATCH/DELETE)로만 한다. raw `$fetch`를 직접 쓰면 401 자동 복구가 안 되어 "화면 이동은 되는데 데이터가 조용히 안 바뀌는" 버그가 된다(실제 사고 사례: 어드민 페이지 8개 전부가 raw `$fetch`였음).
- AT 만료 전 백그라운드 자동 갱신을 12분 간격으로 돌린다.
- 동시 401 발생 시 refresh가 중복 실행되지 않도록 모듈 레벨 싱글턴 Promise로 1회만 실행한다.

> 🔴 **인증 초기화(`fetchMe()`)를 전 페이지에서 실행하지 말 것**(F5). 표준 Nuxt 인증 플러그인은 모든 라우트에서 `fetchMe()`를 호출하지만, **이 프로젝트는 공개 랜딩에 광고 트래픽이 몰리는 구조**라 그대로 두면 방문자 수만큼 `/api/auth/me` 401 요청이 백엔드로 간다(부하 + 로그 오염 + 랜딩 응답 지연). 인증 상태가 필요한 곳은 관리자 화면뿐이므로 경로로 게이팅한다.

```ts
// app/plugins/01.auth.ts
export default defineNuxtPlugin(async (nuxtApp) => {
  // 관리자 경로에서만 인증을 초기화한다. 공개 랜딩은 인증 상태를 알 필요가 없다.
  const path = useRoute().path
  if (!path.startsWith('/admin')) return

  const { fetchMe, user } = useAuth()
  if (import.meta.server) { await fetchMe(); return }
  if (user.value === null) await fetchMe()
})
```
> 관리자 화면은 `i18n: false`(5-4절)라 로케일 프리픽스가 붙지 않으므로 `startsWith('/admin')` 한 줄로 정확히 판별된다 — 공개 화면과 달리 `/ko/admin` 같은 변형이 생기지 않는다.
>
> 백엔드 방어선은 그대로다. 플러그인을 건너뛴다고 인증이 약해지지 않는다 — 관리자 API는 전부 `[Authorize]`로 잠겨 있고, 미들웨어(6-3절)가 `/admin` 진입 시 `user`를 확인한다.

---

## 8. DB 스키마

> 스키마명 `wonjin`. `EFCore.NamingConventions`로 스네이크케이스 자동 변환.
> 🔴 **`__EFMigrationsHistory` 스키마를 반드시 명시 고정할 것**: `npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "wonjin")`. 미지정 시 Postgres `search_path` 규칙 때문에 연결 시점마다 히스토리 테이블 위치가 달라져, 마이그레이션이 매번 재실행되며 `relation already exists`로 컨테이너가 재시작 루프에 빠진다. `HasDefaultSchema()`는 엔티티에만 적용되고 히스토리 테이블엔 먹지 않는 것이 함정.

### 8-1. `users` — 관리자 계정 (고객 계정 없음)

| 컬럼 | 타입 | 제약 |
|---|---|---|
| `id` | int | PK |
| `email` | varchar(254) | **UNIQUE**, 항상 소문자 저장 |
| `password_hash` | varchar(100) | NOT NULL (BCrypt) |
| `role` | varchar(20) | NOT NULL, CHECK `IN ('Admin','HospitalManager','Consultant')` |
| `name` | varchar(30) | NOT NULL |
| `locale` | varchar(10) | NOT NULL DEFAULT `'ko'`, CHECK `IN ('zh-CN','zh-TW','en','ko')` |
| `is_suspended` | boolean | NOT NULL DEFAULT false |
| `created_at` / `updated_at` | timestamptz | NOT NULL |

**인덱스**: `ux_users_email` (UNIQUE), `ix_users_role` — [계정 관리]가 `WHERE role = ?`로 필터한다.

> 🔴 **이 테이블은 "로그인 계정"만 담는다. 실장 마스터 데이터는 `consultants`(8-4)에 따로 있고 두 테이블은 연결되지 않는다**(D8). `role='Consultant'`는 로그인 권한 등급일 뿐이며, 이 값으로 실장 목록을 만들려 하지 말 것.
>
> **계정은 하드 삭제하지 않고 `is_suspended=true`로만 막는다.** 삭제하면 `audit_logs`의 행위자 추적이 끊긴다(감사 로그는 `actor_email`을 별도 보존하지만, 계정을 지울 이유 자체가 없다).

### 8-2. `refresh_tokens`

| 컬럼 | 타입 | 제약 |
|---|---|---|
| `id` | int | PK |
| `user_id` | int | FK → `users.id`, ON DELETE CASCADE |
| `token_hash` | varchar(64) | NOT NULL |
| `expires_at` | timestamptz | NOT NULL |
| `is_revoked` | boolean | NOT NULL DEFAULT false |
| `created_at` | timestamptz | NOT NULL |

**인덱스**: 🔴 `ix_refresh_tokens_token_hash` **(필수)** — 모든 로그인 세션이 12분 간격으로 이 컬럼을 조회한다. 인덱스가 없으면 갱신 요청마다 풀스캔이 되며, 사용자가 체감할 UI 지연이 없어 발견이 매우 늦어진다(실제 사고 사례 있음). `ix_refresh_tokens_user_id` — 전량 폐기·정리 배치용.

### 8-3. `procedures` — 시술·수술 마스터

| 컬럼 | 타입 | 제약 |
|---|---|---|
| `id` | int | PK |
| `code` | varchar(30) | **UNIQUE** (예: `botox`, `rhino`) |
| `name_zh_cn` / `name_zh_tw` / `name_en` / `name_ko` | varchar(50) | NOT NULL |
| `sort_order` | int | NOT NULL DEFAULT 0 |
| `is_active` | boolean | NOT NULL DEFAULT true |
| `created_at` / `updated_at` | timestamptz | NOT NULL |

**인덱스**: `ux_procedures_code` (UNIQUE), `ix_procedures_is_active_sort_order` — 예약 상세의 시술 선택 목록이 `WHERE is_active ORDER BY sort_order`로 조회하므로 복합 인덱스로 커버한다.

> ⚠️ 예약 상세 폼의 시술 체크박스는 **활성 시술만** 노출하되, 편집 화면은 그 예약에 이미 선택된 비활성 시술을 목록에 남겨야 한다 — 빼면 저장 시 조용히 값이 사라진다.

### 8-4. `consultants` — 실장 마스터 (D8 · 계정과 무관한 독립 테이블)

| 컬럼 | 타입 | 제약 |
|---|---|---|
| `id` | int | PK |
| `name` | varchar(30) | NOT NULL — 실장 이름 |
| `team` | varchar(20) | NULL — 소속 팀(참고 화면의 "A팀") |
| `is_active` | boolean | NOT NULL DEFAULT true — **비활성화 = 소프트 삭제**(D13) |
| `sort_order` | int | NOT NULL DEFAULT 0 — 배정 드롭다운 표시 순서 |
| `created_at` / `updated_at` | timestamptz | NOT NULL |

**인덱스**: `ix_consultants_is_active_sort_order` (`is_active`, `sort_order`) — 배정 드롭다운이 `WHERE is_active ORDER BY sort_order`로 조회한다.

> 🔴 **`users`와 FK로 연결하지 않는다**(D8). 실장은 로그인 계정을 가질 수도, 안 가질 수도 있고 그 대응이 1:1이 아니다. 연결이 실제로 필요해지는 요구(예: "실장 본인이 로그인해 자기 예약만 본다")가 생기면 그때 `consultants.user_id`를 추가한다 — 지금은 만들지 않는다.
>
> 🔴 **DELETE 엔드포인트를 만들지 않는다**(D13). 비활성 실장의 노출 규칙은 다음과 같이 **정확히 구분**한다.
>
> | 위치 | 비활성 실장 |
> |---|---|
> | 예약 상세의 담당 실장 드롭다운(신규 배정) | **제외** |
> | 실장 KPI · 예약 통계 | **제외** |
> | 이미 그 실장이 담당한 예약의 상세 화면 표시 | **그대로 표시**(누가 담당했는지는 사실이므로) |
> | 예약 처리 이력(`reservation_logs`) | **그대로 표시** |
> | 대시보드 목록의 담당 실장 필터 | 기본 목록에서는 제외하되, 과거 예약 조회를 위해 "비활성 포함" 옵션 제공 |
>
> ⚠️ 담당 실장 **편집** 드롭다운은 활성 실장만 노출하되, 그 예약에 이미 배정된 비활성 실장은 목록에 남겨야 한다 — 빼면 다른 항목을 고르지 않고 저장했을 때 담당자가 조용히 바뀐다(8-3의 비활성 시술과 같은 함정).

### 8-5. `reservations` — 핵심 테이블

| 컬럼 | 타입 | 제약 · 비고 |
|---|---|---|
| `id` | int | PK |
| `code` | varchar(20) | **UNIQUE** — `WJ-260825-014` 형식 |
| `name` | varchar(50) | NOT NULL — 고객 이름 |
| `birth_date` | date | NOT NULL — 나이는 저장하지 않고 계산 |
| `gender` | varchar(10) | NOT NULL, CHECK `IN ('Female','Male','Other')` |
| `wechat_id` | varchar(50) | NOT NULL |
| `preferred_contact_time` | varchar(20) | NOT NULL, CHECK `IN ('morning','afternoon','evening','anytime')` (D10) |
| `locale` | varchar(10) | NOT NULL — 고객이 신청한 언어. 실장이 응대 언어를 판단하는 근거 |
| `status` | varchar(20) | NOT NULL DEFAULT `'New'`, CHECK `IN ('New','Consulting','Confirmed','Visited','Cancelled')` |
| `consultant_id` | int | NULL, FK → **`consultants.id`** `ON DELETE RESTRICT` (실장은 삭제하지 않으므로 RESTRICT가 안전 — 실수로 삭제를 시도해도 DB가 막는다) |
| `visit_date` | date | NULL — **KST 기준**(9-2절) |
| `visit_time` | time | NULL — **KST 기준**, 타임존 없는 벽시계 시각 |
| `deposit_amount` | numeric(12,2) | NULL, CHECK `>= 0` |
| `deposit_currency` | varchar(3) | NOT NULL DEFAULT `'CNY'`, CHECK `IN ('CNY','KRW')` (D12) |
| `deposit_paid` | boolean | NOT NULL DEFAULT false |
| `cancel_reason` | varchar(200) | NULL |
| `utm_source` / `utm_medium` / `utm_campaign` | varchar(100) | NOT NULL DEFAULT `''` |
| `referral_code` | varchar(50) | NOT NULL DEFAULT `''` |
| `created_at` | timestamptz | NOT NULL — 접수 시각 |
| `updated_at` | timestamptz | NOT NULL |
| `consulting_at` / `confirmed_at` / `visited_at` / `cancelled_at` | timestamptz | NULL — 각 상태 진입 시각(KPI 소요시간 계산용) |

**인덱스** (전부 실제 쿼리에서 역산한 것 — 17장 참고)

| 인덱스 | 커버하는 쿼리 |
|---|---|
| `ux_reservations_code` (UNIQUE) | 예약 코드 조회 |
| `ix_reservations_status_created_at` (`status`, `created_at DESC`) | 대시보드 상태 필터 + 최신순 정렬 |
| `ix_reservations_created_at` (`created_at DESC`) | 필터 없는 전체 목록 정렬, 기간별 통계, **[유입 경로] 기간별 집계** |
| `ix_reservations_visit_date` (`visit_date`) WHERE `status IN ('Confirmed','Visited')` | [예약 달력] 월간 조회 — 부분 인덱스로 크기 최소화 |
| `ix_reservations_consultant_id_status` (`consultant_id`, `status`) | [실장 KPI] 실장별 집계 |

> 🔴 **[유입 경로] 전용 인덱스를 `(referral_code, created_at)`으로 두지 말 것**(F10). 그 화면의 실제 쿼리는 "특정 코드 조회"가 아니라 **"기간 내 전체를 코드별로 그룹"**(`WHERE created_at BETWEEN ? AND ? GROUP BY referral_code, utm_*`)이다. 선행 컬럼이 `referral_code`면 기간 범위 스캔에 쓸 수 없어 인덱스가 놀고, `ix_reservations_created_at`가 이 쿼리를 이미 커버한다. **인덱스는 "이 컬럼을 쓰니까"가 아니라 "실제 쿼리의 WHERE·ORDER BY 순서"에서 역산할 것.**
>
> 🔴 **예약 코드(`code`) 생성은 "그날 최대값 + 1" 방식을 쓰지 말 것**(F4). 광고 유입으로 동시 제출이 겹치면 두 요청이 같은 번호를 읽어 UNIQUE 위반 500이 난다. **전용 시퀀스를 만들어 원자적으로 발급한다.**
>
> ```sql
> -- 일별 리셋이 필요 없다면 전역 시퀀스 하나로 충분하다(가장 단순하고 경쟁 조건이 원천적으로 없음)
> CREATE SEQUENCE wonjin.reservation_code_seq;
> -- 코드 = 'WJ-' + yyMMdd + '-' + lpad(nextval::text, 4, '0')
> ```
>
> 일별 리셋(`-001`부터 다시 시작)을 원한다면 시퀀스만으로는 안 되므로, `INSERT` 후 `ON CONFLICT (code) DO NOTHING` + 재시도 루프(최대 3회)를 쓴다. **어느 쪽이든 "읽고 나서 쓰는" 2단계 구현은 금지**(M3에서 형식 확정 시 함께 결정).

> 🔴 **`dotnet ef migrations add` 결과 파일은 적용 전 반드시 직접 열어 확인할 것.** EF Core scaffolder가 새 복합/부분 인덱스를 기존 단일 컬럼 인덱스의 상위호환으로 오판해 **자동으로 `DropIndex`를 끼워 넣는 사고**가 있었다. 부분 인덱스는 필터 조건을 만족하지 않는 일반 조회를 커버하지 못하므로, 원치 않는 DropIndex가 보이면 모델에 단일 컬럼 인덱스를 명시적으로 재선언해 둘 다 유지시킬 것.

### 8-6. `reservation_procedures` — 예약 ↔ 시술 (M:N)

| 컬럼 | 타입 |
|---|---|
| `reservation_id` | int, FK → `reservations.id` ON DELETE CASCADE |
| `procedure_id` | int, FK → `procedures.id` ON DELETE RESTRICT |

**PK**: (`reservation_id`, `procedure_id`) 복합. **인덱스**: `ix_reservation_procedures_procedure_id` — [예약 통계]의 시술별 집계가 이 방향으로 조회한다(복합 PK의 선행 컬럼이 `reservation_id`라 역방향은 커버되지 않음).

### 8-7. `reservation_notes` — 상담 기록 (누적, D14)

| 컬럼 | 타입 | 비고 |
|---|---|---|
| `id` | int | PK |
| `reservation_id` | int | FK → `reservations.id` ON DELETE CASCADE |
| `body` | varchar(2000) | NOT NULL — 상담 본문 |
| `author_user_id` | int | NULL, FK → `users.id` ON DELETE SET NULL |
| `author_name` | varchar(30) | NOT NULL — 작성 시점 이름 스냅샷(계정이 사라져도 이력 보존) |
| `created_at` / `updated_at` | timestamptz | NOT NULL |

**인덱스**: `ix_reservation_notes_reservation_id_created_at` (`reservation_id`, `created_at`).

> 상담이 여러 차례 오가는 업무이므로 **한 예약에 여러 건이 쌓인다**(D14). 이전 내용을 덮어쓰지 않는다.
> - **삭제 없음** — 잘못 쓴 기록도 남긴다(상담 이력은 분쟁 시 근거가 된다).
> - **수정은 작성자 본인 + 어드민만**. 수정하면 `updated_at`이 갱신되고 화면에 "(수정됨)"을 표시한다.
> - 목록 API에서는 이 테이블을 조회하지 않는다(본문이 커서 목록이 무거워진다). 상세 화면에서만 로드한다.

### 8-8. `reservation_logs` — 예약 처리 이력 (업무 타임라인)

| 컬럼 | 타입 | 비고 |
|---|---|---|
| `id` | int | PK |
| `reservation_id` | int | FK → `reservations.id` ON DELETE CASCADE |
| `action` | varchar(40) | `received`/`assigned`/`status_changed`/`note_added`/`deposit_confirmed`/`cancelled` |
| `note` | varchar(300) | NULL — 짧은 요약만(상담 본문은 `reservation_notes`에 있다) |
| `actor_user_id` | int | NULL — 시스템 접수는 NULL |
| `actor_name` | varchar(30) | NOT NULL — `'SYSTEM'` 또는 조작한 계정 이름(계정이 사라져도 이력 보존) |
| `created_at` | timestamptz | NOT NULL |

**인덱스**: `ix_reservation_logs_reservation_id_created_at` (`reservation_id`, `created_at`).

> **`audit_logs`와 중복이 아니다.** 이 테이블은 예약 상세 화면에 보이는 **업무 타임라인**(고객 폼 제출 등 관리자 행위가 아닌 이벤트 포함)이고, `audit_logs`는 관리자 행위 **감사**용이다. 목적·열람 권한·보존 기준이 달라 분리한다. 상태 변경은 양쪽에 모두 남는다(의도된 이중 기록).

### 8-9. `audit_logs` — 관리자 감사 로그

| 컬럼 | 타입 | 비고 |
|---|---|---|
| `id` | bigint | PK |
| `actor_user_id` | int | NULL |
| `actor_email` | varchar(254) | NOT NULL — 계정 삭제 후에도 보존 |
| `actor_role` | varchar(20) | NOT NULL |
| `action` | varchar(40) | `create`/`update`/`delete`/`change_role`/`suspend` 등 |
| `entity_type` | varchar(40) | `reservation`/`user`/`procedure` 등 |
| `entity_id` | varchar(40) | NULL |
| `summary` | varchar(300) | NOT NULL |
| `ip` | varchar(45) | NULL — IPv6 최대 길이 |
| `status_code` | int | NOT NULL |
| `created_at` | timestamptz | NOT NULL |

**인덱스**: `ix_audit_logs_created_at` (`created_at DESC`), `ix_audit_logs_actor_user_id_created_at`, `ix_audit_logs_entity_type_created_at`.

### 8-10. `landing_daily_stats` — 유입 경로 일별 집계

| 컬럼 | 타입 | 비고 |
|---|---|---|
| `id` | int | PK |
| `stat_date` | date | NOT NULL |
| `referral_code` | varchar(50) | NOT NULL DEFAULT `''` |
| `utm_source` / `utm_medium` / `utm_campaign` | varchar(100) | NOT NULL DEFAULT `''` |
| `visit_count` | int | NOT NULL DEFAULT 0 |

**인덱스**: `ux_landing_daily_stats_key` UNIQUE (`stat_date`, `referral_code`, `utm_source`, `utm_medium`, `utm_campaign`), `ix_landing_daily_stats_stat_date`.

> 🔴 **모든 키 컬럼을 `NOT NULL DEFAULT ''`로 둔다.** PostgreSQL의 UNIQUE 제약은 기본적으로 NULL을 서로 다른 값으로 취급(NULLS DISTINCT)하므로, NULL을 허용하면 같은 조합의 행이 무한히 중복 생성된다.

---

## 9. 입력 값 규칙 (길이 제한 · 날짜/시간)

### 9-1. 입력 필드 길이 제한 (절대 원칙 — 3곳 일치)

**DB 컬럼 제약 / 백엔드 DTO validation / 프론트 `maxlength` 세 값이 항상 정확히 일치해야 한다.** 값을 바꿀 때는 반드시 세 곳을 세트로 수정하고, 수정 후 실제로 일치하는지 직접 대조 확인한다.

| 필드 | DB | 백엔드 | 프론트 |
|---|---|---|---|
| 고객 이름 | `varchar(50)` | `[MaxLength(50)]` | `maxlength="50"` |
| 위챗 ID | `varchar(50)` | `[MaxLength(50)]` | `maxlength="50"` |
| 상담 기록 본문(`reservation_notes.body`) | `varchar(2000)` | `[MaxLength(2000)]` | `maxlength="2000"` |
| 취소 사유 | `varchar(200)` | `[MaxLength(200)]` | `maxlength="200"` |
| 계정 이메일 | `varchar(254)` | `[MaxLength(254)]` | `maxlength="254"` |
| 계정 이름 | `varchar(30)` | `[MaxLength(30)]` | `maxlength="30"` |
| 계정 비밀번호 | (해시 저장) | 8~64자 | `minlength="8" maxlength="64"` |
| 실장 이름(`consultants.name`) | `varchar(30)` | `[MaxLength(30)]` | `maxlength="30"` |
| 실장 팀(`consultants.team`) | `varchar(20)` | `[MaxLength(20)]` | `maxlength="20"` |
| 시술명(언어당) | `varchar(50)` | `[MaxLength(50)]` | `maxlength="50"` |
| 시술 코드 | `varchar(30)` | `[MaxLength(30)]` | `maxlength="30"` |
| 처리 이력 메모 | `varchar(300)` | `[MaxLength(300)]` | `maxlength="300"` |
| UTM 각 항목 | `varchar(100)` | 서버에서 100자로 **절단**(거부 아님) | — (URL 파라미터) |
| 추천 코드 | `varchar(50)` | 서버에서 50자로 **절단** | — (URL 파라미터) |

> **비밀번호 상한 64자는 BCrypt 72바이트 한계 이내이면서 긴 패스프레이즈를 허용하는 값이다.** 서버만 64로 올리고 HTML `maxlength`를 옛 값으로 남겨두면 브라우저가 입력 자체를 잘라버려 사용자가 원인을 알 수 없는 로그인 실패를 겪는다(실제 발생한 버그).
>
> **UTM·추천 코드만 "거부"가 아니라 "절단"인 이유**: 광고 플랫폼이 붙이는 파라미터 길이를 우리가 통제할 수 없는데, 길다고 예약 신청 자체를 실패시키면 고객을 잃는다. 추적 정확도보다 접수 성공이 우선이다.

### 9-2. 🔴 날짜·시간(타임존) 처리 원칙

> 고객은 중국·대만(UTC+9 대비 **1시간 느림**), 병원과 실장은 한국에 있다. 기준시를 정하지 않으면 "오후에 연락 달라"는 요청이 실제로 몇 시인지 아무도 확신할 수 없고, 실장이 1시간 어긋난 시각에 연락하게 된다.

**모든 운영 시각은 KST(UTC+9) 고정으로 통일한다.** 사용자 브라우저 타임존을 따라가지 않는다.

| 대상 | 저장 | 표시 |
|---|---|---|
| `created_at` 등 이벤트 시각 | `timestamptz`(UTC로 저장) | 관리자 화면에서 **항상 KST로 변환해 표시** |
| `visit_date` / `visit_time` | `date` / `time` (타임존 없음) | **병원 현지(KST) 벽시계 시각 그대로**. 타임존 변환을 적용하지 않는다 |
| `preferred_contact_time` | 코드(`morning` 등) | **KST 기준 시간대** — 아래 규칙 |

**① 연락 희망 시간대는 KST 기준으로 정의하고, 폼에 실제 시각 범위를 병기한다.**

선택지에 시간 범위를 함께 적으면 "오전"이 몇 시인지에 대한 해석 차이가 원천적으로 사라진다. 고객은 자기 시간대로 환산해 고르면 되고, 시스템은 변환 로직을 하나도 갖지 않아도 된다.

| 코드 | 표기(예: 한국어) | 표기(예: 간체) |
|---|---|---|
| `morning` | 오전 (09:00–12:00 KST) | 上午 (09:00–12:00 韩国时间) |
| `afternoon` | 오후 (12:00–18:00 KST) | 下午 (12:00–18:00 韩国时间) |
| `evening` | 저녁 (18:00–21:00 KST) | 晚上 (18:00–21:00 韩国时间) |
| `anytime` | 시간 무관 | 任何时间 |

**② 🔴 관리자 화면의 시각 표시는 브라우저 타임존을 쓰지 말 것.**

`new Date(x).toLocaleString()`처럼 브라우저 로컬 타임존에 의존하면, **SSR 서버(UTC)와 브라우저(KST 등)의 결과가 달라 하이드레이션 mismatch가 발생한다**(다른 프로젝트에서 실제로 겪어 전역 수정한 이슈). 서버·클라이언트가 같은 문자열을 만들도록 타임존을 명시적으로 고정한다.

```ts
// app/utils/datetime.ts — 서버·클라이언트 어디서 호출해도 동일한 결과를 낸다
const KST = 'Asia/Seoul'

export function formatKst(value: string | Date, withTime = true): string {
  return new Intl.DateTimeFormat('ko-KR', {
    timeZone: KST,                                  // ⚠️ 이 줄이 없으면 실행 환경 타임존을 따라가 mismatch가 난다
    year: 'numeric', month: '2-digit', day: '2-digit',
    ...(withTime ? { hour: '2-digit', minute: '2-digit', hour12: false } : {}),
  }).format(typeof value === 'string' ? new Date(value) : value)
}
```

**③ "오늘"·"이번 달"의 경계도 KST로 계산한다.** 대시보드의 "이번 달 방문 완료" 같은 집계를 서버가 UTC 기준으로 자르면 매월 1일 오전 9시 이전에 숫자가 어긋난다. 백엔드에서 범위를 만들 때 KST 기준 월초를 UTC로 변환해 넘긴다.

```csharp
// 백엔드 — KST 기준 이번 달 시작 시각을 UTC로 환산
private static readonly TimeZoneInfo Kst = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");

var nowKst = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Kst);
var monthStartKst = new DateTimeOffset(nowKst.Year, nowKst.Month, 1, 0, 0, 0, nowKst.Offset);
var monthStartUtc = monthStartKst.UtcDateTime;   // 쿼리에는 이 값을 쓴다
```

> ⚠️ `TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul")`는 Linux 컨테이너의 tzdata에 의존한다. .NET 8부터 Windows/IANA ID를 양쪽에서 인식하지만, **컨테이너 이미지에 tzdata가 없으면 런타임 예외가 난다** — Phase 0에서 실제 배포 이미지로 한 번 확인할 것(확인 전까지 `[미확인]`).

---

## 10. 예약 상태 머신

```
                    ┌──────────────────────────────────────────┐
                    │                                          │
   [고객 폼 제출]     ▼                                          │
        └──────►  New  ──────►  Consulting  ──────►  Confirmed  ──────►  Visited
                    │              │                    │
                    └──────────────┴────────────────────┴──────►  Cancelled
```

| 전이 | 조건 | 수행 주체 |
|---|---|---|
| → `New` | 폼 제출 | 고객(익명) |
| `New` → `Consulting` | 실장 배정 또는 상담 기록 최초 저장 | Consultant |
| `New`/`Consulting` → `Confirmed` | **`visit_date`가 있고 `deposit_paid = true`** 둘 다 충족 | Consultant |
| `Confirmed` → `Visited` | 실제 내원 확인 | Consultant |
| `New`/`Consulting`/`Confirmed` → `Cancelled` | 취소 사유 입력 필수 | Consultant |

**구현 규칙**
- 상태 전이는 **조건부 원자적 UPDATE**로만 수행한다. "조회 → 판단 → 저장" 3단계로 나누면 실장 두 명이 동시에 저장할 때 경쟁 조건이 생긴다.

```csharp
// 현재 상태가 기대값일 때만 갱신 — 갱신 행 수가 0이면 다른 세션이 먼저 바꾼 것이므로 409 반환
var affected = await db.Reservations
    .Where(r => r.Id == id && r.Status == expectedStatus)
    .ExecuteUpdateAsync(s => s
        .SetProperty(r => r.Status, nextStatus)
        .SetProperty(r => r.ConfirmedAt, now)
        .SetProperty(r => r.UpdatedAt, now));

if (affected == 0)
    return Conflict(new { code = "RESERVATION_STATE_CHANGED" });
```

- `Visited`·`Cancelled`는 **종결 상태**다. 되돌리기가 필요하면 어드민만 가능하게 별도 액션으로 만들되, 요구되기 전까지는 만들지 않는다.
- 취소·완료 등 되돌릴 수 없는 액션은 프론트에서 확인 UI를 거치게 하고, 목록 행이 아니라 **상세 화면 안에서만** 노출한다(실수 클릭 방지).

---

## 11. API 명세

> 전 목록 API는 예외 없이 `PagedResult<T>` + `page`/`pageSize`(서버에서 `Math.Clamp(pageSize, 1, 100)`)를 적용한다. 검색어는 공용 `EscapeLike` 헬퍼(`\`, `%`, `_` 이스케이프)를 통과시킨다.

```csharp
public record PagedResult<T>(IEnumerable<T> Items, int Total, int Page, int PageSize);
```

### 11-1. 공개 (익명)

| 메서드 | 경로 | 비고 |
|---|---|---|
| POST | `/api/reservations` | 예약 신청. rate limit(IP 분당 5회) + honeypot + 개인정보 동의 서버 재검증 |
| GET | `/api/procedures` | 활성 시술 목록(선택 UI용, 캐시 가능) |

> 🔴 **랜딩 방문 기록(`/api/internal/landing-visit`)은 공개 엔드포인트로 두지 않는다**(F11). 익명 공개로 열면 누구나 스크립트로 특정 추천코드의 방문수를 부풀려 전환율 지표를 왜곡할 수 있고, rate limit으로는 막히지 않는다.
>
> **대신 프론트 서버(Nitro)만 호출할 수 있는 내부 전용 경로로 만든다.** 프록시가 백엔드로 전달할 때 쓰는 내부 공유 시크릿 헤더(`X-Internal-Secret`)를 함께 보내고, 백엔드는 그 헤더가 없거나 값이 다르면 **404**를 반환한다(401이 아니라 404 — 엔드포인트 존재 자체를 숨긴다). 브라우저는 이 경로를 호출할 방법이 없으므로 조작 경로가 원천적으로 닫힌다.
>
> 시크릿은 프론트에서 **`NUXT_PUBLIC_` 접두사가 없는 private 런타임 설정**으로만 둔다 — `public`에 두면 브라우저 번들에 그대로 노출되어 의미가 없어진다.

### 11-2. 예약 운영 (Consultant 이상)

| 메서드 | 경로 | 권한 | 비고 |
|---|---|---|---|
| GET | `/api/admin/reservations` | 전 역할 | 필터: `status`, `consultantId`, `from`, `to`, `search`, `includeInactiveConsultants` / 정렬: `created_at DESC` |
| GET | `/api/admin/reservations/summary` | 전 역할 | 상단 4개 카드 — 조건부 집계 1회(아래) |
| GET | `/api/admin/reservations/{id}` | 전 역할 | 상세 + 시술 + 상담 기록 + 처리 이력 |
| PATCH | `/api/admin/reservations/{id}` | Consultant, Admin | 방문일시·담당실장·시술·예약금 저장 (**상담 기록은 아래 별도 엔드포인트**) |
| POST | `/api/admin/reservations/{id}/status` | Consultant, Admin | 상태 전이(10장 조건부 UPDATE) |
| POST | `/api/admin/reservations/{id}/notes` | Consultant, Admin | 상담 기록 **추가**(누적, D14) |
| PATCH | `/api/admin/reservations/{id}/notes/{noteId}` | 작성자 본인, Admin | 상담 기록 수정. 삭제 엔드포인트는 만들지 않는다 |
| GET | `/api/admin/reservations/calendar` | 전 역할 | `year`·`month` 필수, **최대 1개월 범위 검증**(무제한 범위 조회 차단). `status IN ('Confirmed','Visited')` |

> **실장 간 예약 접근은 전면 허용한다**(F8, 2026-08-25 사용자 결정). 실장 A가 실장 B의 예약을 조회·수정하고 담당자를 변경할 수 있다. 단일 병원에서 휴가·교대 대체가 일상적이고, **누가 무엇을 바꿨는지는 처리 이력(`reservation_logs`)과 감사 로그에 전부 남으므로** 접근 제한 대신 추적으로 관리한다. 담당자 변경도 반드시 이력에 남긴다(`action='assigned'`, 이전 담당자 → 새 담당자를 `note`에 기록).

**대시보드 4개 카드 — 조건부 집계 1회로 처리** (F2)

4번째 카드만 `visited_at`에 대한 별도 기간 조건이 붙으므로 **`GROUP BY status` 하나로는 산출되지 않는다.** 조건부 집계로 단일 쿼리를 만든다.

```csharp
// GroupBy(_ => 1)로 전체를 한 그룹으로 묶고 조건부 Count를 나열하면 EF Core가 단일 SQL로 번역한다
// (PostgreSQL의 COUNT(*) FILTER (WHERE ...)에 해당)
var summary = await db.Reservations
    .GroupBy(_ => 1)
    .Select(g => new
    {
        New        = g.Count(r => r.Status == "New"),
        Consulting = g.Count(r => r.Status == "Consulting"),
        Confirmed  = g.Count(r => r.Status == "Confirmed"),
        VisitedThisMonth = g.Count(r => r.Status == "Visited"
                                     && r.VisitedAt != null
                                     && r.VisitedAt >= monthStartUtc),   // 9-2절 ③ KST 기준 월초
    })
    .FirstOrDefaultAsync();

// 행이 하나도 없으면 GroupBy 결과가 비어 null이 된다 — 0으로 채워 반환할 것
```

### 11-3. 마스터 관리 (HospitalManager 이상)

| 메서드 | 경로 | 비고 |
|---|---|---|
| GET / POST / PUT | `/api/admin/consultants[/{id}]` | 실장 마스터 CRUD(D8). **DELETE 없음** — 비활성화는 `PUT`의 `isActive=false`로 (D13). 목록은 `includeInactive` 쿼리로 비활성 포함 여부 선택 |
| GET / POST / PUT | `/api/admin/procedures[/{id}]` | 시술 마스터. **DELETE 없음** — `isActive=false`로 비활성화 |

### 11-4. 통계 (HospitalManager 이상)

| 메서드 | 경로 | 비고 |
|---|---|---|
| GET | `/api/admin/stats/consultants` | 실장 KPI — 배정 건수 / 확정 건수 / 방문 건수 / 확정 전환율 / 평균 최초응대 소요시간. **비활성 실장은 제외**(D13) |
| GET | `/api/admin/stats/reservations` | 예약 통계 — 기간별 접수·확정·방문·취소 추이, 시술별 집계, 언어별 분포. 담당 실장 축으로 나눌 때는 **비활성 실장 제외** |

### 11-5. 어드민 전용

| 메서드 | 경로 | 비고 |
|---|---|---|
| GET / POST / PATCH | `/api/admin/users[/{id}]` | 계정 발급·역할 변경·정지. 자기 자신 조작 차단 + RT 전량 폐기. **DELETE 없음** — 계정도 정지(`is_suspended`)로만 막는다(감사 로그 행위자 추적 유지) |
| GET | `/api/admin/audit-logs` | 필터: `actorId`, `entityType`, `action`, `from`, `to`, `search` |
| GET | `/api/admin/stats/referrals` | **어드민 전용(D5)** — 유입 경로별 방문수·예약수·전환율 |

### 11-6. 🔴 통계 쿼리 작성 시 함정

**`GroupBy(...).Select(g => new SomeRecordDto(...))`처럼 집계 프로젝션 안에서 record 생성자를 직접 호출하지 말 것** — EF Core가 SQL로 변환하지 못해 런타임 예외가 난다. 반드시 (1) **익명 타입**으로 집계 후 `ToListAsync()`로 구체화하고, (2) 메모리에서 DTO로 매핑하는 2단계로 나눈다.

```csharp
// 1단계 — DB에서 익명 타입으로 집계 (전체 로우를 메모리로 올리지 않음)
// activeConsultantIds: 비활성 실장을 KPI에서 제외하기 위한 사전 조회(D13)
var raw = await db.Reservations
    .Where(r => r.CreatedAt >= from && r.CreatedAt < to
             && r.ConsultantId != null
             && activeConsultantIds.Contains(r.ConsultantId.Value))
    .GroupBy(r => r.ConsultantId)
    .Select(g => new
    {
        ConsultantId = g.Key,
        Assigned  = g.Count(),
        Confirmed = g.Count(r => r.Status == "Confirmed" || r.Status == "Visited"),
        Visited   = g.Count(r => r.Status == "Visited"),
    })
    .ToListAsync();

// 2단계 — 메모리에서 DTO 매핑
var items = raw.Select(x => new ConsultantKpiDto(
    x.ConsultantId,
    x.Assigned,
    x.Confirmed,
    x.Visited,
    x.Assigned == 0 ? 0m : Math.Round((decimal)x.Confirmed / x.Assigned * 100, 1))).ToList();
```

또한 **데이터가 없는 구간도 0으로 채워서 내려준다** — 빈 달을 배열에서 빼면 프론트 차트의 X축이 밀린다.

---

## 12. 화면 설계

### 12-1. 공개 화면

| 경로 | 화면 | 비고 |
|---|---|---|
| `/`, `/zh-tw`, `/en`, `/ko` | 랜딩 | 헤더 + 히어로/소개 + **예약 신청 폼** + 푸터 |
| `/privacy` (4언어) | 개인정보 처리방침 | 폼에서 수집하는 항목·보유기간·이용목적 명시 |

**랜딩 예약 신청 폼 필드**

| 필드 | 입력 방식 | 필수 |
|---|---|---|
| 이름 | text | ✅ |
| 생년월일 | `<input type="date">` | ✅ |
| 성별 | radio (여성/남성/기타) | ✅ |
| 위챗 ID | text | ✅ |
| 연락 희망 시간대 | select (오전/오후/저녁/무관) | ✅ |
| 개인정보 수집·이용 동의 | checkbox + 처리방침 링크 | ✅ |
| (honeypot) | 숨김 필드 | — |

> 🔴 **디자인 원칙 (절대 원칙)**: 위 모든 입력 요소에 **보이는 `<label>`**을 붙인다. placeholder로 label을 대신하지 않는다 — placeholder는 입력을 시작하는 순간 사라져 사용자가 무엇을 입력하던 필드인지 잊게 만드는 실제 UX 결함이다. 이 원칙은 랜딩 폼뿐 아니라 **관리자 화면의 모든 input/textarea/select, 목록의 검색창까지 예외 없이** 적용한다.
>
> honeypot 필드만 유일한 예외다(봇 탐지용이라 사람에게 보이면 기능이 성립하지 않음). 스크린리더가 읽지 않도록 `aria-hidden="true"` + `tabindex="-1"`을 함께 준다.

### 12-2. 관리자 로그인 진입점 숨김

푸터의 저작권 표기(`© 2026 원진성형외과`)를 `/admin/login`으로 가는 링크로 만든다. `rel="nofollow"` + robots disallow로 색인에서 제외한다.

> ⚠️ 이것은 **보안 조치가 아니라 UI 노출 억제**일 뿐이다. 실제 보호는 로그인 인증과 백엔드 `[Authorize]`가 담당한다. "숨겼으니 안전하다"고 판단하지 말 것.

### 12-3. 관리자 레이아웃

```
┌──────────┬──────────────────────────────────────────┐
│ 사이드바   │ 상단바(현재 사용자 · 역할 · 언어 · 로그아웃)   │
│ 220px    ├──────────────────────────────────────────┤
│ 고정      │ 본문 (각 메뉴 화면)                        │
└──────────┴──────────────────────────────────────────┘
```

- `.sidebar { width: 220px }`와 `.admin-main { margin-left: 220px }`의 **숫자가 두 곳에 중복**된다. 폭을 바꿀 때 한쪽만 고치면 본문이 사이드바 밑에 깔리므로 항상 세트로 수정한다.
- 768px 이하에서는 사이드바를 `translateX(-100%)`로 숨기고 햄버거 + 오버레이로 전환한다.
- 사이드바 아이콘은 **고정 폭 슬롯**으로 만든다: `.icon { width: 1.4rem; flex-shrink: 0; text-align: center; }`. 폭을 지정하지 않으면 이모지마다 렌더 폭이 달라 메뉴 이름의 시작 위치가 항목마다 어긋난다.
- 로고 `<img>` 크기는 HTML `height` 속성이 아니라 **CSS 클래스**로 지정한다(Tailwind Preflight의 `img { height: auto }`가 HTML 속성을 이긴다).
- 관리자 레이아웃에 `noindex` 메타를 한 번만 부착해 전 하위 페이지에 자동 적용한다.

### 12-4. 예약 대시보드 (`/admin`) — 사용자 지시 7번

**상단**: 참고 화면(`docs/reservation-desk_1.html`)의 4개 상태 카드를 그대로 채택한다.

| 카드 | 집계 기준 |
|---|---|
| 신규 접수 | `status = 'New'` 전체 |
| 상담 진행중 | `status = 'Consulting'` 전체 |
| 예약 확정 | `status = 'Confirmed'` 전체 |
| 이번 달 방문 완료 | `status = 'Visited'` AND `visited_at`이 이번 달 |

**하단**: 참고 화면의 좌우 분할(목록 + 인라인 상세) 방식은 **채택하지 않는다**(사용자 명시). 대신 필터·페이징이 있는 목록만 두고, 행을 선택하면 **별도 상세 페이지**(`/admin/reservations/[id]`)로 이동한다.

- 필터: 상태 / 담당 실장(기본은 활성 실장만, "비활성 포함" 옵션 제공) / 접수 기간 / 검색(이름·위챗ID·예약코드)
- 정렬: 접수일시 최신순 고정
- 페이지 크기: 20
- 모든 시각은 KST로 표시(9-2절 `formatKst`)

> 🔴 **검색 입력값을 반응형 데이터 페칭 훅의 `query`에 직접 물리지 말 것** — 글자 입력마다 API가 재호출된다. 그렇다고 비반응형 스냅샷으로 고정하면 이번엔 검색 버튼을 눌러도 재조회가 안 되는 새 회귀가 생긴다(같은 라우트로의 이동은 페이지를 리마운트하지 않기 때문). **정답은 URL 쿼리를 `computed`로 감싸 그것에만 반응하게 하는 것**이다.

```ts
const route = useRoute()
const query = computed(() => ({
  page: Number(route.query.page) || 1,
  status: route.query.status || undefined,
  search: route.query.search || undefined,
}))
const { data, refresh } = await useApi<PagedResult<ReservationListItem>>('/api/admin/reservations', { query })

function submitSearch(value: string) {
  navigateTo({ query: { ...route.query, search: value || undefined, page: 1 } })
}
```

### 12-5. 예약 상세 (`/admin/reservations/[id]`)

참고 화면의 상세 구성을 그대로 옮긴다.

1. **고객 정보**(읽기 전용): 이름 / 생년월일(나이 계산 표시) / 성별 / 위챗 ID / 연락 희망 시간대(KST 범위 병기) / 신청 언어 / 유입 경로 / 접수 시각 / 예약 코드
2. **상담 기록**(누적, D14): 기존 기록을 작성자·시각과 함께 **시간순으로 모두 나열**하고, 하단에 새 기록 추가용 textarea(2000자) + [기록 추가] 버튼. 기존 기록은 작성자 본인·어드민만 수정 가능하며 수정 시 "(수정됨)" 표시. **삭제 버튼은 두지 않는다**
3. **방문 예약**: 방문 날짜 / 방문 시각(**KST 기준**) / 담당 실장 select(활성 실장만, 단 현재 배정된 비활성 실장은 목록에 유지 — 8-4 함정)
4. **시술·수술 결정**: 활성 시술 다중 선택
5. **예약금**: 통화 select(`CNY` 기본 / `KRW`) + 금액 + 입금 확인 체크박스 (D3·D12). 통화 select에도 보이는 label을 붙인다
6. **처리 이력**: `reservation_logs` 타임라인
7. **액션**: 저장 / 상태 전이 / 취소(사유 입력 필수)

> 상세 조회는 목록 API 응답에 포함하지 않고 **별도 API로 지연 로딩**한다(목록 페이지를 가볍게 유지).

### 12-6. 예약 달력 (`/admin/calendar`)

- 첨부 스크린샷과 동일한 구성: 좌측 월간 그리드 + 우측 "선택한 날짜의 예약 목록".
- 조회 기준은 **`visit_date`(방문 예정일)**이며 **`status IN ('Confirmed','Visited')`**를 표시한다.
  > 🔴 `Confirmed`만 조회하면 **고객이 실제로 내원해 `Visited`가 되는 순간 그 날짜가 달력에서 사라진다**(F1). 실장이 "지난주에 누가 왔었지"를 달력에서 확인할 수 없게 되므로, 확정과 방문완료를 함께 표시하고 배지 색으로 구분한다. 8-5의 부분 인덱스 조건도 이와 동일하게 맞춰야 한다(불일치 시 인덱스를 못 탄다).
- 월 이동 시 `year`·`month` 쿼리로만 재조회하고, **서버에서 최대 1개월 범위를 검증**한다(클라이언트가 임의 범위를 보내 전체 스캔을 유발하지 못하게).
- 날짜에 예약이 없으면 "이 날짜에는 예약이 없습니다"를 표시한다(데이터 없음 안내는 허용 — 13장 참고).
- 라이브러리 없이 자체 월간 그리드로 구현한다(D11).

### 12-7. 나머지 메뉴

| 메뉴 | 핵심 구성 |
|---|---|
| 실장 관리 | `consultants` 마스터 CRUD(D8) — 이름·팀·정렬순서 등록/수정 + **활성/비활성 토글**. 삭제 버튼 없음(D13). "비활성 포함 보기" 체크박스로 퇴사자 조회. **로그인 계정과 무관한 독립 데이터이므로 계정 관리 화면과 혼동하지 말 것** |
| 시술·수술 관리 | 목록 + 4언어 탭 입력 폼(코드·정렬순서·활성). 삭제 버튼 없음 |
| 실장 KPI | 기간 선택 + 실장별 배정/확정/방문/전환율/평균 응대시간 표 |
| 예약 통계 | 기간별 추이 + 시술별 집계 + 언어별 분포 |
| 계정 관리 | 계정 목록 + 발급 폼 + 역할 변경 + 정지 (어드민 전용) |
| 로그 | 감사 로그 목록 + 필터 (어드민 전용) |
| 유입 경로 분석 | 코드/캠페인별 방문수·예약수·전환율 (어드민 전용) |

---

## 13. 화면 깜빡임 금지 이행 방안 (절대 원칙)

### 13-1. SSR 프리로드

- 데이터가 필요한 모든 페이지는 `<script setup>` **최상위에서 `await useApi(...)`**로 조회한다. `onMounted` + 클라이언트 fetch 금지.
- 이것이 가능한 이유는 D7(동일 출처 프록시) 때문이다 — 인증 쿠키가 프론트 도메인 자체 쿠키가 되어 SSR 요청에도 실린다. 크로스 도메인 구조였다면 SSR 시점에 로그인 상태를 알 수 없어 이 원칙을 적용할 수 없었을 것이다.
- 데이터가 **없거나 오류일 때만** 안내 문구를 표시한다.

### 13-2. 페이지 전환 차단 오버레이

SSR 프리로드는 페이지 전환에 지연을 만든다. 그 사이 이전 화면이 조작 가능한 채로 방치되면 사용자가 "렉 걸렸다"고 오인하므로, 전환 중 클릭을 막는 오버레이를 반드시 적용한다.

> 🔴 **오버레이를 `<Transition>`으로 마운트/언마운트하지 말 것.** 실제 발생한 사고: 빠른 연속 페이지 전환(연타) 시 leave 트랜지션이 끝나기 전에 enter가 걸리며 트랜지션이 깨졌고, `opacity:0`인 `position:fixed; inset:0` 엘리먼트가 DOM에 영구히 남아 **사이트 전체 클릭이 전부 씹히는 상태**가 됐다. 눈에 보이지 않아 원인 파악도 어려웠다.
>
> **항상 마운트해둔 채, `pointer-events`와 투명도를 boolean 상태값에 직접 클래스 바인딩으로만 토글한다.** 그래야 트랜지션이 중간에 끊겨도 클릭 차단 여부가 항상 실제 상태와 정확히 일치한다.

```vue
<!-- app/components/RouteOverlay.vue — v-if 없이 항상 마운트 -->
<template>
  <div
    class="fixed inset-0 z-50 bg-white/60 transition-opacity duration-150"
    :class="active ? 'opacity-100 pointer-events-auto' : 'opacity-0 pointer-events-none'"
    aria-hidden="true"
  />
</template>

<script setup lang="ts">
// 전환 시작/완료를 카운터로 관리 — 중첩 전환에도 안전
const { pending } = useRouteOverlay()
const active = computed(() => pending.value > 0)
</script>
```

- 카운터를 쓰는 이유: 전환이 겹칠 때 boolean 하나면 먼저 끝난 전환이 오버레이를 꺼버린다.
- 강제 로그아웃 등에서 `increment()` 후 네트워크 요청을 기다린다면 반드시 **짧은 timeout**을 준다. 응답이 매달리면 `decrement()`에 도달하지 못해 오버레이가 영구 고착된다.

---

## 14. 감사 로그 설계

전역 `AuditLogFilter`(`IAsyncActionFilter`)로 자동 기록한다. 컨트롤러마다 로그 저장 코드를 넣지 않는다.

**이 프로젝트의 핵심 차이점**: 일반적인 가이드는 `role == "Admin"`인 요청만 감사하지만, **사용자 요구는 "실장·병원관리자·어드민이 어드민 페이지에서 실행한 CRUD를 모두 감사"**하는 것이다. 따라서 감사 대상 역할을 **3역할 전부**로 둔다.

```csharp
// 읽기(GET/HEAD/OPTIONS)는 기록하지 않음
// 감사 대상: 로그인한 3역할 전부 (Admin 한정으로 좁히면 실장 행위 전체가 로그에서 빠진다)
var role = context.HttpContext.User.FindFirstValue(ClaimTypes.Role);
if (role is not ("Admin" or "HospitalManager" or "Consultant")) { await next(); return; }

var executed = await next();

// ⚠️ 컨트롤러가 예외를 던지면 next()가 throw하지 않고 ActionExecutedContext.Exception에 담아 반환한다.
//    이걸 확인하지 않으면 실패한 쓰기 시도가 상태코드 기본값(200) = "성공"으로 오기록된다.
var statusCode = executed.Exception is not null ? 500 : context.HttpContext.Response.StatusCode;
```

**반드시 지킬 것**
- **제외 경로**는 "경로"가 아니라 **`(경로, 메서드)` 쌍**으로 판단한다. 경로 문자열만으로 제외하면 같은 경로의 민감한 메서드(DELETE 등)까지 통째로 빠진다.
- 제외 대상: `/api/auth/login`, `/api/auth/logout`, `/api/auth/refresh`, `/api/auth/me*`, `/api/admin/audit-logs` — 본인 계정 관리 행위를 남기면 실제 이상행위 탐지에 노이즈만 쌓인다.
- `RouteMap`은 단일 prefix 문자열이 아니라 **`string[] Segments` AND 매칭 + 세그먼트 개수 내림차순 정렬**로 구현한다. "먼저 등록된 것이 이긴다"를 기본 동작으로 두면 중첩 경로가 부모 경로 규칙으로 오분류된다.
- **새 관리자 API를 추가할 때마다 `RouteMap` 등록 여부를 별도로 확인한다.** 컨트롤러가 `HttpContext.Items["AuditSummary"]`로 요약문을 직접 채우면 로그 텍스트는 정상으로 보이지만, `RouteMap` 미등록이면 `entity_type`이 `unknown`으로 저장돼 분류·필터링에서 빠진다. 로그 텍스트 정상 출력과 분류 정확성은 **서로 다른 체크포인트**다.
- 감사 로그 저장 실패가 본 작업을 실패시키지 않도록 try/catch로 격리한다.
- 클라이언트 IP는 Cloudflare가 직접 설정하는 위조 불가 헤더(`CF-Connecting-IP`)를 우선 읽고, 없을 때만 폴백한다. 브라우저가 보낸 `X-Forwarded-For`를 그대로 신뢰하지 않는다.

---

## 15. 유입 경로 추적 설계 (D4 · D5)

### 15-1. 수집

1. 광고 링크: `https://도메인/?utm_source=xiaomei&utm_medium=wechat&utm_campaign=2026q3&ref=XIAOMEI01`
2. 랜딩 SSR 시점에 **프론트 서버(Nitro)가** `POST /api/internal/landing-visit`을 내부 시크릿 헤더와 함께 호출해 **일별 집계를 UPSERT**한다.

> 🔴 **이 호출을 `await`하지 말 것**(F6). 방문 기록은 화면 렌더에 아무 영향도 주지 않는데, 동기로 기다리면 랜딩 응답 시간이 백엔드 왕복만큼 그대로 늘어난다. **광고 랜딩은 응답 속도가 곧 이탈률**이므로 지표 수집이 사용자 경험을 늦춰서는 안 된다.

```ts
// server/plugins 또는 랜딩 SSR 경로 — fire-and-forget + 실패 무시
// ⚠️ await 없음. 이 요청이 실패해도 랜딩은 정상 렌더되어야 한다(지표 < 접수).
$fetch('/api/internal/landing-visit', {
  baseURL: config.apiBaseInternal,
  method: 'POST',
  headers: { 'X-Internal-Secret': config.internalSecret },   // public 아님 — 브라우저에 노출 금지
  body: { referralCode, utmSource, utmMedium, utmCampaign },
  timeout: 2000,
}).catch(() => {})
```

```sql
INSERT INTO wonjin.landing_daily_stats
    (stat_date, referral_code, utm_source, utm_medium, utm_campaign, visit_count)
VALUES (@date, @ref, @source, @medium, @campaign, 1)
ON CONFLICT (stat_date, referral_code, utm_source, utm_medium, utm_campaign)
DO UPDATE SET visit_count = wonjin.landing_daily_stats.visit_count + 1;
```

3. 예약 폼 제출 시, 그 시점의 UTM·추천코드를 `reservations`에 **스냅샷으로 함께 저장**한다(집계 테이블과 조인하지 않아도 예약 1건의 출처를 알 수 있게).

**설계 근거**: 방문마다 1행을 남기면 광고 트래픽만큼 행이 폭증한다. `(날짜 × 캠페인 조합)`당 1행으로 묶으면 행 수가 구조적으로 제한되고, 전환율 계산에 필요한 것은 개별 방문이 아니라 **일별 합계**뿐이다.

**외부 조작 차단**(F11): 이 엔드포인트는 내부 시크릿 헤더가 있는 요청만 받고, 없으면 **404**를 반환한다(엔드포인트 존재 자체를 숨긴다). 브라우저에서는 호출 경로가 없으므로 스크립트로 방문수를 부풀릴 수 없다. 시크릿은 프론트의 **private 런타임 설정**(`NUXT_PUBLIC_` 접두사 없음)과 백엔드 환경변수에만 둔다.

**남는 한계(명시)**
- 봇·크롤러 방문이 카운트에 섞인다 → User-Agent 봇 필터를 1차로 적용하되 완전히 걸러지지는 않는다. 정밀한 광고 성과는 광고 플랫폼 자체 지표와 교차 확인해야 한다.
- 같은 사람의 재방문이 중복 카운트된다(고유 방문자 수가 아니라 방문 수). 고유 방문자가 필요해지면 그때 IP 해시 기반 일별 중복 제거를 검토한다 — 지금은 만들지 않는다.
- 5-3절 언어 감지 리다이렉트가 `location.search`를 보존하지 않으면 **UTM이 통째로 유실된다** — 반드시 함께 검증할 것.
- 리다이렉트가 걸리는 경우(예: `/` → `/ko`) SSR이 두 번 실행되어 **같은 방문이 2회 집계될 수 있다** → 방문 기록은 **리다이렉트 대상이 아닌 최종 랜딩 렌더에서만** 호출할 것.

### 15-2. 노출 (D5 — 어드민 전용)

`/admin/referrals`에서 기간별로 아래를 표시한다.

| 열 | 산출 |
|---|---|
| 추천코드 / UTM 조합 | `landing_daily_stats` 그룹 |
| 방문 수 | `SUM(visit_count)` |
| 예약 수 | `reservations` 같은 조합 `COUNT` |
| 예약 전환율 | 예약 수 ÷ 방문 수 |
| 확정 수 / 확정 전환율 | `status IN ('Confirmed','Visited')` |

> 이 메뉴는 **어드민에게만** 노출한다. 사이드바 조건부 렌더링과 **API 액션 레벨 `[Authorize(Roles="Admin")]`을 둘 다** 적용한다(6-3절 원칙 2).
>
> 추천코드 ↔ 인플루언서 표시명 매핑 테이블은 **요구되지 않았으므로 만들지 않는다.** 1차에는 코드 원본을 그대로 표시하고, 표시명 관리가 필요해지면 그때 마스터 테이블을 추가한다.

---

## 16. 보안 체크리스트

### 인증·인가
- [ ] RT는 SHA-256 해시로만 DB 저장 (평문 금지)
- [ ] BCrypt workFactor ≥ 12
- [ ] `ClockSkew = TimeSpan.Zero`
- [ ] `AccountStateFilter` 전역 등록 + `[Authorize]` 요구 여부 선확인 (7-3절)
- [ ] 역할 변경·정지 시 `RevokeAllForUserAsync` 호출
- [ ] 자기 자신 역할 변경·정지 차단
- [ ] 회원가입 엔드포인트가 **존재하지 않는지** 확인 (D6)
- [ ] 이메일 `ToLowerInvariant()` 정규화 — 로그인·계정 발급 양쪽 모두
- [ ] 모든 관리자 API에 `[Authorize(Roles=...)]` — 컨트롤러 레벨로 연 컨트롤러 안의 쓰기 액션 전수 재점검
- [ ] 역할별로 숨긴 버튼의 API도 액션 레벨에서 잠갔는지 확인

### 입력·출력
- [ ] 전 입력 필드가 9장 3곳 일치표와 실제로 일치하는지 대조
- [ ] 검색어 전부 `EscapeLike` 통과 (LIKE 인젝션)
- [ ] `pageSize` 서버 클램프 (1~100)
- [ ] JSON-LD `<` → `\u003c` 이스케이프
- [ ] 원시 SQL 문자열 조합 금지 (LINQ만) — 15-1절 UPSERT는 파라미터 바인딩 사용
- [ ] 공개 API 응답 DTO에 내부 식별자·민감 필드를 **아예 넣지 않음**(null로 감추는 방식은 회귀 재발함)

### 요청·전송
- [ ] 상태변경(POST/PUT/PATCH/DELETE) Origin 검증 미들웨어 — 인증·인가보다 **앞단**
- [ ] 보안 헤더: `X-Content-Type-Options`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Permissions-Policy`, API는 `CSP: default-src 'none'; frame-ancestors 'none'`
- [ ] CORS는 명시적 화이트리스트(와일드카드 금지)
- [ ] 미들웨어 순서: `ForwardedHeaders` → 보안헤더 → CSRF Origin → CORS → Authentication → RateLimiter → Authorization
  - `UseAuthentication()` → `UseRateLimiter()` 순서를 지킬 것. 반대면 사용자 ID 기준 rate limit이 전부 IP 폴백된다.
- [ ] 공개 예약 폼: rate limit + honeypot + 개인정보 동의 서버 재검증
- [ ] **`/api/internal/landing-visit`이 내부 시크릿 헤더 없이는 404를 반환하는지**(F11) — 시크릿이 `NUXT_PUBLIC_`이 아닌 private 설정에 있는지 함께 확인
- [ ] `.env`·`appsettings.Development.json`이 `.gitignore`에 포함됐는지

### 데이터 보존
- [ ] **실장·시술·계정에 DELETE 엔드포인트가 없는지**(D13) — 전부 비활성화/정지로만 처리
- [ ] 비활성 실장이 **신규 배정 드롭다운·KPI·통계에서는 빠지고, 과거 예약 상세·이력에는 남는지**
- [ ] 상담 기록에 삭제 엔드포인트가 없는지, 수정이 작성자 본인·어드민으로 제한되는지(D14)
- [ ] 모든 시각 표시가 KST 고정인지, 브라우저 타임존에 의존하는 코드가 없는지(9-2절)

### 운영
- [ ] 관리자 페이지 전체 `noindex`
- [ ] 감사 로그 제외 목록이 (경로, 메서드) 쌍인지
- [ ] 감사 로그가 예외(500)를 200으로 오기록하지 않는지
- [ ] 파괴적 액션(취소·삭제·정지)에 확인 UI

---

## 17. 성능(인덱스 · 페이징) 설계 — 절대 원칙 이행

**새 쿼리를 작성하거나 리뷰할 때마다 아래 3가지를 자체 점검한다.**
1. 이 필터·정렬 컬럼에 인덱스가 있는가?
2. 이 목록 API에 페이징이 있는가?
3. 불필요한 컬럼·관계까지 통째로 가져오고 있지 않은가?

| 화면·경로 | 실제 쿼리 형태 | 커버 인덱스 |
|---|---|---|
| 대시보드 목록 | `WHERE status=? ORDER BY created_at DESC LIMIT 20` | `ix_reservations_status_created_at` |
| 상단 4개 카드 | `GROUP BY status` (+이번 달 `visited_at`) | `ix_reservations_status_created_at` |
| 예약 달력 | `WHERE status IN ('Confirmed','Visited') AND visit_date BETWEEN ? AND ?` | `ix_reservations_visit_date` (부분 — **필터 조건이 쿼리와 정확히 일치해야 탄다**) |
| 실장 KPI | `WHERE created_at >= ? GROUP BY consultant_id` | `ix_reservations_consultant_id_status` + `ix_reservations_created_at` |
| 유입 경로 | `WHERE created_at BETWEEN ? AND ? GROUP BY referral_code, utm_*` | `ix_reservations_created_at` (F10 — 코드 선행 인덱스는 이 쿼리를 못 탄다) |
| 감사 로그 | `ORDER BY created_at DESC LIMIT 20` | `ix_audit_logs_created_at` |
| 토큰 갱신 | `WHERE token_hash=?` | 🔴 `ix_refresh_tokens_token_hash` |
| 계정 관리 | `WHERE role=?` | `ix_users_role` |
| 실장 배정 드롭다운 | `WHERE is_active ORDER BY sort_order` | `ix_consultants_is_active_sort_order` |
| 상담 기록 조회 | `WHERE reservation_id=? ORDER BY created_at` | `ix_reservation_notes_reservation_id_created_at` |

**명시적 한계**: 검색(`ILIKE '%키워드%'`)은 **B-tree 인덱스를 타지 않는다.** 예약이 수만 건을 넘어가면 검색이 느려지므로, 그 시점에 `pg_trgm` GIN 인덱스 도입을 검토한다. 지금 규모에서 미리 넣는 것은 과설계다.

**목록 조회 시 컬럼·관계 최소화**: 목록 API는 `reservation_notes`(건당 2000자, 예약당 여러 건)를 **아예 조인하지 않는다**. 상담 기록은 상세 화면에서만 로드한다. `Select`로 목록에 실제로 표시하는 필드만 프로젝션할 것.

**인덱스 마이그레이션**: 현재 규모(수백~수천 행)에서는 일반 `CREATE INDEX`로 충분하다. 부팅 시 `Database.Migrate()`가 트래픽을 받기 전에 끝나므로 자기 자신의 쓰기를 막지 않는다. 테이블이 수십만 행을 넘어가면 `CREATE INDEX CONCURRENTLY`로 전환한다.

---

## 18. 법적 검토

**2026-08-25 사용자 확인 완료 — 별도 조치 불요.** 이 시스템은 중계·광고 플랫폼이 아니라 병원에 예약 기능만 제공하는 도구이며, 관련 법적 요건은 사용자 측에서 모두 통과 확인했다. **이 주제를 다시 리스크로 제기하지 말 것.**

---

## 19. 구현 Phase 계획

> ⚠️ **이 문서는 설계까지다. 구현 착수는 사용자의 명시적 승인 이후에만 시작한다.**
> 각 Phase 착수 시 변경 대상 파일 목록과 영향 범위를 먼저 제시하고 승인을 받는다.

| Phase | 내용 | 완료 기준 |
|---|---|---|
| 0 | 스캐폴딩(`api/` + `frontend/`), docker-compose, Tailwind v4, DB 마이그레이션(8장 전체) | 컨테이너 기동 + 마이그레이션 적용 + 인덱스 실제 생성 확인 + **컨테이너에서 `Asia/Seoul` 타임존 조회 성공**(9-2절 `[미확인]` 해소) |
| 1 | 인증(로그인·갱신·로그아웃·me), `AccountStateFilter`, 어드민 시딩, 동일 출처 프록시 | 로그인~정지 차단까지 실제 브라우저 E2E. 랜딩에서 `/api/auth/me`가 호출되지 않는지 확인(F5) |
| 2 | 랜딩 4언어 + 예약 신청 폼 + 개인정보 처리방침 + 유입 경로 수집 | 4언어 폼 제출 → DB 적재 + UTM 보존 확인. **내부 시크릿 없이 `landing-visit` 호출 시 404 실측**(F11) |
| 3 | 예약 대시보드(4카드 + 목록/필터/페이징) + 예약 상세 + 상담 기록 누적 + 상태 머신 | 상태 전이 동시성(409) 실측 + **예약 코드 동시 생성 중복 없음 실측**(F4) |
| 4 | 실장 관리(`consultants` CRUD + 비활성화) / 시술·수술 관리 | 4언어 탭 CRUD + **비활성 실장이 배정 드롭다운·KPI에서 빠지고 과거 예약엔 남는지 실측**(D13) |
| 5 | 예약 달력 | 월 범위 검증 + 부분 인덱스 사용 확인 |
| 6 | 실장 KPI / 예약 통계 | 빈 구간 0 채움 확인 |
| 7 | 계정 관리 / 감사 로그 | 3역할 CRUD가 모두 기록되는지 확인 |
| 8 | 유입 경로 분석(어드민 전용) | 비어드민 계정 접근 차단 실측 |
| 9 | SEO(hreflang·sitemap·JSON-LD) + 16장 전 항목 보안 감사 + 배포 | 라이브 curl 검증 |

**코딩 규칙 (절대 원칙)**
- 코드 중간 잘림 금지 — `// 나머지 동일`, `...` 생략 표현을 쓰지 않는다.
- 파일 수정 전 반드시 그 파일 전체를 읽고 시작한다.
- 코드 변경 전 영향 범위를 먼저 설명한다.
- 오류 수정 시 **원인 설명 → 수정 → 검증 방법 제시** 순서를 지킨다.
- 존재가 불확실한 라이브러리·함수는 사용 전 `[미확인]`을 명시한다.

---

## 20. 미결정 사항

| # | 항목 | 결정 필요 시점 |
|---|---|---|
| M2 | **도메인** — 실제 서비스 도메인 및 Cloudflare 계정 연결 | Phase 9 |
| M3 | **예약 코드 일련번호** — `WJ-YYMMDD-NNN`을 일별 리셋할지 전역 증가로 둘지 (동시 생성 대책은 F4 참고) | Phase 3 |
| M5 | **실장 자동 배정 규칙** — 신규 예약을 활성 실장(`consultants.is_active`)에게 자동 라운드로빈 배정할지, 수동 배정만 둘지 | Phase 3 |
| M6 | **랜딩 디자인** — 히어로·소개 섹션의 실제 콘텐츠(사용자가 "추후 디자인" 명시) | Phase 2 이후 |
| M7 | **개인정보 보유기간** — 예약 데이터·위챗ID의 파기 시점(처리방침에 기재할 값) | Phase 2 |

> M1(배포 브랜치 = `main`)·M4(예약금 통화 = CNY/KRW, 기본 CNY)는 2026-08-25에 확정되어 각각 4-3절·D12로 이동했다.

---

*이 문서는 설계 SSOT입니다. 구현 중 결정이 바뀌면 코드보다 이 문서를 먼저 갱신하세요.*
