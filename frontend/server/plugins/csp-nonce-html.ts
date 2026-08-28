// server/middleware/csp-nonce.ts가 심어둔 nonce를 렌더링된 HTML의 모든 <script> 태그(언어감지
// 인라인 스크립트·landing.vue JSON-LD·Nuxt 하이드레이션 페이로드 등)에 부여한다.
// Nuxt 4 공식 예제(https://nuxt.com/docs/4.x/guide/going-further/experimental-features)는 head만
// 다루지만, 하이드레이션 페이로드 등 body 쪽 스크립트도 같은 방식으로 존재할 수 있어 4개 구간을
// 전부 방어적으로 순회한다(없는 구간은 continue로 건너뜀).
export default defineNitroPlugin((nitro) => {
  nitro.hooks.hook('render:html', (ctx, { event }) => {
    const nonce = event.context.cspNonce as string | undefined
    if (!nonce) return

    for (const zone of [ctx.head, ctx.bodyPrepend, ctx.body, ctx.bodyAppend]) {
      if (!zone) continue
      for (let i = 0; i < zone.length; i++) {
        zone[i] = zone[i].replace(/<script(?![^>]*\snonce=)/g, `<script nonce="${nonce}"`)
      }
    }
  })
})
