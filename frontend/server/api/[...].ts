// 브라우저의 모든 /api/* 요청을 백엔드로 그대로 프록시한다(D7, 4-1절).
// 목적: 인증 쿠키가 항상 프론트 도메인 자체 쿠키(퍼스트파티)로 취급되게 해
// 화면 깜빡임 금지 원칙을 SSR 프리로드로 이행할 수 있게 하는 전제(13-1절).
export default defineEventHandler(async (event) => {
  const config = useRuntimeConfig()
  const target = (config.apiBaseInternal as string) || (config.public.apiBase as string)
  if (!target) throw createError({ statusCode: 500, statusMessage: 'API base 미설정' })

  // 프록시를 그냥 통과시키면 백엔드가 보는 접속 IP가 Nitro 서버 자신의 IP가 되어버려
  // IP 기반 rate limit(7-2·7-5절)이 전체 방문자에 하나로 뭉개진다.
  // 🔴 클라이언트 IP는 Cloudflare가 실제 TCP 접속으로 직접 설정하는 위조 불가 헤더(cf-connecting-ip)를
  // 먼저 읽고, 없을 때(로컬 dev 등)만 getRequestIP로 폴백한다(web-security-audit-guide.md 3-2절 —
  // getRequestIP의 xForwardedFor 옵션은 브라우저가 실은 값을 그대로 읽을 수 있어 단독 사용 금지).
  const clientIp = getRequestHeader(event, 'cf-connecting-ip')
    || getRequestIP(event, { xForwardedFor: true })

  // 🔴 보안감사(2026-08-26) 발견 — 백엔드(Render)는 프론트와 달리 Cloudflare 엣지 뒤가 아니라서,
  // CF-Connecting-IP를 무조건 신뢰하면 이 프록시를 건너뛰고 Render를 직접 호출하는 요청이 헤더를
  // 조작해 Rate Limit을 우회할 수 있다. 프론트·백엔드만 아는 내부시크릿을 모든 프록시 요청에 실어,
  // 백엔드가 "이 요청이 정말 우리 프론트를 거쳐왔는지" 검증한 뒤에만 IP 헤더를 신뢰하게 한다.
  // 🔴 2026-08-28 정정 — 백엔드 GetClientIp()·AuditLogFilter가 읽는 헤더 이름은 CF-Connecting-IP인데
  // 여기선 x-forwarded-for로 보내고 있어(헤더 이름 불일치) 백엔드가 값을 못 찾고 항상 RemoteIpAddress
  // (전 방문자 공통값)로 폴백, "IP당" 제한이 "사이트 전체" 제한으로 뭉개져 있었다. 백엔드가 읽는
  // 이름 그대로 cf-connecting-ip로 보낸다. 값이 없으면 빈 문자열로 덮어써 클라이언트가 실어보낸
  // 위조 cf-connecting-ip가 그대로 전달되지 않게 한다.
  const headers: Record<string, string> = {}
  headers['cf-connecting-ip'] = clientIp || ''
  if (config.internalSecret) headers['x-internal-secret'] = config.internalSecret as string

  return proxyRequest(event, `${target}${event.path}`, {
    headers,
    // 🔴 redirect:'manual' 필수 — 기본값(follow)이면 이 프록시 서버 자신의 fetch가 백엔드의
    // 3xx 응답을 대신 소비해버려, 리다이렉트 대상 응답을 "우리 프론트 도메인이 응답한 200"으로
    // 그대로 브라우저에 돌려주게 된다(auth-pattern-reference.md 6-11절, 버그 #13-1).
    fetchOptions: { redirect: 'manual' },
  })
})
