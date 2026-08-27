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
