// 인증 필요 POST/PUT/PATCH/DELETE는 예외 없이 이 함수로만 호출한다(7-4절).
// raw $fetch를 직접 쓰면 401이 자동 복구되지 않아 "화면 이동은 되는데 데이터가 조용히 안 바뀌는" 버그가 된다.
export function useAuthFetch() {
  const { tryRefresh } = useTokenRefresh()
  const { user } = useAuth()

  async function authFetch<T>(url: string, opts?: Parameters<typeof $fetch>[1]): Promise<T> {
    try {
      return await $fetch<T>(url, { credentials: 'include', ...opts })
    } catch (e: any) {
      const status = e?.status ?? e?.response?.status
      if (status !== 401) throw e

      const ok = await tryRefresh()
      if (!ok) {
        user.value = null
        window.location.href = '/admin/login'
        throw e
      }
      return await $fetch<T>(url, { credentials: 'include', ...opts })
    }
  }

  return { authFetch }
}
