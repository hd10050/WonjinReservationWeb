// frontend/app/composables/useScrollReveal.ts
// 스크롤 리빌(2026-08-28, 랜딩 비주얼 리디자인 8절) — IntersectionObserver + tw-animate-css 유틸리티만
// 사용, 신규 라이브러리 없음. revealed 기본값은 true(화면 깜빡임 금지 원칙) — JS가 실패하거나
// prefers-reduced-motion이면 콘텐츠는 항상 보이는 상태로 남고, 클라이언트에서 관찰이 시작될 때만
// 잠깐 숨겼다가 뷰포트 진입 시 다시 보여준다.
export function useScrollReveal() {
  const target = ref<HTMLElement | null>(null)
  const revealed = ref(true)

  onMounted(() => {
    const el = target.value
    if (!el || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return

    revealed.value = false
    const observer = new IntersectionObserver(([entry]) => {
      if (entry.isIntersecting) {
        revealed.value = true
        observer.disconnect()
      }
    }, { threshold: 0.15 })
    observer.observe(el)

    onUnmounted(() => observer.disconnect())
  })

  return { target, revealed }
}
