interface UserDto {
  id: number
  email: string
  role: 'Admin' | 'HospitalManager' | 'Consultant'
  name: string
  locale: string
}

// 🔴 보안감사(2026-08-26) 발견 — SSR에서 fetchMe()와 useApi()가 401을 맞으면 각자 독립적으로
// /api/auth/refresh를 호출했다. RT는 1회용 로테이션(AuthController.Refresh — 갱신마다 기존 폐기 +
// 신규 발급)이라, 같은 SSR 요청 안에서 fetchMe가 먼저 성공하면 그 RT는 이미 폐기되고, 뒤이은
// useApi의 재조회 요청은 원본(옛) 쿠키를 다시 읽어(useRequestHeaders는 응답 헤더 변경을 모른다)
// 이미 폐기된 RT로 refresh를 재시도하다 401째 실패한다 — 로그인 상태인데 페이지 데이터만 조용히
// 에러로 뜨는 버그. useState는 SSR 요청마다 격리되므로, 이번 요청에서 이미 얻은 갱신 쿠키를 여기
// 공유해 두 번째 이후 호출은 재요청 없이 그대로 재사용한다.
export async function ssrRefreshCookie(baseURL: string, headers: Record<string, string>): Promise<string | null> {
  const cached = useState<string | null>('auth:ssrRefreshedCookie', () => null)
  if (cached.value) return cached.value

  // 🔴 web-security-audit-guide.md 6장 재감사(2026-08-27) 발견 — 이 SSR 직접호출은 Origin이
  // 원천적으로 없는데 내부시크릿도 안 보내고 있었다. 백엔드 CSRF 미들웨어가 Origin 없는 요청을
  // 이제 이 시크릿으로만 신뢰하므로 함께 실어야 한다(server/api/[...].ts와 동일 패턴).
  const config = useRuntimeConfig()
  const secretHeaders = config.internalSecret
    ? { ...headers, 'x-internal-secret': config.internalSecret as string }
    : headers

  const event = useRequestEvent()
  const refreshRes = await $fetch.raw(`${baseURL}/api/auth/refresh`, { method: 'POST', headers: secretHeaders })
  const setCookies = typeof refreshRes.headers.getSetCookie === 'function'
    ? refreshRes.headers.getSetCookie()
    : (refreshRes.headers.get('set-cookie') ? [refreshRes.headers.get('set-cookie') as string] : [])
  if (event && setCookies.length > 0) {
    for (const cookie of setCookies) appendResponseHeader(event, 'set-cookie', cookie)
  }
  const newCookieStr = setCookies.map(c => c.split(';')[0]).join('; ')
  if (!newCookieStr) return null

  const merged = headers.cookie ? `${headers.cookie}; ${newCookieStr}` : newCookieStr
  cached.value = merged
  return merged
}

export function useAuth() {
  const user = useState<UserDto | null>('auth:user', () => null)
  const isLoggedIn = computed(() => user.value !== null)

  async function fetchMe() {
    const config = useRuntimeConfig()
    const baseURL = import.meta.server && config.apiBaseInternal
      ? (config.apiBaseInternal as string)
      : (config.public.apiBase as string)

    const headers: Record<string, string> = {}
    if (import.meta.server) {
      const reqHeaders = useRequestHeaders(['cookie'])
      if (reqHeaders.cookie) headers.cookie = reqHeaders.cookie
    }

    const doFetch = (customHeaders?: Record<string, string>) =>
      $fetch<UserDto>(`${baseURL}/api/auth/me`, { credentials: 'include', headers: customHeaders ?? headers })

    try {
      user.value = await doFetch()
    } catch (e: any) {
      const status = e?.status ?? e?.response?.status
      if (!status) return // 네트워크 오류 — 기존 상태 유지

      if (status === 401 && import.meta.client) {
        const { tryRefresh } = useTokenRefresh()
        const ok = await tryRefresh()
        user.value = ok ? await doFetch().catch(() => null) : null
        return
      }

      if (status === 401 && import.meta.server) {
        // AT 만료(15분) 상태로 새로고침한 경우 — RT로 갱신해 새 AT를 SSR 응답에 실어 보낸다.
        // 안 하면 새로고침할 때마다 15분 지난 세션이 로그인 페이지로 튕긴다.
        try {
          const mergedCookie = await ssrRefreshCookie(baseURL, headers)
          if (!mergedCookie) { user.value = null; return }
          user.value = await doFetch({ ...headers, cookie: mergedCookie })
        } catch {
          user.value = null
        }
        return
      }

      user.value = null
    }
  }

  async function login(email: string, password: string) {
    const config = useRuntimeConfig()
    const res = await $fetch<UserDto>(`${config.public.apiBase}/api/auth/login`, {
      method: 'POST',
      credentials: 'include',
      body: { email, password },
    })
    user.value = res
    return res
  }

  // 호출부는 이 함수를 그대로 이벤트 핸들러에 바인딩만 할 것 — 뒤에 navigateTo를 붙이지 말 것.
  // 로그인 필요 페이지에서의 이탈은 admin.vue 레이아웃의 watch(user)가 담당한다.
  async function logout() {
    const config = useRuntimeConfig()
    user.value = null
    // fire-and-forget 금지 — 응답으로 AT 쿠키가 실제로 삭제되기 전까지는 그 AT가 여전히 유효하다.
    await $fetch(`${config.public.apiBase}/api/auth/logout`, {
      method: 'POST',
      credentials: 'include',
      timeout: 3000,
    }).catch(() => {})
  }

  return { user, isLoggedIn, fetchMe, login, logout }
}
