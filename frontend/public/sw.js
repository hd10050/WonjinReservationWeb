// 웹 푸시 전용 Service Worker(새 예약 접수 알림, 어드민 전용). 캐싱·오프라인 기능은 넣지 않는다 —
// PWA 전체를 도입하는 게 아니라 web-push-notification-guide.md 5-1절의 웹푸시 전용 SW다.
self.addEventListener('push', (event) => {
  const data = event.data ? event.data.json() : {}
  event.waitUntil(
    self.registration.showNotification(data.title || '알림', {
      body: data.body || '',
      icon: '/logo.svg',
      data: { url: data.url || '/admin' },
    }),
  )
})

self.addEventListener('notificationclick', (event) => {
  event.notification.close()
  const url = event.notification.data?.url || '/admin'
  event.waitUntil(
    clients.matchAll({ type: 'window', includeUncontrolled: true }).then((windowClients) => {
      for (const client of windowClients) {
        if ('focus' in client) {
          client.focus()
          if ('navigate' in client) client.navigate(url)
          return
        }
      }
      return clients.openWindow(url)
    }),
  )
})
