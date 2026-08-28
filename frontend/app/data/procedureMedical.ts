// frontend/app/data/procedureMedical.ts
// 시술 카테고리 목록 페이지('procedures/[category]/index.vue')의 "고민이 있으신가요?" 영역 위에
// 붙는 병원 시스템·특장점 콘텐츠. k-wonjin.co.kr 각 카테고리 페이지의 마케팅 섹션을 옮긴 것으로,
// 구조적 다국어 데이터라 i18n JSON이 아니라 여기(TS)에 둔다(procedures.ts·hospitalTour.ts와 동일 패턴).
// 원문(ko)은 사용자가 제공, zh-CN/zh-TW/en은 직접 번역(k-wonjin.co.kr은 한국어 전용).
import type { Locale } from './procedures'

type L = Record<Locale, string>

/** headline + (선택)subhead 오버라인 + body 문단 */
export interface MSIntro {
  type: 'intro'
  headline: L
  subhead?: L
  body: L
}
/** 01·02·03… 번호가 붙는 짧은 특징 카드 목록 */
export interface MSSteps {
  type: 'steps'
  items: { no: string; text: L }[]
}
/** 강조 인용문 */
export interface MSQuote {
  type: 'quote'
  text: L
}
/** 제목+본문(+선택 이미지) 카드 목록. image는 '/img/' 아래 상대경로(예: 'nose/innofit_2.png') */
export interface MSFeatures {
  type: 'features'
  items: { title: L; body: L; image?: string }[]
}

export type MedicalBlock = MSIntro | MSSteps | MSQuote | MSFeatures

export const PROCEDURE_MEDICAL: Record<string, MedicalBlock[]> = {
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
}
