// frontend/app/data/hospitalTour.ts
// 홈 [WJ 원진 소개] 둘러보기·시설 섹션 데이터(2026-08-28, 사용자 지시로 k-wonjin.co.kr/hospitalinfo/about
// 전량 반영). 원문(ko)은 그 페이지 그대로, zh-CN/zh-TW/en은 대응 문단이 없어 직접 번역(procedures.ts와
// 동일하게 구조적 다국어 데이터라 i18n JSON이 아니라 이 TS 파일에 둠).
import type { Locale } from './procedures'

export interface HospitalFloor {
  floor: number
  /** /img/about/floors/ 아래 파일명 */
  images: string[]
  items: Record<Locale, string[]>
}

export const HOSPITAL_FLOORS: HospitalFloor[] = [
  {
    floor: 18,
    images: ['18F_01.jpg', '18F_01_1.jpg', '18F_01_2.jpg', '18F_01_3.jpg', '18F_02.jpg', '18F_03.jpg', '18F_04.jpg', '18F_05.jpg', '18F_06.jpg'],
    items: {
      ko: ['상담실', 'VIP ROOM', '임상병리실', '3D-CT 촬영실', 'X-RAY 검사실', '초음파 검사실', '카페 · 파우더룸'],
      'zh-CN': ['咨询室', 'VIP ROOM', '临床病理室', '3D-CT检查室', 'X光检查室', '超声波检查室', '咖啡厅・化妆间'],
      'zh-TW': ['諮詢室', 'VIP ROOM', '臨床病理室', '3D-CT檢查室', 'X光檢查室', '超音波檢查室', '咖啡廳・化妝間'],
      en: ['Consultation Room', 'VIP Room', 'Clinical Pathology Lab', '3D-CT Imaging Room', 'X-Ray Room', 'Ultrasound Room', 'Café & Powder Room'],
    },
  },
  {
    floor: 17,
    images: ['17F_01.jpg', '17F_02.jpg', '17F_03.jpg', '17F_04.jpg', '17F_05.jpg', '17F_06.jpg', '17F_07.jpg', '17F_08.jpg'],
    items: {
      ko: ['데스크', '상담실', '이비인후과', '사진 촬영실'],
      'zh-CN': ['前台', '咨询室', '耳鼻喉科', '摄影室'],
      'zh-TW': ['櫃檯', '諮詢室', '耳鼻喉科', '攝影室'],
      en: ['Front Desk', 'Consultation Room', 'ENT', 'Photography Room'],
    },
  },
  {
    floor: 16,
    images: ['16F_01.jpg', '16F_02.jpg', '16F_03.jpg', '16F_04.jpg', '16F_05.jpg'],
    items: {
      ko: ['수술센터', '회복실'],
      'zh-CN': ['手术中心', '恢复室'],
      'zh-TW': ['手術中心', '恢復室'],
      en: ['Surgery Center', 'Recovery Room'],
    },
  },
  {
    floor: 15,
    images: ['15F_04.jpg', '15F_02.jpg', '15F_03.jpg', '15F_01.jpg'],
    items: {
      ko: ['수술센터', '회복실', '치료실', '줄기세포 연구실'],
      'zh-CN': ['手术中心', '恢复室', '治疗室', '干细胞研究室'],
      'zh-TW': ['手術中心', '恢復室', '治療室', '幹細胞研究室'],
      en: ['Surgery Center', 'Recovery Room', 'Treatment Room', 'Stem Cell Research Lab'],
    },
  },
  {
    floor: 14,
    images: ['14F_01.jpg', '14F_02.jpg', '14F_03.jpg', '14F_04.jpg', '14F_05.jpg', '14F_06.jpg', '14F_07.jpg'],
    items: {
      ko: ['피부과'],
      'zh-CN': ['皮肤科'],
      'zh-TW': ['皮膚科'],
      en: ['Dermatology'],
    },
  },
  {
    floor: 13,
    images: ['13F_01.jpg', '13F_02.jpg', '13F_03.jpg', '13F_05.jpg', '13F_06.jpg', '13F_08.jpg', '13F_09.jpg', '13F_10.jpg', '13F_11.jpg', '13F_12.jpg', '13F_13.jpg'],
    items: {
      ko: ['VIP 안티에이징', '입원실'],
      'zh-CN': ['VIP抗衰老', '住院室'],
      'zh-TW': ['VIP抗衰老', '住院室'],
      en: ['VIP Anti-Aging', 'Inpatient Ward'],
    },
  },
  {
    floor: 12,
    images: ['12F_01.jpg', '12F_02.jpg', '12F_03.jpg', '12F_03_1.jpg', '12F_04.jpg', '12F_05.jpg', '12F_06.jpg', '12F_07.jpg', '12F_08.jpg', '12F_09.jpg', '12F_10.jpg', '12F_11.jpg'],
    items: {
      ko: ['WONJIN GLOBAL HUB', '글로벌 고객 전용 라운지', '쁘띠 시술 센터', 'WJ 코스메틱 체험존'],
      'zh-CN': ['WONJIN GLOBAL HUB', '全球贵宾专属休息室', '小微整形中心', 'WJ化妆品体验区'],
      'zh-TW': ['WONJIN GLOBAL HUB', '全球貴賓專屬休息室', '小微整形中心', 'WJ化妝品體驗區'],
      en: ['WONJIN GLOBAL HUB', 'Global Client Lounge', 'Petit Procedure Center', 'WJ Cosmetics Experience Zone'],
    },
  },
]

export interface HospitalCenterFeature {
  slug: string
  /** /img/about/center/ 아래 파일명 */
  image: string
  title: Record<Locale, string>
  desc: Record<Locale, string>
}

export const HOSPITAL_CENTERS: HospitalCenterFeature[] = [
  {
    slug: 'anti-aging',
    image: 'center-00.jpg',
    title: {
      ko: '프리미엄 안티에이징 센터를 보유하여 최첨단 항노화 의료서비스를 제공합니다.',
      'zh-CN': '拥有高端抗衰老中心，提供尖端抗衰老医疗服务。',
      'zh-TW': '擁有高端抗衰老中心，提供尖端抗衰老醫療服務。',
      en: 'A Premium Anti-Aging Center offering advanced anti-aging medical care.',
    },
    desc: {
      ko: '프리미엄 안티에이징 센터는 첨단 기술을 활용한 줄기세포 치료를 비롯해 개인의 피부 상태와 노화 정도에 맞춘 항노화 프로그램으로 1:1 맞춤 관리를 진행합니다. 편안하고 고급스러운 환경에서 최상의 서비스를 통해 보다 효과적이고 만족스러운 변화를 경험해 보세요.',
      'zh-CN': '高端抗衰老中心不仅提供运用尖端技术的干细胞治疗，还根据个人肌肤状态和老化程度制定专属的抗衰老项目，进行1:1定制管理。在舒适高雅的环境中，通过最优质的服务，体验更有效、更满意的改变。',
      'zh-TW': '高端抗衰老中心不僅提供運用尖端技術的幹細胞治療，還根據個人肌膚狀態和老化程度制定專屬的抗衰老項目，進行1:1定制管理。在舒適高雅的環境中，通過最優質的服務，體驗更有效、更滿意的改變。',
      en: 'Our Premium Anti-Aging Center provides stem cell treatments using cutting-edge technology, along with anti-aging programs tailored 1:1 to your skin condition and degree of aging. Experience a more effective, satisfying transformation through the best service in a comfortable, luxurious setting.',
    },
  },
  {
    slug: 'checkup',
    image: 'center-01.jpg',
    title: {
      ko: '자체 검진센터를 보유하여, 원내에서 편안하고 체계적인 검진이 가능합니다.',
      'zh-CN': '拥有自营体检中心，可在院内轻松接受系统化的检查。',
      'zh-TW': '擁有自營體檢中心，可在院內輕鬆接受系統化的檢查。',
      en: 'An in-house Checkup Center for comfortable, systematic examinations on-site.',
    },
    desc: {
      ko: '스마트 검진 시스템으로, 수술 전 환자의 상태를 정밀하게 파악하여 정확한 수술을 진행합니다.',
      'zh-CN': '通过智能体检系统，在手术前精密掌握患者状态，从而进行精准手术。',
      'zh-TW': '通過智能體檢系統，在手術前精密掌握患者狀態，從而進行精準手術。',
      en: "Our smart examination system precisely assesses each patient's condition before surgery, enabling accurate procedures.",
    },
  },
  {
    slug: 'anesthesia',
    image: 'center-02.jpg',
    title: {
      ko: '수술의 전 과정에 마취통증의학과 전문의가 함께하여 안전한 수술이 가능합니다.',
      'zh-CN': '手术全过程均有麻醉疼痛医学科专科医生陪同，确保手术安全。',
      'zh-TW': '手術全過程均有麻醉疼痛醫學科專科醫生陪同，確保手術安全。',
      en: 'An anesthesiologist accompanies every step of surgery for a safe procedure.',
    },
    desc: {
      ko: 'WJ 원진성형외과에서는 수술 전부터 수술 후 회복까지 상황에 맞는 시스템과 마취통증의학과 전문의들이 함께하고 있습니다.',
      'zh-CN': 'WJ原辰整形外科从术前到术后恢复，均配备相应系统与麻醉疼痛医学科专科医生共同守护。',
      'zh-TW': 'WJ原辰整形外科從術前到術後恢復，均配備相應系統與麻醉疼痛醫學科專科醫生共同守護。',
      en: 'From before surgery through post-operative recovery, WJ WonJin provides the right system and anesthesiologists at every stage.',
    },
  },
  {
    slug: 'equipment',
    image: 'center-03.jpg',
    title: {
      ko: '40여 종의 최신 장비와 안전 시스템을 갖추고 있어, 응급상황에서도 신속한 대처가 가능합니다.',
      'zh-CN': '配备40余种最新设备及安全系统，紧急情况下也可迅速应对。',
      'zh-TW': '配備40餘種最新設備及安全系統，緊急情況下也可迅速應對。',
      en: 'Equipped with over 40 types of the latest equipment and safety systems for rapid response in emergencies.',
    },
    desc: {
      ko: '꼭 맞는 아름다움을 찾기 위해서는 최신 장비를 활용한 정보 수집은 필수입니다. 이제 WJ 원진에서 안전하게 아름다워지세요.',
      'zh-CN': '为找到最适合自己的美，运用最新设备收集信息必不可少。现在就在WJ原辰安全变美吧。',
      'zh-TW': '為找到最適合自己的美，運用最新設備收集信息必不可少。現在就在WJ原辰安全變美吧。',
      en: 'Gathering information with the latest equipment is essential to finding the beauty that truly suits you. Become beautiful safely at WJ WonJin.',
    },
  },
  {
    slug: 'sterilization',
    image: 'center-04.jpg',
    title: {
      ko: '고객의 안전을 최우선으로 하는 멸균소독 관리 시스템을 진행합니다.',
      'zh-CN': '以顾客安全为最优先，进行灭菌消毒管理系统。',
      'zh-TW': '以顧客安全為最優先，進行滅菌消毒管理系統。',
      en: 'A sterilization management system that puts customer safety first.',
    },
    desc: {
      ko: '수술실, 회복실의 365일 소독은 물론, 의료진 멸균 스크럽 및 손세정을 매일 진행하고 있습니다. 수술실 전 스텝 손소독 진행과 모든 수술 도구 1회 사용 후 멸균 소독을 원칙으로 하여, 감염 방지 및 예방 관리를 실시하고 있습니다.',
      'zh-CN': '手术室、恢复室365天消毒自不必说，医疗团队每日进行灭菌洗手消毒。原则上手术室全体工作人员进行手部消毒，所有手术器械均一次性使用后灭菌消毒，实施感染预防与管理。',
      'zh-TW': '手術室、恢復室365天消毒自不必說，醫療團隊每日進行滅菌洗手消毒。原則上手術室全體工作人員進行手部消毒，所有手術器械均一次性使用後滅菌消毒，實施感染預防與管理。',
      en: 'Operating rooms and recovery rooms are disinfected year-round, and our medical staff perform sterile scrubbing and hand washing daily. All surgical staff disinfect their hands before entering the operating room, and every surgical instrument is sterilized after single use, ensuring thorough infection prevention and control.',
    },
  },
  {
    slug: 'amenities',
    image: 'center-05.jpg',
    title: {
      ko: 'WJ원진을 방문해주시는 고객님의 편의를 위해 편의시설 및 충분한 대기공간을 마련하였습니다.',
      'zh-CN': '为方便到访WJ原辰的顾客，特设便利设施及充足的等候空间。',
      'zh-TW': '為方便到訪WJ原辰的顧客，特設便利設施及充足的等候空間。',
      en: 'Convenient facilities and ample waiting space for the comfort of our visitors.',
    },
    desc: {
      ko: '빠른 진료 서비스를 제공하기 위한 리셉션과 고객 대기실, 전문 바리스타가 있는 카페공간이 마련되어 있습니다.',
      'zh-CN': '设有为提供快速诊疗服务的接待处与顾客等候室，以及配备专业咖啡师的咖啡空间。',
      'zh-TW': '設有為提供快速診療服務的接待處與顧客等候室，以及配備專業咖啡師的咖啡空間。',
      en: 'A reception area and waiting room for fast service, along with a café staffed by a professional barista, are available.',
    },
  },
]
