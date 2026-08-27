<template>
  <div class="flex min-h-screen flex-col bg-background">
    <!-- 공개 랜딩 헤더 공유(12-2절, 2026-08-28) — 언어 선택은 로그인 페이지 기존 방식(setLocale + wj_lang raw 쿠키)으로 처리. -->
    <LandingHeader @select-locale="onLocaleChange" />
    <div class="flex flex-1 items-center justify-center px-4 py-10">
      <Card class="w-full max-w-sm">
        <CardHeader>
          <CardTitle>{{ t('admin.login.title') }}</CardTitle>
        </CardHeader>
        <CardContent>
          <!-- novalidate — 브라우저 기본 검증 팝업(브라우저/OS 언어를 따름)을 끄고 아래 커스텀 검증으로 대체한다. -->
          <form class="flex flex-col gap-4" novalidate @submit.prevent="submit">
            <div class="flex flex-col gap-2">
              <Label for="email">{{ t('admin.login.email') }}</Label>
              <Input id="email" v-model="email" type="email" maxlength="254" required autocomplete="username" :aria-invalid="errors.email || errors.emailFormat" />
              <p v-if="errors.email" class="text-sm text-destructive">{{ t('common.fieldRequired') }}</p>
              <p v-else-if="errors.emailFormat" class="text-sm text-destructive">{{ t('admin.login.invalidEmail') }}</p>
            </div>
            <div class="flex flex-col gap-2">
              <Label for="password">{{ t('admin.login.password') }}</Label>
              <Input id="password" v-model="password" type="password" maxlength="64" required autocomplete="current-password" :aria-invalid="errors.password" />
              <p v-if="errors.password" class="text-sm text-destructive">{{ t('common.fieldRequired') }}</p>
            </div>
            <p v-if="errorMessage" class="text-sm text-destructive">{{ errorMessage }}</p>
            <Button type="submit" :disabled="submitting">{{ t('admin.login.submit') }}</Button>
          </form>
        </CardContent>
      </Card>
    </div>
  </div>
</template>

<script setup lang="ts">
// 관리자 화면은 URL 프리픽스 라우팅에서 제외한다(5-4절) — 로케일별 /ko/admin/login 등이 생기지 않게.
definePageMeta({ middleware: 'admin', layout: false, i18n: false })
useHead({ meta: [{ name: 'robots', content: 'noindex, nofollow' }] })

const { t, setLocale } = useI18n()
await useOpsLocale()

const { user, login } = useAuth()
const email = ref('')
const password = ref('')
const submitting = ref(false)
const errorMessage = ref('')

// 로그인 전이라 계정에 저장할 수 없어 wj_lang 쿠키에만 반영(useOpsLocale.ts가 다음 방문 시 읽음).
// 🔴 markManualLocale은 raw document.cookie로 쓴다 — landing.vue와 같은 헤더 컴포넌트를 공유하므로
// 두 곳이 동일 포맷이어야 하고, <head> 인라인 감지 스크립트도 raw 값을 파싱한다(5-3절).
async function onLocaleChange(code: string) {
  await setLocale(code)
  markManualLocale(code)
}

// 이미 로그인한 사용자가 로그인 페이지로 들어오는 처리는 여기서 한다(6-3절) —
// 별도의 게스트 전용 미들웨어는 만들지 않는다.
onMounted(() => {
  if (user.value) navigateTo('/admin')
})

// 브라우저 기본 검증(novalidate로 비활성화, 위 템플릿 참고)을 대체하는 커스텀 검증.
const errors = reactive({ email: false, emailFormat: false, password: false })

function validate(): boolean {
  const emailTrimmed = email.value.trim()
  errors.email = !emailTrimmed
  errors.emailFormat = !errors.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(emailTrimmed)
  errors.password = !password.value
  return !errors.email && !errors.emailFormat && !errors.password
}

async function submit() {
  errorMessage.value = ''
  if (!validate()) return
  submitting.value = true
  try {
    await login(email.value, password.value)
    await navigateTo('/admin')
  } catch (e: any) {
    const code = (e?.data?.code as string | undefined) ?? 'INVALID_CREDENTIALS'
    errorMessage.value = t(`errors.${code}`)
  } finally {
    submitting.value = false
  }
}
</script>
