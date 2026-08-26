// 관리자 화면(i18n:false)의 표시 언어를 계정 locale로 통일하는 전용 컴포저블(5-4절).
// 로그인 전(계정을 아직 모름)에는 wj_lang 쿠키 값을, 없으면 한국어를 기본으로 쓴다 —
// 실장·병원관리자가 한국에서 근무하는 전제(5-4절 로그인 화면 예외).
//
// 🔴 반드시 <script setup> 최상위에서 await로 호출할 것(13-1절 SSR 프리로드 원칙과 동일 이유).
// setLocale()은 비동기이고 lazy 로케일 메시지 파일을 새로 불러온다 — watch(..., {immediate:true})의
// 콜백은 setup 함수가 완료를 기다려주지 않으므로, SSR 렌더링이 로케일 전환보다 먼저 끝나
// 항상 기본 로케일(zh-CN)로 응답이 나가버린다(실측 확인).
export async function useOpsLocale() {
  const { user } = useAuth()
  const { locale, setLocale } = useI18n()
  const wjLang = useCookie<string | null>('wj_lang')

  const target = user.value?.locale ?? wjLang.value ?? 'ko'
  if (target !== locale.value) await setLocale(target)

  // 로그인 후 계정 locale이 바뀌는 경우(관리자가 다른 계정의 locale을 변경한 경우 등)를 대비한 반응형 갱신
  watch(() => user.value?.locale, async (v) => {
    if (v && v !== locale.value) await setLocale(v)
  })
}
