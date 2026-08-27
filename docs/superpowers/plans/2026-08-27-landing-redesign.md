# 랜딩페이지 재설계 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 병원 소개 홈 + 11개 시술 카테고리 페이지 + 76개 시술 상세 페이지 + 상담 신청 페이지로 구성된 다국어(ko/zh-CN/zh-TW/en) 랜딩 사이트를 만든다.

**Architecture:** 정적 데이터 파일(`frontend/app/data/procedures.ts`) 1개 + 동적 라우트 페이지 2개(`procedures/[category].vue`, `procedures/[category]/[procedure].vue`)로 87개 페이지를 전부 커버한다. 관리자 CRUD(`Procedure` 테이블)와는 완전히 분리된 프론트엔드 전용 콘텐츠이며, 백엔드/DB 변경이 없다.

**Tech Stack:** Nuxt 4 + Vue 3 Composition API + Tailwind v4 + `@nuxtjs/i18n` + shadcn-vue(자동 임포트 `~/components/ui/*`) + `@lucide/vue` 아이콘 + reka-ui(Dropdown 등, 기존 `layouts/landing.vue` 패턴 재사용).

**설계 문서(SSOT):** `docs/superpowers/specs/2026-08-27-landing-redesign-design.md` — 이 계획의 모든 카피·이미지 파일명·카테고리 구조는 이 문서(특히 부록 A~G)에서 그대로 가져온다.

## Global Constraints

- 화면 깜빡임 금지: 이 기능의 모든 데이터는 정적 import이므로 비동기 fetch 자체가 없다 — SSR 프리로드 래퍼(`useAsyncData` 등) 불필요, `onMounted` 이후 별도 fetch 금지.
- i18n 번역 규칙: `frontend/i18n/locales/{ko,zh-CN,zh-TW,en}.json` 4개 파일의 키 집합은 항상 완전히 동일해야 한다 — 키 추가 시 4파일 세트로 수정 후 개수 대조.
- 코딩 규칙: 코드 중간 잘림 표현(`// 나머지 동일` 등) 금지, 파일 수정 전 전체 Read, 전체 수정 시 전체 출력.
- 이 프로�트에는 프론트엔드 단위 테스트 러너가 없다(`frontend/package.json`에 `test` 스크립트 없음, Vitest/Jest 미설치) — "테스트"는 `npm run build`(타입체크+빌드 성공)와 `npx nuxi dev` 기동 후 실제 브라우저 확인으로 대체한다. 새로 유닛 테스트 프레임워크를 도입하지 않는다(기존 관행 유지).
- 입력 필드 길이 제한·DB 쿼리 성능 원칙: 이 기능은 신규 input이나 DB 쿼리가 없으므로 해당 없음(설계 문서 9절 확인 완료).
- 라우트 슬러그·파일명은 설계 문서 부록 A(카테고리)·부록 C(이미지)를 그대로 사용 — 임의로 다른 이름을 짓지 않는다.

---

## Task 1: 시술 데이터 타입 + 카테고리 뼈대(11개, 메타데이터만)

카테고리 목록·라우팅에 필요한 최소 데이터부터 만든다. `items`/`intro`는 Task 2에서 채운다(모든 언어 원문이 다 갖춰진 뒤에 채워야 언어별 키 누락이 안 생긴다).

**Files:**
- Create: `frontend/app/data/procedures.ts`

**Interfaces:**
- Produces: `type Locale`, `interface ProcedureItem`, `interface ProcedureOtherItem`, `interface ProcedureCategory`, `export const PROCEDURE_CATEGORIES: ProcedureCategory[]`, `export const CATEGORY_SLUGS: string[]`

- [ ] **Step 1: 타입 정의 + 11개 카테고리 메타데이터(이름 4개 언어·아이콘·히어로 이미지·slug) 작성**

`docs/superpowers/specs/2026-08-27-landing-redesign-design.md` 부록 A(카테고리 목록 및 슬러그)와 부록 C(이미지 파일명)를 그대로 옮긴다.

```ts
// frontend/app/data/procedures.ts
export type Locale = 'zh-CN' | 'zh-TW' | 'en' | 'ko'

export interface ProcedureItem {
  slug: string
  name: Record<Locale, string>
  concerns: Record<Locale, string[]>
  description: Record<Locale, string>
  /** 파일명만(예: 'eye-glam-eye.png') — /img/{imageCategory ?? category.slug}/ 아래 위치 */
  image: string
  /** 다른 카테고리 폴더의 이미지를 재사용할 때만 지정(남자 코성형→men, 여유증→men) */
  imageCategory?: string
  /** 상세페이지 라벨(대체 명칭 등, 없으면 undefined) */
  /** 부록 D(ko)·F(en)만 별도 라벨 컬럼이 있고 부록 E/G(zh-CN/zh-TW)는 라벨을 따로 추출하지 않았다 — 언어별로 있는 것만 채운다 */
  label?: Partial<Record<Locale, string>>
}

export interface ProcedureOtherItem {
  slug: string
  name: Record<Locale, string>
}

export interface ProcedureCategory {
  slug: string
  name: Record<Locale, string>
  /** @lucide/vue 아이콘 컴포넌트명 */
  icon: string
  /** /img/hero/ 아래 파일명 목록 — 1개면 고정, 여러 개면 로테이션(재건만 3개) */
  heroImages: string[]
  intro: Record<Locale, string>
  items: ProcedureItem[]
  otherItems: ProcedureOtherItem[]
}

export const PROCEDURE_CATEGORIES: ProcedureCategory[] = [
  {
    slug: 'eye',
    name: { ko: '눈', 'zh-CN': '眼部', 'zh-TW': '眼部', en: 'Eyes' },
    icon: 'Eye',
    heroImages: ['eye-hero.jpg'],
    intro: { ko: '', 'zh-CN': '', 'zh-TW': '', en: '' },
    items: [],
    otherItems: [],
  },
  {
    slug: 'nose',
    name: { ko: '코', 'zh-CN': '鼻部', 'zh-TW': '鼻部', en: 'Nose' },
    icon: 'ScanFace',
    heroImages: ['nose-hero.jpg'],
    intro: { ko: '', 'zh-CN': '', 'zh-TW': '', en: '' },
    items: [],
    otherItems: [],
  },
  {
    slug: 'ent',
    name: { ko: '이비인후과(코)', 'zh-CN': '耳鼻喉科（鼻部）', 'zh-TW': '耳鼻喉科（鼻部）', en: 'ENT (Nose)' },
    icon: 'Stethoscope',
    heroImages: ['ent-hero.jpg'],
    intro: { ko: '', 'zh-CN': '', 'zh-TW': '', en: '' },
    items: [],
    otherItems: [],
  },
  {
    slug: 'lifting',
    name: { ko: '리프팅', 'zh-CN': '提拉', 'zh-TW': '拉提', en: 'Lifting' },
    icon: 'TrendingUp',
    heroImages: ['lifting-hero.jpg'],
    intro: { ko: '', 'zh-CN': '', 'zh-TW': '', en: '' },
    items: [],
    otherItems: [],
  },
  {
    slug: 'dermatology',
    name: { ko: '피부과', 'zh-CN': '皮肤科', 'zh-TW': '皮膚科', en: 'Dermatology' },
    icon: 'Sparkles',
    heroImages: ['dermatology-hero.jpg'],
    intro: { ko: '', 'zh-CN': '', 'zh-TW': '', en: '' },
    items: [],
    otherItems: [],
  },
  {
    slug: 'stemcell',
    name: { ko: '줄기세포', 'zh-CN': '干细胞', 'zh-TW': '幹細胞', en: 'Stem Cell' },
    icon: 'Dna',
    heroImages: ['stemcell-hero.png'],
    intro: { ko: '', 'zh-CN': '', 'zh-TW': '', en: '' },
    items: [],
    otherItems: [],
  },
  {
    slug: 'breast',
    name: { ko: '가슴', 'zh-CN': '胸部', 'zh-TW': '胸部', en: 'Breast' },
    icon: 'Heart',
    heroImages: ['breast-hero.jpg'],
    intro: { ko: '', 'zh-CN': '', 'zh-TW': '', en: '' },
    items: [],
    otherItems: [],
  },
  {
    slug: 'contour',
    name: { ko: '윤곽·양악', 'zh-CN': '面部轮廓・双颌', 'zh-TW': '臉部輪廓・雙顎', en: 'Facial Contouring & Double Jaw' },
    icon: 'Scan',
    heroImages: ['contour-hero.jpg'],
    intro: { ko: '', 'zh-CN': '', 'zh-TW': '', en: '' },
    items: [],
    otherItems: [],
  },
  {
    slug: 'bodyline',
    name: { ko: '체형', 'zh-CN': '体型', 'zh-TW': '體型', en: 'Body Contouring' },
    icon: 'PersonStanding',
    heroImages: ['bodyline-hero.jpg'],
    intro: { ko: '', 'zh-CN': '', 'zh-TW': '', en: '' },
    items: [],
    otherItems: [],
  },
  {
    slug: 'men',
    name: { ko: '남자', 'zh-CN': '男性', 'zh-TW': '男性', en: 'Men' },
    icon: 'UserRound',
    heroImages: ['men-hero.jpg'],
    intro: { ko: '', 'zh-CN': '', 'zh-TW': '', en: '' },
    items: [],
    otherItems: [],
  },
  {
    slug: 'reconstruction',
    name: { ko: '재건', 'zh-CN': '修复重建', 'zh-TW': '重建', en: 'Reconstructive Surgery' },
    icon: 'HeartHandshake',
    heroImages: ['reconstruction-hero01.jpg', 'reconstruction-hero02.jpg', 'reconstruction-hero03.jpg'],
    intro: { ko: '', 'zh-CN': '', 'zh-TW': '', en: '' },
    items: [],
    otherItems: [],
  },
]

export const CATEGORY_SLUGS = PROCEDURE_CATEGORIES.map(c => c.slug)

export function findCategory(slug: string): ProcedureCategory | undefined {
  return PROCEDURE_CATEGORIES.find(c => c.slug === slug)
}

export function findProcedure(categorySlug: string, procedureSlug: string) {
  const category = findCategory(categorySlug)
  if (!category) return { category: undefined, item: undefined, other: undefined }
  const item = category.items.find(i => i.slug === procedureSlug)
  const other = category.otherItems.find(i => i.slug === procedureSlug)
  return { category, item, other }
}
```

`icon` 값(`Eye`, `ScanFace`, `Stethoscope`, `TrendingUp`, `Sparkles`, `Dna`, `Heart`, `Scan`, `PersonStanding`, `UserRound`, `HeartHandshake`)은 `@lucide/vue`에 실제 존재하는 아이콘명이다(Task 6에서 `import * as icons from '@lucide/vue'` 후 `icons[category.icon]`으로 동적 렌더링).

- [ ] **Step 2: 빌드 확인**

Run: `cd frontend && npm run build`
Expected: 에러 없이 빌드 성공(타입 에러 없음 — `items`/`otherItems`가 빈 배열이어도 타입은 유효함).

- [ ] **Step 3: Commit**

```bash
git add frontend/app/data/procedures.ts
git commit -m "feat: 시술 데이터 타입 및 11개 카테고리 메타데이터 뼈대 추가"
```

---

## Task 2: 76개 시술 데이터 전량 입력 (4개 언어)

**✅ 확인 완료(2026-08-27)**: `docs/superpowers/specs/2026-08-27-landing-redesign-design.md`의 부록 D(한국어)·E(중국어 zh-CN)·F(영어)·G(번체 zh-TW) 전부 존재한다 — 76개 시술·11개 카테고리 소개문 4개 언어 원문이 모두 준비된 상태다.

**Files:**
- Modify: `frontend/app/data/procedures.ts` (Task 1에서 만든 각 카테고리의 `intro`/`items`/`otherItems` 채우기)

**Interfaces:**
- Consumes: Task 1의 `ProcedureCategory`, `ProcedureItem`, `ProcedureOtherItem` 타입, `PROCEDURE_CATEGORIES` 배열(11개 카테고리 이미 존재, 각 항목의 `items`/`otherItems`를 in-place로 채움)

- [ ] **Step 1: 카테고리별 `intro` 4개 언어 채우기**

각 카테고리 객체의 `intro`를 아래 매핑대로 채운다(ko=부록D, zh-CN=부록E, en=부록F, zh-TW=부록G의 "카테고리소개" 항목). 예시(eye):

```ts
intro: {
  ko: '눈은 첫인상을 결정하는 데 가장 중요한 부분으로 손꼽히는부위입니다. WJ 원진은 개인마다 지니고 있는 얼굴의 밸런스와 피부타입이다른 점을 고려하여 상담을 통해 본인의 개성을 살리면서 자연스럽고조화가 잘 이루어질 수 있는 수술법을 추천해드립니다. WJ 원진의 눈 성형은정밀한 진단과 상담을 바탕으로 가장 적합한 수술 방법을 선택해완성도 높은 수술 결과를 보장합니다.',
  'zh-CN': '眼部是决定一个人第一印象的重要部位。WJ原辰考虑到每个人面部及皮肤类型的差异，通过咨询推荐可以彰显个性的同时追求自然跟面部协调的手术方案。WJ原辰的眼部整形以精密诊断及详细咨询为基础，选择最适合的手术方案，保障高满意度的手术效果。',
  en: '(부록 F에서 복사)',
  'zh-TW': '(부록 G에서 복사)',
},
```

나머지 10개 카테고리(nose, ent, lifting, dermatology, stemcell, breast, contour, bodyline, men, reconstruction)도 동일하게 부록 D/E/F/G의 "카테고리소개"를 그대로 복사한다. **men 카테고리는 부록 E에 자체 소개문이 없다** — 부록 D(men)의 소개문(한국어 원문)과, 이걸 F/G에서 각각 영어·번체로 번역한 결과를 사용한다.

- [ ] **Step 2: 각 카테고리의 `items` 배열 채우기 (67개 시술)**

카테고리마다 부록 D 표의 각 행을 `ProcedureItem`으로 변환한다. 예시(eye 카테고리의 `items` 배열, 8개):

```ts
items: [
  {
    slug: 'glam-eye',
    name: {
      ko: '비절개 눈매교정 - 글램아이',
      'zh-CN': '非切开眼型矫正－Glam Eye',
      'zh-TW': '非切開眼型矯正－Glam Eye',
      en: 'Non-Incisional Eye Shape Correction – Glam Eye',
    },
    concerns: {
      ko: ['티 안 나게 눈이 또렷하고 시원하게 커지기를 원해요.', '이마를 이용하여 눈을 뜨는 습관이 있어요.', '쌍꺼풀이 여러 겹이거나 짝짝이에요.'],
      en: ['I want my eyes to look bright and clear, larger, without it being obvious.', 'I have a habit of using my forehead muscles to open my eyes.', 'My double eyelids are multiple or uneven.'],
      // 부록 E(zh-CN)는 이 시술의 상세페이지에 고민 불릿이 없었다(카테고리 목록 페이지에만 있던 패턴과 다름) —
      // 지어내지 않고 빈 배열로 둔다. 부록 G(zh-TW)도 zh-CN 기반이라 동일하게 없음.
      'zh-CN': [],
      'zh-TW': [],
    },
    description: {
      ko: '쌍꺼풀과 눈매교정은 물론 속 눈썹의 위치까지 교정하여아름답고 호감 가는 눈매로 만들어 드리는 토탈 눈 성형입니다.',
      'zh-CN': '双眼皮手术与眼型矫正同时进行，还可以调整睫毛的位置整体提升眼部魅力的综合眼部手术。',
      en: 'This is a total eye surgery that corrects not only the double eyelid and eye shape but also the position of the lower lashes, creating a beautiful, appealing eye shape.',
      'zh-TW': '雙眼皮手術與眼型矯正同時進行，還可以調整睫毛的位置整體提升眼部魅力的綜合眼部手術。',
    },
    // 부록 D(ko)·F(en)만 별도 라벨이 추출됐고 부록 E/G(zh-CN/zh-TW)는 라벨 컬럼 자체가 없었다 — 있는 언어만 기입.
    label: { ko: '쌍꺼풀과 눈매 교정을 동시에', en: 'Double Eyelid and Eye Shape Correction at Once' },
    image: 'eye-glam-eye.png',
  },
  // ... 나머지 7개(더블유착/엔젤아이/오픈아이/눈재수술/고도안검하수/눈밑지방재배치/중년눈성형)도 동일 패턴
  // ... 짝눈(비대칭) 교정, 소아 선천성 안검하수 — 부록 D의 [번역] 표시 행 그대로 사용(사진 없음 주의, image 필드는 Step 4 참고)
],
```

**Step 3 (고민 불릿 언어 간 개수 불일치 처리)**: 부록 D(한국어)·E(중국어)는 시술에 따라 고민 불릿 개수가 다르거나(3~4개), zh-CN은 상세페이지 자체에 고민 불릿이 아예 없는 경우가 흔하다(카테고리 목록 페이지에만 있던 패턴과 다름 — eye 카테고리 8개 중 6개가 이 경우, glam-eye 예시 참고). **없으면 빈 배열 `[]`로 두고 한국어 걸 번역해서 채우지 않는다**(지어내는 것과 같음). **번역 규칙("원문 줄 수 == 번역 줄 수")은 각 언어 안에서 항목 수가 일치해야 한다는 뜻이 아니라, 번역 작업 자체(부록 F/G 생성)가 원문 줄 수를 유지했는지를 검증하는 규칙이다** — 부록 D의 한국어 고민불릿이 3개면 부록 F(영어)도 3개여야 하고, 부록 E의 중국어가 0개(없음)면 부록 G(번체)도 0개(빈 배열)여야 한다. 언어마다 원본 소스(D vs E)가 다르므로 언어 간 개수가 다른 것은 정상이며, 화면에도 그 언어에서만 고민 불릿이 안 보이는 비대칭이 생길 수 있다 — 의도된 데이터 한계다.

**Step 4 (교차 재사용 2건)**: `nose` 카테고리의 "남자 코성형"과 `breast` 카테고리의 "여성형 유방(여유증)"은 `men` 카테고리에 있는 동일 시술과 콘텐츠·이미지가 완전히 같다. `men.items`에서 해당 객체를 찾아 **동일한 값으로** 채우되 `slug`만 각 카테고리에 맞게 다르게 쓰고, `image`/`imageCategory`는 `men` 폴더를 가리키게 한다:

```ts
// nose.items 안에 추가
{
  slug: 'male-rhinoplasty',
  name: { ko: '남자 코성형', 'zh-CN': '男性鼻整形', 'zh-TW': '男性鼻整形', en: 'Male Rhinoplasty' },
  concerns: { /* men.items의 남자 코성형과 동일 값 복사 */ },
  description: { /* men.items의 남자 코성형과 동일 값 복사 */ },
  label: { /* men.items의 남자 코성형과 동일 값 복사 */ },
  image: 'men-rhinoplasty.png',
  imageCategory: 'men',
},
```

`breast.items`의 "여성형 유방(여유증)"도 동일한 방식으로 `men`의 "여유증" 콘텐츠를 복사하고 `imageCategory: 'men'`, `image: 'men-gynecomastia.png'`로 지정한다.

- [ ] **Step 5: 각 카테고리의 `otherItems` 채우기 (9개, 콘텐츠 없음)**

부록 B(전체 시술 목록)에서 이름만 가져온다. 사진·설명이 아예 없는 9개뿐이다:

```ts
// breast.otherItems
otherItems: [
  { slug: 'fat-grafting', name: { ko: '가슴 지방 이식', 'zh-CN': '胸部脂肪填充', 'zh-TW': '胸部脂肪填補', en: 'Breast Fat Grafting' } },
  { slug: 'postpartum-breast', name: { ko: '출산 후 가슴 성형', 'zh-CN': '产后胸部整形', 'zh-TW': '產後胸部整形', en: 'Postpartum Breast Surgery' } },
  { slug: 'stemcell-breast-augmentation', name: { ko: '줄기세포 가슴 성형', 'zh-CN': '干细胞隆胸', 'zh-TW': '幹細胞隆乳', en: 'Stem Cell Breast Augmentation' } },
],

// contour.otherItems
otherItems: [
  { slug: 'self-designed-double-jaw', name: { ko: '셀프 양악수술', 'zh-CN': '自主双颌手术', 'zh-TW': '自主雙顎手術', en: 'Self-Designed Double Jaw Surgery' } },
  { slug: 'underbite-surgery', name: { ko: '주걱턱 수술', 'zh-CN': '地包天手术', 'zh-TW': '戽斗手術', en: 'Underbite Surgery' } },
  { slug: 'receding-chin', name: { ko: '무턱(하악 왜소증) 수술', 'zh-CN': '下巴后缩（下颌发育不足）手术', 'zh-TW': '下巴後縮（下顎發育不足）手術', en: 'Receding Chin (Mandibular Hypoplasia) Surgery' } },
  { slug: 'facial-contouring-revision', name: { ko: '안면윤곽 재수술', 'zh-CN': '面部轮廓修复手术', 'zh-TW': '臉部輪廓修復手術', en: 'Revision Facial Contouring Surgery' } },
  { slug: 'double-jaw-revision', name: { ko: '양악 재수술', 'zh-CN': '双颌修复手术', 'zh-TW': '雙顎修復手術', en: 'Revision Double Jaw Surgery' } },
  { slug: 'postoperative-orthodontics', name: { ko: '수술 후 교정', 'zh-CN': '术后牙齿矫正', 'zh-TW': '術後牙齒矯正', en: 'Postoperative Orthodontic Treatment' } },
],
```

다른 9개 카테고리(이 둘을 제외한 나머지)는 `otherItems: []`로 둔다(전부 콘텐츠 확보됨).

- [ ] **Step 6: 항목 수 자체 검증**

각 카테고리의 `items.length + otherItems.length`가 설계 문서 부록 A의 항목 수와 정확히 일치하는지 대조한다: eye 10, nose 9, ent 5, lifting 6, dermatology 7, stemcell 5, breast 10, contour 14, bodyline 3, men 4, reconstruction 3 (합계 76). 아래 스크립트로 확인:

```bash
cd frontend && node -e "
const { PROCEDURE_CATEGORIES } = require('./app/data/procedures.ts');
" 2>/dev/null || npx tsx -e "
import { PROCEDURE_CATEGORIES } from './frontend/app/data/procedures.ts'
let total = 0
for (const c of PROCEDURE_CATEGORIES) {
  const n = c.items.length + c.otherItems.length
  console.log(c.slug, n)
  total += n
}
console.log('TOTAL', total)
"
```

Expected: 카테고리별 개수가 위 목록과 정확히 일치, TOTAL 76.

- [ ] **Step 7: 빌드 확인**

Run: `cd frontend && npm run build`
Expected: 빌드 성공, 타입 에러 없음(모든 `Record<Locale, string>` 필드가 4개 키를 다 채웠는지 TypeScript가 검증한다 — 하나라도 빠지면 컴파일 에러로 즉시 드러남).

- [ ] **Step 8: Commit**

```bash
git add frontend/app/data/procedures.ts
git commit -m "feat: 76개 시술 데이터 4개 언어 전량 입력"
```

---

## Task 3: UTM 추적 컴포저블

현재 `index.vue`는 같은 페이지에서 폼을 바로 제출하므로 `route.query`의 UTM 파라미터를 그 자리에서 읽어 쓴다. 이번 개편으로 홈(`/`) → 카테고리 → 상세 → `/inquiry`로 여러 페이지를 거치게 되므로, 홈에서 잡은 UTM을 쿠키에 저장해 `/inquiry` 제출 시점까지 유지해야 한다(`docs/design.md`의 "유입 경로 자동 기록" 원칙 유지).

**Files:**
- Create: `frontend/app/composables/useUtm.ts`

**Interfaces:**
- Produces: `function captureUtm(): void`, `function getUtm(): { utmSource: string, utmMedium: string, utmCampaign: string, referralCode: string }`

- [ ] **Step 1: 컴포저블 작성**

```ts
// frontend/app/composables/useUtm.ts
// 홈(랜딩)에서 잡은 UTM/추천코드를 30일 쿠키에 저장해, 여러 페이지를 거쳐 /inquiry에서
// 제출할 때까지 유지한다(단일 페이지였던 기존 구조에서 다중 페이지로 바뀌며 필요해짐).
interface UtmData {
  utmSource: string
  utmMedium: string
  utmCampaign: string
  referralCode: string
}

const EMPTY_UTM: UtmData = { utmSource: '', utmMedium: '', utmCampaign: '', referralCode: '' }

export function captureUtm() {
  const route = useRoute()
  const cookie = useCookie<UtmData>('wj_utm', { maxAge: 60 * 60 * 24 * 30, sameSite: 'lax' })
  const fromQuery: UtmData = {
    utmSource: (route.query.utm_source as string) || '',
    utmMedium: (route.query.utm_medium as string) || '',
    utmCampaign: (route.query.utm_campaign as string) || '',
    referralCode: (route.query.ref as string) || '',
  }
  // 쿼리에 UTM이 하나라도 있으면 새로 덮어쓴다(최신 유입 경로 우선) — 없으면 기존 쿠키값 유지.
  if (fromQuery.utmSource || fromQuery.utmMedium || fromQuery.utmCampaign || fromQuery.referralCode) {
    cookie.value = fromQuery
  }
}

export function getUtm(): UtmData {
  const cookie = useCookie<UtmData>('wj_utm')
  return cookie.value ?? EMPTY_UTM
}
```

- [ ] **Step 2: 빌드 확인**

Run: `cd frontend && npm run build`
Expected: 빌드 성공.

- [ ] **Step 3: Commit**

```bash
git add frontend/app/composables/useUtm.ts
git commit -m "feat: 다중 페이지 유입경로 유지를 위한 UTM 쿠키 컴포저블 추가"
```

---

## Task 4: 상담 신청 페이지(`/inquiry`) — 기존 폼 이전

`frontend/app/pages/index.vue`의 폼 부분을 그대로 옮긴다. 검증 로직·API 호출·honeypot은 변경하지 않는다. UTM은 `route.query` 직접 읽기 대신 `getUtm()`을 쓴다.

**Files:**
- Create: `frontend/app/pages/inquiry.vue`
- Modify: `frontend/app/pages/index.vue` (Task 5에서 홈으로 재작성하며 폼 제거)

**Interfaces:**
- Consumes: Task 3의 `getUtm()`

- [ ] **Step 1: `inquiry.vue` 작성**

기존 `frontend/app/pages/index.vue`(현재 172~224번 줄의 `<script setup>` 로직, 1~110번 줄의 템플릿)를 그대로 옮기되:
- `landing-visit` 내부 추적 호출(15-1절)은 **제거**한다 — 이제 실제 광고 랜딩 지점은 `/`(홈)이므로 그쪽으로 옮긴다(Task 5).
- UTM은 `route.query`에서 직접 읽지 않고 `getUtm()`으로 가져온다.
- `useSeo` 타이틀을 상담신청 전용으로 바꾼다.

```vue
<!-- frontend/app/pages/inquiry.vue -->
<template>
  <div class="mx-auto max-w-3xl px-4 py-10">
    <section class="mb-10 text-center">
      <h1 class="text-2xl font-semibold text-foreground">{{ t('landing.hero.title') }}</h1>
      <p class="mt-2 text-muted-foreground">{{ t('landing.hero.subtitle') }}</p>
    </section>

    <Card>
      <CardHeader>
        <CardTitle>{{ t('landing.form.title') }}</CardTitle>
      </CardHeader>
      <CardContent>
        <div v-if="successResult" class="flex flex-col gap-3">
          <p class="text-foreground">{{ t('landing.success.message') }}</p>
          <p class="text-sm text-muted-foreground">
            {{ t('landing.success.codeLabel') }}: <span class="font-mono font-semibold text-foreground">{{ successResult.code }}</span>
          </p>
          <p class="text-sm text-muted-foreground">
            {{ t('landing.success.wechatLabel') }}: <span class="font-semibold text-foreground">{{ successResult.wechatId }}</span>
          </p>
        </div>

        <form v-else class="flex flex-col gap-4" novalidate @submit.prevent="submit">
          <div class="flex flex-col gap-2">
            <Label for="name">{{ t('landing.form.name') }}</Label>
            <Input id="name" v-model="name" type="text" maxlength="50" required autocomplete="name" :aria-invalid="errors.name" />
            <p v-if="errors.name" class="text-sm text-destructive">{{ t('common.fieldRequired') }}</p>
          </div>

          <div class="flex flex-col gap-2">
            <Label for="birthDate">{{ t('landing.form.birthDate') }}</Label>
            <DatePicker id="birthDate" v-model="birthDate" :locale="inputLang" :invalid="errors.birthDate" />
            <p v-if="errors.birthDate" class="text-sm text-destructive">{{ t('common.fieldRequired') }}</p>
          </div>

          <div class="flex flex-col gap-2">
            <span class="text-sm leading-none font-medium">{{ t('landing.form.gender') }}</span>
            <div class="flex gap-4">
              <label class="flex items-center gap-2 text-sm">
                <input v-model="gender" type="radio" name="gender" value="Female" class="accent-primary" required>
                {{ t('landing.form.genderFemale') }}
              </label>
              <label class="flex items-center gap-2 text-sm">
                <input v-model="gender" type="radio" name="gender" value="Male" class="accent-primary">
                {{ t('landing.form.genderMale') }}
              </label>
              <label class="flex items-center gap-2 text-sm">
                <input v-model="gender" type="radio" name="gender" value="Other" class="accent-primary">
                {{ t('landing.form.genderOther') }}
              </label>
            </div>
            <p v-if="errors.gender" class="text-sm text-destructive">{{ t('common.fieldRequired') }}</p>
          </div>

          <div class="flex flex-col gap-2">
            <Label for="wechatId">{{ t('landing.form.wechatId') }}</Label>
            <Input id="wechatId" v-model="wechatId" type="text" maxlength="50" required autocomplete="off" :aria-invalid="errors.wechatId" />
            <p v-if="errors.wechatId" class="text-sm text-destructive">{{ t('common.fieldRequired') }}</p>
          </div>

          <div class="flex flex-col gap-2">
            <Label for="contactTime">{{ t('landing.form.contactTime') }}</Label>
            <TimePicker id="contactTime" v-model="contactTime" :locale="inputLang" :invalid="errors.contactTime" />
            <p v-if="errors.contactTime" class="text-sm text-destructive">{{ t('common.fieldRequired') }}</p>
          </div>

          <div class="absolute -left-[9999px]" aria-hidden="true">
            <label for="hpField">Website</label>
            <input id="hpField" v-model="honeypot" type="text" tabindex="-1" autocomplete="off">
          </div>

          <div class="flex flex-col gap-1">
            <label class="flex items-start gap-2 text-sm">
              <input v-model="consent" type="checkbox" class="mt-1 accent-primary" required>
              <span>{{ t('landing.form.consentPrefix') }}<button type="button" class="underline" @click="privacyOpen = true">{{ t('landing.form.consentLink') }}</button>{{ t('landing.form.consentSuffix') }}</span>
            </label>
            <p v-if="errors.consent" class="text-sm text-destructive">{{ t('common.fieldRequired') }}</p>
          </div>

          <dialog
            ref="privacyDialogEl"
            class="w-[calc(100%-2rem)] max-w-2xl rounded-lg border border-border bg-card p-0 text-foreground backdrop:bg-black/50"
            @click="onPrivacyBackdropClick"
            @close="privacyOpen = false"
          >
            <div class="flex items-center justify-between border-b border-border px-5 py-3">
              <h2 class="font-semibold text-foreground">{{ t('privacy.title') }}</h2>
              <button type="button" class="text-muted-foreground hover:text-foreground" :aria-label="t('common.close')" @click="privacyOpen = false">
                <X class="size-5" />
              </button>
            </div>
            <div class="max-h-[70vh] overflow-y-auto px-5 py-4">
              <PrivacyContent />
            </div>
          </dialog>

          <p v-if="errorMessage" class="text-sm text-destructive">{{ errorMessage }}</p>

          <Button type="submit" :disabled="submitting">{{ t('landing.form.submit') }}</Button>
        </form>
      </CardContent>
    </Card>
  </div>
</template>

<script setup lang="ts">
import { X } from '@lucide/vue'

definePageMeta({ layout: 'landing' })

const { t, locale } = useI18n()

const inputLang = useInputLang()

useSeo({
  title: () => t('landing.hero.title'),
  description: () => t('landing.hero.subtitle'),
})

const name = ref('')
const birthDate = ref('')
const gender = ref('')
const wechatId = ref('')
const contactTime = ref('')
const consent = ref(false)
const honeypot = ref('')

const privacyOpen = ref(false)
const privacyDialogEl = ref<HTMLDialogElement | null>(null)
watch(privacyOpen, (open) => {
  if (open) privacyDialogEl.value?.showModal()
  else privacyDialogEl.value?.close()
})
function onPrivacyBackdropClick(e: MouseEvent) {
  if (e.target === privacyDialogEl.value) privacyOpen.value = false
}

const submitting = ref(false)
const errorMessage = ref('')
const successResult = ref<{ code: string; wechatId: string } | null>(null)

const errors = reactive({
  name: false,
  birthDate: false,
  gender: false,
  wechatId: false,
  contactTime: false,
  consent: false,
})

function validate(): boolean {
  errors.name = !name.value.trim()
  errors.birthDate = !birthDate.value
  errors.gender = !gender.value
  errors.wechatId = !wechatId.value.trim()
  errors.contactTime = !contactTime.value
  errors.consent = !consent.value
  return !Object.values(errors).some(Boolean)
}

async function submit() {
  errorMessage.value = ''
  if (!validate()) return
  submitting.value = true
  try {
    const utm = getUtm()
    const res = await $fetch<{ code: string; wechatId: string }>('/api/reservations', {
      method: 'POST',
      body: {
        name: name.value,
        birthDate: birthDate.value,
        gender: gender.value,
        wechatId: wechatId.value,
        preferredContactTime: `${contactTime.value}:00`,
        locale: locale.value,
        privacyConsent: consent.value,
        honeypot: honeypot.value,
        utmSource: utm.utmSource,
        utmMedium: utm.utmMedium,
        utmCampaign: utm.utmCampaign,
        referralCode: utm.referralCode,
      },
    })
    successResult.value = res
  } catch (e: any) {
    const code = (e?.data?.code as string | undefined) ?? 'SUBMIT_FAILED'
    errorMessage.value = t(`errors.${code}`)
  } finally {
    submitting.value = false
  }
}
</script>
```

- [ ] **Step 2: 빌드 확인**

Run: `cd frontend && npm run build`
Expected: 빌드 성공.

- [ ] **Step 3: Commit**

```bash
git add frontend/app/pages/inquiry.vue
git commit -m "feat: 상담 신청 폼을 /inquiry 페이지로 이전"
```

---

## Task 5: 홈페이지 재작성 — 병원소개 + 카테고리 바로가기

`frontend/app/pages/index.vue`를 히어로(축소) → 카테고리 바로가기 그리드 → 병원소개(플레이스홀더, 병원 제공 대기) → 로 재작성한다. `landing-visit` 추적과 UTM 캡처를 여기로 옮긴다.

**Files:**
- Modify: `frontend/app/pages/index.vue` (전체 재작성)

**Interfaces:**
- Consumes: Task 1의 `PROCEDURE_CATEGORIES`, Task 3의 `captureUtm()`

- [ ] **Step 1: 전체 파일 재작성**

```vue
<!-- frontend/app/pages/index.vue -->
<template>
  <div>
    <section class="mx-auto max-w-3xl px-4 py-12 text-center">
      <h1 class="text-3xl font-bold text-foreground">{{ t('landing.home.heroTitle') }}</h1>
      <p class="mt-3 text-muted-foreground">{{ t('landing.home.heroSubtitle') }}</p>
    </section>

    <section class="border-y bg-muted/30 py-10">
      <div class="mx-auto max-w-3xl px-4">
        <h2 class="mb-6 text-center text-lg font-semibold text-foreground">{{ t('landing.home.categoriesHeading') }}</h2>
        <div class="grid grid-cols-3 gap-3 sm:grid-cols-4">
          <NuxtLink
            v-for="category in PROCEDURE_CATEGORIES"
            :key="category.slug"
            :to="localePath({ name: 'procedures-category', params: { category: category.slug } })"
            class="flex flex-col items-center gap-2 rounded-lg border bg-card p-3 text-center transition-colors hover:border-primary"
          >
            <component :is="categoryIcon(category.icon)" class="size-6 text-primary" />
            <span class="text-xs font-medium text-foreground">{{ category.name[locale as Locale] }}</span>
          </NuxtLink>
        </div>
      </div>
    </section>

    <section class="mx-auto max-w-3xl px-4 py-12">
      <h2 class="mb-4 text-xl font-semibold text-foreground">{{ t('landing.home.introHeading') }}</h2>
      <p class="whitespace-pre-line text-muted-foreground">{{ t('landing.home.introBody') }}</p>
    </section>
  </div>
</template>

<script setup lang="ts">
import * as icons from '@lucide/vue'
import { PROCEDURE_CATEGORIES, type Locale } from '~/data/procedures'

definePageMeta({ layout: 'landing' })

const { t, locale } = useI18n()
const localePath = useLocalePath()

useSeo({
  title: () => t('landing.home.heroTitle'),
  description: () => t('landing.home.heroSubtitle'),
})

function categoryIcon(name: string) {
  return (icons as Record<string, unknown>)[name]
}

// UTM 캡처(Task 3) — 이제 광고 랜딩 지점은 홈이므로 여기서 잡아 쿠키에 저장한다.
captureUtm()

// 15-1절 — 랜딩 SSR 시점에 프론트 서버가 내부 시크릿 헤더와 함께 방문을 기록한다.
// 🔴 await 하지 않는다(F6) — 방문 집계 실패·지연이 랜딩 렌더 응답 시간에 영향을 주면 안 된다.
const route = useRoute()
const config = useRuntimeConfig()
if (import.meta.server) {
  const utmQuery = {
    referralCode: (route.query.ref as string) || '',
    utmSource: (route.query.utm_source as string) || '',
    utmMedium: (route.query.utm_medium as string) || '',
    utmCampaign: (route.query.utm_campaign as string) || '',
  }
  $fetch(`${config.apiBaseInternal}/api/internal/landing-visit`, {
    method: 'POST',
    headers: { 'X-Internal-Secret': config.internalSecret as string },
    body: utmQuery,
    timeout: 2000,
  }).catch(() => {})
}
</script>
```

`landing.home.introBody`는 부록 D(병원소개 콘텐츠, 아직 병원 제공 대기 — 설계 문서 10절 3번)가 오기 전까지는 i18n 키 자체는 존재해야 하므로(4개 로케일 키 집합 규칙), Task 7에서 "준비 중" 안내 문구로 채우고 실제 병원소개 문구가 오면 교체한다.

- [ ] **Step 2: 빌드 확인**

Run: `cd frontend && npm run build`
Expected: 빌드는 아직 실패할 수 있음(i18n 키·`localePath`의 `procedures-category` 라우트명은 Task 8에서 생김, `captureUtm`은 auto-import 대상). 이 태스크만으로 최종 그린을 기대하지 말고, Task 8·10 완료 후 다시 빌드해서 확인한다(아래 "최종 검증" 참고).

- [ ] **Step 3: Commit**

```bash
git add frontend/app/pages/index.vue
git commit -m "feat: 홈페이지를 병원소개+카테고리 바로가기 구조로 재작성"
```

---

## Task 6: 상시 문의 배지(FAB)

**Files:**
- Create: `frontend/app/components/InquiryFab.vue`

- [ ] **Step 1: 컴포넌트 작성**

데스크톱은 아이콘+텍스트, 모바일은 아이콘만(Tailwind `hidden sm:inline`으로 순수 CSS 처리 — JS 분기 불필요).

```vue
<!-- frontend/app/components/InquiryFab.vue -->
<template>
  <NuxtLink
    :to="localePath('inquiry')"
    class="fixed right-5 bottom-5 z-40 flex items-center gap-2 rounded-full bg-primary px-4 py-3 text-sm font-semibold text-primary-foreground shadow-lg transition-transform hover:scale-105"
  >
    <MessageCircle class="size-5 shrink-0" />
    <span class="hidden sm:inline">{{ t('landing.nav.inquiryFab') }}</span>
  </NuxtLink>
</template>

<script setup lang="ts">
import { MessageCircle } from '@lucide/vue'

const { t } = useI18n()
const localePath = useLocalePath()
</script>
```

- [ ] **Step 2: 빌드 확인**

Run: `cd frontend && npm run build`
Expected: 빌드 성공(아직 아무 페이지도 이 컴포넌트를 쓰지 않지만 컴파일은 통과해야 함).

- [ ] **Step 3: Commit**

```bash
git add frontend/app/components/InquiryFab.vue
git commit -m "feat: 상시 문의 배지(FAB) 컴포넌트 추가"
```

---

## Task 7: 내비게이션 개편 — 홈/시술안내 드롭다운/문의하기 + FAB 삽입

기존 헤더는 로고+언어선택만 있다. "홈 / 시술안내(11개 드롭다운) / 문의하기" 3개 링크를 추가하고 FAB을 레이아웃에 삽입한다.

**참고**: 원래 설계(문서 7절)는 "데스크톱 드롭다운 / 모바일 햄버거"였으나, 실제 기존 레이아웃이 `max-w-3xl`의 좁은 중앙 정렬 컬럼(넓은 가로 내비바 자체가 없음)이라 별도 햄버거 메뉴 없이 드롭다운 하나로 데스크톱·모바일 공통 처리한다 — 컴포넌트 수를 늘리지 않는 단순화(라이브러리·상태 추가 없음).

**Files:**
- Modify: `frontend/app/layouts/landing.vue:1-55` (템플릿), `:57-70` (스크립트 상단)

**Interfaces:**
- Consumes: Task 1의 `PROCEDURE_CATEGORIES`, Task 6의 `InquiryFab`

- [ ] **Step 1: 템플릿의 `<header>` 안 내용 교체**

`frontend/app/layouts/landing.vue`의 4~30번 줄(`<div class="mx-auto flex max-w-3xl items-center justify-between px-4 py-3">` 안쪽 전체)을 아래로 교체:

```html
      <div class="mx-auto flex max-w-3xl items-center justify-between gap-4 px-4 py-3">
        <NuxtLink :to="localePath('index')" class="flex shrink-0 items-center">
          <img src="/logo.svg" :alt="t('common.appName')" class="h-12 w-auto">
        </NuxtLink>

        <nav class="flex flex-1 items-center justify-center gap-4 text-sm font-medium">
          <NuxtLink :to="localePath('index')" class="text-muted-foreground hover:text-foreground">{{ t('landing.nav.home') }}</NuxtLink>
          <DropdownMenuRoot>
            <DropdownMenuTrigger class="flex items-center gap-1 text-muted-foreground hover:text-foreground aria-expanded:text-foreground">
              {{ t('landing.nav.procedures') }}
              <ChevronDown class="size-3.5" />
            </DropdownMenuTrigger>
            <DropdownMenuPortal>
              <DropdownMenuContent :side-offset="8" align="center" class="z-50 max-h-[70vh] min-w-40 overflow-y-auto rounded-lg border bg-card p-1 text-sm shadow-md">
                <DropdownMenuItem
                  v-for="category in PROCEDURE_CATEGORIES"
                  :key="category.slug"
                  as-child
                  class="block cursor-pointer rounded-md px-3 py-1.5 text-foreground outline-none data-[highlighted]:bg-accent data-[highlighted]:text-accent-foreground"
                >
                  <NuxtLink :to="localePath({ name: 'procedures-category', params: { category: category.slug } })">
                    {{ category.name[locale as Locale] }}
                  </NuxtLink>
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenuPortal>
          </DropdownMenuRoot>
          <NuxtLink :to="localePath('inquiry')" class="text-muted-foreground hover:text-foreground">{{ t('landing.nav.inquiry') }}</NuxtLink>
        </nav>

        <DropdownMenuRoot>
          <DropdownMenuTrigger
            class="flex shrink-0 items-center gap-1 rounded-full border px-3 py-1.5 text-xs font-medium text-muted-foreground transition-colors hover:border-primary hover:text-foreground aria-expanded:border-primary aria-expanded:text-foreground"
          >
            <Globe class="size-3.5" />
            {{ currentLocaleName }}
            <ChevronDown class="size-3.5" />
          </DropdownMenuTrigger>
          <DropdownMenuPortal>
            <DropdownMenuContent :side-offset="8" align="end" class="z-50 min-w-32 rounded-lg border bg-card p-1 text-sm shadow-md">
              <DropdownMenuItem
                v-for="loc in locales"
                :key="loc.code"
                as-child
                class="block w-full cursor-pointer rounded-md px-3 py-1.5 text-foreground outline-none data-[highlighted]:bg-accent data-[highlighted]:text-accent-foreground"
                :class="{ 'font-semibold': loc.code === locale }"
              >
                <NuxtLink :to="switchLocalePath(loc.code)" @click="markManualLocale(loc.code)">{{ loc.name }}</NuxtLink>
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenuPortal>
        </DropdownMenuRoot>
      </div>
```

- [ ] **Step 2: FAB을 `</footer>` 바로 뒤, `</div>`(최상위) 앞에 삽입**

```html
    </footer>

    <InquiryFab />
  </div>
</template>
```

- [ ] **Step 3: 스크립트에 데이터 임포트 추가**

`<script setup lang="ts">` 최상단(58번 줄, `import { ChevronDown, Globe } from '@lucide/vue'` 다음 줄)에 추가:

```ts
import { PROCEDURE_CATEGORIES, type Locale } from '~/data/procedures'
```

- [ ] **Step 4: 빌드 확인**

Run: `cd frontend && npm run build`
Expected: 빌드 성공.

- [ ] **Step 5: Commit**

```bash
git add frontend/app/layouts/landing.vue
git commit -m "feat: 내비게이션에 시술안내 드롭다운·문의하기 추가, FAB 삽입"
```

---

## Task 8: 카테고리 페이지 (`/procedures/[category]`)

**Files:**
- Create: `frontend/app/pages/procedures/[category].vue`

**Interfaces:**
- Consumes: Task 1/2의 `findCategory()`, `ProcedureCategory`

- [ ] **Step 1: 페이지 작성**

```vue
<!-- frontend/app/pages/procedures/[category].vue -->
<template>
  <div v-if="category">
    <section
      class="relative flex min-h-80 items-end bg-cover bg-center text-background"
      :style="{ backgroundImage: `linear-gradient(to top, rgba(0,0,0,.6), rgba(0,0,0,.25)), url(/img/hero/${category.heroImages[0]})` }"
    >
      <div class="mx-auto w-full max-w-3xl px-4 pb-10">
        <component :is="categoryIcon(category.icon)" class="mb-3 size-8" />
        <h1 class="text-3xl font-bold">{{ category.name[locale as Locale] }}</h1>
        <p class="mt-3 max-w-xl text-background/90">{{ category.intro[locale as Locale] }}</p>
      </div>
    </section>

    <section class="mx-auto max-w-3xl px-4 py-10">
      <h2 class="mb-6 text-xl font-semibold text-foreground">
        {{ t('procedures.concernHeading', { category: category.name[locale as Locale] }) }}
      </h2>

      <ul class="divide-y divide-border">
        <li v-for="(item, i) in category.items" :key="item.slug">
          <NuxtLink
            :to="localePath({ name: 'procedures-category-procedure', params: { category: category.slug, procedure: item.slug } })"
            class="flex flex-col gap-4 py-6 sm:flex-row sm:items-center"
            :class="{ 'sm:flex-row-reverse': i % 2 === 1 }"
          >
            <img
              :src="`/img/${item.imageCategory ?? category.slug}/${item.image}`"
              :alt="item.name[locale as Locale]"
              class="h-48 w-full rounded-lg object-cover sm:w-64 sm:shrink-0"
            >
            <div class="flex flex-1 flex-col gap-2">
              <ul class="space-y-1 text-sm text-muted-foreground">
                <li v-for="(concern, ci) in item.concerns[locale as Locale]" :key="ci">{{ concern }}</li>
              </ul>
              <h3 class="text-lg font-semibold text-foreground">{{ item.name[locale as Locale] }}</h3>
            </div>
          </NuxtLink>
        </li>
      </ul>

      <div v-if="category.otherItems.length" class="mt-10 rounded-lg border bg-muted/30 p-5">
        <h3 class="mb-3 text-sm font-semibold text-muted-foreground">{{ t('procedures.otherHeading') }}</h3>
        <div class="flex flex-wrap gap-2">
          <NuxtLink
            v-for="other in category.otherItems"
            :key="other.slug"
            :to="localePath({ name: 'procedures-category-procedure', params: { category: category.slug, procedure: other.slug } })"
            class="rounded-full border px-3 py-1.5 text-sm text-foreground hover:border-primary"
          >
            {{ other.name[locale as Locale] }}
          </NuxtLink>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import * as icons from '@lucide/vue'
import { findCategory, type Locale } from '~/data/procedures'

definePageMeta({ layout: 'landing' })

const route = useRoute()
const { t, locale } = useI18n()
const localePath = useLocalePath()

const category = computed(() => findCategory(route.params.category as string))

if (!category.value) {
  throw createError({ statusCode: 404, statusMessage: 'Category not found' })
}

function categoryIcon(name: string) {
  return (icons as Record<string, unknown>)[name]
}

useSeo({
  title: () => category.value?.name[locale.value as Locale] ?? '',
  description: () => category.value?.intro[locale.value as Locale] ?? '',
})
</script>
```

**참고(ponytail)**: `category`를 `computed`로 만들었지만 존재하지 않는 슬러그로의 404 판정은 setup 최초 실행 시 1회만 검사한다 — 우리 UI가 생성하는 링크는 전부 유효한 슬러그이므로 실사용 경로에서 잘못된 슬러그에 도달하는 경우는 없다(오탈자 URL 직접 입력 정도). 이 정도로 충분하며, 라우트 파라미터 변경을 감시하는 `watch` 기반 재검증은 지금 규모에서는 오버엔지니어링.

- [ ] **Step 2: 빌드 확인**

Run: `cd frontend && npm run build`
Expected: 빌드 성공.

- [ ] **Step 3: 개발 서버로 실제 확인**

Run: `cd frontend && npm run dev` (별도 터미널), 브라우저에서 `http://localhost:3000/procedures/eye` 접속.
Expected: 히어로 이미지·소개문·8개 시술 카드(사진+고민불릿+이름) + "그 외" 없음(eye는 otherItems가 비어있으므로 미표시). `http://localhost:3000/procedures/contour` 접속 시 8개 카드 + 하단에 "그 외" 라벨 6개 노출 확인.

- [ ] **Step 4: Commit**

```bash
git add frontend/app/pages/procedures/[category].vue
git commit -m "feat: 시술 카테고리 페이지 추가"
```

---

## Task 9: 시술 상세 페이지 (`/procedures/[category]/[procedure]`)

**Files:**
- Create: `frontend/app/pages/procedures/[category]/[procedure].vue`

**Interfaces:**
- Consumes: Task 1/2의 `findProcedure()`

- [ ] **Step 1: 페이지 작성**

콘텐츠가 있는 시술(`item`)과 "그 외"로만 존재하는 시술(`other`, 콘텐츠 없음)을 모두 처리한다.

```vue
<!-- frontend/app/pages/procedures/[category]/[procedure].vue -->
<template>
  <div v-if="item" class="mx-auto max-w-3xl px-4 py-12">
    <div class="grid gap-8 sm:grid-cols-2 sm:items-center">
      <div>
        <p v-if="item.label?.[locale as Locale]" class="mb-2 text-sm text-muted-foreground">{{ item.label[locale as Locale] }}</p>
        <h1 class="text-3xl font-bold text-foreground">{{ item.name[locale as Locale] }}</h1>
        <p class="mt-4 whitespace-pre-line text-muted-foreground">{{ item.description[locale as Locale] }}</p>
        <Button as-child class="mt-6">
          <NuxtLink :to="localePath('inquiry')">{{ t('procedures.inquireCta') }}</NuxtLink>
        </Button>
      </div>
      <img
        :src="`/img/${item.imageCategory ?? categorySlug}/${item.image}`"
        :alt="item.name[locale as Locale]"
        class="w-full rounded-xl object-cover"
      >
    </div>
  </div>

  <div v-else-if="other" class="mx-auto max-w-3xl px-4 py-16 text-center">
    <h1 class="text-2xl font-bold text-foreground">{{ other.name[locale as Locale] }}</h1>
    <p class="mt-4 text-muted-foreground">{{ t('procedures.comingSoon') }}</p>
    <Button as-child class="mt-6">
      <NuxtLink :to="localePath('inquiry')">{{ t('procedures.inquireCta') }}</NuxtLink>
    </Button>
  </div>
</template>

<script setup lang="ts">
import { findProcedure, type Locale } from '~/data/procedures'

definePageMeta({ layout: 'landing' })

const route = useRoute()
const { t, locale } = useI18n()
const localePath = useLocalePath()

const categorySlug = route.params.category as string
const found = findProcedure(categorySlug, route.params.procedure as string)

if (!found.category || (!found.item && !found.other)) {
  throw createError({ statusCode: 404, statusMessage: 'Procedure not found' })
}

const item = found.item
const other = found.other

useSeo({
  title: () => (item?.name[locale.value as Locale] ?? other?.name[locale.value as Locale] ?? ''),
  description: () => item?.description[locale.value as Locale] ?? '',
})
</script>
```

- [ ] **Step 2: 빌드 확인**

Run: `cd frontend && npm run build`
Expected: 빌드 성공.

- [ ] **Step 3: 개발 서버로 실제 확인**

`http://localhost:3000/procedures/eye/glam-eye` — 라벨+제목+설명+CTA버튼+사진 확인.
`http://localhost:3000/procedures/contour/self-designed-double-jaw` — 제목만 있고 "준비 중" 안내+CTA버튼만 보이는지 확인(사진·설명 없음이 정상).
`http://localhost:3000/procedures/nose/male-rhinoplasty` — men 카테고리와 동일한 사진(men-rhinoplasty.png)이 뜨는지 확인(교차재사용).

- [ ] **Step 4: Commit**

```bash
git add "frontend/app/pages/procedures/[category]/[procedure].vue"
git commit -m "feat: 시술 상세 페이지 추가(콘텐츠 없는 항목은 준비중 안내)"
```

---

## Task 10: i18n 키 추가 (4개 언어) + 최종 검증

**Files:**
- Modify: `frontend/i18n/locales/ko.json`, `zh-CN.json`, `zh-TW.json`, `en.json`

- [ ] **Step 1: 4개 파일에 동일한 키 구조로 추가**

`landing` 최상위 객체 안에 `nav`와 `home`을 추가하고, 최상위에 `procedures`를 추가한다. ko.json 예시(281번 줄 `"landing": {` 바로 다음, `"hero": {` 앞에 삽입):

```json
    "nav": {
      "home": "홈",
      "procedures": "시술안내",
      "inquiry": "문의하기",
      "inquiryFab": "상담하기"
    },
    "home": {
      "heroTitle": "WJ 원진성형외과",
      "heroSubtitle": "정밀한 진단과 상담으로 완성하는 아름다움",
      "categoriesHeading": "시술 카테고리",
      "introHeading": "WJ 원진 소개",
      "introBody": "병원 소개 내용을 준비 중입니다."
    },
```

같은 구조를 `zh-CN.json`/`zh-TW.json`/`en.json`에도 추가한다(값은 각 언어로):

zh-CN:
```json
    "nav": { "home": "首页", "procedures": "项目介绍", "inquiry": "咨询预约", "inquiryFab": "咨询" },
    "home": { "heroTitle": "WJ原辰整形外科", "heroSubtitle": "以精密诊断和咨询完成的美丽", "categoriesHeading": "项目分类", "introHeading": "WJ原辰介绍", "introBody": "医院介绍内容准备中。" },
```

zh-TW:
```json
    "nav": { "home": "首頁", "procedures": "項目介紹", "inquiry": "諮詢預約", "inquiryFab": "諮詢" },
    "home": { "heroTitle": "WJ原辰整形外科", "heroSubtitle": "以精密診斷和諮詢完成的美麗", "categoriesHeading": "項目分類", "introHeading": "WJ原辰介紹", "introBody": "醫院介紹內容準備中。" },
```

en:
```json
    "nav": { "home": "Home", "procedures": "Procedures", "inquiry": "Inquiry", "inquiryFab": "Inquire" },
    "home": { "heroTitle": "WJ WonJin Plastic Surgery", "heroSubtitle": "Beauty completed through precise diagnosis and consultation", "categoriesHeading": "Procedure Categories", "introHeading": "About WJ WonJin", "introBody": "Hospital introduction content coming soon." },
```

최상위(파일 맨 위 `"common": {...}` 형제 레벨)에 `procedures` 네임스페이스 추가:

ko:
```json
  "procedures": {
    "concernHeading": "{category}에 어떤 고민이 있으신가요?",
    "otherHeading": "그 외 시술",
    "inquireCta": "지금 문의하기",
    "comingSoon": "상세 내용을 준비 중입니다."
  },
```

zh-CN: `{ "concernHeading": "{category}有什么困扰吗？", "otherHeading": "其他项目", "inquireCta": "立即咨询", "comingSoon": "详细内容准备中。" }`
zh-TW: `{ "concernHeading": "{category}有什麼困擾嗎？", "otherHeading": "其他項目", "inquireCta": "立即諮詢", "comingSoon": "詳細內容準備中。" }`
en: `{ "concernHeading": "What concerns you about {category}?", "otherHeading": "Other Procedures", "inquireCta": "Inquire Now", "comingSoon": "Details coming soon." }`

- [ ] **Step 2: 4개 로케일 키 집합 개수 일치 확인**

```bash
cd frontend && node -e "
const fs = require('fs');
function count(o) { let n = 0; for (const k in o) { n += typeof o[k] === 'object' ? count(o[k]) : 1; } return n; }
for (const f of ['ko','zh-CN','zh-TW','en']) {
  const d = JSON.parse(fs.readFileSync(\`i18n/locales/\${f}.json\`, 'utf-8'));
  console.log(f, count(d));
}
"
```

Expected: 4개 파일 전부 같은 숫자(기존 340 + 이번에 추가한 키 수만큼 동일하게 증가).

- [ ] **Step 3: 최종 빌드 + 전체 라우트 확인**

```bash
cd frontend && npm run build
```
Expected: 에러 없이 성공.

개발 서버(`npm run dev`)로 아래를 전부 확인:
- `/` — 히어로+카테고리 11개 그리드+병원소개(준비중 문구) 노출, FAB 우측 하단 표시
- `/procedures/eye` → 카드 클릭 시 `/procedures/eye/glam-eye`로 정상 이동
- `/inquiry` — 기존과 동일한 폼 동작(제출까지 1건 테스트, UTM 쿼리(`?utm_source=test`)를 달고 `/`에 먼저 들어간 뒤 카테고리 거쳐 `/inquiry`에서 제출 → 백엔드에 `utm_source=test`로 잘 들어가는지 확인 — UTM 쿠키 유지 검증)
- 헤더 "시술안내" 드롭다운에서 11개 카테고리 전부 열리고 이동되는지 확인
- 4개 로케일(`/`, `/ko`, `/zh-TW`, `/en`) 각각에서 헤더 텍스트가 번역되어 나오는지 확인

- [ ] **Step 4: Commit**

```bash
git add frontend/i18n/locales/ko.json frontend/i18n/locales/zh-CN.json frontend/i18n/locales/zh-TW.json frontend/i18n/locales/en.json
git commit -m "feat: 랜딩 재설계 관련 i18n 키 추가 및 4개 언어 키 집합 일치 확인"
```

---

## Self-Review 체크리스트 (계획 작성자용, 실행 전 참고)

- **스펙 커버리지**: 설계 문서 5절(페이지별 설계)의 홈/카테고리/상세/문의 4개 페이지 전부 Task 4/5/8/9로 커버. 6절(FAB) → Task 6. 7절(내비게이션) → Task 7(단, 햄버거 생략 사유 명시). 8절(i18n 전략) → Task 10. 4절(데이터 아키텍처) → Task 1/2.
- **플레이스홀더 스캔**: `home.introBody`("준비 중" 안내)만 의도된 임시 문구다 — 병원소개 콘텐츠(설계 문서 10절 3번, 미해결)가 오기 전까지 정직하게 "준비 중"이라고 표시하는 것이지, 가짜 병원소개를 지어내지 않는다. 다른 모든 카피는 설계 문서 부록 D~G의 실제 원문/번역이다.
- **타입 일관성**: `findCategory`/`findProcedure`/`ProcedureItem`/`ProcedureCategory` 명명이 Task 1 정의부터 Task 8·9 소비부까지 동일하게 사용됨.
