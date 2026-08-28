// frontend/app/data/procedureMedical.ts
// 시술 카테고리 목록 페이지('procedures/[category]/index.vue')의 "고민이 있으신가요?" 영역 위에
// 붙는 병원 시스템·특장점 콘텐츠. k-wonjin.co.kr 각 카테고리 페이지의 마케팅 섹션을 옮긴 것으로,
// 구조적 다국어 데이터라 i18n JSON이 아니라 여기(TS)에 둔다(procedures.ts·hospitalTour.ts와 동일 패턴).
// 원문(ko)은 사용자가 제공, zh-CN/zh-TW/en은 직접 번역(k-wonjin.co.kr은 한국어 전용).
import type { Locale } from './procedures'

type L = Record<Locale, string>

/** headline(선택) + subhead 오버라인(선택) + body 문단 */
export interface MSIntro {
  type: 'intro'
  headline?: L
  subhead?: L
  body: L
}
/** 01·02·03… 번호가 붙는 짧은 특징 카드 목록 */
export interface MSSteps {
  type: 'steps'
  items: { no: string; text: L }[]
}
/** 강조 인용문 (+ 선택 출처/면책 문구) */
export interface MSQuote {
  type: 'quote'
  text: L
  cite?: L
}
/**
 * (선택)번호 + 제목 + 본문 (+선택 이미지) 카드 목록. image는 '/img/' 아래 상대경로.
 * imageFit 'contain'이면 원형 크롭 대신 사각형 전체 표시(도해·설명 이미지용).
 */
export interface MSFeatures {
  type: 'features'
  items: { no?: string; title: L; body: L; image?: string; imageFit?: 'contain' }[]
}
/** 이미지 갤러리(인증서·논문·수상 등) + 선택 캡션. images는 '/img/' 아래 상대경로 */
export interface MSGallery {
  type: 'gallery'
  images: string[]
  caption?: L
}

export type MedicalBlock = MSIntro | MSSteps | MSQuote | MSFeatures | MSGallery

export const PROCEDURE_MEDICAL: Record<string, MedicalBlock[]> = {
  // ─────────────────────────────── nose ───────────────────────────────
  nose: [
    {
      type: 'intro',
      subhead: {
        ko: '오직 나만을 위한 1:1 맞춤 보형물',
        'zh-CN': '只为你打造的1:1定制假体',
        'zh-TW': '只為你打造的1:1客製化假體',
        en: 'A 1:1 custom implant made only for you',
      },
      headline: {
        ko: 'WJ 원진 3D FIT 코성형',
        'zh-CN': 'WJ原辰 3D FIT 鼻整形',
        'zh-TW': 'WJ原辰 3D FIT 鼻整形',
        en: 'WJ WonJin 3D FIT Rhinoplasty',
      },
      body: {
        ko: '3D 프린트 기법을 적용한 시뮬레이션 프로그램을 통해 개인의 해부학적 구조에 맞는 최적화된 맞춤 보형물을 제작하여 부작용을 줄이고 완성도 높은 코성형을 진행합니다.',
        'zh-CN': '通过应用3D打印技术的模拟程序，制作符合个人解剖结构的最优化定制假体，减少副作用，实现完成度更高的鼻整形。',
        'zh-TW': '透過應用3D列印技術的模擬程式，製作符合個人解剖結構的最佳化客製化假體，減少副作用，實現完成度更高的鼻整形。',
        en: 'Using a simulation program based on 3D printing technology, we craft an optimized custom implant that matches your individual anatomical structure — reducing side effects and delivering a highly refined rhinoplasty result.',
      },
    },
    {
      type: 'steps',
      items: [
        {
          no: '01',
          text: {
            ko: '코 굴곡에 따른 보형물 맞춤 제작',
            'zh-CN': '根据鼻部曲线定制假体',
            'zh-TW': '根據鼻部曲線客製假體',
            en: 'Implants custom-made to the curve of your nose',
          },
        },
        {
          no: '02',
          text: {
            ko: '안정적인 밀착력으로 부작용 최소화',
            'zh-CN': '稳定的贴合力，将副作用降至最低',
            'zh-TW': '穩定的貼合力，將副作用降至最低',
            en: 'A stable, close fit that minimizes side effects',
          },
        },
        {
          no: '03',
          text: {
            ko: '수술 후 모습 예측 가능',
            'zh-CN': '可预测术后效果',
            'zh-TW': '可預測術後效果',
            en: 'Predictable post-surgery results',
          },
        },
        {
          no: '04',
          text: {
            ko: '매부리코, 휜코 등 코유형별 수술 가능',
            'zh-CN': '可针对驼峰鼻、歪鼻等各类鼻型手术',
            'zh-TW': '可針對駝峰鼻、歪鼻等各類鼻型手術',
            en: 'Applicable to hump noses, deviated noses, and other nose types',
          },
        },
      ],
    },
    {
      type: 'quote',
      text: {
        ko: 'WJ 원진은 정밀 진단을 바탕으로 개개인 맞춤 최적화된 코성형 솔루션을 제안합니다.',
        'zh-CN': 'WJ原辰以精密诊断为基础，为每个人提供量身定制的最优化鼻整形方案。',
        'zh-TW': 'WJ原辰以精密診斷為基礎，為每個人提供量身打造的最佳化鼻整形方案。',
        en: 'Based on precise diagnosis, WJ WonJin proposes an optimized rhinoplasty solution tailored to each individual.',
      },
    },
    {
      type: 'features',
      items: [
        {
          image: 'nose/innofit_2.png',
          title: {
            ko: '무보형물 코성형',
            'zh-CN': '无假体鼻整形',
            'zh-TW': '無假體鼻整形',
            en: 'Implant-Free Rhinoplasty',
          },
          body: {
            ko: '인공보형물을 사용하지 않고 자가조직, 혹은 기증 연골과 진피를 사용하여 안전하고 자연스러운 라인을 완성합니다.',
            'zh-CN': '不使用人工假体，而是使用自体组织或捐赠软骨与真皮，打造安全自然的鼻部线条。',
            'zh-TW': '不使用人工假體，而是使用自體組織或捐贈軟骨與真皮，打造安全自然的鼻部線條。',
            en: 'Without artificial implants, we use autologous tissue or donated cartilage and dermis to complete a safe, natural nose line.',
          },
        },
        {
          image: 'nose/innofit_3.png',
          title: {
            ko: '맞춤 디자인 코성형',
            'zh-CN': '定制设计鼻整形',
            'zh-TW': '客製設計鼻整形',
            en: 'Custom-Design Rhinoplasty',
          },
          body: {
            ko: '데이터를 기반으로 3D 프린팅 기법을 통해 개인의 코(코뼈, 연골, 비강)에 맞는 1:1 맞춤 보형물과 부목을 제작합니다.',
            'zh-CN': '以数据为基础，通过3D打印技术，制作贴合个人鼻部（鼻骨、软骨、鼻腔）的1:1定制假体与夹板。',
            'zh-TW': '以數據為基礎，透過3D列印技術，製作貼合個人鼻部（鼻骨、軟骨、鼻腔）的1:1客製化假體與夾板。',
            en: 'Based on your data, we use 3D printing technology to fabricate a 1:1 custom implant and splint that fit your individual nose — bone, cartilage, and nasal cavity.',
          },
        },
      ],
    },
  ],

  // ─────────────────────────────── ent ───────────────────────────────
  ent: [
    {
      type: 'intro',
      headline: {
        ko: 'WJ 원진만의 비중격 2중 강화 수술법',
        'zh-CN': 'WJ原辰独有的鼻中隔双重强化术式',
        'zh-TW': 'WJ原辰獨有的鼻中隔雙重強化術式',
        en: "WJ WonJin's Exclusive Double-Reinforcement Septal Surgery",
      },
      body: {
        ko: '휘어진 비중격을 바로 잡아주고 환자에게 적합한 재료를 선택하여 지지대를 보강해주는 수술법은 원진만이 가지고 있는 고난이도의 기술로, 오랜 경험과 숙련된 전문의만이 할 수 있는 수술법입니다.',
        'zh-CN': '矫正弯曲的鼻中隔，并选择适合患者的材料来加固支撑结构——这是原辰独有的高难度技术，只有经验丰富、技术娴熟的专业医生才能完成。',
        'zh-TW': '矯正彎曲的鼻中隔，並選擇適合患者的材料來加固支撐結構——這是原辰獨有的高難度技術，只有經驗豐富、技術嫻熟的專業醫師才能完成。',
        en: 'Straightening a deviated septum and reinforcing its support with materials suited to each patient is a highly advanced technique unique to WonJin — one only experienced, skilled specialists can perform.',
      },
    },
    {
      type: 'features',
      items: [
        {
          image: 'ent/img_premium_strength1.jpg',
          imageFit: 'contain',
          title: {
            ko: '① 휜 비중격 교정',
            'zh-CN': '① 矫正弯曲的鼻中隔',
            'zh-TW': '① 矯正彎曲的鼻中隔',
            en: '① Correcting the deviated septum',
          },
          body: {
            ko: '휘어진 비중격 연골을 곧게 펴고, 남은 연골을 제거해 중앙에 반듯하게 배치합니다.',
            'zh-CN': '将弯曲的鼻中隔软骨拉直，去除多余软骨并端正地置于中央。',
            'zh-TW': '將彎曲的鼻中隔軟骨拉直，去除多餘軟骨並端正地置於中央。',
            en: 'The deviated septal cartilage is straightened, and the remaining cartilage is trimmed and repositioned squarely at the center.',
          },
        },
        {
          image: 'ent/img_premium_strength2.jpg',
          imageFit: 'contain',
          title: {
            ko: '② 지지대 2중 강화',
            'zh-CN': '② 支撑结构双重强化',
            'zh-TW': '② 支撐結構雙重強化',
            en: '② Double-reinforcing the support',
          },
          body: {
            ko: '중앙을 단단하게 고정한 뒤, 연골 지지대를 한 번 더 튼튼하게 보강합니다.',
            'zh-CN': '牢固地固定中央后，再次加固软骨支撑结构。',
            'zh-TW': '牢固地固定中央後，再次加固軟骨支撐結構。',
            en: 'The center is firmly fixed, then the cartilage support is reinforced once more for lasting stability.',
          },
        },
      ],
    },
  ],

  // ─────────────────────────── dermatology ───────────────────────────
  dermatology: [
    {
      type: 'intro',
      headline: {
        ko: 'WJ 원진 메디컬 시스템',
        'zh-CN': 'WJ原辰医疗系统',
        'zh-TW': 'WJ原辰醫療系統',
        en: 'The WJ WonJin Medical System',
      },
      body: {
        ko: '정확한 진단부터 개인 맞춤 처방까지, 피부 상태를 정교하게 개선하는 원진만의 시스템입니다.',
        'zh-CN': '从精准诊断到个人定制处方，这是原辰精细改善肌肤状态的独有系统。',
        'zh-TW': '從精準診斷到個人客製處方，這是原辰精細改善肌膚狀態的獨有系統。',
        en: "From accurate diagnosis to personalized prescriptions — WonJin's own system for refining your skin condition.",
      },
    },
    {
      type: 'features',
      items: [
        {
          no: '01',
          image: 'dermatology/img_skin_medical_01.png',
          title: {
            ko: '1:1 안심 책임 전담제',
            'zh-CN': '1:1安心责任专属制',
            'zh-TW': '1:1安心責任專屬制',
            en: '1:1 Dedicated Responsibility System',
          },
          body: {
            ko: '인증 받은 정품, 정량만을 사용하는 1:1 책임전담제로 운영되며 탄력부터 색소, 모공까지 정확히 진단하고 정교하게 개선합니다.',
            'zh-CN': '采用仅使用认证正品、正量的1:1责任专属制运营，从弹力到色素、毛孔精准诊断并精细改善。',
            'zh-TW': '採用僅使用認證正品、正量的1:1責任專屬制營運，從彈力到色素、毛孔精準診斷並精細改善。',
            en: 'A 1:1 dedicated-responsibility system using only certified, authentic products in proper doses — diagnosing and refining everything from elasticity to pigmentation and pores.',
          },
        },
        {
          no: '02',
          image: 'dermatology/img_skin_medical_02.png',
          title: {
            ko: '교차시술 적극 활용',
            'zh-CN': '积极运用交叉治疗',
            'zh-TW': '積極運用交叉治療',
            en: 'Active Use of Combination Treatments',
          },
          body: {
            ko: '폭넓은 지식과 경험 많은 의료진의 노하우를 활용해 최적의 교차시술을 제공합니다.',
            'zh-CN': '运用知识广博、经验丰富的医疗团队的专业技巧，提供最优化的交叉治疗。',
            'zh-TW': '運用知識廣博、經驗豐富的醫療團隊的專業技巧，提供最佳化的交叉治療。',
            en: 'Drawing on the broad knowledge and experience of our medical team, we provide the optimal combination of treatments.',
          },
        },
        {
          no: '03',
          image: 'dermatology/img_skin_medical_03.png',
          title: {
            ko: '다양한 장비 보유',
            'zh-CN': '拥有多样化设备',
            'zh-TW': '擁有多樣化設備',
            en: 'A Wide Range of Equipment',
          },
          body: {
            ko: '80여 종의 장비로 효과적인 시술을 위한 개인별 맞춤 처방을 진행합니다.',
            'zh-CN': '以80余种设备为效果显著的治疗提供个人定制化处方。',
            'zh-TW': '以80餘種設備為效果顯著的治療提供個人客製化處方。',
            en: 'With over 80 types of equipment, we tailor an individual prescription for effective treatment.',
          },
        },
        {
          no: '04',
          image: 'dermatology/img_skin_medical_04.png',
          title: {
            ko: '개인 맞춤 피부 솔루션 제공',
            'zh-CN': '提供个人定制皮肤方案',
            'zh-TW': '提供個人客製皮膚方案',
            en: 'Personalized Skin Solutions',
          },
          body: {
            ko: '최신 의료기술과 맞춤형 프로그램을 통해 각자의 피부 상태에 최적화된 치료를 제공합니다.',
            'zh-CN': '通过最新医疗技术和定制化项目，提供针对各自皮肤状态的最优化治疗。',
            'zh-TW': '透過最新醫療技術和客製化項目，提供針對各自皮膚狀態的最佳化治療。',
            en: "Through the latest medical technology and customized programs, we deliver treatment optimized for each person's skin.",
          },
        },
        {
          no: '05',
          image: 'dermatology/img_skin_medical_05.png',
          title: {
            ko: '프리미엄 안티에이징센터 운영',
            'zh-CN': '运营高级抗衰老中心',
            'zh-TW': '營運高級抗衰老中心',
            en: 'A Premium Anti-Aging Center',
          },
          body: {
            ko: '프라이빗한 환경에서 경험하는 프리미엄 줄기세포 치료 및 항노화 의료 서비스.',
            'zh-CN': '在私密的环境中体验的高级干细胞治疗及抗衰老医疗服务。',
            'zh-TW': '在私密的環境中體驗的高級幹細胞治療及抗衰老醫療服務。',
            en: 'Premium stem-cell therapy and anti-aging medical care, experienced in a private setting.',
          },
        },
      ],
    },
  ],

  // ─────────────────────────────── stemcell ───────────────────────────────
  stemcell: [
    {
      type: 'intro',
      headline: {
        ko: '줄기세포 효능 범위',
        'zh-CN': '干细胞的功效范围',
        'zh-TW': '幹細胞的功效範圍',
        en: 'The Range of Stem Cell Efficacy',
      },
      body: {
        ko: '탈모, 안과, 치과, 노화, 미용·성형, 재수술(구축 등), 허리·목 디스크, 통증, 관절, 연골 재생, 성기능, 만성피로, 아토피 등 피부질환, 갱년기.',
        'zh-CN': '脱发、眼科、牙科、老化、美容·整形、再次手术（挛缩等）、腰椎间盘、颈椎间盘、疼痛、关节、软骨再生、性功能、慢性疲劳、特应性皮炎等皮肤疾病、更年期。',
        'zh-TW': '掉髮、眼科、牙科、老化、美容·整形、再次手術（攣縮等）、腰椎間盤、頸椎間盤、疼痛、關節、軟骨再生、性功能、慢性疲勞、異位性皮膚炎等皮膚疾病、更年期。',
        en: 'Hair loss, ophthalmology, dentistry, aging, aesthetics and plastic surgery, revision surgery (including contracture), lumbar and cervical disc conditions, pain, joints, cartilage regeneration, sexual function, chronic fatigue, skin conditions such as atopic dermatitis, and menopause.',
      },
    },
    {
      type: 'features',
      items: [
        {
          title: {
            ko: '항노화 (Antiaging)',
            'zh-CN': '抗衰老 (Antiaging)',
            'zh-TW': '抗衰老 (Antiaging)',
            en: 'Antiaging',
          },
          body: {
            ko: '산화 스트레스 물질 감소를 유도해 미토콘드리아 유전체 손상을 줄이고, 항노화 호르몬과 단백질의 양을 늘립니다.',
            'zh-CN': '诱导减少氧化应激物质，降低线粒体基因组损伤，增加抗衰老激素与蛋白质的含量。',
            'zh-TW': '誘導減少氧化壓力物質，降低粒線體基因組損傷，增加抗衰老激素與蛋白質的含量。',
            en: 'By reducing oxidative-stress substances, it lessens mitochondrial genome damage and raises levels of anti-aging hormones and proteins.',
          },
        },
        {
          title: {
            ko: '호밍 효과 (Homing Effect)',
            'zh-CN': '归巢效应 (Homing Effect)',
            'zh-TW': '歸巢效應 (Homing Effect)',
            en: 'Homing Effect',
          },
          body: {
            ko: '손상된 조직을 스스로 감지해 치료하고, 몸 전체의 세포와 조직을 새롭게 바꿔 신체 전반의 문제를 현격히 호전시킵니다.',
            'zh-CN': '自行侦测并修复受损组织，更新全身细胞与组织，显著改善身体的整体问题。',
            'zh-TW': '自行偵測並修復受損組織，更新全身細胞與組織，顯著改善身體的整體問題。',
            en: 'It detects and repairs damaged tissue on its own, renewing cells and tissue throughout the body to markedly improve overall health.',
          },
        },
        {
          title: {
            ko: '콜라겐·엘라스틴 세포 재생',
            'zh-CN': '胶原蛋白·弹力蛋白细胞再生',
            'zh-TW': '膠原蛋白·彈力蛋白細胞再生',
            en: 'Collagen & Elastin Cell Regeneration',
          },
          body: {
            ko: '노화로 감소된 조직을 재생시켜 회복을 촉진하고, 탄력과 혈색 개선·리프팅 등 피부 컨디션을 최상으로 복구합니다.',
            'zh-CN': '再生因老化而减少的组织，促进恢复，改善弹力与气色、提拉等，将肌肤状态修复至最佳。',
            'zh-TW': '再生因老化而減少的組織，促進恢復，改善彈力與氣色、拉提等，將肌膚狀態修復至最佳。',
            en: 'It regenerates tissue lost to aging, speeding recovery and restoring skin to its best — improving elasticity, complexion, and lift.',
          },
        },
        {
          title: {
            ko: '면역·대사·컨디션 개선',
            'zh-CN': '改善免疫·代谢·状态',
            'zh-TW': '改善免疫·代謝·狀態',
            en: 'Better Immunity, Metabolism & Condition',
          },
          body: {
            ko: '침체된 세포 기능을 활성화하고 말초 세포까지 대사를 끌어올려 신체 모든 기관과 기능을 회복시킵니다.',
            'zh-CN': '激活低下的细胞功能，将代谢提升至末梢细胞，恢复身体所有器官与功能。',
            'zh-TW': '活化低下的細胞功能，將代謝提升至末梢細胞，恢復身體所有器官與功能。',
            en: 'It reactivates sluggish cell function and lifts metabolism down to the peripheral cells, restoring every organ and function of the body.',
          },
        },
      ],
    },
    {
      type: 'intro',
      headline: {
        ko: '감성이 아닌 과학으로 만듭니다',
        'zh-CN': '以科学而非感性打造',
        'zh-TW': '以科學而非感性打造',
        en: 'Built on Science, Not Sentiment',
      },
      body: {
        ko: 'WJ 원진은 자체 줄기세포 연구소를 운용하는 데 그치지 않고, 전문 연구시설과의 기술 협업을 바탕으로 안전한 고효능 줄기세포 시술을 제공합니다.',
        'zh-CN': 'WJ原辰不仅运营自有干细胞研究所，还以与专业研究机构的技术合作为基础，提供安全的高效能干细胞治疗。',
        'zh-TW': 'WJ原辰不僅營運自有幹細胞研究所，還以與專業研究機構的技術合作為基礎，提供安全的高效能幹細胞治療。',
        en: 'WJ WonJin operates its own stem-cell laboratory and goes further — collaborating with specialized research facilities to deliver safe, high-efficacy stem-cell procedures.',
      },
    },
    {
      type: 'features',
      items: [
        {
          title: {
            ko: '안전하고 검증된 시술',
            'zh-CN': '安全且经过验证的治疗',
            'zh-TW': '安全且經過驗證的治療',
            en: 'Safe, Verified Procedures',
          },
          body: {
            ko: '연구개발전담부서 인증과 첨단 줄기세포 시스템·장비를 활용해 면역거부반응과 부작용 염려 없이 빠른 치료 결과로 만족도가 높습니다.',
            'zh-CN': '凭借研发专属部门认证及先进的干细胞系统与设备，无需担心免疫排斥反应与副作用，治疗见效快、满意度高。',
            'zh-TW': '憑藉研發專屬部門認證及先進的幹細胞系統與設備，無需擔心免疫排斥反應與副作用，治療見效快、滿意度高。',
            en: 'Certified by a dedicated R&D division and using advanced stem-cell systems and equipment, it delivers fast results with high satisfaction and no concern over immune rejection or side effects.',
          },
        },
        {
          title: {
            ko: '고품질 지방세포 채취 기술',
            'zh-CN': '高品质脂肪细胞采集技术',
            'zh-TW': '高品質脂肪細胞採集技術',
            en: 'High-Quality Fat-Cell Harvesting',
          },
          body: {
            ko: '20여 년 이상의 지방흡입 노하우를 토대로 최고의 줄기세포만을 선별하고, Active 인자와 함께 충분히 주입해 뛰어난 개선 효과를 기대할 수 있습니다.',
            'zh-CN': '以20余年的脂肪抽吸经验为基础，精选最优质的干细胞，与Active因子一同充分注入，可期待卓越的改善效果。',
            'zh-TW': '以20餘年的脂肪抽吸經驗為基礎，精選最優質的幹細胞，與Active因子一同充分注入，可期待卓越的改善效果。',
            en: 'Built on more than 20 years of liposuction expertise, we select only the best stem cells and inject them generously with Active factors for outstanding improvement.',
          },
        },
        {
          title: {
            ko: '대학병원급 검진과 연구소',
            'zh-CN': '大学医院级检查与研究所',
            'zh-TW': '大學醫院級檢查與研究所',
            en: 'University-Hospital-Grade Screening & Lab',
          },
          body: {
            ko: '건강 상태를 확인하는 대학병원급 검진 시스템과 내부 줄기세포 연구소를 운영해, 복잡한 과정이나 입원 없이 프라이빗하게 시술받을 수 있습니다.',
            'zh-CN': '运营检查健康状态的大学医院级检查系统与内部干细胞研究所，无需复杂流程或住院，可私密地接受治疗。',
            'zh-TW': '營運檢查健康狀態的大學醫院級檢查系統與內部幹細胞研究所，無需複雜流程或住院，可私密地接受治療。',
            en: 'With a university-hospital-grade screening system and an in-house stem-cell lab, you can be treated privately without complex procedures or hospitalization.',
          },
        },
        {
          title: {
            ko: '줄기세포 셀 뱅킹',
            'zh-CN': '干细胞细胞库',
            'zh-TW': '幹細胞細胞庫',
            en: 'Stem Cell Banking',
          },
          body: {
            ko: '건강한 상태의 면역세포와 줄기세포를 안전하게 보관해, 필요할 때 별도의 채취 없이 언제든 프리미엄 줄기세포 시술이 가능합니다.',
            'zh-CN': '安全保存健康状态的免疫细胞与干细胞，需要时无需另行采集，随时可进行高级干细胞治疗。',
            'zh-TW': '安全保存健康狀態的免疫細胞與幹細胞，需要時無需另行採集，隨時可進行高級幹細胞治療。',
            en: 'Healthy immune cells and stem cells are stored safely, so premium stem-cell treatment is available anytime without another harvest.',
          },
        },
      ],
    },
    {
      type: 'intro',
      headline: {
        ko: '미래 의학의 핵심 효능',
        'zh-CN': '未来医学的核心功效',
        'zh-TW': '未來醫學的核心功效',
        en: 'A Core Therapy of Future Medicine',
      },
      body: {
        ko: '보건복지부도 줄기세포 분야 지원을 강화하고 있으며, 최근 COVID-19에서도 효과가 확인될 만큼 새로운 의학적 돌파구로 인식되고 있습니다.',
        'zh-CN': '韩国保健福祉部也在加强对干细胞领域的支持，近来在COVID-19中亦确认了其效果，被视为新的医学突破。',
        'zh-TW': '韓國保健福祉部也在加強對幹細胞領域的支持，近來在COVID-19中亦確認了其效果，被視為新的醫學突破。',
        en: "Korea's Ministry of Health and Welfare is expanding support for the stem-cell field, and with efficacy recently confirmed even in COVID-19, it is regarded as a new medical breakthrough.",
      },
    },
    {
      type: 'quote',
      text: {
        ko: '다방면의 중간엽줄기세포(MSC) 적용은 COVID-19 중환자에 대해 긍정적인 효과를 입증했으며, 이 효과는 치료와 사망률 감소에 중요한 역할을 합니다.',
        'zh-CN': '多方面的间充质干细胞（MSC）应用对COVID-19重症患者证实了积极效果，该效果在治疗及降低死亡率方面起到重要作用。',
        'zh-TW': '多方面的間質幹細胞（MSC）應用對COVID-19重症患者證實了正面效果，該效果在治療及降低死亡率方面起到重要作用。',
        en: 'Multifaceted application of mesenchymal stem cells (MSCs) demonstrated a positive effect in critical COVID-19 patients, playing an important role in treatment and in reducing mortality.',
      },
      cite: {
        ko: 'SOURCE: The Systematic Effect of Mesenchymal Stem Cell Therapy in Critical COVID-19 Patients: A Prospective Double Controlled Trial · 본 내용은 연구 자료를 활용해 환자의 이해를 돕기 위한 비상업적 정보 전달 목적입니다.',
        'zh-CN': 'SOURCE: The Systematic Effect of Mesenchymal Stem Cell Therapy in Critical COVID-19 Patients: A Prospective Double Controlled Trial · 本内容为运用研究资料、以帮助患者理解为目的的非商业性信息传达。',
        'zh-TW': 'SOURCE: The Systematic Effect of Mesenchymal Stem Cell Therapy in Critical COVID-19 Patients: A Prospective Double Controlled Trial · 本內容為運用研究資料、以幫助患者理解為目的的非商業性資訊傳達。',
        en: 'SOURCE: The Systematic Effect of Mesenchymal Stem Cell Therapy in Critical COVID-19 Patients: A Prospective Double Controlled Trial · Provided for non-commercial, educational purposes to help patients understand the research.',
      },
    },
    {
      type: 'intro',
      headline: {
        ko: 'WJ 원진만의 줄기세포 시술 프로세스',
        'zh-CN': 'WJ原辰独有的干细胞治疗流程',
        'zh-TW': 'WJ原辰獨有的幹細胞治療流程',
        en: "WJ WonJin's Stem Cell Procedure Process",
      },
      body: {
        ko: '세계적인 기술을 집약한 줄기세포 추출·시술 프로세스로 효과와 만족도를 높입니다. 시술 종류와 환자 상태에 따라 과정이 달라질 수 있습니다.',
        'zh-CN': '以汇集世界级技术的干细胞提取·治疗流程，提升效果与满意度。流程会因治疗种类与患者状态而有所不同。',
        'zh-TW': '以匯集世界級技術的幹細胞提取·治療流程，提升效果與滿意度。流程會因治療種類與患者狀態而有所不同。',
        en: 'A stem-cell extraction and treatment process built on world-class technology raises both efficacy and satisfaction. Steps may vary by procedure type and patient condition.',
      },
    },
    {
      type: 'steps',
      items: [
        {
          no: '01',
          text: {
            ko: '지방 및 세포 채취',
            'zh-CN': '采集脂肪及细胞',
            'zh-TW': '採集脂肪及細胞',
            en: 'Fat and cell harvesting',
          },
        },
        {
          no: '02',
          text: {
            ko: '원심 분리 및 불순물 제거',
            'zh-CN': '离心分离及去除杂质',
            'zh-TW': '離心分離及去除雜質',
            en: 'Centrifugation and impurity removal',
          },
        },
        {
          no: '03',
          text: {
            ko: '지방층 분리',
            'zh-CN': '分离脂肪层',
            'zh-TW': '分離脂肪層',
            en: 'Fat-layer separation',
          },
        },
        {
          no: '04',
          text: {
            ko: '줄기세포 추출',
            'zh-CN': '提取干细胞',
            'zh-TW': '提取幹細胞',
            en: 'Stem cell extraction',
          },
        },
        {
          no: '05',
          text: {
            ko: 'Active 인자 농축',
            'zh-CN': '浓缩Active因子',
            'zh-TW': '濃縮Active因子',
            en: 'Active-factor concentration',
          },
        },
        {
          no: '06',
          text: {
            ko: '줄기세포 주입',
            'zh-CN': '注入干细胞',
            'zh-TW': '注入幹細胞',
            en: 'Stem cell injection',
          },
        },
      ],
    },
  ],

  // ─────────────────────────────── breast ───────────────────────────────
  breast: [
    {
      type: 'intro',
      headline: {
        ko: '체계적인 시스템으로 수술 만족도를 높입니다',
        'zh-CN': '以系统化的体系提升手术满意度',
        'zh-TW': '以系統化的體系提升手術滿意度',
        en: 'A Systematic Approach That Raises Satisfaction',
      },
      body: {
        ko: '수술 전 3D 가상성형으로 개인별 체형의 특징과 좌우대칭 등을 과학적으로 분석해, 본인에게 가장 잘 어울리는 보형물을 선택할 수 있습니다.',
        'zh-CN': '术前通过3D虚拟整形，科学分析个人体型特征与左右对称等，可选择最适合本人的假体。',
        'zh-TW': '術前透過3D虛擬整形，科學分析個人體型特徵與左右對稱等，可選擇最適合本人的假體。',
        en: 'Before surgery, 3D virtual simulation scientifically analyzes your body shape and symmetry so you can choose the implant that suits you best.',
      },
    },
    {
      type: 'quote',
      text: {
        ko: '수술 전·후 사후관리까지, WJ 원진의 체계적인 가슴성형 시스템',
        'zh-CN': '从术前、术后到后续管理，WJ原辰系统化的胸部整形体系',
        'zh-TW': '從術前、術後到後續管理，WJ原辰系統化的胸部整形體系',
        en: "From before and after surgery to aftercare — WJ WonJin's systematic breast-surgery approach",
      },
    },
    {
      type: 'features',
      items: [
        {
          image: 'breast/img_breast_circle01.png',
          title: {
            ko: 'HD 내시경으로 정교하게',
            'zh-CN': 'HD内窥镜下精细操作',
            'zh-TW': 'HD內視鏡下精細操作',
            en: 'Precision with HD Endoscopy',
          },
          body: {
            ko: 'HD 내시경으로 가슴 조직의 신경과 혈관을 직접 확인하며 수술해 통증이 거의 없고 회복이 빠릅니다.',
            'zh-CN': '通过HD内窥镜直接确认胸部组织的神经与血管进行手术，几乎无痛且恢复快。',
            'zh-TW': '透過HD內視鏡直接確認胸部組織的神經與血管進行手術，幾乎無痛且恢復快。',
            en: 'With HD endoscopy, we operate while directly checking the nerves and blood vessels of the breast tissue — for minimal pain and fast recovery.',
          },
        },
        {
          image: 'breast/img_breast_circle02.png',
          title: {
            ko: '안전한 보형물 삽입 · 켈러펀넬',
            'zh-CN': '安全植入假体 · 凯乐漏斗',
            'zh-TW': '安全植入假體 · 凱樂漏斗',
            en: 'Safe Implant Insertion · Keller Funnel',
          },
          body: {
            ko: '켈러펀넬은 최소 절개로 보형물을 삽입해 흉터가 거의 없고, 보형물에 손을 대지 않아 조직·신경 손상과 염증·부작용을 미연에 방지합니다.',
            'zh-CN': '凯乐漏斗以最小切口植入假体，几乎不留疤痕，且不用手接触假体，预防组织与神经损伤及炎症、副作用。',
            'zh-TW': '凱樂漏斗以最小切口植入假體，幾乎不留疤痕，且不用手接觸假體，預防組織與神經損傷及炎症、副作用。',
            en: 'The Keller Funnel inserts the implant through a minimal incision with barely any scarring, and without touching the implant by hand — preventing tissue and nerve damage, inflammation, and side effects.',
          },
        },
        {
          image: 'breast/img_breast_circle03.png',
          title: {
            ko: '통증을 최소화하는 늑간신경 마취',
            'zh-CN': '将疼痛降至最低的肋间神经麻醉',
            'zh-TW': '將疼痛降至最低的肋間神經麻醉',
            en: 'Intercostal Nerve Block for Minimal Pain',
          },
          body: {
            ko: '흉부 통증을 대뇌로 전달하는 늑간신경을 마취하는 고난도 기법으로, 수술 중·후 가슴 부위 통증을 차단합니다.',
            'zh-CN': '以麻醉将胸部疼痛传至大脑的肋间神经的高难度技法，阻断术中及术后胸部疼痛。',
            'zh-TW': '以麻醉將胸部疼痛傳至大腦的肋間神經的高難度技法，阻斷術中及術後胸部疼痛。',
            en: 'An advanced technique that anesthetizes the intercostal nerves carrying chest pain to the brain, blocking breast-area pain during and after surgery.',
          },
        },
      ],
    },
    {
      type: 'features',
      items: [
        {
          image: 'breast/img_breast_circle04.png',
          title: {
            ko: '사후관리 시스템',
            'zh-CN': '术后管理体系',
            'zh-TW': '術後管理體系',
            en: 'Aftercare System',
          },
          body: {
            ko: '수술 후 캡슐 구축, 힐라이트 등 관리부터 정기적인 초음파 검진까지 체계적으로 운영해 빠른 회복을 돕습니다.',
            'zh-CN': '从术后包膜挛缩、HealLite等管理到定期超声波检查，系统化运营，帮助快速恢复。',
            'zh-TW': '從術後包膜攣縮、HealLite等管理到定期超音波檢查，系統化營運，幫助快速恢復。',
            en: 'From post-op care for capsular contracture and HealLite to regular ultrasound check-ups, everything is run systematically to speed recovery.',
          },
        },
        {
          image: 'breast/img_breast_circle05.png',
          title: {
            ko: '안전 시스템',
            'zh-CN': '安全体系',
            'zh-TW': '安全體系',
            en: 'Safety System',
          },
          body: {
            ko: '1:1 전담 마취 시스템, 위급 상황 대비 시스템, 정밀 온도 제어 시스템, 단트롤렌 보유, 무정전 전원공급 장치 등 안전을 최우선으로 한 시스템을 갖췄습니다.',
            'zh-CN': '配备1:1专属麻醉系统、紧急情况应对系统、精密温度控制系统、丹曲林储备、不间断电源装置等以安全为最优先的系统。',
            'zh-TW': '配備1:1專屬麻醉系統、緊急情況應對系統、精密溫度控制系統、丹曲林儲備、不斷電電源裝置等以安全為最優先的系統。',
            en: 'A safety-first setup: a 1:1 dedicated anesthesia system, emergency-response system, precision temperature control, dantrolene on hand, and an uninterruptible power supply.',
          },
        },
      ],
    },
  ],

  // ─────────────────────────────── contour ───────────────────────────────
  contour: [
    {
      type: 'intro',
      subhead: {
        ko: '하나부터 열까지 믿을 수 있게',
        'zh-CN': '从头到尾值得信赖',
        'zh-TW': '從頭到尾值得信賴',
        en: 'Trustworthy from Start to Finish',
      },
      headline: {
        ko: 'WJ 원진의 철저한 윤곽·양악 안전 시스템',
        'zh-CN': 'WJ原辰周密的轮廓·正颌安全系统',
        'zh-TW': 'WJ原辰周密的輪廓·正顎安全系統',
        en: "WJ WonJin's Thorough Facial-Contouring & Double-Jaw Safety System",
      },
      body: {
        ko: '전통과 명성의 자부심, 얼굴 뼈 수술의 표본입니다.',
        'zh-CN': '传统与声誉的自豪，面部骨骼手术的典范。',
        'zh-TW': '傳統與聲譽的自豪，面部骨骼手術的典範。',
        en: 'The pride of tradition and reputation — a benchmark in facial-bone surgery.',
      },
    },
    {
      type: 'features',
      items: [
        {
          no: '01',
          image: 'contour/img_facial_medical_01.png',
          title: {
            ko: '1:1 전담 마취 시스템',
            'zh-CN': '1:1专属麻醉系统',
            'zh-TW': '1:1專屬麻醉系統',
            en: '1:1 Dedicated Anesthesia System',
          },
          body: {
            ko: '마취통증의학과 전문의가 상주해 수술 전 과정을 실시간으로 모니터링합니다.',
            'zh-CN': '麻醉疼痛医学科专业医生常驻，实时监测手术全过程。',
            'zh-TW': '麻醉疼痛醫學科專業醫師常駐，即時監測手術全過程。',
            en: 'An anesthesiology and pain-medicine specialist is on site, monitoring the entire procedure in real time.',
          },
        },
        {
          no: '02',
          image: 'contour/img_facial_medical_02.png',
          title: {
            ko: '3D 디지털 양악수술',
            'zh-CN': '3D数字化正颌手术',
            'zh-TW': '3D數位化正顎手術',
            en: '3D Digital Double-Jaw Surgery',
          },
          body: {
            ko: '3D 모의수술로 0.1mm 단위까지 구현하는 정확하고 정밀한 1:1 개인 맞춤형 양악수술 시스템입니다.',
            'zh-CN': '通过3D模拟手术实现至0.1mm单位的精确精密的1:1个人定制正颌手术系统。',
            'zh-TW': '透過3D模擬手術實現至0.1mm單位的精確精密的1:1個人客製正顎手術系統。',
            en: 'A 3D surgical simulation delivers an accurate, precise 1:1 personalized double-jaw system down to 0.1 mm.',
          },
        },
        {
          no: '03',
          image: 'contour/img_facial_medical_03.png',
          title: {
            ko: '24시간 집중케어유닛 [ICU]',
            'zh-CN': '24小时重症监护病房 [ICU]',
            'zh-TW': '24小時重症監護病房 [ICU]',
            en: '24-Hour Intensive Care Unit [ICU]',
          },
          body: {
            ko: '수술 후 24시간 동안 양악 집중케어 유닛에서 환자를 집중 모니터링합니다.',
            'zh-CN': '术后24小时内在正颌重症监护病房对患者进行集中监测。',
            'zh-TW': '術後24小時內在正顎重症監護病房對患者進行集中監測。',
            en: 'For 24 hours after surgery, patients are closely monitored in the double-jaw intensive care unit.',
          },
        },
        {
          no: '04',
          image: 'contour/img_facial_medical_04_n.png',
          title: {
            ko: '자체 검진센터 운영',
            'zh-CN': '运营自有检查中心',
            'zh-TW': '營運自有檢查中心',
            en: 'In-House Screening Center',
          },
          body: {
            ko: '자체 검진센터와 첨단 장비를 보유해, 수술 전 정밀 검진을 진행합니다.',
            'zh-CN': '拥有自有检查中心与先进设备，术前进行精密检查。',
            'zh-TW': '擁有自有檢查中心與先進設備，術前進行精密檢查。',
            en: 'We have our own screening center and advanced equipment for a thorough pre-operative examination.',
          },
        },
        {
          no: '05',
          image: 'contour/img_facial_medical_05.png',
          title: {
            ko: '정밀 온도 제어 시스템',
            'zh-CN': '精密温度控制系统',
            'zh-TW': '精密溫度控制系統',
            en: 'Precision Temperature Control (Heated Circuit)',
          },
          body: {
            ko: '마취 시 안정적인 산소 공급을 위한 정밀 온도 제어 시스템으로, HME 필터가 바이러스·박테리아 유입과 환자 간 교차 감염을 방지합니다.',
            'zh-CN': '为麻醉时稳定供氧的精密温度控制系统，HME过滤器防止病毒、细菌侵入及患者之间的交叉感染。',
            'zh-TW': '為麻醉時穩定供氧的精密溫度控制系統，HME過濾器防止病毒、細菌侵入及患者之間的交叉感染。',
            en: 'A precision temperature-control system for stable oxygen delivery during anesthesia; the HME filter blocks viral and bacterial entry and cross-infection between patients.',
          },
        },
      ],
    },
  ],

  // ─────────────────────────────── bodyline ───────────────────────────────
  bodyline: [
    {
      type: 'intro',
      headline: {
        ko: '지방흡입, 20여 년의 노하우',
        'zh-CN': '脂肪抽吸，20余年的经验积累',
        'zh-TW': '脂肪抽吸，20餘年的經驗積累',
        en: 'Liposuction, Backed by 20+ Years of Expertise',
      },
      body: {
        ko: 'WJ 원진성형외과는 국내외 다수의 학회에서 지방흡입술의 노하우를 인정받았으며, 국내외 성형 발전을 위해 변함없이 노력하고 있습니다.',
        'zh-CN': 'WJ原辰整形外科在国内外多个学会上获得脂肪抽吸术经验的认可，并为国内外整形事业的发展持续不懈地努力。',
        'zh-TW': 'WJ原辰整形外科在國內外多個學會上獲得脂肪抽吸術經驗的認可，並為國內外整形事業的發展持續不懈地努力。',
        en: 'WJ WonJin Plastic Surgery has been recognized for its liposuction expertise by numerous academic societies at home and abroad, and continues to work steadily toward advancing plastic surgery worldwide.',
      },
    },
    {
      type: 'gallery',
      images: [
        'bodyline/img_license_01.png',
        'bodyline/img_license_02.png',
        'bodyline/img_license_03.png',
        'bodyline/img_license_04.png',
        'bodyline/img_license_05.png',
        'bodyline/img_license_06.png',
        'bodyline/img_license_07.png',
        'bodyline/img_license_08.png',
        'bodyline/img_license_09.png',
        'bodyline/img_license_10.png',
      ],
      caption: {
        ko: '국내외 학회 인정 및 관련 인증',
        'zh-CN': '国内外学会认可及相关认证',
        'zh-TW': '國內外學會認可及相關認證',
        en: 'Recognition and certifications from academic societies at home and abroad',
      },
    },
  ],

  // ─────────────────────────────── men ───────────────────────────────
  men: [
    {
      type: 'intro',
      body: {
        ko: '여성과는 다른 남성의 미적 기준에 맞춘 차별화된 방법으로, 개개인에 맞는 자연스러운 남성미를 완성합니다.',
        'zh-CN': '以契合男性（有别于女性）审美标准的差异化方法，完成适合每个人的自然男性魅力。',
        'zh-TW': '以契合男性（有別於女性）審美標準的差異化方法，完成適合每個人的自然男性魅力。',
        en: "With an approach tailored to men's aesthetic standards — distinct from women's — we complete a natural masculine look suited to each individual.",
      },
    },
    {
      type: 'intro',
      headline: {
        ko: '남자 성형, 풍부한 경험과 연구',
        'zh-CN': '男性整形，丰富的经验与研究',
        'zh-TW': '男性整形，豐富的經驗與研究',
        en: "Men's Surgery, Backed by Deep Experience and Research",
      },
      body: {
        ko: 'WJ 원진은 남자 성형에 대한 풍부한 수술 경험과 오랜 연구로 안전하고 만족스러운 결과를 만듭니다. 다양한 논문 발표와 꾸준한 연구를 통해 수술력과 안전성을 인정받고 있습니다.',
        'zh-CN': 'WJ原辰凭借在男性整形方面丰富的手术经验与长期研究，打造安全且令人满意的结果。通过多篇论文发表与持续研究，其手术能力与安全性获得认可。',
        'zh-TW': 'WJ原辰憑藉在男性整形方面豐富的手術經驗與長期研究，打造安全且令人滿意的結果。透過多篇論文發表與持續研究，其手術能力與安全性獲得認可。',
        en: 'With extensive surgical experience and long-term research in male plastic surgery, WJ WonJin produces safe, satisfying results. Its surgical skill and safety are recognized through numerous published papers and ongoing research.',
      },
    },
    {
      type: 'gallery',
      images: [
        'men/img_cfs_book_01.jpg',
        'men/img_cfs_book_02.jpg',
        'men/img_cfs_book_03.jpg',
        'men/img_cfs_book_04.jpg',
        'men/img_cfs_book_05.jpg',
        'men/img_cfs_book_06.jpg',
        'men/img_cfs_book_07.jpg',
        'men/img_cfs_book_08.jpg',
      ],
      caption: {
        ko: 'WJ 원진 의료진의 논문·저서',
        'zh-CN': 'WJ原辰医疗团队的论文·著作',
        'zh-TW': 'WJ原辰醫療團隊的論文·著作',
        en: 'Papers and books by the WJ WonJin medical team',
      },
    },
  ],

  // ─────────────────────────── reconstruction ───────────────────────────
  reconstruction: [
    {
      type: 'quote',
      text: {
        ko: '상처받은 마음까지 치료하겠습니다.',
        'zh-CN': '连受伤的心也一并治愈。',
        'zh-TW': '連受傷的心也一併治癒。',
        en: 'We will heal even the wounded heart.',
      },
    },
    {
      type: 'intro',
      body: {
        ko: 'WJ 원진은 재건 성형에 대한 풍부한 수술 경험과 오랜 연구로 안전하고 만족스러운 결과를 만듭니다. 그동안의 노하우를 담은 논문들이 수많은 학회지에 게재되며, 재건 성형에 대한 전문성을 국내외에서 인정받고 있습니다.',
        'zh-CN': 'WJ原辰凭借在重建整形方面丰富的手术经验与长期研究，打造安全且令人满意的结果。凝聚多年经验的论文刊载于众多学术期刊，重建整形的专业性在国内外获得认可。',
        'zh-TW': 'WJ原辰憑藉在重建整形方面豐富的手術經驗與長期研究，打造安全且令人滿意的結果。凝聚多年經驗的論文刊載於眾多學術期刊，重建整形的專業性在國內外獲得認可。',
        en: 'With extensive surgical experience and long-term research in reconstructive surgery, WJ WonJin produces safe, satisfying results. Papers distilling this expertise have appeared in many academic journals, earning recognition for its reconstructive expertise at home and abroad.',
      },
    },
    // award_01~04.png는 4000px 투명 캔버스 안에 저널 표지 하나만 구석에 있는 형태라 균일 갤러리로
    // 못 씀(사용자가 "필요 시"로 표시한 선택 이미지) — 인용문+본문으로 마무리.
  ],
}
