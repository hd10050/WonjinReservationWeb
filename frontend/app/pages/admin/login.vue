<template>
  <div class="flex min-h-screen items-center justify-center bg-background px-4">
    <Card class="w-full max-w-sm">
      <CardHeader>
        <div class="flex items-center justify-between gap-2">
          <CardTitle>{{ t('admin.login.title') }}</CardTitle>
          <!-- 디자인 원칙(절대 원칙) — select에는 보이는 label 필수. admin.vue 상단바와 동일 패턴,
               로그인 전이라 계정 저장(PATCH /api/auth/me/locale) 대신 wj_lang 쿠키에만 반영한다
               (useOpsLocale.ts가 로그인 전 이 쿠키를 그대로 읽음). -->
          <div class="flex items-center gap-1">
            <label for="login-locale-select" class="text-xs text-muted-foreground">{{ t('admin.common.language') }}</label>
            <select
              id="login-locale-select"
              :value="locale"
              class="rounded-md border bg-background px-2 py-1 text-xs text-foreground"
              @change="onLocaleChange"
            >
              <option v-for="loc in locales" :key="loc.code" :value="loc.code">{{ loc.name }}</option>
            </select>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <form class="flex flex-col gap-4" @submit.prevent="submit">
          <div class="flex flex-col gap-2">
            <Label for="email">{{ t('admin.login.email') }}</Label>
            <Input id="email" v-model="email" type="email" maxlength="254" required autocomplete="username" />
          </div>
          <div class="flex flex-col gap-2">
            <Label for="password">{{ t('admin.login.password') }}</Label>
            <Input id="password" v-model="password" type="password" maxlength="64" required autocomplete="current-password" />
          </div>
          <p v-if="errorMessage" class="text-sm text-destructive">{{ errorMessage }}</p>
          <Button type="submit" :disabled="submitting">{{ t('admin.login.submit') }}</Button>
        </form>
      </CardContent>
    </Card>
  </div>
</template>

<script setup lang="ts">
// 관리자 화면은 URL 프리픽스 라우팅에서 제외한다(5-4절) — 로케일별 /ko/admin/login 등이 생기지 않게.
definePageMeta({ middleware: 'admin', layout: false, i18n: false })
useHead({ meta: [{ name: 'robots', content: 'noindex, nofollow' }] })

const { t, locale, locales, setLocale } = useI18n()
await useOpsLocale()
const wjLang = useCookie<string | null>('wj_lang')

const { user, login } = useAuth()
const email = ref('')
const password = ref('')
const submitting = ref(false)
const errorMessage = ref('')

// 로그인 전이라 계정에 저장할 수 없어 wj_lang 쿠키에만 반영(useOpsLocale.ts가 다음 방문 시 이 값을 읽음).
async function onLocaleChange(e: Event) {
  const code = (e.target as HTMLSelectElement).value
  await setLocale(code)
  wjLang.value = code
}

// 이미 로그인한 사용자가 로그인 페이지로 들어오는 처리는 여기서 한다(6-3절) —
// 별도의 게스트 전용 미들웨어는 만들지 않는다.
onMounted(() => {
  if (user.value) navigateTo('/admin')
})

async function submit() {
  errorMessage.value = ''
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
