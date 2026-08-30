import { parseDate } from '@internationalized/date'

// 서버(UTC)·클라이언트(브라우저 로컬)가 같은 문자열을 만들도록 타임존을 KST로 명시 고정한다(9-2절②).
// new Date(x).toLocaleString()처럼 브라우저 로컬 타임존에 의존하면 SSR과 하이드레이션 결과가 달라진다.
const KST = 'Asia/Seoul'

export function formatKst(value: string | Date, withTime = true): string {
  return new Intl.DateTimeFormat('ko-KR', {
    timeZone: KST,
    year: 'numeric', month: '2-digit', day: '2-digit',
    ...(withTime ? { hour: '2-digit', minute: '2-digit', hour12: false } : {}),
  }).format(typeof value === 'string' ? new Date(value) : value)
}

// [예약 달력] 기본 조회월 계산용 — 서버(SSR)·클라이언트 호스트의 로컬 타임존과 무관하게
// 항상 KST 기준 "오늘"을 반환한다(9-2절②와 동일한 이유, formatKst와 같은 패턴).
export function getKstToday(): { year: number, month: number, day: number } {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone: KST, year: 'numeric', month: '2-digit', day: '2-digit',
  }).formatToParts(new Date())
  const map = Object.fromEntries(parts.map(p => [p.type, p.value]))
  return { year: Number(map.year), month: Number(map.month), day: Number(map.day) }
}

// [실장 KPI]·[예약 통계] 기간 기본값(오늘, KST 기준 YYYY-MM-DD) — en-CA 로케일 포맷이 그대로 이 형식을 낸다.
export function todayKst(): string {
  return new Intl.DateTimeFormat('en-CA', { timeZone: KST }).format(new Date())
}

// 검색필터 시작~종료일(YYYY-MM-DD) 조회 상한 = 1년+1일. useDateRangeFilter가 필터 폼(UI) 조작은
// 막지만, route.query(URL 직접 입력·북마크)로 들어온 값은 그 폼 상태를 거치지 않아 방어가 안 먹는다
// — 실제 데이터 조회(SSR useApi 등)에 쓰는 query 자체를 여기서 한 번 더 clamp해 우회를 막는다.
export function clampDateRangeEnd(from: string, to: string): string {
  const maxTo = parseDate(from).add({ years: 1, days: 1 })
  return parseDate(to).compare(maxTo) > 0 ? maxTo.toString() : to
}

// birthDate는 date(타임존 없음) 컬럼이지만, "오늘"은 KST로 고정해야 한다 — 예약 상세는 상위
// await useApi로 SSR 프리로드되므로 이 함수가 서버(UTC 호스트)에서도 실행된다. new Date()로 오늘을
// 잡으면 서버(UTC)·클라이언트(KST 브라우저)가 KST 00:00~09:00 구간에 하루 어긋나, 생일 당일엔
// 나이가 1살 달라져 하이드레이션 불일치가 난다. getKstToday()로 formatKst와 같은 패턴을 쓴다.
export function calculateAge(birthDate: string): number {
  const [y, m, d] = birthDate.split('-').map(Number)
  const today = getKstToday()
  let age = today.year - y
  if (today.month < m || (today.month === m && today.day < d)) age--
  return age
}
