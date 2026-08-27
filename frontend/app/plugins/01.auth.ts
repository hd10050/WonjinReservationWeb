// 🔴 관리자 경로에서만 인증을 초기화한다(F5, 7-5절). 이 프로젝트는 공개 랜딩에 광고 트래픽이
// 몰리는 구조라, 전 페이지에서 fetchMe()를 부르면 방문자 수만큼 /api/auth/me 401이 백엔드로 간다.
// 🔴 성능(2026-08-27, "로그인이 느림" 재조사) — /admin/login도 '/admin'로 시작해 이 가드에
// 걸리고 있었다. 로그인 페이지는 미인증 방문자가 오는 곳이라 fetchMe()는 항상 401로 실패하고,
// SSR 경로는 그 뒤 ssrRefreshCookie()로 refresh까지 재시도해 실패한다 — 로그인 폼이 뜨기도 전에
// 매번 백엔드 왕복 2회(me→refresh)를 순차로 기다리던 것이 실제 지연의 근본 원인이었다
// (middleware/admin.ts는 이미 로그인 페이지를 제외하는데 이 플러그인만 빠져 있었음).
export default defineNuxtPlugin(async () => {
  const path = useRoute().path
  if (!path.startsWith('/admin') || path === '/admin/login') return

  const { fetchMe, user, isLoggedIn } = useAuth()
  if (import.meta.server) {
    await fetchMe()
    return
  }
  if (user.value === null) await fetchMe()

  // AT 만료(15분) 전에 미리 조용히 갱신 — 세션 중 401 자체를 안 맞게 한다(7-4절).
  const { tryRefresh } = useTokenRefresh()
  setInterval(() => {
    if (isLoggedIn.value) tryRefresh()
  }, 12 * 60 * 1000)
})
