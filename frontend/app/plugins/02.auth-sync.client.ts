// 🔴 다른 탭 즉시 로그아웃(2026-08-27, auth-pattern-reference.md 6-7절 패턴) — useState('auth:user')는
// 탭마다 독립된 상태라 한 탭의 로그아웃이 다른 탭엔 반영되지 않았다. useAuth().logout()이 방송한
// BroadcastChannel('wj_auth') 메시지를 받아 user만 비우면, admin.vue 레이아웃의 기존
// watch(user)(174절, "여기 watch가 유일한 이탈 경로다")가 그대로 /admin/login 하드 리다이렉트를 수행한다 —
// 이 플러그인에서 별도로 navigateTo/location.href를 다시 호출하지 않는다(중복 네비게이션 방지).
export default defineNuxtPlugin(() => {
  const path = useRoute().path
  if (!path.startsWith('/admin')) return // 공개 랜딩 방문자에겐 불필요(01.auth.ts와 동일 원칙)

  const { user } = useAuth()
  const channel = new BroadcastChannel('wj_auth')
  channel.onmessage = (event: MessageEvent) => {
    if (event.data === 'logout') user.value = null
  }
})
