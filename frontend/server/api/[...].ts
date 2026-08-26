// 브라우저의 모든 /api/* 요청을 백엔드로 그대로 프록시한다(D7, 4-1절).
// 목적: 인증 쿠키가 항상 프론트 도메인 자체 쿠키(퍼스트파티)로 취급되게 해
// 화면 깜빡임 금지 원칙을 SSR 프리로드로 이행할 수 있게 하는 전제(13-1절).
export default defineEventHandler(async (event) => {
  const config = useRuntimeConfig()
  const target = (config.apiBaseInternal as string) || (config.public.apiBase as string)
  if (!target) throw createError({ statusCode: 500, statusMessage: 'API base 미설정' })

  // 프록시를 그냥 통과시키면 백엔드가 보는 접속 IP가 Nitro 서버 자신의 IP가 되어버려
  // IP 기반 rate limit(7-2·7-5절)이 전체 방문자에 하나로 뭉개진다.
  const clientIp = getRequestIP(event, { xForwardedFor: true })

  return proxyRequest(event, `${target}${event.path}`, {
    headers: clientIp ? { 'x-forwarded-for': clientIp } : undefined,
    // 🔴 redirect:'manual' 필수 — 기본값(follow)이면 이 프록시 서버 자신의 fetch가 백엔드의
    // 3xx 응답을 대신 소비해버려, 리다이렉트 대상 응답을 "우리 프론트 도메인이 응답한 200"으로
    // 그대로 브라우저에 돌려주게 된다(auth-pattern-reference.md 6-11절, 버그 #13-1).
    fetchOptions: { redirect: 'manual' },
  })
})
