// DatePicker/TimePicker(9-2절①)와 네이티브 <input :lang> 둘 다 BCP-47 태그가 필요하다.
// nuxt.config.ts의 locales[].language를 그대로 재사용 — 공개(index.vue)·관리자(useOpsLocale) 양쪽 다
// 동일한 useI18n() locale 상태를 쓰므로 이 로직 하나로 충분하다.
export function useInputLang() {
  const { locale, locales } = useI18n()
  return computed(() => locales.value.find(l => l.code === locale.value)?.language ?? locale.value)
}
