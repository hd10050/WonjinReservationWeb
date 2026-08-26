<template>
  <div class="flex min-h-screen items-center justify-center bg-background px-4">
    <Card class="w-full max-w-sm">
      <CardHeader>
        <CardTitle>{{ t('admin.login.title') }}</CardTitle>
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

const { t } = useI18n()
await useOpsLocale()

const { user, login } = useAuth()
const email = ref('')
const password = ref('')
const submitting = ref(false)
const errorMessage = ref('')

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
