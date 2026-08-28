// 브라우저의 모든 /api/* 요청을 백엔드로 그대로 프록시한다(D7, 4-1절).
// 목적: 인증 쿠키가 항상 프론트 도메인 자체 쿠키(퍼스트파티)로 취급되게 해
// 화면 깜빡임 금지 원칙을 SSR 프리로드로 이행할 수 있게 하는 전제(13-1절).
export default defineEventHandler(async (event) => {
  const config = useRuntimeConfig()
  const target = (config.apiBaseInternal as string) || (config.public.apiBase as string)
  if (!target) throw createError({ statusCode: 500, statusMessage: 'API base 미설정' })

  // 프록시를 그냥 통과시키면 백엔드가 보는 접속 IP가 Nitro 서버 자신의 IP가 되어버려
  // IP 기반 rate limit(7-2·7-5절)이 전체 방문자에 하나로 뭉개진다.
  // 클라이언트 IP는 Cloudflare가 이 프론트(Workers)로 들어오는 요청에 직접 설정하는 위조 불가
  // 헤더(cf-connecting-ip)를 먼저 읽고, 없을 때(로컬 dev 등)만 getRequestIP로 폴백한다
  // (web-security-audit-guide.md 3-2절 — getRequestIP의 xForwardedFor 옵션은 브라우저가 실은 값을
  // 그대로 읽을 수 있어 단독 사용 금지).
  const clientIp = getRequestHeader(event, 'cf-connecting-ip')
    || getRequestIP(event, { xForwardedFor: true })

  // 🔴 2026-08-28 재수정 — 프론트→백엔드 relay 헤더 이름을 `cf-connecting-ip` 그대로 쓰면 안 된다.
  // 실측 확인(`/api/internal/debug-ip` 임시 진단): Render(onrender.com)도 Cloudflare 엣지 뒤에 있어서,
  // 이 프록시가 실어보낸 cf-connecting-ip 값은 Render 앞단 Cloudflare 엣지가 "위조 방지"를 위해
  // 항상 실제 TCP 접속(Workers의 아웃바운드 IP, PoP마다 달라짐)으로 덮어써버린다 — 이름이 Cloudflare
  // 예약 헤더와 같으면 어느 Cloudflare 존을 거치든 재작성 대상이 된다. 예약되지 않은 커스텀 헤더
  // 이름(x-wj-client-ip)으로 보내야 Render 앞단에서 건드리지 않고 그대로 통과한다. 백엔드
  // Program.cs GetClientIp()·AuditLogFilter도 동일 헤더 이름으로 함께 수정.
  const headers: Record<string, string> = {}
  headers['x-wj-client-ip'] = clientIp || ''
  if (config.internalSecret) headers['x-internal-secret'] = config.internalSecret as string

  return proxyRequest(event, `${target}${event.path}`, {
    headers,
    // 🔴 redirect:'manual' 필수 — 기본값(follow)이면 이 프록시 서버 자신의 fetch가 백엔드의
    // 3xx 응답을 대신 소비해버려, 리다이렉트 대상 응답을 "우리 프론트 도메인이 응답한 200"으로
    // 그대로 브라우저에 돌려주게 된다(auth-pattern-reference.md 6-11절, 버그 #13-1).
    fetchOptions: { redirect: 'manual' },
  })
})
