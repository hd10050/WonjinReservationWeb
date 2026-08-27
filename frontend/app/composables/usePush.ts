// 새 예약 접수 웹 푸시 구독 관리(어드민 전용). web-push-notification-guide.md 5-3절의 검증된
// 패턴을 그대로 따른다 — 이 프로젝트는 비로그인 구독자 개념이 없어(관리자만) 3-5절의 익명→로그인
// 병합 로직은 필요 없다.

function urlBase64ToUint8Array(base64String: string): Uint8Array {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4)
  const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/')
  const rawData = atob(base64)
  return Uint8Array.from([...rawData].map(c => c.charCodeAt(0)))
}

// 기존 구독이 물고 있는 키와 서버의 현재 VAPID 공개키가 같은지 바이트 단위로 비교(VAPID 재발급 대응)
function sameApplicationServerKey(existing: ArrayBuffer | null, currentKey: Uint8Array): boolean {
  if (!existing) return false
  const existingBytes = new Uint8Array(existing)
  if (existingBytes.length !== currentKey.length) return false
  return existingBytes.every((byte, i) => byte === currentKey[i])
}

// 🔴 timeout 옵션이 없는 네이티브 Promise(requestPermission·serviceWorker.ready)용 —
// 특히 Windows/Edge에서 OS 알림 설정 승인이 안 끝나면 응답 없이 무한 대기할 수 있다(5-3절).
function withTimeout<T>(promise: Promise<T>, ms: number, fallback: T): Promise<T> {
  return new Promise((resolve) => {
    let settled = false
    promise.then((v) => { if (!settled) { settled = true; resolve(v) } })
    setTimeout(() => { if (!settled) { settled = true; resolve(fallback) } }, ms)
  })
}

export function usePush() {
  const isSupported = computed(() =>
    import.meta.client && 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window)

  // useState — 여러 컴포넌트(배너·끄기 토글)에서 호출돼도 상태 공유(4-7절, ref()면 각자 따로 생김)
  const permission = useState<NotificationPermission | null>('push-permission', () =>
    import.meta.client && 'Notification' in window ? Notification.permission : null)

  // 🔴 필수 — SSR에선 initFn이 항상 null을 반환해 payload에 굳어버리고, hydration 시 useState는
  // payload 값이 있으면 initFn을 재실행하지 않는다. 클라이언트에서 매번 실제 값으로 재동기화해야
  // permission이 영원히 null로 고정되는 사고를 막는다(실제 발견된 버그, 5-3절).
  if (import.meta.client && 'Notification' in window) {
    permission.value = Notification.permission
  }

  const isSubscribed = useState<boolean | null>('push-is-subscribed', () => null)

  async function refreshStatus() {
    if (!isSupported.value || permission.value !== 'granted') { isSubscribed.value = false; return }
    try {
      const reg = await navigator.serviceWorker.getRegistration('/sw.js')
      const sub = await reg?.pushManager.getSubscription()
      if (sub) {
        // 브라우저엔 구독이 있는데 서버 저장이 예전에 실패했을 수 있음 — 멱등 재전송으로 자가 치유(4-7절)
        const json = sub.toJSON()
        $fetch('/api/admin/push/subscribe', {
          method: 'POST',
          timeout: 3000,
          body: { endpoint: sub.endpoint, p256dh: json.keys?.p256dh ?? '', auth: json.keys?.auth ?? '' },
        }).catch(() => {})
      }
      isSubscribed.value = !!sub
    } catch { isSubscribed.value = false }
  }

  async function subscribe() {
    if (!isSupported.value) return false
    const result = await withTimeout(Notification.requestPermission(), 20000, 'default' as NotificationPermission)
    permission.value = result
    if (result !== 'granted') return false

    const reg = (await withTimeout(navigator.serviceWorker.getRegistration('/sw.js'), 5000, undefined))
      ?? (await navigator.serviceWorker.register('/sw.js'))
    await withTimeout(navigator.serviceWorker.ready, 5000, undefined)

    const { publicKey } = await $fetch<{ publicKey: string }>('/api/admin/push/public-key', { timeout: 3000 })
    const currentKey = urlBase64ToUint8Array(publicKey)
    let sub = await reg.pushManager.getSubscription()

    // 🔴 VAPID 키가 재발급된 뒤에도 옛 구독을 그대로 쓰면 발송이 에러 없이 조용히 영구 실패한다(5-3절)
    if (sub && !sameApplicationServerKey(sub.options.applicationServerKey, currentKey)) {
      await sub.unsubscribe()
      sub = null
    }

    if (!sub) {
      sub = await reg.pushManager.subscribe({ userVisibleOnly: true, applicationServerKey: currentKey })
    }
    const json = sub.toJSON()
    await $fetch('/api/admin/push/subscribe', {
      method: 'POST',
      timeout: 3000,
      body: { endpoint: sub.endpoint, p256dh: json.keys?.p256dh ?? '', auth: json.keys?.auth ?? '' },
    })
    isSubscribed.value = true
    return true
  }

  async function unsubscribe() {
    const reg = await navigator.serviceWorker.getRegistration('/sw.js')
    const sub = await reg?.pushManager.getSubscription()
    if (!sub) { isSubscribed.value = false; return true }
    await sub.unsubscribe()
    await $fetch('/api/admin/push/unsubscribe', { method: 'POST', timeout: 3000, body: { endpoint: sub.endpoint } }).catch(() => {})
    isSubscribed.value = false
    return true
  }

  return { isSupported, permission, isSubscribed, refreshStatus, subscribe, unsubscribe }
}
