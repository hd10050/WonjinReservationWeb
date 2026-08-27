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
    intro: {
      ko: '눈은 첫인상을 결정하는 데 가장 중요한 부분으로 손꼽히는부위입니다. WJ 원진은 개인마다 지니고 있는 얼굴의 밸런스와 피부타입이다른 점을 고려하여 상담을 통해 본인의 개성을 살리면서 자연스럽고조화가 잘 이루어질 수 있는 수술법을 추천해드립니다. WJ 원진의 눈 성형은정밀한 진단과 상담을 바탕으로 가장 적합한 수술 방법을 선택해완성도 높은 수술 결과를 보장합니다.',
      'zh-CN': '眼部是决定一个人第一印象的重要部位。WJ原辰考虑到每个人面部及皮肤类型的差异，通过咨询推荐可以彰显个性的同时追求自然跟面部协调的手术方案。WJ原辰的眼部整形以精密诊断及详细咨询为基础，选择最适合的手术方案，保障高满意度的手术效果。',
      'zh-TW': '眼部是決定一個人第一印象的重要部位。WJ原辰考慮到每個人面部及皮膚類型的差異，通過諮詢推薦可以彰顯個性的同時追求自然跟面部協調的手術方案。WJ原辰的眼部整形以精密診斷及詳細諮詢為基礎，選擇最適合的手術方案，保障高滿意度的手術效果。',
      en: "The eyes are considered one of the most important features in shaping a first impression. Because every face has its own unique balance and skin type, WJ WonJin recommends the surgical approach that best suits your individual features through in-depth consultation, aiming for a natural, harmonious result. Based on precise diagnosis and consultation, WJ WonJin's eye surgery selects the most suitable surgical method to guarantee a highly refined outcome.",
    },
    items: [
      {
        slug: 'glam-eye',
        name: { ko: '비절개 눈매교정 - 글램아이', 'zh-CN': '非切开眼型矫正－Glam Eye', 'zh-TW': '非切開眼型矯正－Glam Eye', en: 'Non-Incisional Eye Shape Correction – Glam Eye' },
        concerns: {
          ko: ['티 안 나게 눈이 또렷하고 시원하게 커지기를 원해요.', '이마를 이용하여 눈을 뜨는 습관이 있어요.', '쌍꺼풀이 여러 겹이거나 짝짝이에요.'],
          en: ['I want my eyes to look bright and clear, larger, without it being obvious.', 'I have a habit of using my forehead muscles to open my eyes.', 'My double eyelids are multiple or uneven.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '쌍꺼풀과 눈매교정은 물론 속 눈썹의 위치까지 교정하여아름답고 호감 가는 눈매로 만들어 드리는 토탈 눈 성형입니다.',
          'zh-CN': '双眼皮手术与眼型矫正同时进行，还可以调整睫毛的位置整体提升眼部魅力的综合眼部手术。',
          en: 'This is a total eye surgery that corrects not only the double eyelid and eye shape but also the position of the lower lashes, creating a beautiful, appealing eye shape.',
          'zh-TW': '雙眼皮手術與眼型矯正同時進行，還可以調整睫毛的位置整體提升眼部魅力的綜合眼部手術。',
        },
        label: { ko: '쌍꺼풀과 눈매 교정을 동시에', en: 'Double Eyelid and Eye Shape Correction at Once' },
        image: 'eye-glam-eye.png',
      },
      {
        slug: 'double-adhesion',
        name: { ko: '부분절개 눈매교정 - 더블유착', 'zh-CN': '部分切开眼型矫正－Double Adhesion', 'zh-TW': '部分切開眼型矯正－Double Adhesion', en: 'Partial-Incision Eye Shape Correction – Double Adhesion' },
        concerns: {
          ko: ['심하게 졸려 보이는 안검하수가 있어요.', '눈을 뜰 때 이마에 주름이 생겨요.', '눈꺼풀이 두껍거나 지방이 많아요.'],
          en: ['I have ptosis that makes my eyes look severely sleepy.', 'Wrinkles form on my forehead when I open my eyes.', 'My eyelids are thick or have a lot of fat.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '단순히 쌍꺼풀을 만드는 것이 아니라 눈을 뜨는근육의 힘을 조절하여 더욱 선명하고 또렷한 눈매를완성하는 토탈 눈 성형입니다.',
          'zh-CN': '不仅是单纯打造双眼皮而是调节睁眼肌肉的力量打造更加鲜明明亮的眼部整形手术。',
          en: 'Rather than simply creating a double eyelid, this total eye surgery adjusts the strength of the eye-opening muscle to create a clearer, more defined eye shape.',
          'zh-TW': '不僅是單純打造雙眼皮而是調節睜眼肌肉的力量打造更加鮮明明亮的眼部整形手術。',
        },
        label: { ko: '풀리지 않은 강한 고정력', en: 'Strong, Long-Lasting Fixation' },
        image: 'eye-double-adhesion.png',
      },
      {
        slug: 'angel-eye',
        name: { ko: '눈썹 올림술 - 엔젤아이', 'zh-CN': '提眉术－Angel Eye', 'zh-TW': '提眉術－Angel Eye', en: 'Brow Lift – Angel Eye' },
        concerns: {
          ko: ['쌍꺼풀 수술을 했음에도 불구하고 부족한 부분이 있는 것 같아요.', '쌍꺼풀 수술 없이 예쁘고 선한 눈매를 원해요.', '눈썹 때문에 인상이 강해 보여요.'],
          en: ['Even after double eyelid surgery, I feel something is still lacking.', 'I want pretty, gentle-looking eyes without double eyelid surgery.', 'My eyebrows make my expression look harsh.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '눈 주위 주름과 이마의 모양이 동시에 개선되며, 처진 피부도사라져 부드럽고 온화한 이미지로 변화시켜 드립니다.',
          'zh-CN': '同时改善眼周皱纹和额头形状解决松弛下垂的肌肤问题，打造温柔的形象。',
          en: 'Wrinkles around the eyes and the shape of the forehead are improved at the same time, and sagging skin disappears, transforming your look into a softer, gentler image.',
          'zh-TW': '同時改善眼周皺紋和額頭形狀解決鬆弛下垂的肌膚問題，打造溫柔的形象。',
        },
        label: { ko: '눈썹, 눈매 교정만으로 이미지가 개선되는', en: 'A Softer Look with Just Eyebrow and Eye Shape Correction' },
        image: 'eye-angel-eye.png',
      },
      {
        slug: 'open-eye',
        name: { ko: '트임 성형 - 오픈아이', 'zh-CN': '开眼角手术－Open Eye', 'zh-TW': '開眼角手術－Open Eye', en: 'Eye Opening Surgery – Open Eye' },
        concerns: {
          ko: ['몽고주름이 있어 눈이 답답해 보여요.', '눈과 눈 사이가 멀어요.', '눈의 가로 폭이 짧아 답답해 보여요.'],
          en: ['I have epicanthic folds that make my eyes look cramped.', 'My eyes are set far apart.', 'The horizontal width of my eyes is short, making them look cramped.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '자연스러움을 유지하면서 또렷하고시원한 이미지의 눈을 완성할 수 있습니다.',
          'zh-CN': '自然的同时清爽的双眸打造有灵气的大眼睛。',
          en: 'While maintaining a natural look, this procedure completes clear, bright-looking eyes.',
          'zh-TW': '自然的同時清爽的雙眸打造有靈氣的大眼睛。',
        },
        label: { ko: '답답함 없이 시원하게', en: 'Bright and Open, Without the Cramped Look' },
        image: 'eye-open-eye.png',
      },
      {
        slug: 'eye-revision',
        name: { ko: '눈 재수술', 'zh-CN': '眼部修复手术', 'zh-TW': '眼部修復手術', en: 'Revision Eye Surgery' },
        concerns: {
          ko: ['앞이 크고 눈매 라인 따라 뒤로 갈수록 낮아져요.', '라인이 앞이 눌려 보이지 않고 뒤쪽 라인만 보여요.', '쌍꺼풀 라인이 앞과 뒤만 보이고 눈을 떴을 때 중간이 눌려 보여요.'],
          en: ["The front is large and the eyelid line gets lower toward the back.", "The front line isn't visible and only the back line shows.", 'Only the front and back of the double eyelid line are visible, with the middle looking pressed down when the eyes are open.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '현재 눈 상태에 따른 정밀한 진단을 바탕으로 가장 적합한수술 방법을 선택해 완성도 높은 수술 결과를 보장합니다.',
          'zh-CN': '对眼部状态进行精密的诊断，以此为基础选择最适合的手术方法，提高手术的完成度。',
          en: 'Based on a precise diagnosis of your current eye condition, we select the most suitable surgical method to guarantee a highly refined result.',
          'zh-TW': '對眼部狀態進行精密的診斷，以此為基礎選擇最適合的手術方法，提高手術的完成度。',
        },
        label: { ko: '눈 재수술 + 눈매 라인 재교정', en: 'Revision Eye Surgery + Eyelid Line Re-Correction' },
        image: 'eye-eye-revision.png',
      },
      {
        slug: 'severe-ptosis-correction',
        name: { ko: '고도 안검하수 눈매교정', 'zh-CN': '重度上睑下垂眼型矫正', 'zh-TW': '重度上瞼下垂眼型矯正', en: 'Severe Ptosis Correction' },
        concerns: {
          ko: ['눈썹이나 이마에 힘을 주어도 눈이 잘 떠지지 않아요.', '선천적으로 안검하수를 가지고 있어요.', '눈매교정 수술후에도 눈꺼풀 움직임이 느려요.'],
          en: ["Even when I strain my eyebrows or forehead, my eyes don't open well.", 'I have congenital ptosis.', 'Even after eye shape correction surgery, my eyelid movement is slow.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '미용 및 기능적으로 결과가 우수하며 재발이 적은 고난도 눈매교정입니다.',
          'zh-CN': '是兼顾美容性和功能性手术成果，复发性小的高难度眼型矫正术。',
          en: 'This is an advanced eye shape correction with excellent aesthetic and functional results and a low recurrence rate.',
          'zh-TW': '是兼顧美容性和功能性手術成果，復發性小的高難度眼型矯正術。',
        },
        label: { ko: '고도 안검하수를 개선하는', en: 'Correcting Severe Ptosis' },
        image: 'eye-severe-ptosis-correction.png',
      },
      {
        slug: 'lower-eyelid-fat-repositioning',
        name: { ko: '눈밑지방재배치', 'zh-CN': '眼袋脂肪重置', 'zh-TW': '眼袋脂肪重置', en: 'Lower Eyelid Fat Repositioning' },
        concerns: {
          ko: ['눈밑 지방이 볼록하게 튀어나왔어요.', '눈밑 피부가 쳐져 나이 들어 보인대요.', '다크서클이 심해서 피곤해 보여요.'],
          en: ['The fat under my eyes bulges out.', 'The skin under my eyes sags, making me look older.', 'My dark circles are severe, making me look tired.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '노안의 원인, 눈밑 지방을 재배치하여어두운 눈가를 밝고 매끄럽게 개선시켜 줍니다.',
          'zh-CN': '因老化，通过眼底脂肪再配置使暗沉的眼周变得明亮光滑。',
          en: 'By repositioning the under-eye fat, a leading cause of an aged look, this procedure brightens and smooths dark, dull under-eye areas.',
          'zh-TW': '因老化，通過眼底脂肪再配置使暗沉的眼周變得明亮光滑。',
        },
        label: { ko: '밝고 환한 동안 눈가', en: 'Bright, Youthful-Looking Under-Eyes' },
        image: 'eye-lower-eyelid-fat-repositioning.png',
      },
      {
        slug: 'middle-aged-eye-surgery',
        name: { ko: '중년 눈성형', 'zh-CN': '中老年眼部整形', 'zh-TW': '中老年眼部整形', en: 'Middle-Aged Eye Surgery' },
        concerns: {
          ko: ['눈꺼풀이 처져서 속눈썹을 찔러요.', '눈꺼풀이 많이 내려와서 시야를 방해해요.', '눈밑 피부가 늘어지고 불룩해졌어요.', '눈밑 주름과 다크서클이 심해요.'],
          en: ['My drooping eyelids poke my eyelashes.', 'My eyelids sag so much they block my vision.', 'The skin under my eyes is loose and puffy.', 'I have severe under-eye wrinkles and dark circles.'],
          'zh-CN': ['眼皮下垂，眼睫毛刺扎眼睛', '眼皮下垂遮挡视线', '眼底皮肤下垂变得肿泡', '眼底皱纹和黑眼圈严重'],
          'zh-TW': ['眼皮下垂，眼睫毛刺扎眼睛', '眼皮下垂遮擋視線', '眼底皮膚下垂變得腫泡', '眼底皺紋和黑眼圈嚴重'],
        },
        description: {
          ko: '노화로 인해 처진 눈가를 탄력 있고, 밝은 인상의 눈매로개선이 가능한 중.장년층을 위한 안티에이징 수술입니다.',
          'zh-CN': '使因老化而下垂的眼周富有弹力明亮眼眸为了中老年层的抗衰老手术。',
          en: 'This is an anti-aging surgery for middle-aged and older patients that restores firmness and a bright impression to eyes that have sagged with age.',
          'zh-TW': '使因老化而下垂的眼周富有彈力明亮眼眸為了中老年層的抗衰老手術。',
        },
        label: { ko: '노화로 처진 눈매를 개선하는', en: 'Correcting Eyes That Have Drooped with Age' },
        image: 'eye-middle-aged-eye-surgery.png',
      },
      {
        slug: 'asymmetrical-eye-correction',
        name: { ko: '짝눈(비대칭) 교정', 'zh-CN': '大小眼（眼部不对称）矫正', 'zh-TW': '大小眼（眼部不對稱）矯正', en: 'Asymmetrical Eye Correction' },
        concerns: {
          ko: ['한쪽에만 쌍꺼풀이 있어요.', '양쪽 눈 앞트임 높이가 달라요.', '양쪽 눈 크기나 모양이 달라요.', '양쪽 눈 뜨는 힘이 달라요.', '쌍꺼풀 수술 후 라인이 다르게 나왔어요.', '한쪽 쌍꺼풀이 흐리거나 풀렸어요.'],
          en: ['I have a double eyelid on only one side.', 'The inner corner height differs between my two eyes.', 'My two eyes differ in size or shape.', 'The eye-opening strength differs between my two eyes.', 'My double eyelid lines came out different after surgery.', 'The double eyelid on one side is faint or has come undone.'],
          'zh-CN': ['只一侧有双眼皮', '双眼眼角高度不对称', '双眼大小或形状不一样', '双眼睁眼肌肉不一样', '双眼皮手术后线条不一样', '一侧双眼皮不明显或松开'],
          'zh-TW': ['只一側有雙眼皮', '雙眼眼角高度不對稱', '雙眼大小或形狀不一樣', '雙眼睜眼肌肉不一樣', '雙眼皮手術後線條不一樣', '一側雙眼皮不明顯或鬆開'],
        },
        description: {
          ko: '두 눈의 균형을 맞춰드립니다! 누구나 어느 정도의 짝눈은 있을 수 있지만, 그 차이가 커서 고민이 될 정도라면 개선이 필요합니다. 짝눈의 원인은 눈꺼풀 모양 차이, 좌우 얼굴뼈 차이, 뜨는 근육 힘의 차이 등 다양합니다. 본인의 짝눈 원인을 정확히 찾아내어 맞춤 수술 방법으로 양쪽 눈의 균형을 맞춰드립니다.',
          'zh-CN': '调整两眼的平衡！两眼不同的原因有眼皮形状不同、面部左右骨骼差异、睁眼肌肉差异等，通过量身定做的手术方案调整两眼平衡。',
          en: "We'll bring balance to both your eyes! Everyone has some degree of eye asymmetry, but if the difference is significant enough to be a concern, correction may be needed. The causes vary — eyelid shape, facial bone differences, eye-opening muscle strength, and more. We pinpoint the exact cause and use a customized surgical approach to bring balance to both eyes.",
          'zh-TW': '調整兩眼的平衡！兩眼不同的原因有眼皮形狀不同、面部左右骨骼差異、睜眼肌肉差異等，通過量身定做的手術方案調整兩眼平衡。',
        },
        image: 'eye-asymmetrical-eye-correction.png',
      },
      {
        slug: 'congenital-ptosis-children',
        name: { ko: '소아 선천성 안검하수', 'zh-CN': '儿童先天性上睑下垂', 'zh-TW': '兒童先天性上瞼下垂', en: 'Congenital Ptosis Surgery for Children' },
        concerns: {
          ko: ['약시(시력 발달 장애)', '정서 불안(비대칭이 심하면 아이가 스트레스를 받음)', '안검하수 증상(이마 주름, 두통, 집중력 저하, 불안한 행동 등)'],
          en: ['Amblyopia (impaired visual development)', 'Emotional distress (severe asymmetry can cause stress for the child)', 'Ptosis symptoms (forehead wrinkles, headaches, reduced concentration, restless behavior, etc.)'],
          'zh-CN': ['弱势(视力发育障碍)', '情绪不安(压力)', '眼睑下垂症状(额头皱纹，头痛，集中障碍)'],
          'zh-TW': ['弱勢(視力發育障礙)', '情緒不安(壓力)', '眼瞼下垂症狀(額頭皺紋，頭痛，集中障礙)'],
        },
        description: {
          ko: '소중한 우리 아이의 눈을 지켜주세요. 눈을 뜨는 근육이 완전히 발달하지 못해 나타나는 증상으로, 한쪽 눈에만 나타날 수도 있고 양쪽 눈 모두에 나타날 수도 있습니다. 대부분 한쪽 눈에만 나타나는 경우가 많아 비대칭을 완전히 교정하기 어려울 수 있습니다. 그럼에도 이 증상을 개선해야 하는 중요한 이유는 시력 발달에 영향을 줄 수 있고, 또래로부터 놀림을 받을 수 있기 때문입니다.',
          'zh-CN': '保护我们孩子宝贵的双眼。睁眼肌肉发育不完全导致，大部分单眼发生，很难完全改正不对称情况，重要的是影响视力发育及可能被嘲笑。',
          en: "Protect your precious child's eyes. This condition occurs when the eye-opening muscle has not fully developed, appearing in one or both eyes — most often just one, making full correction of the asymmetry difficult. Treating it is still important because it can affect visual development and expose the child to teasing.",
          'zh-TW': '保護我們孩子寶貴的雙眼。睜眼肌肉發育不完全導致，大部分單眼發生，很難完全改正不對稱情況，重要的是影響視力發育及可能被嘲笑。',
        },
        image: 'eye-congenital-ptosis-children.png',
      },
    ],
    otherItems: [],
  },
  {
    slug: 'nose',
    name: { ko: '코', 'zh-CN': '鼻部', 'zh-TW': '鼻部', en: 'Nose' },
    icon: 'ScanFace',
    heroImages: ['nose-hero.jpg'],
    intro: {
      ko: 'WJ 원진성형외과에서는 단순히 코를 높여 이미지를변화시키는 것이 아니라 얼굴 전체의 밸런스를 고려하여당신에게 가장 이상적인 비율과 각도를 찾아 드립니다.미용과 기능 모두 고려한 만족스러운 수술 결과를 자부합니다.',
      'zh-CN': '在WJ原辰整形外科，鼻整形不是单纯地提高鼻梁而是根据面部整体帮您打造最理想的比例和角度，在考虑鼻部审美与功能的双重基础上，为您献上满意的手术结果。',
      'zh-TW': '在WJ原辰整形外科，鼻整形不是單純地提高鼻樑而是根據面部整體幫您打造最理想的比例和角度，在考慮鼻部審美與功能的雙重基礎上，為您獻上滿意的手術結果。',
      en: "At WJ WonJin Plastic Surgery, we don't simply raise the bridge of your nose to change your image — we consider the balance of your entire face to find the ideal proportion and angle for you. We take pride in delivering satisfying results that consider both aesthetics and function.",
    },
    items: [
      {
        slug: 'upturned-short-nose',
        name: { ko: '들창코·짧은 코 성형', 'zh-CN': '朝天鼻・短鼻整形', 'zh-TW': '朝天鼻・短鼻整形', en: 'Upturned Nose & Short Nose Surgery' },
        concerns: {
          ko: ['코가 짧고 코끝이 들려있어요.', '코 끝이 올라가서 정면에서 콧구멍이 잘 보여요.', '코가 짧아서 얼굴이 납작해 보여요.'],
          en: ['My nose is short and the tip turns up.', 'The tip is upturned so my nostrils are visible from the front.', 'My nose is short, making my face look flat.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '돼지코 스트레스는 이제 그만!매끈하고 세련된 코라인을 완성해 드립니다.',
          'zh-CN': '再也不用担心不能做猪鼻子啦！为您打造清秀挺拔的鼻部曲线。',
          en: 'No more stress over a "pig nose"! We complete a smooth, refined nose line for you.',
          'zh-TW': '再也不用擔心不能做豬鼻子啦！為您打造清秀挺拔的鼻部曲線。',
        },
        label: { ko: '코 끝 연장술 (비중격 연장술)', en: 'Nasal Tip Lengthening (Septal Extension)' },
        image: 'nose-upturned-short-nose.png',
      },
      {
        slug: 'alar-reduction',
        name: { ko: '콧볼 축소', 'zh-CN': '鼻翼缩小', 'zh-TW': '鼻翼縮小', en: 'Alar Reduction' },
        concerns: {
          ko: ['코 끝이 낮고 벌어져있거나 쳐저 있어요.', '콧볼이 넓고 끝이 납작해요.', '코 끝이 뭉툭하고 넓어서 답답해보여요.'],
          en: ['My nasal tip is low and flared or drooping.', 'My nostrils are wide and the tip is flat.', 'My nasal tip is blunt and wide, making it look heavy.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '퍼진 코를 모아주어 세련된 인상으로변화시켜 드립니다.',
          'zh-CN': '聚拢宽鼻，塑造干练形象。',
          en: 'We bring together a spread-out nose to transform it into a refined look.',
          'zh-TW': '聚攏寬鼻，塑造幹練形象。',
        },
        label: { ko: '엘라스티꿈 비절개 콧볼 축소', en: 'ElastiGum Non-Incisional Alar Reduction' },
        image: 'nose-alar-reduction.png',
      },
      {
        slug: 'hump-nose',
        name: { ko: '매부리코 성형', 'zh-CN': '驼峰鼻整形', 'zh-TW': '駝峰鼻整形', en: 'Hump Nose Surgery' },
        concerns: {
          ko: ['미간이 낮아 보이고 코끝이 처져 있어요.', '코가 화살코처럼 생겼어요.', '인상이 억세고 고집스러워 보여요.'],
          en: ['The area between my brows looks low and my nasal tip droops.', 'My nose looks like an arrow-shaped nose.', 'My expression looks tough and stubborn.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '돌출 정도와 원인에 따라 적합한 수술 방법을 통해억세고 고집스러운 인상에서 세련된 인상으로 개선시켜드립니다.',
          'zh-CN': '根据鼻部凸出的程度和原因选择合适的手术方案，将刻薄固执的形象塑造为时尚干练的形象。',
          en: 'Depending on the degree and cause of the protrusion, we transform a tough, stubborn look into a refined impression.',
          'zh-TW': '根據鼻部凸出的程度和原因選擇合適的手術方案，將刻薄固執的形象塑造為時尚幹練的形象。',
        },
        label: { ko: '코 뼈 절골술', en: 'Nasal Bone Osteotomy' },
        image: 'nose-hump-nose.png',
      },
      {
        slug: 'deviated-nose',
        name: { ko: '휜 코 성형', 'zh-CN': '歪鼻整形', 'zh-TW': '歪鼻整形', en: 'Deviated Nose Surgery' },
        concerns: {
          ko: ['코가 휘어있고 비대칭이에요.', '콧대와 콧구멍이 삐뚤어져있어요.', '코가 휘어서 코막힘, 비염 등의 질환이 있어요.'],
          en: ['My nose is crooked and asymmetrical.', 'My nasal bridge and nostrils are tilted.', 'My crooked nose causes conditions like nasal congestion and rhinitis.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '코의 모양과 기능적인 부분을 동시에 개선해 균형 있고아름다운 코를 완성할 수 있습니다.',
          'zh-CN': '同时改善鼻部的形状及功能方面的问题，塑造比例均衡漂亮的鼻型。',
          en: 'By improving both the shape and function of the nose at the same time, we create a well-balanced, beautiful nose.',
          'zh-TW': '同時改善鼻部的形狀及功能方面的問題，塑造比例均衡漂亮的鼻型。',
        },
        label: { ko: '내외 측 비골 절골술', en: 'Internal and External Lateral Osteotomy' },
        image: 'nose-deviated-nose.png',
      },
      {
        slug: 'nose-revision',
        name: { ko: '코 재수술', 'zh-CN': '鼻部修复手术', 'zh-TW': '鼻部修復手術', en: 'Revision Rhinoplasty' },
        concerns: {
          ko: ['수술을 여러 번 해서 피부가 얇고 보형물과 연골이 보여요.', '수술 후에도 코 끝의 높이가 유지되지 않아요.', '코 끝이 들려 콧구멍이 많이 보여요.'],
          en: ['After multiple surgeries, my skin is thin and the implant and cartilage show through.', "The height of my nasal tip doesn't hold even after surgery.", 'My nasal tip is upturned, showing my nostrils too much.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '단순히 눈에 보이는 문제만 교정하는 것이 아니라실패 원인을 분석하여 안전하고 정확한 재수술을 진행합니다.',
          'zh-CN': '不只是矫正肉眼看得到的问题，分析手术失败的原因后进行安全精确的手术。',
          en: "Rather than simply correcting the visible issues, we analyze the cause of the previous surgery's failure and carry out a safe, precise revision.",
          'zh-TW': '不只是矯正肉眼看得到的問題，分析手術失敗的原因後進行安全精確的手術。',
        },
        label: { ko: '개인별 맞춤 코 재수술', en: 'Personalized Revision Rhinoplasty' },
        image: 'nose-nose-revision.png',
      },
      {
        slug: 'non-implant-rhinoplasty',
        name: { ko: '무보형물코성형', 'zh-CN': '无假体鼻整形', 'zh-TW': '無假體鼻整形', en: 'Non-Implant Rhinoplasty' },
        concerns: {
          ko: ['인공 보형물에 대한 거부감이 있어요.', '수술 후 염증이나 이물감이 걱정되요.', '자연스러운 코성형을 원해요.'],
          en: ["I'm uncomfortable with artificial implants.", "I'm worried about inflammation or a foreign-body sensation after surgery.", 'I want a natural-looking rhinoplasty.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '무보형물 코성형은 인공보형물로 인한 이물감과 염증은최소화하고, 티 나지않는 자연스러운 코라인을 완성합니다.',
          'zh-CN': '无假体鼻整形可以将人工假体引起的异物感和炎症最小化，打造自然不突兀的鼻部线条。',
          en: 'Non-implant rhinoplasty minimizes the foreign-body sensation and inflammation caused by artificial implants, completing a natural-looking, undetectable nose line.',
          'zh-TW': '無假體鼻整形可以將人工假體引起的異物感和炎症最小化，打造自然不突兀的鼻部線條。',
        },
        label: { ko: '인공보형물 삽입 없이 매끄럽고 오똑하게', en: 'Smooth and Well-Defined, Without an Artificial Implant' },
        image: 'nose-non-implant-rhinoplasty.png',
      },
      {
        slug: 'bulbous-nose',
        name: { ko: '복 코 성형', 'zh-CN': '宽大鼻整形', 'zh-TW': '寬大鼻整形', en: 'Bulbous Nose Surgery' },
        concerns: { ko: [], en: [], 'zh-CN': [], 'zh-TW': [] },
        description: {
          ko: '넓고 뭉툭한 코끝을 개선해 갸름하고 생기 있는 인상을 만들어드립니다! 복코란 코끝과 콧볼이 넓고 둥근 코를 말하며, 동양인은 코끝 피부가 두껍고 납작해 복코 비율이 높은 편입니다. 이런 복코는 코끝이 납작해 전체적으로 투박하고 둔한 인상을 줍니다. 원래 코끝 모양에 맞춰 연골로 교정하고, 필요시 지방 제거를 병행해 갸름하고 오똑한 코를 만들어드립니다.',
          'zh-CN': '改善宽扁的鼻头，打造修长灵动的形象！福鼻是鼻头和鼻翼又宽又圆的鼻子，东方人鼻头皮肤厚且扁平所以比例较高，用软骨矫正鼻头，需要时配合去除脂肪。',
          en: 'We improve a wide, blunt nasal tip to create a slimmer, more vibrant impression! A "bulbous nose" refers to a nose with a wide, round tip and nostrils — East Asians tend to have thick, flat nasal tip skin, making this common. We correct the tip\'s shape using cartilage, combined with fat removal when needed, to create a slim, well-defined nose.',
          'zh-TW': '改善寬扁的鼻頭，打造修長靈動的形象！福鼻是鼻頭和鼻翼又寬又圓的鼻子，東方人鼻頭皮膚厚且扁平所以比例較高，用軟骨矯正鼻頭，需要時配合去除脂肪。',
        },
        image: 'nose-bulbous-nose.png',
      },
      {
        slug: 'tip-plasty',
        name: { ko: '코끝 성형', 'zh-CN': '鼻尖整形', 'zh-TW': '鼻尖整形', en: 'Tip Plasty' },
        concerns: { ko: [], en: [], 'zh-CN': [], 'zh-TW': [] },
        description: {
          ko: '코끝 수술은 자가 조직만 사용해 부작용 걱정 없이 자연스럽게 움직이는 코끝을 만들 수 있습니다! 콧대가 높아도 코끝이 둥글고 뭉툭하거나 낮으면 투박한 인상을 줍니다. 콧대 모양도 중요하지만 코끝 역시 코 전체 인상을 결정짓는 요소입니다. 코끝 성형은 기본적으로 자가 조직으로 수술을 진행해 자연스러운 결과와 낮은 부작용을 기대할 수 있으며, 개인의 얼굴 특징과 전체 비율을 고려해 콧대와 코끝 라인을 개선, 원래 코처럼 부드럽게 움직이는 코끝을 만들어드립니다.',
          'zh-CN': '鼻头手术时只使用自体组织不用担心副作用可以打造活动自然的鼻头！考虑个人面部特点和整体比例改善鼻梁和鼻头整体线条。',
          en: 'Tip plasty uses only your own tissue, giving you a natural, freely moving nasal tip with no worries about side effects! The tip is just as important as the bridge in determining the overall impression of the nose — we improve the line of the bridge and tip using your own tissue, considering your individual features and proportions.',
          'zh-TW': '鼻頭手術時只使用自體組織不用擔心副作用可以打造活動自然的鼻頭！考慮個人面部特點和整體比例改善鼻樑和鼻頭整體線條。',
        },
        image: 'nose-tip-plasty.png',
      },
      {
        slug: 'male-rhinoplasty',
        name: { ko: '남자 코성형', 'zh-CN': '男性鼻整形', 'zh-TW': '男性鼻整形', en: 'Male Rhinoplasty' },
        concerns: {
          ko: ['매부리코, 휜코가 고민이에요.', '남자다운 이미지로 개선을 원해요.', '코막힘과 비염증상이 심해요.'],
          en: ["I'm concerned about a hooked or crooked nose.", 'I want a more masculine image.', 'I have severe nasal congestion and rhinitis symptoms.'],
          'zh-CN': ['因鹰钩鼻、歪鼻而烦恼。', '希望改善为更有男人味的形象。', '鼻塞和鼻炎症状严重。'],
          'zh-TW': ['因鷹鉤鼻、歪鼻而煩惱。', '希望改善為更有男人味的形象。', '鼻塞和鼻炎症狀嚴重。'],
        },
        description: {
          ko: '단순히 코모양의 변화가 아닌 얼굴의 전체적인 이미지 변화를 만듭니다.',
          'zh-CN': '不仅仅是改变鼻型，而是带来整体脸部形象的变化。',
          en: 'This creates a change not just in the shape of the nose, but in the overall image of the face.',
          'zh-TW': '不僅僅是改變鼻型，而是帶來整體臉部形象的變化。',
        },
        label: { ko: '볼륨과 직선으로 살아나는 얼굴의 입체감', en: 'Facial Dimension Brought to Life with Volume and Straight Lines', 'zh-CN': '用轮廓感和直线条唤醒脸部立体感', 'zh-TW': '用輪廓感和直線條喚醒臉部立體感' },
        image: 'men-rhinoplasty.png',
        imageCategory: 'men',
      },
    ],
    otherItems: [],
  },
  {
    slug: 'ent',
    name: { ko: '이비인후과(코)', 'zh-CN': '耳鼻喉科（鼻部）', 'zh-TW': '耳鼻喉科（鼻部）', en: 'ENT (Nose)' },
    icon: 'Stethoscope',
    heroImages: ['ent-hero.jpg'],
    intro: {
      ko: 'WJ 원진성형외과에서는 개개인이 가지고 있는 기능적인 문제를 개선하기 위해,코의 해부학적 구조를 잘 알고 있는 숙련된 의료진이 직접집도하여 안전하고 만족스러운 결과를 자부합니다.',
      'zh-CN': 'WJ原辰整形外科为了改善个人的功能性鼻部问题，由熟悉鼻子解剖学构造的熟练医疗团队亲自主刀，保证安全，为给出满意的效果而全力以赴。',
      'zh-TW': 'WJ原辰整形外科為了改善個人的功能性鼻部問題，由熟悉鼻子解剖學構造的熟練醫療團隊親自主刀，保證安全，為給出滿意的效果而全力以赴。',
      en: 'At WJ WonJin Plastic Surgery, to improve each patient\'s individual functional issues, experienced medical staff who thoroughly understand the anatomical structure of the nose perform every procedure personally, ensuring safe and satisfying results.',
    },
    items: [
      {
        slug: 'deviated-septum',
        name: { ko: '비중격만곡증', 'zh-CN': '鼻中隔偏曲', 'zh-TW': '鼻中隔彎曲', en: 'Deviated Nasal Septum' },
        concerns: {
          ko: ['휜 코 때문에 비중격 만곡증이 생겼어요.', '코피가 자주 나고, 코가 약해요.', '편두통, 기억력 감퇴, 집중력 저하가 생겼어요.'],
          en: ['My crooked nose has caused a deviated septum.', 'I get frequent nosebleeds and my nose feels weak.', "I've developed migraines, memory decline, and reduced concentration."],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '경력 20년 이상의 이비인후과 전문의가상담, 검진, 수술까지 1:1 원스톱으로전담합니다.',
          'zh-CN': '有着二十余年经验的耳鼻喉科专业院长从术前咨询，检查到手术进行1:1责任制管理。',
          en: 'An ENT specialist with over 20 years of experience personally handles everything from consultation to surgery, in a one-on-one, one-stop process.',
          'zh-TW': '有著二十餘年經驗的耳鼻喉科專業院長從術前諮詢，檢查到手術進行1:1責任制管理。',
        },
        label: { ko: '보험이 적용되는 코질환', en: 'A Nasal Condition Covered by Insurance' },
        image: 'ent-deviated-septum.png',
      },
      {
        slug: 'nasal-valve-stenosis',
        name: { ko: '비밸브협착증', 'zh-CN': '鼻瓣区狭窄', 'zh-TW': '鼻瓣區狹窄', en: 'Nasal Valve Stenosis' },
        concerns: {
          ko: ['비염과 축농증 증상이 있어요.', '수면 시 코콜이가 심하고, 무호흡도 있어요.', '코막힘 때문에 편두통, 인후통이 있어요.'],
          en: ['I have symptoms of rhinitis and sinusitis.', 'I snore heavily during sleep and also have sleep apnea.', 'Nasal congestion causes me migraines and sore throats.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '협착된 비밸브를 수술적인 치료를 통해호흡을 편안하게 개선시켜줍니다.',
          'zh-CN': '通过手术治疗狭窄的鼻阀，改善呼吸不畅问题。',
          en: 'Surgical treatment of the narrowed nasal valve comfortably improves your breathing.',
          'zh-TW': '通過手術治療狹窄的鼻閥，改善呼吸不暢問題。',
        },
        label: { ko: '답답한 호흡을 편안하게', en: 'Turning Labored Breathing into Comfortable Breathing' },
        image: 'ent-nasal-valve-stenosis.png',
      },
      {
        slug: 'tonsillectomy',
        name: { ko: '편도선수술', 'zh-CN': '扁桃体手术', 'zh-TW': '扁桃腺手術', en: 'Tonsillectomy' },
        concerns: {
          ko: ['1년에 3~4차례 편도선염이 생겨요.', '목부위에 심한 통증이나 이물감이 느껴져요.', '중이염, 부비동염이 자주 재발해요.'],
          en: ['I get tonsillitis 3-4 times a year.', 'I feel severe pain or a foreign-body sensation in my throat.', 'I have frequent recurrences of otitis media and sinusitis.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '면역 기능과 밀접한 편도선, 원인에 맞는 확실한 치료가 중요합니다.',
          'zh-CN': '与免疫功能密切相关的扁桃体对症治疗是非常重要的。',
          en: "The tonsils are closely tied to immune function, so it's important to receive treatment tailored precisely to the cause.",
          'zh-TW': '與免疫功能密切相關的扁桃體對症治療是非常重要的。',
        },
        label: { ko: '안전하고 확실한', en: 'Safe and Certain' },
        image: 'ent-tonsillectomy.png',
      },
      {
        slug: 'rhinitis',
        name: { ko: '비염', 'zh-CN': '鼻炎', 'zh-TW': '鼻炎', en: 'Rhinitis' },
        concerns: {
          ko: ['재채기, 콧물, 코막힘이 심해서 힘들어요.', '꽃가루가 날리는 계절마다 코와 눈이 가려워요.', '감기에 걸리면 콧물이나 재채기가 오래가요.'],
          en: ['Severe sneezing, runny nose, and congestion make daily life difficult.', 'My nose and eyes itch every pollen season.', 'When I catch a cold, my runny nose and sneezing linger for a long time.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '콧물, 재채기, 코막힘 등 여러가지 증상을 동반하는 비염.정확한 원인을 찾아 맞춤 치료를 진행합니다.',
          'zh-CN': '伴有流鼻涕、打喷嚏、鼻塞等各种症状的鼻炎找出正确的原因，进行量身定制的治疗。',
          en: 'Rhinitis comes with a range of symptoms. We identify the exact cause and provide a treatment tailored to you.',
          'zh-TW': '伴有流鼻涕、打噴嚏、鼻塞等各種症狀的鼻炎找出正確的原因，進行量身定製的治療。',
        },
        label: { ko: '숨쉬기 조차 어려웠던 비염 해결', en: 'Resolving Rhinitis That Made Even Breathing Difficult' },
        image: 'ent-rhinitis.png',
      },
      {
        slug: 'sinusitis',
        name: { ko: '축농증(부비동염)', 'zh-CN': '鼻窦炎（副鼻窦炎）', 'zh-TW': '鼻竇炎（副鼻竇炎）', en: 'Sinusitis' },
        concerns: {
          ko: ['누런 콧물이 수시로 나와요.', '목뒤로 콧물이 넘어가는 느낌이 들어요.', '만성적인 코막힘이 심해요.'],
          en: ['I frequently have yellow nasal discharge.', 'I feel postnasal drip running down the back of my throat.', 'I have severe chronic nasal congestion.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '여러가지 증상을 동반하는 축농증. 숙련된이비인후과 전문의의 확실한 치료가 중요합니다.',
          'zh-CN': '伴有多种症状的鼻窦炎、关键就是有着经验丰富的耳鼻喉科专业院长的对症治疗。',
          en: "Sinusitis comes with a range of symptoms. It's important to receive definitive treatment from an experienced ENT specialist.",
          'zh-TW': '伴有多種症狀的鼻竇炎、關鍵就是有著經驗豐富的耳鼻喉科專業院長的對症治療。',
        },
        label: { ko: '답답했던 호흡을 편안하게', en: 'Turning Labored Breathing into Comfortable Breathing' },
        image: 'ent-sinusitis.png',
      },
    ],
    otherItems: [],
  },
  {
    slug: 'lifting',
    name: { ko: '리프팅', 'zh-CN': '提拉', 'zh-TW': '拉提', en: 'Lifting' },
    icon: 'TrendingUp',
    heroImages: ['lifting-hero.jpg'],
    intro: {
      ko: '세대별, 개인별로 차이가 있는 피부탄력도를 진단하고파악하는 것은 리프팅 시술의 기본입니다.자연스러우면서도 조화로운 동안을 위해, WJ 원진은 개인별로최적의 리프팅 수술법을 추천합니다.',
      'zh-CN': '诊断并掌握不同年龄段不同个人的皮肤弹性度是面部提升的基本。为打造自然和谐的童颜，WJ原辰针对个人推荐最佳的提升手术方案。通过WJ原辰的面部提升术一起来体验时光倒流般梦幻的瞬间吧。高弹性、组织损伤最小化、快速恢复以及长效维持。',
      'zh-TW': '診斷並掌握不同年齡段不同個人的皮膚彈性度是面部提升的基本。為打造自然和諧的童顏，WJ原辰針對個人推薦最佳的提升手術方案。通過WJ原辰的面部提升術一起來體驗時光倒流般夢幻的瞬間吧。高彈性、組織損傷最小化、快速恢復以及長效維持。',
      en: 'Diagnosing and understanding skin elasticity, which varies by generation and individual, is the foundation of any lifting procedure. For a natural yet harmonious youthful look, WJ WonJin recommends the optimal lifting method for each individual.',
    },
    items: [
      {
        slug: 'elastigum-lifting',
        name: { ko: '엘라스티꿈 리프팅', 'zh-CN': 'ElastiGum 提拉', 'zh-TW': 'ElastiGum 拉提', en: 'ElastiGum Lifting' },
        concerns: {
          ko: ['윤곽, 양악 수술을 했는데 볼 처짐이 생겼어요.', '목에 탄력이 떨어지고 주름도 생겼어요.', '볼살과 턱살이 두툼하고 피부가 늘어졌어요.'],
          en: ['After facial contouring/double jaw surgery, my cheeks started sagging.', 'My neck has lost elasticity and developed wrinkles.', 'My cheeks and jawline are thick and my skin has become loose.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '느슨해진 인대를 다시 형성하여 자연스러운 움직임과강한 리프팅 효과를 경험 할 수 있습니다.',
          'zh-CN': '使松弛的韧带重新恢复弹性，体验自然表情及强效提升效果。特殊弹力线，半永久性。',
          en: 'By reforming loosened ligaments, you can experience natural movement along with a strong lifting effect.',
          'zh-TW': '使鬆弛的韌帶重新恢復彈性，體驗自然表情及強效提升效果。特殊彈力線，半永久性。',
        },
        label: { ko: '최소절개로 안면거상의 효과를 누리는', en: 'Facelift-Level Results Through Minimal Incision' },
        image: 'lifting-elastigum-lifting.png',
      },
      {
        slug: 'facelift',
        name: { ko: '안면 거상', 'zh-CN': '面部提升术', 'zh-TW': '臉部拉皮手術', en: 'Facelift' },
        concerns: {
          ko: ['주름이 너무 깊게 패었어요.', '실 리프팅 시술 후 만족스럽지 못해요.', '한 번의 시술로 긴 효과를 보고 싶어요.'],
          en: ["My wrinkles have become too deeply set.", "I wasn't satisfied with the results of thread lifting.", 'I want a long-lasting effect from a single procedure.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '10년 전 모습으로 앞으로의 10년을, 세월을 거스르는 20년의 효과를 경험할 수 있습니다.',
          'zh-CN': '回到10年前的样貌同时维持年轻容颜，根据面部每个部位皮肤厚度、下垂程度，1:1量身制定拉皮方案。',
          en: 'Turn back the clock to how you looked 10 years ago, carrying that look forward for the next 10 years — a 20-year turnaround in effect.',
          'zh-TW': '回到10年前的樣貌同時維持年輕容顏，根據面部每個部位皮膚厚度、下垂程度，1:1量身制定拉皮方案。',
        },
        label: { ko: 'SMAS(근막)층과 유지인대의 동시교정', en: 'Simultaneous Correction of the SMAS (Fascia) Layer and Retaining Ligaments' },
        image: 'lifting-facelift.png',
      },
      {
        slug: 'forehead-lift',
        name: { ko: '이마 거상술', 'zh-CN': '额头提升术', 'zh-TW': '額頭拉提術', en: 'Forehead Lift' },
        concerns: {
          ko: ['이마 주름이 깊게 생겼어요.', '눈, 눈썹이 처져 우울해 보여요.', '눈가의 주름이 생겼어요.'],
          en: ['Deep wrinkles have formed on my forehead.', 'My eyes and eyebrows droop, giving a gloomy look.', "I've developed wrinkles around my eyes."],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '이마 처짐은 물론 처진 눈썹과 눈꺼풀까지 개선하여 밝은 인상으로 변화 시켜 드립니다.',
          'zh-CN': '改善额头下垂的同时一起改善下垂的眉毛和眼皮，发际线内侧切开小口通过内视镜进行安全手术。',
          en: 'This corrects not only forehead sagging but also drooping eyebrows and eyelids, transforming your look into a brighter impression.',
          'zh-TW': '改善額頭下垂的同時一起改善下垂的眉毛和眼皮，髮際線內側切開小口通過內視鏡進行安全手術。',
        },
        label: { ko: 'HD 내시경을 이용한', en: 'Using HD Endoscopy' },
        image: 'lifting-forehead-lift.png',
      },
      {
        slug: 'forehead-reduction',
        name: { ko: '이마 축소술', 'zh-CN': '额头缩小术', 'zh-TW': '額頭縮小術', en: 'Forehead Reduction' },
        concerns: {
          ko: ['이마가 넓어 얼굴이 길어 보여요.', "이마라인이 M자 모양이에요.", "이마라인이 'ㄷ'자로 각진 모양이에요."],
          en: ['My wide forehead makes my face look long.', 'My hairline has an "M" shape.', 'My hairline has an angular, box-like shape.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '넓은 이마를 전체적인 균형에 맞게 줄여 측면에서는 볼록한 이마곡선을 정면에서는 알맞은 비율로 세련된 이미지를 만들어 드립니다.',
          'zh-CN': '根据面部比例减小额头宽度，考虑毛发生长方向切开，剥离头皮向前拉伸切除多余皮肤，保护发根毛囊。特征：保留毛囊切开、最多可缩小3cm额头长度、专用固定钉牢牢固定。',
          en: 'We reduce a wide forehead to match your overall facial balance, creating a well-proportioned, refined look.',
          'zh-TW': '根據面部比例減小額頭寬度，考慮毛髮生長方向切開，剝離頭皮向前拉伸切除多餘皮膚，保護髮根毛囊。特徵：保留毛囊切開、最多可縮小3cm額頭長度、專用固定釘牢牢固定。',
        },
        label: { ko: '이마축소로 헤어라인 교정까지 동시에', en: 'Forehead Reduction with Hairline Correction, All at Once' },
        image: 'lifting-forehead-reduction.png',
      },
      {
        slug: 'mint-lifting',
        name: { ko: '민트 리프팅', 'zh-CN': 'Mint 提拉', 'zh-TW': 'Mint 拉提', en: 'Mint Lifting' },
        concerns: {
          ko: ['전체적인 탄력 개선을 원해요.', '팔자주름, 처진 볼살이 고민이에요.', '얼굴 비대칭을 개선하고 싶어요.'],
          en: ['I want to improve my overall skin elasticity.', "I'm concerned about nasolabial folds and sagging cheeks.", 'I want to correct facial asymmetry.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '민트 리프팅으로 주름 개선과 피부 탄력까지 한 번에! 시간을 돌린 듯 자연스러운 V라인을 완성합니다.',
          'zh-CN': '一次同时改善皱纹和肌肤弹力！最小沉浸式疗法，快速恢复，皮肤里的线有助于胶原蛋白生成。',
          en: 'Mint Lifting improves wrinkles and skin elasticity all at once! It completes a natural V-line look, as if turning back the clock.',
          'zh-TW': '一次同時改善皺紋和肌膚彈力！最小沉浸式療法，快速恢復，皮膚裡的線有助於膠原蛋白生成。',
        },
        label: { ko: '더 강력한 리프팅, 오래 유지되는 효과', en: 'Stronger Lifting, Longer-Lasting Results' },
        image: 'lifting-mint-lifting.png',
      },
      {
        slug: 'fat-grafting',
        name: { ko: '지방이식', 'zh-CN': '脂肪填充', 'zh-TW': '脂肪填補', en: 'Fat Grafting' },
        concerns: {
          ko: ['이마가 꺼져서 인상이 답답해 보여요.', '볼살이 빠져서 나이들어 보여요.', '팔자주름이 깊어 고민이 돼요.', '얼굴에 자연스러운 볼륨감을 원해요.'],
          en: ['My sunken forehead gives a dull impression.', 'My hollow cheeks make me look older.', 'My deep nasolabial folds are a concern.', 'I want natural-looking volume in my face.'],
          'zh-CN': ['短额头，宽额头', '眉骨突出的额头', '额头眉间细纹严重', '法令纹较深', '凸嘴', '鼻翼两侧凹陷', '脸颊凹陷', '面部细纹较多', '颧骨突出', '无下巴'],
          'zh-TW': ['短額頭，寬額頭', '眉骨突出的額頭', '額頭眉間細紋嚴重', '法令紋較深', '凸嘴', '鼻翼兩側凹陷', '臉頰凹陷', '面部細紋較多', '顴骨突出', '無下巴'],
        },
        description: {
          ko: '높은 생착률, 자연스럽게 차오르는 볼륨감, 안전까지 모두 만족시킵니다.',
          'zh-CN': '高存活率、自然饱满、安全。',
          en: 'It satisfies everything at once — a high fat-survival rate, naturally filling volume, and safety.',
          'zh-TW': '高存活率、自然飽滿、安全。',
        },
        label: { ko: '생착률 높은 지방이식', en: 'Fat Grafting with a High Survival Rate' },
        image: 'lifting-fat-grafting.png',
      },
    ],
    otherItems: [],
  },
  {
    slug: 'dermatology',
    name: { ko: '피부과', 'zh-CN': '皮肤科', 'zh-TW': '皮膚科', en: 'Dermatology' },
    icon: 'Sparkles',
    heroImages: ['dermatology-hero.jpg'],
    intro: {
      ko: '피부 고민은 단순한 미용이 아니라, 개인별 상태에 맞춘체계적인 관리가 필요합니다. WJ 원진은 최신 의료 장비와숙련된 의료진을 통해 색소, 모공, 여드름, 홍조 등 다양한피부 문제를 효과적으로 개선하며, 리프팅 시술을 통해피부 탄력을 높이고 건강한 피부로 가꿔드립니다.레이저 치료, 스킨부스터, 맞춤형 피부 재생 프로그램까지안전하고 정교한 치료로 근본적인 피부 개선을 제공합니다.',
      'zh-CN': '皮肤问题不仅是简单的美容需求，更需要根据个人状况进行系统性管理。WJ原辰通过最新医疗设备和资深医疗团队，有效改善色素、毛孔、痘痘、泛红等多种皮肤问题，并通过提升紧致疗程增强皮肤弹性，打造健康肌肤。从激光治疗、营养针剂套餐到定制化肌肤再生方案，我们以安全精细的治疗提供根本性的肌肤改善。',
      'zh-TW': '皮膚問題不僅是簡單的美容需求，更需要根據個人狀況進行系統性管理。WJ原辰通過最新醫療設備和資深醫療團隊，有效改善色素、毛孔、痘痘、泛紅等多種皮膚問題，並通過提升緊緻療程增強皮膚彈性，打造健康肌膚。從激光治療、營養針劑套餐到定制化肌膚再生方案，我們以安全精細的治療提供根本性的肌膚改善。',
      en: "Skin concerns aren't just a matter of cosmetics — they require systematic care tailored to each individual's condition. Using the latest medical equipment and experienced medical staff, WJ WonJin effectively treats pigmentation, enlarged pores, acne, and redness, and improves skin elasticity through lifting procedures. From laser treatments and skin boosters to customized skin regeneration programs, we provide safe, precise treatment for fundamental skin improvement.",
    },
    items: [
      {
        slug: 'ulthera-prime',
        name: { ko: '울쎄라피 프라임', 'zh-CN': 'Ulthera Prime', 'zh-TW': 'Ulthera Prime', en: 'Ulthera Prime' },
        concerns: {
          ko: ['이중턱이나 턱선 처짐이 고민이에요.', '얼굴 전체적인 V라인을 만들고 싶어요.', '처짐과 주름을 개선하고싶어요.'],
          en: ["I'm concerned about a double chin or sagging jawline.", 'I want an overall V-line facial shape.', 'I want to improve sagging and wrinkles.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '더 깊은 초음파, 더 정교한 타겟팅.',
          'zh-CN': '高强度聚焦超声波能量，60~70°C形成热凝固点（TCP）实现提升效果，不损伤皮肤表面。',
          en: 'Deeper ultrasound, more precise targeting.',
          'zh-TW': '高強度聚焦超聲波能量，60~70°C形成熱凝固點（TCP）實現提升效果，不損傷皮膚表面。',
        },
        label: { ko: '업그레이드 된 NEW 울쎄라', en: 'The Upgraded NEW Ulthera' },
        image: 'dermatology-ulthera-prime.png',
      },
      {
        slug: 'thermage-flx',
        name: { ko: '써마지 FLX', 'zh-CN': 'Thermage FLX', 'zh-TW': 'Thermage FLX', en: 'Thermage FLX' },
        concerns: {
          ko: ['피부 탄력·피부결을 개선하고 싶어요.', '깊어지는 팔자주름과 볼 처짐을 개선하고 싶어요.', '자연스러운 리프팅을 원해요.'],
          en: ['I want to improve skin elasticity and texture.', 'I want to improve deepening nasolabial folds and sagging cheeks.', 'I want a natural lifting effect.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '섬세하고 안전한 고주파로 차오르는 콜라겐.',
          'zh-CN': '第四代设备，射频能量传递至真皮和皮下组织层，促进胶原收缩与再生。',
          en: 'Collagen builds up with delicate, safe radiofrequency energy.',
          'zh-TW': '第四代設備，射頻能量傳遞至真皮和皮下組織層，促進膠原收縮與再生。',
        },
        label: { ko: '더 진화된 4세대 써마지', en: 'The More Advanced 4th-Generation Thermage' },
        image: 'dermatology-thermage-flx.png',
      },
      {
        slug: 'volnewmer',
        name: { ko: '볼뉴머', 'zh-CN': 'Volnewmer', 'zh-TW': 'Volnewmer', en: 'Volnewmer' },
        concerns: {
          ko: ['전체적으로 탄력이 떨어진 얼굴이 고민이에요.', '잔주름과 피부결 개선을 원해요.', '자연스러운 볼륨을 채우고 싶어요.'],
          en: ['My face has lost overall elasticity.', 'I want to improve fine lines and skin texture.', 'I want to fill in natural-looking volume.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '피부 속 조직까지 도달하는안전하고 강력한 고주파 에너지.',
          'zh-CN': '单极性高周波（6.48MHz）能量传递至皮肤层，诱导组织凝固反应，提供4种专用探头。',
          en: "Safe, powerful radiofrequency energy that reaches deep into the skin's tissue.",
          'zh-TW': '單極性高周波（6.48MHz）能量傳遞至皮膚層，誘導組織凝固反應，提供4種專用探頭。',
        },
        label: { ko: '피부 깊숙한 곳에서 차오르는 콜라겐', en: 'Collagen Building Up Deep Within the Skin' },
        image: 'dermatology-volnewmer.png',
      },
      {
        slug: 'laser-anti-aging',
        name: { ko: '레이저 안티에이징', 'zh-CN': '激光抗衰老', 'zh-TW': '雷射抗老化', en: 'Laser Anti-Aging' },
        concerns: {
          ko: ['피부 탄력이 저하되어 처짐이 고민이에요.', '자연스러운 리프팅 효과를 원해요.', '얼굴 라인을 정리하고 싶어요.'],
          en: ["My skin elasticity has declined and I'm concerned about sagging.", 'I want a natural lifting effect.', 'I want to define my facial lines.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '레이저 안티에이징은 피부 깊숙이 레이저를 조사해콜라겐 생성을 촉진하고 탄력을 개선하는비침습적 시술로, 주름 개선과 리프팅 효과를 제공합니다.',
          'zh-CN': '深层激光能量精准照射促进胶原新生，三重深度调节精准靶向治疗。',
          en: 'A non-invasive procedure that delivers laser energy deep into the skin to promote collagen production, improving wrinkles and elasticity.',
          'zh-TW': '深層激光能量精準照射促進膠原新生，三重深度調節精準靶向治療。',
        },
        label: { ko: '수술없이 되찾은 탄력과 V라인', en: 'Restoring Elasticity and a V-Line, Without Surgery' },
        image: 'dermatology-laser-anti-aging.png',
      },
      {
        slug: 'skin-booster',
        name: { ko: '스킨부스터', 'zh-CN': '水光针／Skin Booster', 'zh-TW': '水光針／Skin Booster', en: 'Skin Booster' },
        concerns: {
          ko: ['피부 속부터 수분을 채우고 싶어요.', '피부 톤과 탄력을 개선하고 싶어요.', '노화로 인해 생긴 잔주름을 개선하고 싶어요.', '빠른 회복과 피부 재생이 필요해요.'],
          en: ['I want to hydrate my skin from deep within.', 'I want to improve my skin tone and elasticity.', 'I want to improve fine lines caused by aging.', 'I need fast recovery and skin regeneration.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '스킨 부스터는 유효성분을 피부 속까지 전달해피부 재생, 미백, 보습, 콜라겐 형성, 주름 및탄력 개선에 효과가 있습니다.',
          'zh-CN': '注射方式将有效成分直达肌肤深层，促进再生、美白、保湿、胶原形成。',
          en: 'Skin Booster delivers active ingredients deep into the skin, supporting regeneration, brightening, hydration, collagen formation, and elasticity.',
          'zh-TW': '注射方式將有效成分直達肌膚深層，促進再生、美白、保濕、膠原形成。',
        },
        label: { ko: '피부 속 깊은 곳부터 차오르는 수분과 탄력', en: 'Hydration and Elasticity Building Up from Deep Within the Skin' },
        image: 'dermatology-skin-booster.png',
      },
      {
        slug: 'pigmentation-pores',
        name: { ko: '색소·모공', 'zh-CN': '色素・毛孔', 'zh-TW': '色素・毛孔', en: 'Pigmentation & Pores' },
        concerns: {
          ko: ['기미, 잡티, 주근깨를 개선하고 싶어요.', '넓어진 모공을 정리하고 싶어요.', '칙칙한 피부톤을 환하게 만들고 싶어요.'],
          en: ['I want to improve melasma, blemishes, and freckles.', 'I want to refine enlarged pores.', 'I want to brighten a dull skin tone.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '기미, 주근깨, 잡티, 모공 등 피부 고민 별맞춤 솔루션을 진행하여 피부 톤을 맑게 해주고모공으로 인한 피부의 빈틈을 매끄럽고 탄력 있게 채워줍니다.',
          'zh-CN': '针对黄褐斑、雀斑、色素沉淀及毛孔粗大提供个性化方案，三阶焕白。',
          en: 'A solution tailored to each skin concern — melasma, freckles, blemishes, pores — brightens the skin tone and smooths the texture.',
          'zh-TW': '針對黃褐斑、雀斑、色素沉澱及毛孔粗大提供個性化方案，三階煥白。',
        },
        label: { ko: '개인별 맞춤 치료로 피부를 맑고 매끈하게', en: 'Clear, Smooth Skin Through Personalized Treatment' },
        image: 'dermatology-pigmentation-pores.png',
      },
      {
        slug: 'acne-redness',
        name: { ko: '여드름·홍조', 'zh-CN': '痘痘・泛红', 'zh-TW': '痘痘・泛紅', en: 'Acne & Facial Redness' },
        concerns: {
          ko: ['성인 여드름이 자주 생겨요.', '붉은 홍조 피부를 진정시키고 싶어요.', '여드름 자국과 흉터가 고민이에요.'],
          en: ['I frequently get adult acne.', 'I want to calm red, flushed skin.', "I'm concerned about acne marks and scars."],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '여드름, 여드름 흉터, 그리고 얼굴이심하게 붉어지는 안면 홍조를 꼼꼼한 진단과맞춤형 치료로 개선하여, 울퉁불퉁하고 울긋불긋한피부를 깨끗하게 개선합니다.',
          'zh-CN': '细致诊断改善痘痘、痘印及面部泛红，血管扩张型疾病需及时治疗防止反复发炎。',
          en: 'Through thorough diagnosis and customized treatment, we improve acne, acne scars, and facial redness, clearing up uneven, blotchy skin.',
          'zh-TW': '細緻診斷改善痘痘、痘印及面部泛紅，血管擴張型疾病需及時治療防止反覆發炎。',
        },
        label: { ko: '원인부터 분석해 치료하는 여드름, 여드름 흉터, 홍조 개선', en: 'Treating Acne, Acne Scars, and Redness by Analyzing the Root Cause' },
        image: 'dermatology-acne-redness.png',
      },
    ],
    otherItems: [],
  },
  {
    slug: 'stemcell',
    name: { ko: '줄기세포', 'zh-CN': '干细胞', 'zh-TW': '幹細胞', en: 'Stem Cell' },
    icon: 'Dna',
    heroImages: ['stemcell-hero.png'],
    intro: {
      ko: '줄기세포는 소실된 세포를 재공급해주는 근원 에너지로서 손상되고 약해진 세포를 복원해 노화의 근본 원인을 해결하고 새로운 신생 세포를 재생시키는 최첨단 안티에이징 치료입니다.',
      'zh-CN': '干细胞是重新供应流失细胞的根源能量，修复受损脆弱的细胞，解决老化的根本原因，再生新生细胞的抗老治疗。逆转细胞生命力，重现年轻肌肤的高端手术。',
      'zh-TW': '幹細胞是重新供應流失細胞的根源能量，修復受損脆弱的細胞，解決老化的根本原因，再生新生細胞的抗老治療。逆轉細胞生命力，重現年輕肌膚的高端手術。',
      en: 'Stem cells are a fundamental source of energy that resupplies lost cells, restoring damaged and weakened cells to address the root cause of aging and regenerate new cells — a state-of-the-art anti-aging treatment.',
    },
    items: [
      {
        slug: 'injection',
        name: { ko: '줄기세포 주사', 'zh-CN': '干细胞注射', 'zh-TW': '幹細胞注射', en: 'Stem Cell Injection' },
        concerns: {
          ko: ['노화된 피부는 물론 전체적인 컨디션, 조직 기능, 면역력을 높이고 싶어요.'],
          en: ['I want to improve not only aged skin but also my overall condition, tissue function, and immunity.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '세포 면역력을 높이고 신체 밸런스를 맞춰 젊고 건강한 시기로 복원을 도와줍니다.',
          'zh-CN': '提高细胞免疫力，调节身体平衡，制造新生血管，增加血流速度。',
          en: "It boosts cellular immunity and restores your body's balance, helping to bring you back to a younger, healthier state.",
          'zh-TW': '提高細胞免疫力，調節身體平衡，製造新生血管，增加血流速度。',
        },
        label: { ko: '세포 재생으로 근본적 노화 원인 해결', en: 'Solving the Root Cause of Aging Through Cell Regeneration' },
        image: 'stemcell-injection.png',
      },
      {
        slug: 'fat-grafting',
        name: { ko: '줄기세포 지방이식', 'zh-CN': '干细胞脂肪填充', 'zh-TW': '幹細胞脂肪填補', en: 'Stem Cell Fat Grafting' },
        concerns: {
          ko: ['석회화나 뭉침 걱정 없이 볼륨감 있고 생착률 높은 지방이식을 하고 싶어요.'],
          en: ['I want fat grafting with good volume and a high survival rate, without worrying about calcification or lumping.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '높은 생착률, 탄력있는 피부, 생기 넘치는 입체감 모든 장점을 한 번에 누릴 수 있습니다.',
          'zh-CN': '使受损脂肪细胞再生，仅需1次即可获得充分效果，同时注入Active因子增加术后效果。',
          en: 'You can enjoy all the benefits at once — a high fat-survival rate, firm skin, and vibrant dimension.',
          'zh-TW': '使受損脂肪細胞再生，僅需1次即可獲得充分效果，同時注入Active因子增加術後效果。',
        },
        label: { ko: '세포에 볼륨과 젊음을 주입하는 진화된 기술', en: 'Advanced Technology That Infuses Cells with Volume and Youth' },
        image: 'stemcell-fat-grafting.png',
      },
      {
        slug: 'lifting',
        name: { ko: '줄기세포 리프팅', 'zh-CN': '干细胞提拉', 'zh-TW': '幹細胞拉提', en: 'Stem Cell Lifting' },
        concerns: {
          ko: ['흉터나 통증 없이 손상된 피부를 되돌려 탄력 있고 어려보이는 동안이 되고 싶어요.'],
          en: ['I want to restore damaged skin without scars or pain, to achieve firm, youthful-looking skin.'],
          'zh-CN': ['想要恢复无疤痕或疼痛受损的皮肤。'],
          'zh-TW': ['想要恢復無疤痕或疼痛受損的皮膚。'],
        },
        description: {
          ko: '피부 세포 재생으로 자연스럽게 어려지며 고급스러운 미모와 건강함을 겸유할 수 있습니다.',
          'zh-CN': '',
          en: "Through skin cell regeneration, your skin naturally looks younger, allowing you refined beauty and health together.",
          'zh-TW': '',
        },
        label: { ko: '피부 시간을 되감는 더블 시너지의 힘', en: "The Power of Double Synergy That Rewinds Your Skin's Clock" },
        image: 'stemcell-lifting.png',
      },
      {
        slug: 'hair-loss',
        name: { ko: '줄기세포 탈모개선', 'zh-CN': '干细胞脱发改善', 'zh-TW': '幹細胞落髮改善', en: 'Stem Cell Hair Loss Treatment' },
        concerns: {
          ko: ['번거로운 모발이식 대신 간편하면서 효과적으로 탈모를 치료하고 싶어요.'],
          en: ['I want a simple yet effective hair loss treatment instead of a cumbersome hair transplant.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '손상된 모낭세포를 건강한 모낭세포로 재생하여 근본적으로 탈모를 개선시켜 줍니다.',
          'zh-CN': '代替繁琐的毛发移植可以更加简便有效地治疗脱发问题。',
          en: 'It regenerates damaged hair follicle cells into healthy ones, fundamentally improving hair loss.',
          'zh-TW': '代替繁瑣的毛髮移植可以更加簡便有效地治療脫髮問題。',
        },
        label: { ko: '모낭세포를 새로 증식시키는 방법', en: 'A Method for Newly Proliferating Hair Follicle Cells' },
        image: 'stemcell-hair-loss.png',
      },
      {
        slug: 'mens-wellness',
        name: { ko: '줄기세포 남성활력', 'zh-CN': '干细胞男性活力', 'zh-TW': '幹細胞男性活力', en: "Stem Cell Men's Wellness" },
        concerns: {
          ko: ['스트레스와 노화로 인해 지쳐 약해진 활력을 되찾고 싶어요.'],
          en: ["I want to restore the vitality I've lost due to stress and aging."],
          'zh-CN': ['因压力和老化带来的疲惫缺少活力。'],
          'zh-TW': ['因壓力和老化帶來的疲憊缺少活力。'],
        },
        description: {
          ko: '성기능 저하의 원인이 되는 혈관과 신경을 재생시키고 남성호르몬을 증대해 활력을 되찾아 줍니다.',
          'zh-CN': '',
          en: 'It regenerates the blood vessels and nerves responsible for declining sexual function and increases male hormone levels.',
          'zh-TW': '',
        },
        label: { ko: '남성호르몬을 끌어올려 활력에너지 활성화', en: 'Boosting Male Hormones to Activate Vital Energy' },
        image: 'stemcell-mens-wellness.png',
      },
    ],
    otherItems: [],
  },
  {
    slug: 'breast',
    name: { ko: '가슴', 'zh-CN': '胸部', 'zh-TW': '胸部', en: 'Breast' },
    icon: 'Heart',
    heroImages: ['breast-hero.jpg'],
    intro: {
      ko: '당신은 충분히 아름다울 권리가 있고 또 아름다워질 수 있습니다. WJ 원진은 가슴에 대한 솔직한 이야기를 들어줌으로써여자의 마음을 이해하겠습니다. 20년 이상 대한민국 가슴성형의 정상을 지켜온 프리미엄가슴 성형 센터를 WJ 원진에서 만나보세요.',
      'zh-CN': '您有变美的权利，也可以变美丽。WJ原辰愿意和您共享关于胸部的小秘密，理解女人的心情。20多年来WJ原辰一直引领着韩国胸部整形的发展，我们相约在WJ原辰高端胸部整形中心邂逅。',
      'zh-TW': '您有變美的權利，也可以變美麗。WJ原辰願意和您共享關於胸部的小秘密，理解女人的心情。20多年來WJ原辰一直引領著韓國胸部整形的發展，我們相約在WJ原辰高端胸部整形中心邂逅。',
      en: "You have every right to be beautiful, and you can be. WJ WonJin will listen to your honest concerns about your breasts to truly understand a woman's heart. Meet a premium breast surgery center at WJ WonJin, one that has stood at the top of Korean breast surgery for over 20 years.",
    },
    items: [
      {
        slug: 'augmentation',
        name: { ko: '가슴 확대 성형', 'zh-CN': '隆胸手术', 'zh-TW': '隆乳手術', en: 'Breast Augmentation' },
        concerns: {
          ko: ['수술 후 빠른 일상 복귀를 원해요.', '구형 구축으로 고통받고 있어요.', '수술 부작용으로 가슴 재건 수술이 필요해요.', '촉감뿐만 아닌 모양과 움직임까지 확실하게 자연스러웠으면 좋겠어요.'],
          en: ['I want to return to my daily life quickly after surgery.', "I'm suffering from capsular contracture.", 'I need breast reconstruction due to complications.', 'I want the shape and movement, not just the feel, to be reliably natural.'],
          'zh-CN': ['快速回归日常生活', '包膜挛缩需修复', '胸部再建', '自然律动手感'],
          'zh-TW': ['快速回歸日常生活', '包膜攣縮需修復', '胸部再建', '自然律動手感'],
        },
        description: {
          ko: '품질과 만족도가 우수한 보형물과프리미엄 가슴 성형센터 WJ 원진이 만났습니다.',
          'zh-CN': '品质优秀，满意度高的假体与您邂逅在WJ原辰专业胸部整形中心。曼托圆形光面假体受用保障政策，手术后有破裂及3,4级的包膜挛缩时可以免费换假体。曼托圆形光面假体是经过长时间的临床试验拿到美国FDA认证的假体，表面非常光滑，材质柔软，无论在哪种姿势下模样及律动都非常自然，与真实胸部类似，是非常安全的假体。MOTIVA魔滴假体若在5年以内发生3,4级的包膜挛缩时可以支援修复手术，最高端的假体，根据重力原理硅胶随着身体律动而自然移动，线条均衡，手感柔软，弹性好，只需切开2.5cm~3cm即可植入。曼托XTRA（曼托高端记忆假体）：在相同的胸距下可以达到更加丰满的效果，胸围小的女性也可以拥有自然丰满的胸部曲线，可以根据个人胸围及大小要求来调节硅胶的量，打造丰满曲线的同时保证柔软的手感。',
          en: 'High-quality, highly satisfying implants meet the premium breast surgery center, WJ WonJin.',
          'zh-TW': '品質優秀，滿意度高的假體與您邂逅在WJ原辰專業胸部整形中心。曼托圓形光面假體受用保障政策，手術後有破裂及3,4級的包膜攣縮時可以免費換假體。曼托圓形光面假體是經過長時間的臨床試驗拿到美國FDA認證的假體，表面非常光滑，材質柔軟，無論在哪種姿勢下模樣及律動都非常自然，與真實胸部類似，是非常安全的假體。MOTIVA魔滴假體若在5年以內發生3,4級的包膜攣縮時可以支援修復手術，最高端的假體，根據重力原理硅膠隨著身體律動而自然移動，線條均衡，手感柔軟，彈性好，只需切開2.5cm~3cm即可植入。曼托XTRA（曼托高端記憶假體）：在相同的胸距下可以達到更加豐滿的效果，胸圍小的女性也可以擁有自然豐滿的胸部曲線，可以根據個人胸圍及大小要求來調節硅膠的量，打造豐滿曲線的同時保證柔軟的手感。',
        },
        label: { ko: '보형물 삽입술', en: 'Implant Insertion' },
        image: 'breast-augmentation.png',
      },
      {
        slug: 'hybrid-augmentation',
        name: { ko: '하이브리드 가슴성형', 'zh-CN': '混合式隆胸', 'zh-TW': '混合式隆乳', en: 'Hybrid Breast Augmentation' },
        concerns: {
          ko: ['자연스러운 가슴 촉감을 원해요.', '심하게 마른 체형을 가지고 있어요.', '가슴 보형물로 수술한 후 보형물 비침이 있어요.'],
          en: ['I want a natural breast feel.', 'I have a very thin body type.', 'My breast implants show visibly through the skin after surgery.'],
          'zh-CN': ['自然触感', '特别瘦小体型', '假体透光现象'],
          'zh-TW': ['自然觸感', '特別瘦小體型', '假體透光現象'],
        },
        description: {
          ko: '풍만한 볼륨과 자연스러운 가슴골, 부드러운 촉감으로 완성할 수 있습니다.',
          'zh-CN': '饱满的弧度，自然的乳沟，打造自然柔软触感。饱满的胸部，深邃的乳沟，自然的模样与触感。合适的胸部假体与脂肪填充一起进行，先进行假体植入后，用脂肪解决假体填充不了的部位，打造自然完美胸部。WJ原辰的Hybrid胸部整形通过1:1定制检查选择最适合您的假体，把不需要的脂肪抽取后填充，能同时拥有想要的形状，自然的触感及律动。',
          en: 'You can achieve full volume, a natural cleavage, and a soft feel.',
          'zh-TW': '飽滿的弧度，自然的乳溝，打造自然柔軟觸感。飽滿的胸部，深邃的乳溝，自然的模樣與觸感。合適的胸部假體與脂肪填充一起進行，先進行假體植入後，用脂肪解決假體填充不了的部位，打造自然完美胸部。WJ原辰的Hybrid胸部整形通過1:1定製檢查選擇最適合您的假體，把不需要的脂肪抽取後填充，能同時擁有想要的形狀，自然的觸感及律動。',
        },
        label: { ko: '가슴 보형물과 지방 이식 장점만을 담은', en: 'Combining Only the Advantages of Breast Implants and Fat Grafting' },
        image: 'breast-hybrid-augmentation.png',
      },
      {
        slug: 'reduction',
        name: { ko: '가슴 축소 성형', 'zh-CN': '缩胸手术', 'zh-TW': '縮乳手術', en: 'Breast Reduction' },
        concerns: {
          ko: ['가슴이 심하게 커 정신적인 스트레스가 심해요.', '큰 가슴 때문에 목과 허리 통증이 있어요.', '심하게 가슴이 처져 습진 등 피부 질환이 생겼어요.'],
          en: ['My breasts are severely large, causing significant psychological stress.', 'My large breasts cause neck and back pain.', 'Severe breast sagging has led to skin conditions like eczema.'],
          'zh-CN': ['精神压力', '颈腰疼痛', '湿疹等皮肤疾病'],
          'zh-TW': ['精神壓力', '頸腰疼痛', '濕疹等皮膚疾病'],
        },
        description: {
          ko: '안전한 유선조직 절제로 몸의 건강은 물론비율까지 생각합니다.',
          'zh-CN': '安全地切开乳腺组织，既考虑身体健康，同时考虑身材比例。切除多余皮肤的同时矫正组织，增加弹性。巨乳的标准按照每个人的身体比例来定，一般是单侧乳房体积在250-300cc属于理想型胸部，一般超过400cc属于巨乳，胸部上方到下方的距离超过20cm（E罩杯以上）的情况可以称为巨乳症。',
          en: 'Safe removal of glandular tissue considers not only your physical health but also your body proportions.',
          'zh-TW': '安全地切開乳腺組織，既考慮身體健康，同時考慮身材比例。切除多餘皮膚的同時矯正組織，增加彈性。巨乳的標準按照每個人的身體比例來定，一般是單側乳房體積在250-300cc屬於理想型胸部，一般超過400cc屬於巨乳，胸部上方到下方的距離超過20cm（E罩杯以上）的情況可以稱為巨乳症。',
        },
        label: { ko: "수직·유륜·'오'자형 절개법", en: 'Vertical, Periareolar, and "Anchor"-Shaped Incision Methods' },
        image: 'breast-reduction.png',
      },
      {
        slug: 'lift',
        name: { ko: '처진 가슴 교정', 'zh-CN': '乳房下垂矫正', 'zh-TW': '下垂乳房矯正', en: 'Breast Lift' },
        concerns: {
          ko: ['가슴 볼륨이 적으면서 처졌어요.', '가슴 볼륨이 크고 처졌어요.', '임신과 출산으로 인해 가슴이 처졌어요.', '급격한 다이어트로 인해 가슴의 탄력이 떨어졌어요.'],
          en: ['My breasts have low volume and are sagging.', 'My breasts are large in volume and sagging.', 'My breasts have sagged due to pregnancy and childbirth.', 'My breast elasticity has declined due to rapid weight loss.'],
          'zh-CN': ['扁平下垂', '大且下垂', '产后下垂', '减肥后弹力减少'],
          'zh-TW': ['扁平下垂', '大且下垂', '產後下垂', '減肥後彈力減少'],
        },
        description: {
          ko: '간단한 유선조직 교정으로 처진 탄력과볼륨을 올려줍니다.',
          'zh-CN': '通过简单的乳腺组织矫正，提高胸部弹性，增加饱满度，增加弹性，减少副作用。胸部下垂是根据乳头到胸部底线的距离来判断的，正常的乳头离胸部底线的距离是4~5cm，呈45度角，乳头严重靠下，需要进行WJ原辰下垂胸部矫正。',
          en: 'A simple correction of the glandular tissue restores lost elasticity and volume.',
          'zh-TW': '通過簡單的乳腺組織矯正，提高胸部彈性，增加飽滿度，增加彈性，減少副作用。胸部下垂是根據乳頭到胸部底線的距離來判斷的，正常的乳頭離胸部底線的距離是4~5cm，呈45度角，乳頭嚴重靠下，需要進行WJ原辰下垂胸部矯正。',
        },
        label: { ko: '처진 가슴 교정술', en: 'Breast Lift Surgery' },
        image: 'breast-lift.png',
      },
      {
        slug: 'revision',
        name: { ko: '가슴 재수술', 'zh-CN': '胸部修复手术', 'zh-TW': '胸部修復手術', en: 'Revision Breast Surgery' },
        concerns: {
          ko: ['수술 후 촉감이 딱딱해요.', '수술 후 크기와 모양이 마음에 안 들어요.', '보형물을 교체하고 싶어요.'],
          en: ['My breasts feel hard after surgery.', "I'm unhappy with the size and shape after surgery.", 'I want to replace my implants.'],
          'zh-CN': ['手感特别硬', '大小模样不满意', '想更换假体'],
          'zh-TW': ['手感特別硬', '大小模樣不滿意', '想更換假體'],
        },
        description: {
          ko: 'WJ 원진은 정밀 진단을 통해 철저한 재수술 원인을 분석하여 안전하고 정확하게 재수술을 진행합니다.',
          'zh-CN': 'WJ原辰通过精密诊断分析修复手术的原因后安全准确地进行修复手术。最后一次胸部手术，自然的触感及模样，满意的效果——"胸部修复手术不是单纯的更换假体"。建议第一次手术6个月~1年后进行修复手术，因为这段时期胸部组织基本恢复、模样定型，便于准确掌握问题原因；根据个体差异手术方法不同，需与院长充分咨询后确定时间。植入假体后身体会把假体当做异物，为保护身体使免疫细胞活性化，围着假体会形成保护膜（包膜）。充分掌握修复原因后去除变厚的包膜，充分剥离假体空间，换上与本人体型相匹配的假体，可获得柔软触感、深邃自然乳沟的最理想手术效果。',
          en: 'Through precise diagnosis, WJ WonJin thoroughly analyzes the cause requiring revision and carries out the procedure safely and accurately.',
          'zh-TW': 'WJ原辰通過精密診斷分析修復手術的原因後安全準確地進行修復手術。最後一次胸部手術，自然的觸感及模樣，滿意的效果——"胸部修復手術不是單純的更換假體"。建議第一次手術6個月~1年後進行修復手術，因為這段時期胸部組織基本恢復、模樣定型，便於準確掌握問題原因；根據個體差異手術方法不同，需與院長充分諮詢後確定時間。植入假體後身體會把假體當做異物，為保護身體使免疫細胞活性化，圍著假體會形成保護膜（包膜）。充分掌握修復原因後去除變厚的包膜，充分剝離假體空間，換上與本人體型相匹配的假體，可獲得柔軟觸感、深邃自然乳溝的最理想手術效果。',
        },
        label: { ko: '가슴 재수술은 더 신중하고 정확하게!', en: 'Revision Breast Surgery, Done More Carefully and Precisely!' },
        image: 'breast-revision.png',
      },
      {
        slug: 'nipple-surgery',
        name: { ko: '유두 성형', 'zh-CN': '乳头整形', 'zh-TW': '乳頭整形', en: 'Nipple Surgery' },
        concerns: { ko: [], en: [], 'zh-CN': [], 'zh-TW': [] },
        description: {
          ko: '완벽한 가슴을 만들어드립니다! 가슴은 볼륨만 풍만하다고 예쁜 것이 아니라, 가슴 크기와 어울리는 유두·유륜이 있어야 완벽한 가슴이 완성됩니다. 유두 지름은 약 1cm, 유륜 너비는 약 3.5~4.5cm가 가장 이상적입니다. 함몰 유두, 비대 유두를 개선하는 동시에 기능적·미용적 문제를 함께 해결해드립니다. (하위: 함몰유두·유두축소·유륜축소)',
          'zh-CN': '打造完美胸部！胸部的形态并不是饱满就好看，有与乳房大小相匹配的乳头及乳晕才是完美胸部。乳头的直径1cm左右，乳晕的宽度3.5~4.5cm左右是最合适的。改善凹陷乳头、肥大乳头，同时解决机能性、美观性问题。（下设：凹陷乳头矫正、乳头缩小、乳晕缩小）',
          en: "We create the perfect breast! A beautiful breast isn't just about ample volume — a nipple and areola that suit the breast's size are essential. The ideal nipple diameter is about 1 cm, and the ideal areola width is about 3.5-4.5 cm. We correct inverted or enlarged nipples while resolving functional and aesthetic concerns. (Subtypes: Inverted Nipple Correction, Nipple Reduction, Areola Reduction)",
          'zh-TW': '打造完美胸部！胸部的形態並不是飽滿就好看，有與乳房大小相匹配的乳頭及乳暈才是完美胸部。乳頭的直徑1cm左右，乳暈的寬度3.5~4.5cm左右是最合適的。改善凹陷乳頭、肥大乳頭，同時解決機能性、美觀性問題。（下設：凹陷乳頭矯正、乳頭縮小、乳暈縮小）',
        },
        image: 'breast-nipple-surgery.png',
      },
      {
        slug: 'gynecomastia',
        name: { ko: '여성형 유방(여유증)', 'zh-CN': '男性女乳症（男性乳房发育）', 'zh-TW': '男性女乳症（男性乳房發育）', en: 'Gynecomastia' },
        concerns: {
          ko: ['가슴에 몽우리가 만져져요.', '유난히 가슴이 발달되어 있어요.', '가슴 때문에 얇은 옷 입기가 꺼려져요.'],
          en: ['I can feel a lump in my chest.', 'My chest is unusually developed.', 'I avoid wearing thin clothing because of my chest.'],
          'zh-CN': ['瘦弱但胸部突出', '减肥后脂肪未减少', '摸到硬结', '女性化胸部突出'],
          'zh-TW': ['瘦弱但胸部突出', '減肥後脂肪未減少', '摸到硬結', '女性化胸部突出'],
        },
        description: {
          ko: '가리고 감춰야만 했던 여성형 가슴, WJ 원진에서 자신감 넘치는 남자다운 가슴으로 만들어 드립니다.',
          'zh-CN': '从像女性的胸部打造成男性的胸部！是指男性的乳房因为乳腺或脂肪发达像女性乳房似的突出的情况，出现此状况的原因可能是内分泌问题导致荷尔蒙分泌异常或者其他原因，不仅是美观上的问题，精神上也会有很多压力，建议尽快矫正。',
          en: "The feminized chest you've had to hide and cover up — WJ WonJin will transform it into a confident, masculine chest.",
          'zh-TW': '從像女性的胸部打造成男性的胸部！是指男性的乳房因為乳腺或脂肪發達像女性乳房似的突出的情況，出現此狀況的原因可能是內分泌問題導致荷爾蒙分泌異常或者其他原因，不僅是美觀上的問題，精神上也會有很多壓力，建議儘快矯正。',
        },
        label: { ko: '남자들의 말 못할 고민', en: "A Concern Men Don't Like to Talk About" },
        image: 'men-gynecomastia.png',
        imageCategory: 'men',
      },
    ],
    otherItems: [
      { slug: 'fat-grafting', name: { ko: '가슴 지방 이식', 'zh-CN': '胸部脂肪填充', 'zh-TW': '胸部脂肪填補', en: 'Breast Fat Grafting' } },
      { slug: 'postpartum-breast', name: { ko: '출산 후 가슴 성형', 'zh-CN': '产后胸部整形', 'zh-TW': '產後胸部整形', en: 'Postpartum Breast Surgery' } },
      { slug: 'stemcell-breast-augmentation', name: { ko: '줄기세포 가슴 성형', 'zh-CN': '干细胞隆胸', 'zh-TW': '幹細胞隆乳', en: 'Stem Cell Breast Augmentation' } },
    ],
  },
  {
    slug: 'contour',
    name: { ko: '윤곽·양악', 'zh-CN': '面部轮廓・双颌', 'zh-TW': '臉部輪廓・雙顎', en: 'Facial Contouring & Double Jaw' },
    icon: 'Scan',
    heroImages: ['contour-hero.jpg'],
    intro: {
      ko: '틀어진 얼굴뼈를 바로잡는 양악수술, 시선을 잡아끄는 자연스럽고아름다운 페이스라인을 디자인해야 하는 윤곽수술은 그 어떤 수술보다전문적이고 고도의 의술을 필요로 하는 수술입니다. WJ 원진에서는 완성도높은 양악, 윤곽 수술을 위해 전문의의 진료 연계 시스템을 제공합니다.고난이도 수술인 양악, 윤곽 수술. WJ 원진이라면 안심하셔도 됩니다.',
      'zh-CN': '通过双颚手术调整面部弯曲的骨骼问题，通过轮廓手术提高面部的流畅度，打造自然又精致的脸庞。骨骼类手术是比其他手术更需要拥有专业的手术经验和专业知识，WJ原辰为了达到最佳的手术效果，提供专业院长共同协诊系统。双颚、轮廓手术是高难度手术，选择WJ原辰可以使您放心。',
      'zh-TW': '通過雙顎手術調整面部彎曲的骨骼問題，通過輪廓手術提高面部的流暢度，打造自然又精緻的臉龐。骨骼類手術是比其他手術更需要擁有專業的手術經驗和專業知識，WJ原辰為了達到最佳的手術效果，提供專業院長共同協診系統。雙顎、輪廓手術是高難度手術，選擇WJ原辰可以使您放心。',
      en: 'Double jaw surgery corrects a misaligned facial skeleton, and facial contouring surgery designs a natural, beautiful facial line — both require more specialized, advanced surgical skill than almost any other procedure. WJ WonJin provides a coordinated specialist care system to ensure highly refined results. Double jaw and contouring surgery are highly demanding procedures — but with WJ WonJin, you can feel at ease.',
    },
    items: [
      {
        slug: 'long-face-surgery',
        name: { ko: '긴 얼굴 수술', 'zh-CN': '长脸手术', 'zh-TW': '長臉手術', en: 'Long Face Surgery' },
        concerns: {
          ko: ['웃을 때 치아와 잇몸이 보여요.', '얼굴이 길면서 돌출 입이에요.', '턱 끝만 길어요.'],
          en: ['My teeth and gums show when I smile.', 'My face is long and I have a protruding mouth.', 'Only my chin tip is long.'],
          'zh-CN': ['笑时牙龈外露', '脸长', '凸嘴下巴长'],
          'zh-TW': ['笑時牙齦外露', '臉長', '凸嘴下巴長'],
        },
        description: {
          ko: '턱의 길이를 줄임과 동시에 얼굴뼈에 관련된 모든문제를 해결하여 숨어있는 황금비율을 찾아드립니다.',
          'zh-CN': '缩短下巴长度同时解决关于面部骨骼方面相关的所有问题，找出黄金比率。横向纵向均衡，黄金比例，均衡又协调。1:1个人定制双颚手术系统——将手术前-手术中-手术后全过程数字化，通过3D模拟手术提前确认并研究手术结果，同时确立最适合患者手术计划的手术方法。进行诊断所需的3D CT拍摄→通过R2 GATE程序结合评估审美性和功能问题制定手术方案→利用3D技术制作符合患者骨骼的SAW GUIDE和FACE PLATE→使用1:1量身定制的FACE GUIDE（面部固定钉）进行精密安全的手术。',
          en: 'By shortening the length of the jaw while resolving every issue related to the facial bones, we uncover your hidden golden ratio.',
          'zh-TW': '縮短下巴長度同時解決關於面部骨骼方面相關的所有問題，找出黃金比率。橫向縱向均衡，黃金比例，均衡又協調。1:1個人定製雙顎手術系統——將手術前-手術中-手術後全過程數字化，通過3D模擬手術提前確認並研究手術結果，同時確立最適合患者手術計劃的手術方法。進行診斷所需的3D CT拍攝→通過R2 GATE程序結合評估審美性和功能問題制定手術方案→利用3D技術製作符合患者骨骼的SAW GUIDE和FACE PLATE→使用1:1量身定製的FACE GUIDE（面部固定釘）進行精密安全的手術。',
        },
        label: { ko: '턱 끝 수술 or 양악 수술', en: 'Chin Surgery or Double Jaw Surgery' },
        image: 'contour-long-face-surgery.png',
      },
      {
        slug: 'facial-asymmetry-correction',
        name: { ko: '안면 비대칭 교정', 'zh-CN': '面部不对称矫正', 'zh-TW': '臉部不對稱矯正', en: 'Facial Asymmetry Correction' },
        concerns: {
          ko: ['중심선은 정상이나 좌우 턱의 크기가 달라요.', '중심선은 정상이나 좌우 턱선의 윤곽만 달라요.', '중심선이 비뚤어지고 얼굴이 긴 주걱턱이에요.'],
          en: ['My midline is normal, but the size of my left and right jaw differs.', 'My midline is normal, but only the contour of my left and right jawline differs.', 'My midline is off-center and I have a long face with an underbite.'],
          'zh-CN': ['中心线正常但左右下巴大小不同', '下颌角轮廓不同', '中心线弯曲(地包天)'],
          'zh-TW': ['中心線正常但左右下巴大小不同', '下顎角輪廓不同', '中心線彎曲(地包天)'],
        },
        description: {
          ko: '얼굴의 중심을 잡아 크고 비대칭인 얼굴도작고 균형있는 얼굴로 완성해 드립니다.',
          'zh-CN': '找到面部中心，将大且不对称的脸变成小巧均衡的脸。为您塑造面部中心比例均衡、小巧精致的脸庞。1:1个人量身定制的双颚手术系统，通过3D模拟手术提前确认并审核手术结果，制定最适合患者的手术方式。',
          en: 'By aligning the center of the face, even a large, asymmetrical face can be transformed into a smaller, well-balanced one.',
          'zh-TW': '找到面部中心，將大且不對稱的臉變成小巧均衡的臉。為您塑造面部中心比例均衡、小巧精緻的臉龐。1:1個人量身定製的雙顎手術系統，通過3D模擬手術提前確認並審核手術結果，制定最適合患者的手術方式。',
        },
        label: { ko: '사각 턱 수술 or 턱 끝 수술 or 양악 수술', en: 'Jaw Angle Surgery, Chin Surgery, or Double Jaw Surgery' },
        image: 'contour-facial-asymmetry-correction.png',
      },
      {
        slug: 'protruding-mouth-correction',
        name: { ko: '돌출 입 교정', 'zh-CN': '凸嘴矫正', 'zh-TW': '凸嘴矯正', en: 'Protruding Mouth Correction' },
        concerns: {
          ko: ['치아만 돌출되었어요.', '입만 나오고 얼굴은 길지 않아요.', '돌출 입과 무턱이 함께 있어요.'],
          en: ['Only my teeth protrude.', 'Only my mouth protrudes, without a long face.', 'I have a protruding mouth along with a receding chin.'],
          'zh-CN': ['仅牙齿凸出', '仅嘴凸出脸不长', '凸嘴+下巴短小'],
          'zh-TW': ['僅牙齒凸出', '僅嘴凸出臉不長', '凸嘴+下巴短小'],
        },
        description: {
          ko: '치아와 골격교정으로 작고 갸름한 얼굴로완성해 드립니다.',
          'zh-CN': '通过牙齿和骨骼矫正，完成较小纤柔的面部轮廓，符合现代审美，改善牙齿咬合功能。东洋人的凸嘴现象中，比起牙齿凸出，牙槽骨本身凸出造成的凸嘴情况更多，这种情况仅通过牙齿矫正不能有效矫正，牙槽骨凸出需将牙槽骨向后移进行上下颌前部骨切开术（ASO），复合性的凸嘴则需要进行双颚手术。',
          en: 'Correcting both the teeth and the skeletal structure completes a smaller, slimmer face.',
          'zh-TW': '通過牙齒和骨骼矯正，完成較小纖柔的面部輪廓，符合現代審美，改善牙齒咬合功能。東洋人的凸嘴現象中，比起牙齒凸出，牙槽骨本身凸出造成的凸嘴情況更多，這種情況僅通過牙齒矯正不能有效矯正，牙槽骨凸出需將牙槽骨向後移進行上下顎前部骨切開術（ASO），複合性的凸嘴則需要進行雙顎手術。',
        },
        label: { ko: '전방분절 절골술 or 양악 수술', en: 'Anterior Segmental Osteotomy or Double Jaw Surgery' },
        image: 'contour-protruding-mouth-correction.png',
      },
      {
        slug: 'comprehensive-facial-contouring',
        name: { ko: '복합 안면 윤곽', 'zh-CN': '综合面部轮廓手术', 'zh-TW': '複合式臉部輪廓手術', en: 'Comprehensive Facial Contouring' },
        concerns: {
          ko: ['턱과 광대가 모두 돌출되어있어요.', '정면에서 봤을 때 얼굴이 크고 각져 보여요.', '뼈가 두꺼워 턱이 넓어 보여요.', '전체적으로 얼굴의 조화가 잘 어우러지지 않아요.'],
          en: ['Both my jaw and cheekbones protrude.', 'My face looks large and angular from the front.', 'My bones are thick, making my jaw look wide.', 'My face lacks overall harmony.'],
          'zh-CN': ['下颌角和颧骨都凸出', '脸大棱角分明', '下颌部分较宽'],
          'zh-TW': ['下顎角和顴骨都凸出', '臉大稜角分明', '下顎部分較寬'],
        },
        description: {
          ko: '개인의 이목구비와 이미지 등 여러 여건을 고려해내 얼굴에 맞는 에그라인을 찾아드립니다.',
          'zh-CN': '根据个人的五官及形象等多种要素，找出适合的完美脸型。小脸效果，有立体感的面部，小而纤长的下颌线条，童颜效果。',
          en: 'Considering various factors such as your individual features and image, we find the egg-shaped facial line that suits you best.',
          'zh-TW': '根據個人的五官及形象等多種要素，找出適合的完美臉型。小臉效果，有立體感的面部，小而纖長的下顎線條，童顏效果。',
        },
        label: { ko: 'U라인·V라인 복합 안면 윤곽', en: 'Combined U-Line/V-Line Facial Contouring' },
        image: 'contour-comprehensive-facial-contouring.png',
      },
      {
        slug: 'jaw-angle-surgery',
        name: { ko: '사각 턱 수술', 'zh-CN': '下颌角手术', 'zh-TW': '下顎角手術', en: 'Jaw Angle Surgery' },
        concerns: {
          ko: ['얼굴을 정면에서 봤을 때 넓은 사각형처럼 보여요.', '턱 끝이 뭉툭해서 턱과 볼이 구별되지 않아요.', '옆에서 봤을 때 귀밑의 턱 라인이 심하게 각져 보여요.', '인상이 강하다는 얘기를 많이 들어요.'],
          en: ['My face looks like a wide square from the front.', 'My jaw tip is blunt, making it hard to distinguish my jaw from my cheeks.', 'My jawline below the ears looks severely angular from the side.', "I'm often told I look intimidating."],
          'zh-CN': ['正面看方形', '下巴宽脸颊无区别', '侧面下颌角明显', '印象强势'],
          'zh-TW': ['正面看方形', '下巴寬臉頰無區別', '側面下顎角明顯', '印象強勢'],
        },
        description: {
          ko: '무조건 턱의 각을 없애는 것이 아니라얼굴과 조화로운 V라인을 살리는 것이 포인트입니다.',
          'zh-CN': '不是单纯的去掉下颌角，而是按照面部比例达到最佳的V脸效果，考虑面部均衡的自然V脸，和谐的面部线条，自信感上升。①避开神经线位置进行下巴T型截骨→取出下巴中间骨头→截断下巴尖聚拢后行下颌角切除术，去除凸出侧面骨头→固定钉固定 ②截断上端及中间骨头取出→截断下巴尖聚拢后行下颌角切除术→固定钉固定 ③截断骨骼移动到面部中央→下颌角切除术→固定钉固定',
          en: "The key isn't simply eliminating the angle of the jaw, but bringing out a V-line that's in harmony with your face.",
          'zh-TW': '不是單純的去掉下顎角，而是按照面部比例達到最佳的V臉效果，考慮面部均衡的自然V臉，和諧的面部線條，自信感上升。①避開神經線位置進行下巴T型截骨→取出下巴中間骨頭→截斷下巴尖聚攏後行下顎角切除術，去除凸出側面骨頭→固定釘固定 ②截斷上端及中間骨頭取出→截斷下巴尖聚攏後行下顎角切除術→固定釘固定 ③截斷骨骼移動到面部中央→下顎角切除術→固定釘固定',
        },
        label: { ko: '피질골 절제술, T 절제술', en: 'Cortical Bone Ostectomy, T-Osteotomy' },
        image: 'contour-jaw-angle-surgery.png',
      },
      {
        slug: 'zygoma-reduction',
        name: { ko: '광대뼈 축소술', 'zh-CN': '颧骨缩小术', 'zh-TW': '顴骨縮小術', en: 'Zygoma Reduction' },
        concerns: {
          ko: ['광대뼈가 튀어나와 인상이 강해 보여요.', '옆 광대가 발달하여 정면에서 보면 얼굴이 울퉁불퉁해요.', '앞 광대와 옆 광대가 모두 발달하여 얼굴 좌우 폭이 넓어요.', '광대뼈가 좌우 비대칭이에요.'],
          en: ['My cheekbones protrude, giving an intimidating look.', 'My side cheekbones are prominent, making my face look uneven from the front.', 'Both my front and side cheekbones are prominent, widening my face.', 'My cheekbones are asymmetrical.'],
          'zh-CN': ['侧面颧骨发达显凶', '正面凹凸不平', '前后颧骨都发达', '左右不对称'],
          'zh-TW': ['側面顴骨發達顯凶', '正面凹凸不平', '前後顴骨都發達', '左右不對稱'],
        },
        description: {
          ko: '옆 광대의 크기를 줄이고 앞 광대의 볼륨은 살려, 작고 갸름한 얼굴을 기대할 수 있습니다.',
          'zh-CN': '保持正面颧骨饱满度的同时把侧面颧骨内推使面部变小。①3D颧骨缩小术——把截断的骨头截短后旋转，保持正面颧骨饱满度的同时把侧面颧骨往里推，得到立体小巧的脸型 ②I QUICK颧骨缩小术（第四代）——使用3D HD内视镜以最小切口进行手术，将颧骨以L字型截骨后向内推入使其紧密粘连，无需固定 ③颧骨假体——增加前颧骨部分的饱满感 ④自体脂肪填充/玻尿酸——增加饱满度',
          en: 'By reducing the side cheekbones while preserving front volume, you can expect a smaller, slimmer face.',
          'zh-TW': '保持正面顴骨飽滿度的同時把側面顴骨內推使面部變小。①3D顴骨縮小術——把截斷的骨頭截短後旋轉，保持正面顴骨飽滿度的同時把側面顴骨往裡推，得到立體小巧的臉型 ②I QUICK顴骨縮小術（第四代）——使用3D HD內視鏡以最小切口進行手術，將顴骨以L字型截骨後向內推入使其緊密粘連，無需固定 ③顴骨假體——增加前顴骨部分的飽滿感 ④自體脂肪填充/玻尿酸——增加飽滿感',
        },
        label: { ko: '3D 광대 축소술, 아이퀵 광대 축소술, 앞 광대 보형물', en: '3D Zygoma Reduction, iQuick Zygoma Reduction, Front Cheekbone Implant' },
        image: 'contour-zygoma-reduction.png',
      },
      {
        slug: 'chin-surgery',
        name: { ko: '턱 끝 수술', 'zh-CN': '下巴整形', 'zh-TW': '下巴整形', en: 'Chin Surgery' },
        concerns: {
          ko: ['교합은 정상이나 턱 끝이 길고 앞으로 나왔어요.', '교합은 정상이나 턱뼈 발육 저하로 턱끝이 왜소해요.', '턱 끝의 길이가 길거나 짧아요.', '턱 끝이 넓고 뭉툭해요.'],
          en: ['My bite is normal, but my chin tip is long and protrudes forward.', 'My bite is normal, but underdeveloped jaw bone makes my chin tip small.', 'My chin tip is too long or too short.', 'My chin tip is wide and blunt.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '살짝만 채워도 몰라보게 달라져요. 양악 수술 필요 없는 뚜렷한 변화, WJ 원진의 턱 끝 수술.',
          'zh-CN': '稍微填充一点就会变得不一样，无需双颚手术的明显变化，WJ原辰的下巴手术。修长的小V脸，精巧脸型，形成立体感，精致的形象。下巴手术是从下巴截骨的方法到简单的假体或脂肪移植的手术方法等，可根据情况适用多种方法，存在咬合不正、严重的下巴后缩的情况需要进行双颚手术。',
          en: 'Even a small addition makes a dramatic difference — a clear transformation without needing double jaw surgery.',
          'zh-TW': '稍微填充一點就會變得不一樣，無需雙顎手術的明顯變化，WJ原辰的下巴手術。修長的小V臉，精巧臉型，形成立體感，精緻的形象。下巴手術是從下巴截骨的方法到簡單的假體或脂肪移植的手術方法等，可根據情況適用多種方法，存在咬合不正、嚴重的下巴後縮的情況需要進行雙顎手術。',
        },
        label: { ko: '보형물·지방이식, 턱뼈 절골', en: 'Implant/Fat Grafting, Chin Bone Osteotomy' },
        image: 'contour-chin-surgery.png',
      },
      {
        slug: 'facial-contouring-reconstruction',
        name: { ko: '윤곽재건복원술', 'zh-CN': '面部轮廓重建修复术', 'zh-TW': '臉部輪廓重建修復術', en: 'Facial Contouring Reconstruction' },
        concerns: {
          ko: ['안면윤곽 수술 후 얼굴 비대칭이 생겼어요.', '안면윤곽 과절제 수술로 개턱증상이 생겼어요.', '안면윤곽 수술 후 이차각이 생겼어요.', '안면윤곽,양악 수술 결과가 불만족스러워요.'],
          en: ['I developed facial asymmetry after facial contouring surgery.', 'Over-resection during facial contouring gave me a "witch chin."', 'A secondary angle developed after surgery.', "I'm unsatisfied with the results of my facial contouring/double jaw surgery."],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '윤곽수술로 인한 불만족, 불편함을 턱의 형태와 기능을 고려하여 원하는 모양으로 재수술 및 복원이 가능합니다.',
          'zh-CN': '因轮廓手术引起的不满意、不便之处，同时顾虑到骨头的功能性和形态，以想要的形态进行修复手术。面部轮廓需要重建的情况：[下颌角] 因过度V型而变尖的下巴/下颌角手术之后出现二次角/因过度切除导致下颌线条凹陷。[颧骨] 颧骨拱形部分不贴合/因过度切除凹陷/手术后颧骨脱落。[下巴] 下巴手术后出现骨头凹陷、变形/植入的假体有副作用/下巴短且向后凹陷。为了这次是最后的手术，我们将承诺进行正确的诊断和安全的手术。',
          en: 'Dissatisfaction caused by contouring surgery can be corrected through revision and restoration to achieve your desired look.',
          'zh-TW': '因輪廓手術引起的不滿意、不便之處，同時顧慮到骨頭的功能性和形態，以想要的形態進行修復手術。面部輪廓需要重建的情況：[下顎角] 因過度V型而變尖的下巴/下顎角手術之後出現二次角/因過度切除導致下顎線條凹陷。[顴骨] 顴骨拱形部分不貼合/因過度切除凹陷/手術後顴骨脫落。[下巴] 下巴手術後出現骨頭凹陷、變形/植入的假體有副作用/下巴短且向後凹陷。為了這次是最後的手術，我們將承諾進行正確的診斷和安全的手術。',
        },
        label: { ko: '정확하고 안전한 맞춤형 재건복원술', en: 'Precise, Safe, Customized Reconstruction and Restoration' },
        image: 'contour-facial-contouring-reconstruction.png',
      },
    ],
    otherItems: [
      { slug: 'self-designed-double-jaw', name: { ko: '셀프 양악수술', 'zh-CN': '自主双颌手术', 'zh-TW': '自主雙顎手術', en: 'Self-Designed Double Jaw Surgery' } },
      { slug: 'underbite-surgery', name: { ko: '주걱턱 수술', 'zh-CN': '地包天手术', 'zh-TW': '戽斗手術', en: 'Underbite Surgery' } },
      { slug: 'receding-chin', name: { ko: '무턱(하악 왜소증) 수술', 'zh-CN': '下巴后缩（下颌发育不足）手术', 'zh-TW': '下巴後縮（下顎發育不足）手術', en: 'Receding Chin (Mandibular Hypoplasia) Surgery' } },
      { slug: 'facial-contouring-revision', name: { ko: '안면윤곽 재수술', 'zh-CN': '面部轮廓修复手术', 'zh-TW': '臉部輪廓修復手術', en: 'Revision Facial Contouring Surgery' } },
      { slug: 'double-jaw-revision', name: { ko: '양악 재수술', 'zh-CN': '双颌修复手术', 'zh-TW': '雙顎修復手術', en: 'Revision Double Jaw Surgery' } },
      { slug: 'postoperative-orthodontics', name: { ko: '수술 후 교정', 'zh-CN': '术后牙齿矫正', 'zh-TW': '術後牙齒矯正', en: 'Postoperative Orthodontic Treatment' } },
    ],
  },
  {
    slug: 'bodyline',
    name: { ko: '체형', 'zh-CN': '体型', 'zh-TW': '體型', en: 'Body Contouring' },
    icon: 'PersonStanding',
    heroImages: ['bodyline-hero.jpg'],
    intro: {
      ko: 'WJ 원진의 체형 성형은 단순히 지방을 제거하는 것이 아닌, 고객의 니즈 및 개인별 갖고 있는 체형 밸런스를 고려하여 가장 이상적인 바디라인을 완성합니다.',
      'zh-CN': '瘦身整形，打造吸睛的理想身材曲线——WJ原辰的瘦身整形不是单纯的去除脂肪，而是根据顾客的需求和个人的体型比例打造最理想的身材曲线。',
      'zh-TW': '瘦身整形，打造吸睛的理想身材曲線——WJ原辰的瘦身整形不是單純的去除脂肪，而是根據顧客的需求和個人的體型比例打造最理想的身材曲線。',
      en: "WJ WonJin's body contouring surgery isn't just about removing fat — it considers each client's needs and their individual body balance to complete the most ideal body line.",
    },
    items: [
      {
        slug: 'liposuction',
        name: { ko: '지방 흡입', 'zh-CN': '吸脂', 'zh-TW': '抽脂', en: 'Liposuction' },
        concerns: {
          ko: ['몸 전체에 지방량이 많아요.', '특정 부위에 군살이 많아요.', '단기간 내에 체형교정을 원해요.'],
          en: ['I have a high overall body fat level.', 'I have excess fat in specific areas.', 'I want to reshape my body in a short period of time.'],
          'zh-CN': ['脂肪量多', '局部赘肉多', '短期矫正体型'],
          'zh-TW': ['脂肪量多', '局部贅肉多', '短期矯正體型'],
        },
        description: {
          ko: '단순히 사이즈 감소가 아닌 개인의 체형을 고려한 균형 잡힌 바디라인을 완성합니다.',
          'zh-CN': '不是单纯地缩小尺寸，而是根据每个人的体型打造匀称的身材曲线。WJ原辰的吸脂从术前到术后都有各领域的专业医生团队管理和监督，通过精密的脂肪细胞分离破坏术，最小化组织损伤。（부위별: 手臂吸脂——选择性去除保留 / 腹部抽脂——保持无下垂同时打造平滑线条 / 大腿抽脂——最大限度去除深层脂肪打造无凹凸线条）',
          en: 'Rather than simply reducing size, this completes a balanced body line that considers your individual body type.',
          'zh-TW': '不是單純地縮小尺寸，而是根據每個人的體型打造勻稱的身材曲線。WJ原辰的吸脂從術前到術後都有各領域的專業醫生團隊管理和監督，通過精密的脂肪細胞分離破壞術，最小化組織損傷。（부위별: 手臂吸脂——選擇性去除保留 / 腹部抽脂——保持無下垂同時打造平滑線條 / 大腿抽脂——最大限度去除深層脂肪打造無凹凸線條）',
        },
        label: { ko: '성형외과 전문의 집도, 지방·체형 성형', en: 'Performed by a Plastic Surgery Specialist — Fat and Body Contouring' },
        image: 'bodyline-liposuction.png',
      },
      {
        slug: 'abdominoplasty',
        name: { ko: '복부 성형술', 'zh-CN': '腹部整形术', 'zh-TW': '腹部整形術', en: 'Abdominoplasty' },
        concerns: {
          ko: ['출산 후 복부가 처졌어요.', '튼살이 심하고 살이 늘어났어요.', '지방흡입 후 복부 모양이 울퉁불퉁해요.'],
          en: ['My abdomen sags after childbirth.', 'I have severe stretch marks and loose skin.', 'My abdominal shape is uneven after liposuction.'],
          'zh-CN': ['产后下垂', '结实但赘肉下垂', '抽脂后凹凸不平'],
          'zh-TW': ['產後下垂', '結實但贅肉下垂', '抽脂後凹凸不平'],
        },
        description: {
          ko: '잦은 체중 변화나 출산, 노화현상으로 인해 늘어지고 처진 살은 지방흡입만으로는 개선이 어렵습니다. 이미 탄력을 잃고 복부 피부가 늘어져 있는 경우, 늘어진 피부를 잘라내고 벌어진 복직근을 모아줌으로 탄력 있고 매끈한 라인으로 개선할 수 있습니다.',
          'zh-CN': '不是单纯地抽脂，让下垂的腹部有弹力。经常的体重变化或妊娠、衰老引起的松弛和下垂仅通过抽脂是较难改善的，对于腹部皮肤失去弹力且下垂的情况，通过去除下垂的皮肤并聚拢分离的腹直肌，打造有弹力且平滑的曲线。',
          en: 'Loose, sagging skin from weight changes, childbirth, or aging is difficult to improve with liposuction alone — cutting away excess skin and bringing together the abdominal muscles creates a firm, smooth line.',
          'zh-TW': '不是單純地抽脂，讓下垂的腹部有彈力。經常的體重變化或妊娠、衰老引起的鬆弛和下垂僅通過抽脂是較難改善的，對於腹部皮膚失去彈力且下垂的情況，通過去除下垂的皮膚並聚攏分離的腹直肌，打造有彈力且平滑的曲線。',
        },
        label: { ko: '단순 지방흡입이 아닌, 처지고 늘어진 복부를 탄력 있게', en: 'More Than Just Liposuction — Firming a Sagging, Loose Abdomen' },
        image: 'bodyline-abdominoplasty.png',
      },
      {
        slug: 'hip-augmentation',
        name: { ko: '힙업 성형', 'zh-CN': '翘臀整形', 'zh-TW': '提臀整形', en: 'Hip Augmentation' },
        concerns: {
          ko: ['힙이 처지고 볼륨이 없어요.', '옷을 입었을 때 볼륨감 있게 보이고 싶어요.', '힙이 비대칭이에요.', '힙이 처지고 납작해서 다리가 짧아 보여요.', '골반이 작아요.', '다이어트·운동·요가로 힙업에 실패했어요.', '사고나 질병으로 힙 조직 일부가 제거됐어요.'],
          en: ['My hips sag and lack volume.', 'I want to look more voluminous in my clothes.', 'My hips are asymmetrical.', 'My hips sag and are flat, making my legs look short.', 'My pelvis is small.', "Diet, exercise, and yoga haven't given me a hip lift.", 'Some hip tissue was removed due to an accident or illness.'],
          'zh-CN': ['下垂无丰满感', '想穿衣显丰满', '不对称', '下垂扁平显腿短', '骨盆小', '减肥健身失败', '事故疾病导致组织切除'],
          'zh-TW': ['下垂無豐滿感', '想穿衣顯豐滿', '不對稱', '下垂扁平顯腿短', '骨盆小', '減肥健身失敗', '事故疾病導致組織切除'],
        },
        description: {
          ko: '탄력 있고 볼륨감 있는 힙은 가슴과 더불어 여성스러운 S라인을 완성하는 데 빼놓을 수 없는 포인트입니다. 힙이 처지고 볼륨이 없으면 옷맵시가 살지 않고 다리도 짧아 보이므로, 힙을 위로 끌어올려 매력적인 힙 라인을 만들어드립니다. (세부 기법: 하비스트젯 자가지방이식/보형물 삽입/복합)',
          'zh-CN': '胸部再加上有弹力和饱满感的臀部是打造女性S线条必不可缺的亮点，如果臀部下垂没有丰满感，穿衣服体态不会好看而且看起来腿短，所以将臀部向上提起打造魅力的臀部线条。（세부 기법: Harvest-jet脂肪移植——自体脂肪提臀 / 假体植入——永久性提臀 / 多功能——假体+脂肪移植）',
          en: "Along with the chest, firm, voluminous hips are essential for a feminine S-line figure. We lift the hips upward to create an attractive hip line. (Specific techniques: Harvest Jet Autologous Fat Grafting / Implant Insertion / Combined Approach)",
          'zh-TW': '胸部再加上有彈力和飽滿感的臀部是打造女性S線條必不可缺的亮點，如果臀部下垂沒有豐滿感，穿衣服體態不會好看而且看起來腿短，所以將臀部向上提起打造魅力的臀部線條。（세부 기법: Harvest-jet脂肪移植——自體脂肪提臀 / 假體植入——永久性提臀 / 多功能——假體+脂肪移植）',
        },
        image: 'bodyline-hip-augmentation.png',
      },
    ],
    otherItems: [],
  },
  {
    slug: 'men',
    name: { ko: '남자', 'zh-CN': '男性', 'zh-TW': '男性', en: 'Men' },
    icon: 'UserRound',
    heroImages: ['men-hero.jpg'],
    intro: {
      ko: 'WJ 원진의 남자성형은 여성과는 다른 개개인에 골격과 피부 특성 등을 체계적으로 분석하고 남성만을 위한 디자인과 수술 방법을 계획하여 만족 그 이상의 자신감을 찾아드립니다.',
      'zh-CN': 'WJ原辰的男士整形与女性不同，系统分析每位客人独特的骨骼和皮肤特性，为男性量身规划专属的设计和手术方法，帮您找回超越满足感的自信。',
      'zh-TW': 'WJ原辰的男士整形與女性不同，系統分析每位客人獨特的骨骼和皮膚特性，為男性量身規劃專屬的設計和手術方法，幫您找回超越滿足感的自信。',
      en: "WJ WonJin's surgery for men systematically analyzes each individual's bone structure and skin characteristics — which differ from women's — and plans designs and surgical methods made specifically for men.",
    },
    items: [
      {
        slug: 'eye-surgery',
        name: { ko: '남자 눈성형', 'zh-CN': '男性眼部整形', 'zh-TW': '男性眼部整形', en: 'Male Eye Surgery' },
        concerns: {
          ko: ['쌍꺼풀 없이 또렷한 눈매를 갖고 싶어요.', '졸려 보이는 눈매를 개선하고 싶어요.', '무쌍, 속쌍 상관없이 큰눈을 원해요.'],
          en: ['I want clear, defined eyes without a double eyelid.', 'I want to improve a sleepy-looking gaze.', 'I want bigger eyes, whether monolid or hidden double lid.'],
          'zh-CN': ['想要不用双眼皮就拥有清晰的眼神。', '想要改善看起来很困倦的眼神。', '无论是无重睑还是内双，都想要大眼睛。'],
          'zh-TW': ['想要不用雙眼皮就擁有清晰的眼神。', '想要改善看起來很困倦的眼神。', '無論是無重瞼還是內雙，都想要大眼睛。'],
        },
        description: {
          ko: '여성과는 다른 깔끔하고 자연스러운 라인으로 매력적인 눈매를 완성합니다.',
          'zh-CN': '用与女性不同的干净自然线条，打造有魅力的眼神。',
          en: "Using clean, natural lines different from women's surgery, this completes an attractive, defined gaze.",
          'zh-TW': '用與女性不同的乾淨自然線條，打造有魅力的眼神。',
        },
        label: { ko: '티 나지 않는 또렷함', en: 'Subtle, Undetectable Definition', 'zh-CN': '不着痕迹的清晰眼神', 'zh-TW': '不著痕跡的清晰眼神' },
        image: 'men-eye-surgery.png',
      },
      {
        slug: 'rhinoplasty',
        name: { ko: '남자 코성형', 'zh-CN': '男性鼻整形', 'zh-TW': '男性鼻整形', en: 'Male Rhinoplasty' },
        concerns: {
          ko: ['매부리코, 휜코가 고민이에요.', '남자다운 이미지로 개선을 원해요.', '코막힘과 비염증상이 심해요.'],
          en: ["I'm concerned about a hooked or crooked nose.", 'I want a more masculine image.', 'I have severe nasal congestion and rhinitis symptoms.'],
          'zh-CN': ['因鹰钩鼻、歪鼻而烦恼。', '希望改善为更有男人味的形象。', '鼻塞和鼻炎症状严重。'],
          'zh-TW': ['因鷹鉤鼻、歪鼻而煩惱。', '希望改善為更有男人味的形象。', '鼻塞和鼻炎症狀嚴重。'],
        },
        description: {
          ko: '단순히 코모양의 변화가 아닌 얼굴의 전체적인 이미지 변화를 만듭니다.',
          'zh-CN': '不仅仅是改变鼻型，而是带来整体脸部形象的变化。',
          en: 'This creates a change not just in the shape of the nose, but in the overall image of the face.',
          'zh-TW': '不僅僅是改變鼻型，而是帶來整體臉部形象的變化。',
        },
        label: { ko: '볼륨과 직선으로 살아나는 얼굴의 입체감', en: 'Facial Dimension Brought to Life with Volume and Straight Lines', 'zh-CN': '用轮廓感和直线条唤醒脸部立体感', 'zh-TW': '用輪廓感和直線條喚醒臉部立體感' },
        image: 'men-rhinoplasty.png',
      },
      {
        slug: 'facial-contouring',
        name: { ko: '남자 안면 윤곽', 'zh-CN': '男性面部轮廓整形', 'zh-TW': '男性臉部輪廓整形', en: 'Male Facial Contouring' },
        concerns: {
          ko: ['얼굴이 비대칭이에요.', '귀밑 사각턱이 심해요.', '턱뼈와 근육이 발달해 얼굴이 크고 넓어요.', '갸름한 라인을 원해요.'],
          en: ['My face is asymmetrical.', 'I have a severe square jaw below my ears.', 'My jaw bone and muscles are developed, making my face look large and wide.', 'I want a slimmer facial line.'],
          'zh-CN': ['脸部不对称。', '耳下方形下颌角严重。', '下颌骨和肌肉发达导致脸大且宽。', '想要拥有小巧的脸型线条。'],
          'zh-TW': ['臉部不對稱。', '耳下方形下顎角嚴重。', '下顎骨和肌肉發達導致臉大且寬。', '想要擁有小巧的臉型線條。'],
        },
        description: {
          ko: 'WJ 원진의 노하우로 선과 각을 살리면서자연스럽고, 샤프하게 만들어드립니다.',
          'zh-CN': '以WJ原辰的独家技术，在保留线条和棱角的同时，打造自然又立体的脸型。',
          en: "With WJ WonJin's expertise, we bring out lines and angles to create a natural yet sharp look.",
          'zh-TW': '以WJ原辰的獨家技術，在保留線條和稜角的同時，打造自然又立體的臉型。',
        },
        label: { ko: '남자만의 매력을 살린 얼굴형', en: 'A Facial Shape That Brings Out Distinctly Masculine Appeal', 'zh-CN': '展现男性专属魅力的脸型', 'zh-TW': '展現男性專屬魅力的臉型' },
        image: 'men-facial-contouring.png',
      },
      {
        slug: 'gynecomastia',
        name: { ko: '여유증', 'zh-CN': '男性女乳症', 'zh-TW': '男性女乳症', en: 'Gynecomastia' },
        concerns: {
          ko: ['가슴에 몽우리가 만져져요.', '유난히 가슴이 발달되어 있어요.', '가슴 때문에 얇은 옷 입기가 꺼려져요.'],
          en: ['I can feel a lump in my chest.', 'My chest is unusually developed.', 'I avoid wearing thin clothing because of my chest.'],
          'zh-CN': ['瘦弱但胸部突出', '减肥后脂肪未减少', '摸到硬结', '女性化胸部突出'],
          'zh-TW': ['瘦弱但胸部突出', '減肥後脂肪未減少', '摸到硬結', '女性化胸部突出'],
        },
        description: {
          ko: '가리고 감춰야만 했던 여성형 가슴, WJ 원진에서 자신감 넘치는 남자다운 가슴으로 만들어 드립니다.',
          'zh-CN': '从像女性的胸部打造成男性的胸部！是指男性的乳房因为乳腺或脂肪发达像女性乳房似的突出的情况，出现此状况的原因可能是内分泌问题导致荷尔蒙分泌异常或者其他原因，不仅是美观上的问题，精神上也会有很多压力，建议尽快矫正。',
          en: "The feminized chest you've had to hide and cover up — WJ WonJin will transform it into a confident, masculine chest.",
          'zh-TW': '從像女性的胸部打造成男性的胸部！是指男性的乳房因為乳腺或脂肪發達像女性乳房似的突出的情況，出現此狀況的原因可能是內分泌問題導致荷爾蒙分泌異常或者其他原因，不僅是美觀上的問題，精神上也會有很多壓力，建議儘快矯正。',
        },
        label: { ko: '남자들의 말 못할 고민', en: "A Concern Men Don't Like to Talk About" },
        image: 'men-gynecomastia.png',
      },
    ],
    otherItems: [],
  },
  {
    slug: 'reconstruction',
    name: { ko: '재건', 'zh-CN': '修复重建', 'zh-TW': '重建', en: 'Reconstructive Surgery' },
    icon: 'HeartHandshake',
    heroImages: ['reconstruction-hero01.jpg', 'reconstruction-hero02.jpg', 'reconstruction-hero03.jpg'],
    intro: {
      ko: 'WJ 원진은 수년간의 경험과 끊임 없는 연구를 통해 미용성형 뿐만 아니라 재건성형의 풍부한 경험을 가지고 있습니다. WJ 원진만의 노하우로 상처받은 마음까지 치료하고자 노력합니다.',
      'zh-CN': '重建整形——梦想着平凡的外貌和平凡生活的您，WJ原辰与您同行。WJ原辰经过多年的经验和不断的研究不仅具有美容整形的经验，还具有重建整形的丰富经验，WJ原辰通过独有的技术诀窍尽最大的努力治疗您受伤的心灵。',
      'zh-TW': '重建整形——夢想著平凡的外貌和平凡生活的您，WJ原辰與您同行。WJ原辰經過多年的經驗和不斷的研究不僅具有美容整形的經驗，還具有重建整形的豐富經驗，WJ原辰通過獨有的技術訣竅盡最大的努力治療您受傷的心靈。',
      en: 'Through years of experience and continuous research, WJ WonJin has built extensive expertise not only in cosmetic surgery but also in reconstructive surgery. With our own unique know-how, we strive to heal even the wounded heart.',
    },
    items: [
      {
        slug: 'cleft-lip-palate',
        name: { ko: '구순구개열', 'zh-CN': '唇腭裂', 'zh-TW': '唇顎裂', en: 'Cleft Lip & Palate' },
        concerns: {
          ko: ['선천적으로 윗입술이 갈라져 있어요.', '1차 수술 이후로 2차 수술시기를 놓쳤어요.', '수술 후 변형이 와서 치료가 필요해요.'],
          en: ['I was born with a cleft in my upper lip.', 'I missed the timing for a second surgery after the initial procedure.', 'I need treatment for deformity that developed after surgery.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '코, 인중은 물론 입술까지 종합적인 치료를 필요로 하는 수술입니다.',
          'zh-CN': '自然、精巧的复原是核心，鼻子、人中乃至嘴唇都需要综合治疗的手术。唇腭裂2次变形症状：第一次手术即使在婴幼儿期完美完成，也因生长组织速度差异和生长潜力不足会导致鼻子软骨变形或嘴唇侧面出现斑痕等二次变形症状，第二次手术在鼻子成长、脸部骨骼发育全部结束后的16岁以后进行最好。',
          en: 'This surgery requires comprehensive treatment covering the nose and philtrum as well as the lips.',
          'zh-TW': '自然、精巧的復原是核心，鼻子、人中乃至嘴唇都需要綜合治療的手術。唇顎裂2次變形症狀：第一次手術即使在嬰幼兒期完美完成，也因生長組織速度差異和生長潛力不足會導致鼻子軟骨變形或嘴唇側面出現斑痕等二次變形症狀，第二次手術在鼻子成長、臉部骨骼發育全部結束後的16歲以後進行最好。',
        },
        label: { ko: '자연스럽고 정교한 복원이 핵심', en: 'Natural, Precise Restoration Is Key' },
        image: 'reconstruction-cleft-lip-palate.png',
      },
      {
        slug: 'cleft-lip-rhinoplasty',
        name: { ko: '구순열코성형', 'zh-CN': '唇裂鼻整形', 'zh-TW': '唇裂鼻整形', en: 'Cleft Lip Rhinoplasty' },
        concerns: {
          ko: ['구순열 1차 수술 후 코에 변형이 일어났어요.', '코가 한쪽으로 휘고 콧구멍의 변형이 생겼어요.'],
          en: ['My nose became deformed after the initial cleft lip surgery.', 'My nose is bent to one side and my nostrils have become deformed.'],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '외관상의 문제뿐만 아니라 코의 기능까지 회복 시켜 자연스러운 아름다움과 편안한 일상생활을 드립니다.',
          'zh-CN': '改善变形的鼻部，不仅能改善外观问题，还能恢复鼻部功能，让您拥有自然美丽的鼻子，也让您过上舒适的日常生活。',
          en: 'By restoring not only appearance but also nasal function, we give you back natural beauty and a comfortable daily life.',
          'zh-TW': '改善變形的鼻部，不僅能改善外觀問題，還能恢復鼻部功能，讓您擁有自然美麗的鼻子，也讓您過上舒適的日常生活。',
        },
        label: { ko: '변형된 코 개선', en: 'Correcting a Deformed Nose' },
        image: 'reconstruction-cleft-lip-rhinoplasty.png',
      },
      {
        slug: 'ear-reconstruction',
        name: { ko: '귀성형', 'zh-CN': '耳部整形', 'zh-TW': '耳部整形', en: 'Ear Reconstruction / Ear Surgery' },
        concerns: {
          ko: ['귓불이 날카롭게 당겨져 보여요.', '귀가 너무 돌출되어 보여요.', '귀 모양 때문에 콤플렉스가 생겼어요.'],
          en: ['My earlobe looks sharply pulled.', 'My ears look too protruding.', "I've developed a complex because of the shape of my ears."],
          'zh-CN': [],
          'zh-TW': [],
        },
        description: {
          ko: '귀성형의 핵심은 조화로운 이목구비와 자연스러움입니다.',
          'zh-CN': '匀称的面部协调，耳朵整形的核心是打造协调自然的五官。（수술법: 埋线刀耳矫正术, 耳后三角皮瓣切开术）',
          en: 'The key to ear surgery is harmonious features and a natural look.',
          'zh-TW': '勻稱的面部協調，耳朵整形的核心是打造協調自然的五官。（수술법: 埋線刀耳矯正術, 耳後三角皮瓣切開術）',
        },
        label: { ko: '균형 잡힌 얼굴의 조화', en: 'Balanced Harmony for the Face' },
        image: 'reconstruction-ear-reconstruction.png',
      },
    ],
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
