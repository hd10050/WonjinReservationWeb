// ⚠️ 동일 출처 프록시(D7)라 SSR 요청에도 인증 쿠키가 실린다 → SSR 스킵(import.meta.server return) 금지.
//    스킵하면 인증 체크 자체가 무력화된 구멍이 된다(admin-panel-pattern-reference.md 8-1절).
const LOGIN_PATH = '/admin/login'

const ALLOWED: Record<string, string[]> = {
  HospitalManager: ['/admin', '/admin/reservations', '/admin/consultants', '/admin/procedures', '/admin/calendar', '/admin/kpi', '/admin/stats'],
  Consultant: ['/admin', '/admin/reservations', '/admin/calendar'],
}

export default defineNuxtRouteMiddleware((to) => {
  // 🔴 로그인 페이지는 이 미들웨어의 대상이 아니다 — 없으면 미로그인 → navigateTo(LOGIN_PATH)
  //    → 그 페이지에서 미들웨어 재실행 → 다시 navigateTo(LOGIN_PATH) … 무한 리다이렉트가 된다(6-3절).
  if (to.path === LOGIN_PATH) return

  const { user } = useAuth()
  if (!user.value) return navigateTo(LOGIN_PATH)
  if (user.value.role === 'Admin') return

  const allowed = ALLOWED[user.value.role]
  if (!allowed) return navigateTo(LOGIN_PATH)
  // 🔴 '/admin'(대시보드 루트)은 접두사 매칭에서 제외한다 — 제외하지 않으면 '/admin/'로 시작하는
  // 모든 경로(예: '/admin/kpi', '/admin/users')가 전부 매치돼 화이트리스트 자체가 무력화된다
  // (실측 확인: Consultant가 '/admin/kpi'에 실제로 접근됨 — 8-4절 비활성 시술과 같은 "빼먹으면 조용히 뚫리는" 함정).
  if (allowed.some(p => to.path === p || (p !== '/admin' && to.path.startsWith(`${p}/`)))) return
  return navigateTo('/admin')
})
