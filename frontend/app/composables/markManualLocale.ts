// 헤더(components/LandingHeader.vue)에서 언어를 수동 선택했을 때 wj_lang 쿠키를 기록한다.
// 🔴 5-3절 — <head> 인라인 언어감지 스크립트가 정규식으로 raw 쿠키 값을 파싱하므로, useCookie()의
// JSON 인코딩("ko")과 어긋나면 안 된다. 반드시 raw document.cookie로 직접 쓸 것.
// landing.vue와 admin/login.vue가 같은 헤더 컴포넌트를 공유하므로 두 곳이 동일 포맷을 써야 한다.
export function markManualLocale(code: string) {
  if (import.meta.server) return
  const expires = new Date(Date.now() + 31536000000).toUTCString()
  document.cookie = `wj_lang=${code}; expires=${expires}; path=/; samesite=lax`
  document.cookie = `wj_lang_manual=1; expires=${expires}; path=/; samesite=lax`
}
