// 어드민 전용 웹 푸시 Service Worker 부팅 시 등록(web-push-notification-guide.md 5-2절).
// 이 프로젝트엔 옛 SW 정리 플러그인이 없어 경쟁 상태 걱정은 없지만, 등록 자체는 유저 액션(배너
// 클릭)을 기다리지 않고 미리 해둬야 usePush()의 subscribe()가 register() 재호출 없이 바로
// getRegistration()으로 재사용할 수 있다.
export default defineNuxtPlugin(() => {
  const path = useRoute().path
  if (!path.startsWith('/admin')) return // 공개 랜딩 방문자에겐 불필요

  if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('/sw.js').catch(() => {})
  }
})
