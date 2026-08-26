// 페이지 전환 차단 오버레이 상태(13-2절). 카운터인 이유 — 전환이 겹칠 때 boolean 하나면
// 먼저 끝난 전환이 오버레이를 꺼버린다. increment/decrement만 쓰고 절대 boolean으로 대입하지 않는다.
export function useRouteOverlay() {
  const pending = useState('route-overlay:pending', () => 0)
  function increment() { pending.value++ }
  function decrement() { pending.value = Math.max(0, pending.value - 1) }
  return { pending, increment, decrement }
}
