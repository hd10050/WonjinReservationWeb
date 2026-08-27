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
