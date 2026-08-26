// 인증 필요 GET은 예외 없이 이 함수로만 호출한다(7-4절). SSR 프리로드(13-1절)의 유일한 GET 경로 —
// <script setup> 최상위에서 await로 호출해야 화면 깜빡임 금지 원칙이 성립한다(D7 동일 출처 프록시 전제).
// url/query가 반응형이면 값이 바뀔 때마다 자동 재조회한다(라우트 파라미터·URL 쿼리 필터용, 12-4절).
export function useApi<T>(
  url: MaybeRefOrGetter<string>,
  opts?: { query?: MaybeRefOrGetter<Record<string, unknown> | undefined> },
) {
  const config = useRuntimeConfig()
  const { user } = useAuth()

  async function fetchOnce(headers: Record<string, string>) {
    const baseURL = import.meta.server && config.apiBaseInternal
      ? (config.apiBaseInternal as string)
      : (config.public.apiBase as string)
    return await $fetch<T>(toValue(url), {
      baseURL, credentials: 'include', headers, query: toValue(opts?.query),
    })
  }

  async function fetcher(): Promise<T> {
    const headers: Record<string, string> = {}
    if (import.meta.server) {
      const reqHeaders = useRequestHeaders(['cookie'])
      if (reqHeaders.cookie) headers.cookie = reqHeaders.cookie
    }

    try {
      return await fetchOnce(headers)
    } catch (e: any) {
      const status = e?.status ?? e?.response?.status
      if (status !== 401) throw e

      if (import.meta.client) {
        const { tryRefresh } = useTokenRefresh()
        const ok = await tryRefresh()
        if (!ok) { user.value = null; throw e }
        return await fetchOnce(headers)
      }

      // SSR에서 AT 만료 — RT로 갱신해 새 쿠키를 응답에 실어 보낸 뒤 재요청(useAuth.ts fetchMe와 동일 패턴)
      // 🔴 fetchOnce()와 동일하게 삼항연산자로 apiBaseInternal 우선 처리 — ||를 쓰면 이 프로젝트가
      // 절대원칙으로 금지하는 패턴(4-1절, public.apiBase=""가 의도된 값인데 falsy로 폴백됨)과
      // 같은 모양이 된다(재감사 발견 — 현재 값으로는 오동작 안 하지만 일관성·재발 방지 위해 통일).
      const baseURL = import.meta.server && config.apiBaseInternal
        ? (config.apiBaseInternal as string)
        : (config.public.apiBase as string)
      const event = useRequestEvent()
      const refreshRes = await $fetch.raw(`${baseURL}/api/auth/refresh`, { method: 'POST', headers })
      const setCookies = typeof refreshRes.headers.getSetCookie === 'function'
        ? refreshRes.headers.getSetCookie()
        : (refreshRes.headers.get('set-cookie') ? [refreshRes.headers.get('set-cookie') as string] : [])
      if (event && setCookies.length > 0) {
        for (const cookie of setCookies) appendResponseHeader(event, 'set-cookie', cookie)
      }
      const newCookieStr = setCookies.map(c => c.split(';')[0]).join('; ')
      if (!newCookieStr) throw e
      const mergedCookie = headers.cookie ? `${headers.cookie}; ${newCookieStr}` : newCookieStr
      return await fetchOnce({ ...headers, cookie: mergedCookie })
    }
  }

  // 🔴 key.value(호출 시점 스냅샷)가 아니라 반응형 key 자체를 넘긴다 — useAsyncData는 Ref<string>을
  // 직접 받아 반응형으로 추적하는데, .value로 풀어 넘기면 이후 url/query가 바뀌어도 캐시 키가 고정된
  // 채로 남는다(재감사 발견 — watch가 재조회 자체는 트리거해 지금까지 관측된 오동작은 없었지만,
  // useAsyncData의 API 계약과 어긋나는 잠재 위험이라 바로잡는다).
  const key = computed(() => `useApi:${toValue(url)}?${JSON.stringify(toValue(opts?.query) ?? {})}`)
  return useAsyncData<T>(key, fetcher, { watch: [() => toValue(url), () => toValue(opts?.query)] })
}
