// page:start~page:finish 구간(다음 페이지의 <script setup> 최상위 await 포함)을 전환 차단 오버레이로 덮는다(13-2절).
export default defineNuxtPlugin((nuxtApp) => {
  const { increment, decrement } = useRouteOverlay()
  nuxtApp.hook('page:start', () => increment())
  nuxtApp.hook('page:finish', () => decrement())
})
