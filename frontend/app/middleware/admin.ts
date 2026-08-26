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
  if (allowed.some(p => to.path === p || to.path.startsWith(`${p}/`))) return
  return navigateTo('/admin')
})
