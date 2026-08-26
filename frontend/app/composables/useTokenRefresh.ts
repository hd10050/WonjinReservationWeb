// 모듈 레벨 singleton — 동시에 여러 401이 발생해도 refresh는 1회만 실행한다(7-4절).
let refreshPromise: Promise<boolean> | null = null

export function useTokenRefresh() {
  async function tryRefresh(): Promise<boolean> {
    if (refreshPromise) return refreshPromise

    refreshPromise = (async () => {
      try {
        await $fetch('/api/auth/refresh', { method: 'POST', credentials: 'include' })
        return true
      } catch {
        return false
      } finally {
        refreshPromise = null
      }
    })()

    return refreshPromise
  }

  return { tryRefresh }
}
