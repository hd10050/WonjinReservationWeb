<template>
  <div class="min-h-screen bg-background">
    <header class="flex items-center justify-between border-b bg-card px-6 py-3">
      <span class="font-semibold text-foreground">{{ t('common.appName') }} Admin</span>
      <div v-if="user" class="flex items-center gap-3 text-sm text-muted-foreground">
        <span>{{ user.name }} · {{ user.role }}</span>
        <Button variant="outline" size="sm" @click="logout">{{ t('admin.common.logout') }}</Button>
      </div>
    </header>
    <main class="p-6">
      <slot />
    </main>
  </div>
</template>

<script setup lang="ts">
// 관리자 레이아웃에 noindex를 한 번만 부착해 전 하위 페이지에 자동 적용한다(12-3절).
useHead({ meta: [{ name: 'robots', content: 'noindex, nofollow' }] })

const { t } = useI18n()
const { user, logout } = useAuth()
await useOpsLocale()

// 세션 만료·로그아웃으로 user가 null이 되는 순간 로그인 페이지로 이동한다.
// logout() 호출부(위 버튼)는 이동 책임을 지지 않는다 — 여기 watch가 유일한 이탈 경로다.
watch(user, (v) => {
  if (v === null) window.location.href = '/admin/login'
})
</script>
