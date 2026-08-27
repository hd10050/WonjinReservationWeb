# 랜딩페이지 재설계 — 설계 문서 (2026-08-27)

## 1. 배경 및 목적

WonjinReservationWeb은 광고를 보고 유입된 외국인(주로 중화권) 고객이 시술 종류를 확인하고 위챗으로 상담을 요청하도록 만드는 것이 목적이다. 현재 어드민 패널(3역할 9메뉴)은 전부 완성되어 있으나, 실제 고객이 보는 랜딩페이지는 히어로 문구 한 줄 + 상담 신청 폼([frontend/app/pages/index.vue](../../../frontend/app/pages/index.vue))만 있고 시술을 둘러보는 기능이 전혀 없다. `docs/design.md` 20장의 "M6 랜딩 히어로·소개 콘텐츠"가 미결정 상태로 남아있던 부분을 이번에 확정한다.

참고 사이트: 같은 병원(원진성형외과의원, 사업자번호 824-67-00414)의 국내용 기존 사이트 `k-wonjin.co.kr` — 같은 병원의 자체 콘텐츠이므로 구조·문구를 그대로 참고 대상으로 삼는다.

## 2. 범위

### 포함
- 우측 하단 상시 문의 배지 (전 페이지 공통)
- 홈: 병원 소개 + 카테고리 바로가기
- 카테고리 페이지 11개 (`/procedures/[category]`)
- 시술 상세 페이지 76개 (`/procedures/[category]/[procedure]`)
- 기존 상담 신청 폼을 `/inquiry` 페이지로 이전

### 제외 (명시적 지시)
- 관리자 CRUD 연동 없음 — `Procedure`/`AdminProceduresController` 등 기존 어드민 시술 관리와 완전히 별개, 순수 프론트엔드 콘텐츠
- BEFORE/AFTER 이미지
- 소개 멘트·슬로건 (카테고리 페이지의 "티 안나게..." 류 캐치프레이즈)
- 원장 소개
- WJ 원진 Shorts (영상 후기 캐러셀)
- 함께하면 좋은 수술 (연관 시술 추천)
- 참고 사이트의 다른 상담 채널(Ai상담/카카오톡상담/빠른비용상담/바로예약)과 프로모션 배너 — 이번 사이트는 위챗 상담 신청 폼 하나로 통일 (기존 확정 설계 "위챗 탑재 취소" 원칙과 무관 — 이건 상담 *신청 채널*이 아니라 참고사이트의 부가 마케팅 위젯들을 뜻함)

## 3. 정보구조 (사이트맵)

```
/                              홈 — 병원 소개 + 카테고리 바로가기
/procedures/[category]        카테고리 목록 (11개)
/procedures/[category]/[proc] 시술 상세 (76개)
/inquiry                      상담 신청 폼 (기존 index.vue 내용 이전)
/privacy                      개인정보 처리방침 (기존 유지, 변경 없음)
```

`@nuxtjs/i18n`의 `prefix_except_default` 그대로 적용 — zh-CN은 prefix 없음, zh-TW/en/ko는 `/ko/procedures/eye` 형태.

## 4. 데이터 아키텍처

**결정: 정적 데이터 파일 + 동적 라우트 페이지** (3가지 대안 중 채택, 사용자 승인 완료)

- `frontend/app/data/procedures.ts` 1개 파일에 11개 카테고리 × 76개 시술의 4개 언어 데이터를 구조화해 하드코딩한다. 관리자 CRUD와 무관 — 코드에 박히는 콘텐츠다.
- 페이지 파일은 `pages/procedures/[category].vue` 1개, `pages/procedures/[category]/[procedure].vue` 1개, 이렇게 2개로 87개 페이지(카테고리 11 + 상세 76) 전부 커버한다.
- 데이터가 정적 import이므로 비동기 fetch 자체가 없다 — "화면 깜빡임 금지" 원칙이 요구하는 SSR 프리로드가 애초에 불필요할 만큼 안전하다(9절 참고).

기각한 대안:
- **카테고리별 개별 .vue 파일 11~76개**: 코드 중복이 심해 유지보수가 감당 안 됨
- **기존 i18n JSON에 전부 포함**: "4개 로케일 키 집합 항상 동일" 검증 대상이 76개 시술×설명까지 늘어나 로케일 파일이 감당 못 할 정도로 비대해짐

### 타입 스케치

```ts
type Locale = 'zh-CN' | 'zh-TW' | 'en' | 'ko'

interface ProcedureItem {
  slug: string
  name: Record<Locale, string>
  concerns: Record<Locale, string[]>   // "OO에 어떤 고민이 있으신가요?" 불릿 3~4개 — 미정, 10절 참고
  heroImage?: string                    // 미정, 10절 참고
}

interface ProcedureCategory {
  slug: string
  name: Record<Locale, string>
  icon: string
  heroImage?: string                   // 미정, 10절 참고
  intro: Record<Locale, string>        // 카테고리 히어로 소개문 — 미정, 10절 참고
  items: ProcedureItem[]
}
```

## 5. 페이지별 설계

### 5.1 홈 (`/`)

순서 확정(사용자 승인): **히어로 → 카테고리 바로가기 → 병원소개 → 푸터**. 광고 유입 방문자는 시술 종류부터 확인하고 싶어 하므로 관심사(카테고리)를 먼저 보여주고, 신뢰를 쌓는 병원소개 콘텐츠는 그다음에 배치한다.

1. **히어로**: 간단한 타이틀/서브타이틀 (기존 `landing.hero.title/subtitle` i18n 키 재사용)
2. **카테고리 바로가기**: 11개 카테고리 카드(아이콘+이름) 그리드, 클릭 시 해당 `/procedures/[category]`로 이동
3. **병원소개**: 참고사이트 `hospitalinfo/about` 구조 기반 — 소개 문단 + 시설(층별) 소개. 실제 문구·사진은 병원 제공 필요(10절)
4. **푸터**: 기존 유지

### 5.2 카테고리 페이지 (`/procedures/[category]`)

참고사이트 구조에서 BEFORE/AFTER·소개멘트/슬로건·원장소개를 뺀 나머지:

1. **히어로**: 사진 배경 위에 카테고리 아이콘 + 카테고리명 + 소개문(`intro`)
2. **"OO에 어떤 고민이 있으신가요?" 목록**: 시술마다 한 행(사진 + 고민 불릿 3~4줄 + 시술명), 클릭 시 `/procedures/[category]/[procedure]`로 이동. 참고사이트는 사진-텍스트 좌우 교차 배치(지그재그)를 쓴다.
3. **푸터**

### 5.3 시술 상세 페이지 (`/procedures/[category]/[procedure]`)

참고사이트에서 BEFORE/AFTER·WJ 원진 Shorts·함께하면 좋은 수술을 빼면 남는 것:

1. **히어로**: 라벨(대체 명칭, 선택) + 시술명 + 짧은 설명 + 인물사진 1장 (좌: 텍스트, 우: 사진 — 참고사이트 `zygoma` 페이지 레이아웃) + **"지금 문의하기" CTA 버튼**(`/inquiry`로 이동, 확정)
2. **푸터**

### 5.4 문의 페이지 (`/inquiry`)

기존 `frontend/app/pages/index.vue`의 폼(이름/생년월일/성별/위챗ID/희망연락시각/개인정보동의)을 그대로 이 경로로 이전한다. 검증 로직·API 호출(`POST /api/reservations`)·honeypot·UTM 트래킹은 변경 없음.

## 6. 상시 문의 배지 (FAB)

결정(사용자 승인): **항상 표시, 데스크톱은 아이콘+"상담하기" 텍스트, 모바일은 텍스트 없는 작은 원형 아이콘만.** 참고사이트는 5단 채널 스택(Ai상담/카카오톡상담/빠른비용상담/바로예약/피부과)+채팅 버블까지 있지만, 우리는 채널이 위챗 상담 신청 폼 하나뿐이라 훨씬 가볍다. 클릭 시 `/inquiry`로 이동.

이 FAB는 "화면 깜빡임 금지" 절의 전환 차단 오버레이(9절 참고)와는 별개 컴포넌트다 — 단순 상시 표시 링크이며 `<Transition>`으로 마운트/언마운트되는 대상이 아니므로 그 사고 패턴(9-3절)이 적용되지 않는다.

## 7. 내비게이션 (확정)

- **데스크톱**: "시술안내" 드롭다운 메뉴 안에 11개 카테고리 나열 + 홈 + 문의하기
- **모바일**: 햄버거 메뉴

참고사이트는 11개를 전부 가로로 펼쳐놓지만(WJ코스메틱·의료진소개·원진TV까지 총 14개), 우리는 그런 부가 메뉴가 없고 화면 폭도 더 좁을 수 있어 드롭다운으로 묶었다.

## 8. i18n / 콘텐츠 전략

- **UI 문자열**(내비게이션 라벨, "OO에 어떤 고민이 있으신가요?" 같은 섹션 제목 패턴, 버튼 텍스트 등): 기존 `frontend/i18n/locales/*.json`에 추가, 4개 로케일 키 집합 동일성 규칙 그대로 적용
- **카탈로그 데이터**(카테고리/시술 이름, 소개문, 고민 불릿): `frontend/app/data/procedures.ts`에 로케일별로 직접 기록 — 키-값 번역이 아니라 4개 언어 전용 구조체이므로 i18n JSON 키 집합 동일성 규칙의 대상이 아니다. 대신 구현 시 4개 언어 항목 수가 카테고리·시술 단위로 정확히 일치하는지 별도로 대조한다(부록 B는 이미 1차 대조 완료 — 11개 카테고리 전부 76개 일치 확인).

## 9. 절대원칙 준수 검토 (루트 CLAUDE.md 기준)

| 절 | 인용 | 이번 설계 위반 여부 |
|---|---|---|
| 화면 깜빡임 금지 | "데이터가 있는 경우: 화면 로드와 동시에 데이터가 이미 표시된 상태여야 함... SSR 프리로드 적용 필수" | **위반 없음.** 카탈로그 데이터가 정적 import라 비동기 fetch 자체가 없음 — SSR 프리로드가 필요 없을 만큼 원천적으로 안전. 페이지 전환 오버레이는 기존 전역 컴포넌트(`useLoadingIndicator` 기반, 이미 구현됨)를 그대로 재사용하고 새로 만들지 않는다. |
| 입력 필드 길이 제한 | "사용자가 값을 입력할 수 있는 모든 input/textarea에는 프론트엔드와 백엔드 양쪽 모두에 maxlength를 적용" | **해당 없음.** 이번 범위(홈/카테고리/상세)엔 신규 input/textarea가 없다. `/inquiry`로 이전하는 기존 폼은 필드를 변경하지 않고 그대로 옮기므로 기존 maxlength+백엔드 검증 세트가 그대로 유지된다. |
| DB 쿼리 성능 원칙 | "WHERE·ORDER BY·GroupBy에 쓰는 컬럼은 인덱스가 있는지 항상 먼저 확인... 목록 조회 API는 예외 없이 페이징" | **해당 없음.** 신규 DB 쿼리·API 엔드포인트가 없다(정적 데이터 파일, 백엔드 변경 없음). |
| 디자인 원칙 | "input/textarea/select 옆(또는 위)에 보이는 label을 항상 넣을 것" | **해당 없음(직접).** 이번 범위엔 입력 필드가 없다. `/inquiry`로 이전하는 기존 폼은 라벨을 그대로 유지한 채 이동만 한다. |
| 번역 규칙 | "원문 줄 수 == 번역 줄 수... 숫자·고유명사 절대 변경 금지" | **적용 방식 다름.** 4개 언어 카탈로그 데이터는 사용자가 이미 4개 언어 전부 직접 제공했으므로 이번엔 번역 작업이 없다 — 옮겨적을 때 항목 수 일치만 재확인(위 8절). |
| 절대 원칙(반복 금지) 중 "지시 범위를 문자 그대로 지킬 것" | "'설계해'는 설계까지만, 구현·파일 수정으로 넘어가지 말 것" | **준수.** "1로 진행해"는 4절의 데이터 아키텍처 방식 승인이며, 이 문서 작성까지가 브레인스토밍 스킬의 다음 단계다. 이 문서에는 실제 애플리케이션 코드(.vue/.ts)를 작성하지 않았다. |

## 10. 미해결 항목 (Open Items)

1. **카테고리 소개문·시술 짧은 설명·고민 불릿** — 새로 작성하지 않고 참고사이트(k-wonjin.co.kr) 원문을 그대로 재사용하는 것으로 확정(2026-08-27, 같은 병원 소유 콘텐츠). 11개 카테고리 전체를 병렬 에이전트로 추출 중 — 완료되면 부록 D로 이 문서에 추가.
   - **4개 언어 중 3개(zh-CN/zh-TW/en) 소싱 방법은 별도 확인 필요**: 원진은 중국어(wonjincn.com)·영어(wonjinbeauty.com/en) 자매 사이트를 이미 운영 중이라 그쪽 원문을 그대로 가져올지, 이번에 추출한 한국어 원문을 번역할지 결정 필요.
2. **사진 자료** — 카테고리 히어로 배경 11장 + 시술별 인물사진 76장. 파일명 목록을 채팅으로 전달 — 병원 측 준비 대기.
3. **홈 병원소개 섹션 콘텐츠** — 참고사이트의 층별 시설 소개 등, 실제 문구·사진은 병원 제공 필요.

## 부록 A. 카테고리 목록 및 슬러그

| 순서 | 슬러그 | 한국어 | 中文（简体） | 中文（繁體） | English |
|---|---|---|---|---|---|
| 1 | eye | 눈 | 眼部 | 眼部 | Eyes |
| 2 | nose | 코 | 鼻部 | 鼻部 | Nose |
| 3 | ent | 이비인후과(코) | 耳鼻喉科（鼻部） | 耳鼻喉科（鼻部） | ENT (Nose) |
| 4 | lifting | 리프팅 | 提拉 | 拉提 | Lifting |
| 5 | dermatology | 피부과 | 皮肤科 | 皮膚科 | Dermatology |
| 6 | stemcell | 줄기세포 | 干细胞 | 幹細胞 | Stem Cell |
| 7 | breast | 가슴 | 胸部 | 胸部 | Breast |
| 8 | contour | 윤곽·양악 | 面部轮廓・双颌 | 臉部輪廓・雙顎 | Facial Contouring & Double Jaw |
| 9 | bodyline | 체형 | 体型 | 體型 | Body Contouring |
| 10 | men | 남자 | 男性 | 男性 | Men |
| 11 | reconstruction | 재건 | 修复重建 | 重建 | Reconstructive Surgery |

## 부록 B. 시술 전체 목록 (4개 언어, 76개, 사용자 제공 원문)

### 눈 / 眼部 / Eyes (10)

| 한국어 | 中文（简体） | 中文（繁體） | English |
|---|---|---|---|
| 비절개 눈매교정 - 글램아이 | 非切开眼型矫正－Glam Eye | 非切開眼型矯正－Glam Eye | Non-Incisional Eye Shape Correction – Glam Eye |
| 부분절개 눈매교정 - 더블유착 | 部分切开眼型矫正－Double Adhesion | 部分切開眼型矯正－Double Adhesion | Partial-Incision Eye Shape Correction – Double Adhesion |
| 눈썹 올림술 - 엔젤아이 | 提眉术－Angel Eye | 提眉術－Angel Eye | Brow Lift – Angel Eye |
| 트임 성형 - 오픈아이 | 开眼角手术－Open Eye | 開眼角手術－Open Eye | Eye Opening Surgery – Open Eye |
| 눈 재수술 | 眼部修复手术 | 眼部修復手術 | Revision Eye Surgery |
| 고도 안검하수 눈매교정 | 重度上睑下垂眼型矫正 | 重度上瞼下垂眼型矯正 | Severe Ptosis Correction |
| 눈밑지방재배치 | 眼袋脂肪重置 | 眼袋脂肪重置 | Lower Eyelid Fat Repositioning |
| 중년 눈성형 | 中老年眼部整形 | 中老年眼部整形 | Middle-Aged Eye Surgery |
| 짝눈(비대칭) 교정 | 大小眼（眼部不对称）矫正 | 大小眼（眼部不對稱）矯正 | Asymmetrical Eye Correction |
| 소아 선천성 안검하수 | 儿童先天性上睑下垂 | 兒童先天性上瞼下垂 | Congenital Ptosis Surgery for Children |

### 코 / 鼻部 / Nose (9)

| 한국어 | 中文（简体） | 中文（繁體） | English |
|---|---|---|---|
| 들창코·짧은 코 성형 | 朝天鼻・短鼻整形 | 朝天鼻・短鼻整形 | Upturned Nose & Short Nose Surgery |
| 콧볼 축소 | 鼻翼缩小 | 鼻翼縮小 | Alar Reduction |
| 매부리코 성형 | 驼峰鼻整形 | 駝峰鼻整形 | Hump Nose Surgery |
| 휜 코 성형 | 歪鼻整形 | 歪鼻整形 | Deviated Nose Surgery |
| 코 재수술 | 鼻部修复手术 | 鼻部修復手術 | Revision Rhinoplasty |
| 무보형물코성형 | 无假体鼻整形 | 無假體鼻整形 | Non-Implant Rhinoplasty |
| 복 코 성형 | 宽大鼻整形 | 寬大鼻整形 | Bulbous Nose Surgery |
| 코끝 성형 | 鼻尖整形 | 鼻尖整形 | Tip Plasty |
| 남자 코성형 | 男性鼻整形 | 男性鼻整形 | Male Rhinoplasty |

### 이비인후과(코) / 耳鼻喉科（鼻部） / ENT (Nose) (5)

| 한국어 | 中文（简体） | 中文（繁體） | English |
|---|---|---|---|
| 비중격만곡증 | 鼻中隔偏曲 | 鼻中隔彎曲 | Deviated Nasal Septum |
| 비밸브협착증 | 鼻瓣区狭窄 | 鼻瓣區狹窄 | Nasal Valve Stenosis |
| 편도선수술 | 扁桃体手术 | 扁桃腺手術 | Tonsillectomy |
| 비염 | 鼻炎 | 鼻炎 | Rhinitis |
| 축농증(부비동염) | 鼻窦炎（副鼻窦炎） | 鼻竇炎（副鼻竇炎） | Sinusitis |

### 리프팅 / 提拉・拉提 / Lifting (6)

| 한국어 | 中文（简体） | 中文（繁體） | English |
|---|---|---|---|
| 엘라스티꿈 리프팅 | ElastiGum 提拉 | ElastiGum 拉提 | ElastiGum Lifting |
| 안면 거상 | 面部提升术 | 臉部拉皮手術 | Facelift |
| 이마 거상술 | 额头提升术 | 額頭拉提術 | Forehead Lift |
| 이마 축소술 | 额头缩小术 | 額頭縮小術 | Forehead Reduction |
| 민트 리프팅 | Mint 提拉 | Mint 拉提 | Mint Lifting |
| 지방이식 | 脂肪填充 | 脂肪填補 | Fat Grafting |

### 피부과 / 皮肤科 / Dermatology (7)

| 한국어 | 中文（简体） | 中文（繁體） | English |
|---|---|---|---|
| 울쎄라피 프라임 | Ulthera Prime | Ulthera Prime | Ulthera Prime |
| 써마지 FLX | Thermage FLX | Thermage FLX | Thermage FLX |
| 볼뉴머 | Volnewmer | Volnewmer | Volnewmer |
| 레이저 안티에이징 | 激光抗衰老 | 雷射抗老化 | Laser Anti-Aging |
| 스킨부스터 | 水光针／Skin Booster | 水光針／Skin Booster | Skin Booster |
| 색소·모공 | 色素・毛孔 | 色素・毛孔 | Pigmentation & Pores |
| 여드름·홍조 | 痘痘・泛红 | 痘痘・泛紅 | Acne & Facial Redness |

### 줄기세포 / 干细胞 / Stem Cell (5)

| 한국어 | 中文（简体） | 中文（繁體） | English |
|---|---|---|---|
| 줄기세포 주사 | 干细胞注射 | 幹細胞注射 | Stem Cell Injection |
| 줄기세포 지방이식 | 干细胞脂肪填充 | 幹細胞脂肪填補 | Stem Cell Fat Grafting |
| 줄기세포 리프팅 | 干细胞提拉 | 幹細胞拉提 | Stem Cell Lifting |
| 줄기세포 탈모개선 | 干细胞脱发改善 | 幹細胞落髮改善 | Stem Cell Hair Loss Treatment |
| 줄기세포 남성활력 | 干细胞男性活力 | 幹細胞男性活力 | Stem Cell Men's Wellness |

### 가슴 / 胸部 / Breast (10)

| 한국어 | 中文（简体） | 中文（繁體） | English |
|---|---|---|---|
| 가슴 확대 성형 | 隆胸手术 | 隆乳手術 | Breast Augmentation |
| 하이브리드 가슴성형 | 混合式隆胸 | 混合式隆乳 | Hybrid Breast Augmentation |
| 가슴 축소 성형 | 缩胸手术 | 縮乳手術 | Breast Reduction |
| 처진 가슴 교정 | 乳房下垂矫正 | 下垂乳房矯正 | Breast Lift |
| 가슴 재수술 | 胸部修复手术 | 胸部修復手術 | Revision Breast Surgery |
| 가슴 지방 이식 | 胸部脂肪填充 | 胸部脂肪填補 | Breast Fat Grafting |
| 출산 후 가슴 성형 | 产后胸部整形 | 產後胸部整形 | Postpartum Breast Surgery |
| 유두 성형 | 乳头整形 | 乳頭整形 | Nipple Surgery |
| 여성형 유방(여유증) | 男性女乳症（男性乳房发育） | 男性女乳症（男性乳房發育） | Gynecomastia |
| 줄기세포 가슴 성형 | 干细胞隆胸 | 幹細胞隆乳 | Stem Cell Breast Augmentation |

### 윤곽·양악 / 面部轮廓・双颌 / Facial Contouring & Double Jaw (14)

| 한국어 | 中文（简体） | 中文（繁體） | English |
|---|---|---|---|
| 긴 얼굴 수술 | 长脸手术 | 長臉手術 | Long Face Surgery |
| 안면 비대칭 교정 | 面部不对称矫正 | 臉部不對稱矯正 | Facial Asymmetry Correction |
| 돌출 입 교정 | 凸嘴矫正 | 凸嘴矯正 | Protruding Mouth Correction |
| 복합 안면 윤곽 | 综合面部轮廓手术 | 複合式臉部輪廓手術 | Comprehensive Facial Contouring |
| 사각 턱 수술 | 下颌角手术 | 下顎角手術 | Jaw Angle Surgery |
| 광대뼈 축소술 | 颧骨缩小术 | 顴骨縮小術 | Zygoma Reduction |
| 턱 끝 수술 | 下巴整形 | 下巴整形 | Chin Surgery |
| 윤곽재건복원술 | 面部轮廓重建修复术 | 臉部輪廓重建修復術 | Facial Contouring Reconstruction |
| 셀프 양악수술 | 自主双颌手术 | 自主雙顎手術 | Self-Designed Double Jaw Surgery |
| 주걱턱 수술 | 地包天手术 | 戽斗手術 | Underbite Surgery |
| 무턱(하악 왜소증) 수술 | 下巴后缩（下颌发育不足）手术 | 下巴後縮（下顎發育不足）手術 | Receding Chin (Mandibular Hypoplasia) Surgery |
| 안면윤곽 재수술 | 面部轮廓修复手术 | 臉部輪廓修復手術 | Revision Facial Contouring Surgery |
| 양악 재수술 | 双颌修复手术 | 雙顎修復手術 | Revision Double Jaw Surgery |
| 수술 후 교정 | 术后牙齿矫正 | 術後牙齒矯正 | Postoperative Orthodontic Treatment |

### 체형 / 体型 / Body Contouring (3)

| 한국어 | 中文（简体） | 中文（繁體） | English |
|---|---|---|---|
| 지방 흡입 | 吸脂 | 抽脂 | Liposuction |
| 복부 성형술 | 腹部整形术 | 腹部整形術 | Abdominoplasty |
| 힙업 성형 | 翘臀整形 | 提臀整形 | Hip Augmentation |

### 남자 / 男性 / Men (4)

| 한국어 | 中文（简体） | 中文（繁體） | English |
|---|---|---|---|
| 남자 눈성형 | 男性眼部整形 | 男性眼部整形 | Male Eye Surgery |
| 남자 코성형 | 男性鼻整形 | 男性鼻整形 | Male Rhinoplasty |
| 남자 안면 윤곽 | 男性面部轮廓整形 | 男性臉部輪廓整形 | Male Facial Contouring |
| 여유증 | 男性女乳症 | 男性女乳症 | Gynecomastia |

### 재건 / 修复重建 / Reconstructive Surgery (3)

| 한국어 | 中文（简体） | 中文（繁體） | English |
|---|---|---|---|
| 구순구개열 | 唇腭裂 | 唇顎裂 | Cleft Lip & Palate |
| 구순열코성형 | 唇裂鼻整形 | 唇裂鼻整形 | Cleft Lip Rhinoplasty |
| 귀성형 | 耳部整形 | 耳部整形 | Ear Reconstruction / Ear Surgery |

**합계: 11개 카테고리, 76개 시술, 4개 언어 항목 수 전수 일치 확인 완료.**

## 부록 C. 이미지 파일명 목록 (병원 준비용)

명명 규칙: 카테고리 히어로는 `{category}-hero`, 시술 사진은 `{category}-{procedure-slug}`. 확장자는 jpg/png/webp 무엇이든 상관없음(파일명만 아래와 일치시키면 됨). 총 11(히어로) + 76(시술) = 87장.

### 카테고리 히어로 (11)

```
eye-hero
nose-hero
ent-hero
lifting-hero
dermatology-hero
stemcell-hero
breast-hero
contour-hero
bodyline-hero
men-hero
reconstruction-hero
```

### 시술 사진 (76, 카테고리별)

```
# eye (10)
eye-glam-eye
eye-double-adhesion
eye-angel-eye
eye-open-eye
eye-eye-revision
eye-severe-ptosis-correction
eye-lower-eyelid-fat-repositioning
eye-middle-aged-eye-surgery
eye-asymmetrical-eye-correction
eye-congenital-ptosis-children

# nose (9)
nose-upturned-short-nose
nose-alar-reduction
nose-hump-nose
nose-deviated-nose
nose-nose-revision
nose-non-implant-rhinoplasty
nose-bulbous-nose
nose-tip-plasty
nose-male-rhinoplasty

# ent (5)
ent-deviated-septum
ent-nasal-valve-stenosis
ent-tonsillectomy
ent-rhinitis
ent-sinusitis

# lifting (6)
lifting-elastigum-lifting
lifting-facelift
lifting-forehead-lift
lifting-forehead-reduction
lifting-mint-lifting
lifting-fat-grafting

# dermatology (7)
dermatology-ulthera-prime
dermatology-thermage-flx
dermatology-volnewmer
dermatology-laser-anti-aging
dermatology-skin-booster
dermatology-pigmentation-pores
dermatology-acne-redness

# stemcell (5)
stemcell-injection
stemcell-fat-grafting
stemcell-lifting
stemcell-hair-loss
stemcell-mens-wellness

# breast (10)
breast-augmentation
breast-hybrid-augmentation
breast-reduction
breast-lift
breast-revision
breast-fat-grafting
breast-postpartum
breast-nipple-surgery
breast-gynecomastia
breast-stemcell-augmentation

# contour (14)
contour-long-face-surgery
contour-facial-asymmetry-correction
contour-protruding-mouth-correction
contour-comprehensive-facial-contouring
contour-jaw-angle-surgery
contour-zygoma-reduction
contour-chin-surgery
contour-facial-contouring-reconstruction
contour-self-designed-double-jaw
contour-underbite-surgery
contour-receding-chin
contour-facial-contouring-revision
contour-double-jaw-revision
contour-postoperative-orthodontics

# bodyline (3)
bodyline-liposuction
bodyline-abdominoplasty
bodyline-hip-augmentation

# men (4)
men-eye-surgery
men-rhinoplasty
men-facial-contouring
men-gynecomastia

# reconstruction (3)
reconstruction-cleft-lip-palate
reconstruction-cleft-lip-rhinoplasty
reconstruction-ear-reconstruction
```
