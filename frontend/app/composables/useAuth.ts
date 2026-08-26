interface UserDto {
  id: number
  email: string
  role: 'Admin' | 'HospitalManager' | 'Consultant'
  name: string
  locale: string
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
          const event = useRequestEvent()
          const refreshRes = await $fetch.raw(`${baseURL}/api/auth/refresh`, { method: 'POST', headers })
          const setCookies = typeof refreshRes.headers.getSetCookie === 'function'
            ? refreshRes.headers.getSetCookie()
            : (refreshRes.headers.get('set-cookie') ? [refreshRes.headers.get('set-cookie') as string] : [])

          if (event && setCookies.length > 0) {
            for (const cookie of setCookies) appendResponseHeader(event, 'set-cookie', cookie)
          }
          const newCookieStr = setCookies.map(c => c.split(';')[0]).join('; ')
          if (!newCookieStr) { user.value = null; return }

          const mergedCookie = headers.cookie ? `${headers.cookie}; ${newCookieStr}` : newCookieStr
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
