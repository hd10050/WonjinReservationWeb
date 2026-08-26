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

// 통계 화면 기간 기본값(오늘, KST 기준 YYYY-MM-DD) — en-CA 로케일 포맷이 그대로 이 형식을 낸다.
export function todayKst(): string {
  return new Intl.DateTimeFormat('en-CA', { timeZone: KST }).format(new Date())
}

// birthDate는 date(타임존 없음) 컬럼이라 KST 변환이 필요 없다 — 달력 나이 계산만 한다.
export function calculateAge(birthDate: string): number {
  const [y, m, d] = birthDate.split('-').map(Number)
  const today = new Date()
  let age = today.getFullYear() - y
  if (today.getMonth() + 1 < m || (today.getMonth() + 1 === m && today.getDate() < d)) age--
  return age
}
