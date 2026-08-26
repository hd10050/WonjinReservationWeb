// 🔴 관리자 경로에서만 인증을 초기화한다(F5, 7-5절). 이 프로젝트는 공개 랜딩에 광고 트래픽이
// 몰리는 구조라, 전 페이지에서 fetchMe()를 부르면 방문자 수만큼 /api/auth/me 401이 백엔드로 간다.
export default defineNuxtPlugin(async () => {
  const path = useRoute().path
  if (!path.startsWith('/admin')) return

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
