// 매 요청마다 CSP script-src용 nonce를 생성해 응답 헤더에 심고, event.context에 저장한다.
// server/plugins/csp-nonce-html.ts의 render:html 훅이 이 nonce를 읽어 인라인 <script> 태그에 부여한다
// (Nuxt 4 공식 패턴, https://nuxt.com/docs/4.x/guide/going-further/experimental-features).
// style-src만 'unsafe-inline' — reka-ui Popper 위치계산 등이 style 속성을 동적으로 쓰는 UI 라이브러리
// 특성상 인라인 스타일까지 nonce로 잠그는 건 이번 범위 밖(script-src만 엄격 적용이 목표).
export default defineEventHandler((event) => {
  const nonce = crypto.randomUUID()
  event.context.cspNonce = nonce

  const csp = [
    "default-src 'self'",
    `script-src 'self' 'nonce-${nonce}'`,
    "style-src 'self' 'unsafe-inline'",
    "img-src 'self' data:",
    "font-src 'self'",
    "connect-src 'self'",
    "worker-src 'self'",
    "object-src 'none'",
    "base-uri 'self'",
    "frame-ancestors 'none'",
    "form-action 'self'",
  ].join('; ')

  setResponseHeader(event, 'Content-Security-Policy', csp)
})
