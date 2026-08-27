<template>
  <!-- 12-3절 — 사이드바 220px 고정 + 상단바(사용자·역할·언어·로그아웃) + 본문. 폭 숫자는
       --sidebar-w 변수 하나로만 정의해 두 소비처(사이드바 폭·본문 오프셋)가 항상 일치하게 한다. -->
  <div class="min-h-screen bg-background" style="--sidebar-w: 220px">
    <!-- 🔴 Tailwind 임의값(md:translate-x-0 계열)을 :class 삼항연산자로 토글하면 실제로 안 먹는 것을
         실측으로 확인(빌드된 CSS에 해당 클래스 규칙 자체가 안 생김) — 순수 CSS로 직접 처리한다. -->
    <aside
      class="admin-sidebar fixed inset-y-0 left-0 z-40 flex w-[var(--sidebar-w)] flex-col overflow-y-auto border-r bg-card transition-transform duration-150"
      :class="{ 'admin-sidebar--open': mobileNavOpen }"
    >
      <NuxtLink to="/admin" class="flex items-center gap-2 border-b px-4 py-4">
        <img src="/logo.svg" :alt="t('common.appName')" class="h-12 w-auto shrink-0">
      </NuxtLink>
      <nav class="flex-1 space-y-0.5 px-3 py-4 text-sm">
        <NuxtLink
          v-for="link in navLinks" :key="link.to" :to="link.to"
          class="flex items-center gap-2.5 rounded-lg px-3 py-2.5 font-medium transition-colors"
          :class="isActive(link) ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'"
          @click="mobileNavOpen = false"
        >
          <component :is="link.icon" class="size-4 shrink-0" />
          {{ link.label }}
        </NuxtLink>
      </nav>
    </aside>

    <!-- 768px 이하 전용 오버레이 — 사이드바 밖을 클릭하면 닫힘 -->
    <div
      v-if="mobileNavOpen"
      class="fixed inset-0 z-30 bg-black/20 md:hidden"
      aria-hidden="true"
      @click="mobileNavOpen = false"
    />

    <div class="flex min-h-screen flex-col md:ms-[var(--sidebar-w)]">
      <header class="flex items-center justify-between border-b bg-card px-4 py-3 md:px-6">
        <div class="flex items-center gap-3">
          <button
            type="button"
            class="rounded-md p-1.5 text-muted-foreground hover:bg-accent hover:text-accent-foreground md:hidden"
            :aria-label="t('admin.common.menu')"
            @click="mobileNavOpen = !mobileNavOpen"
          >
            <Menu class="size-5" />
          </button>
        </div>
        <div v-if="user" class="flex items-center gap-3 text-sm text-muted-foreground">
          <!-- 디자인 원칙(절대 원칙) — select에는 보이는 label 필수, aria-label만으로 대체 금지 -->
          <div class="flex items-center gap-1">
            <label for="admin-locale-select" class="text-xs">{{ t('admin.common.language') }}</label>
            <!-- 🔴 v-model 직접 대입 금지(로케일 절대 원칙) — :value+@change로 setLocale()만 거치게 한다 -->
            <select
              id="admin-locale-select"
              :value="locale"
              class="rounded-md border bg-background px-2 py-1 text-xs text-foreground"
              @change="onLocaleChange"
            >
              <option v-for="loc in locales" :key="loc.code" :value="loc.code">{{ loc.name }}</option>
            </select>
          </div>
          <!-- 새 예약 웹 푸시 토글 — 노출 조건은 isSupported뿐(5-5절, granted로만 게이팅하면
               default·denied 유저는 여기서 켤 방법이 사라진다). ClientOnly로 감싸는 이유는 이
               v-if가 브라우저 전용 값으로 엘리먼트 존재 자체를 게이팅해 hydration mismatch를
               일으키기 때문(5-3절 — 텍스트 보간과 다른 종류의 문제). -->
          <ClientOnly>
            <button
              v-if="isSupported"
              type="button"
              class="rounded-md p-1.5 text-muted-foreground hover:bg-accent hover:text-accent-foreground"
              :aria-label="pushButtonLabel"
              :title="pushButtonLabel"
              @click="onTogglePush"
            >
              <BellOff v-if="permission === 'denied'" class="size-4" />
              <Bell v-else class="size-4" :class="{ 'fill-current text-primary': isSubscribed }" />
            </button>
          </ClientOnly>
          <!-- 2026-08-27 — 계정 정보를 배지로 구분 + 로그아웃 버튼 바로 왼쪽으로 배치 -->
          <span class="hidden items-center rounded-full border border-border bg-accent px-3 py-1 text-xs font-medium text-accent-foreground sm:inline-flex">
            {{ user.name }} · {{ user.role }}
          </span>
          <Button variant="outline" size="sm" @click="logout">{{ t('admin.common.logout') }}</Button>
        </div>
      </header>
      <main class="flex-1 p-6">
        <slot />
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
// 어드민 사이드바 네비게이션(12-3절) — 병합 시점 일괄 정리 항목이었던 것을 이 세션에서 구현.
// 메뉴 구성은 루트 CLAUDE.md 역할×메뉴 권한표 그대로: Consultant는 대시보드·달력만,
// HospitalManager는 계정관리·로그·유입경로만 빠짐, Admin은 전부.
import {
  BarChart3, Bell, BellOff, CalendarDays, FileClock, LayoutDashboard, LineChart, Menu, Stethoscope, Tag, UserCog, Users,
} from '@lucide/vue'

useHead({ meta: [{ name: 'robots', content: 'noindex, nofollow' }] })

const { t, locale, locales, setLocale } = useI18n()
const { user, logout } = useAuth()
const { authFetch } = useAuthFetch()
const config = useRuntimeConfig()
const route = useRoute()
await useOpsLocale()

const mobileNavOpen = ref(false)

// 새 예약 접수 웹 푸시(어드민 전용, 2026-08-27) — 항상 켜져있는 종 아이콘 하나로 상태별 분기.
const { isSupported, permission, isSubscribed, refreshStatus, subscribe, unsubscribe } = usePush()
if (import.meta.client) refreshStatus()

const pushButtonLabel = computed(() => {
  if (permission.value === 'denied') return t('admin.common.pushDenied')
  return isSubscribed.value ? t('admin.common.pushOn') : t('admin.common.pushOff')
})

async function onTogglePush() {
  if (permission.value === 'denied') return // 재요청해도 네이티브 팝업이 다시 안 뜬다(5-4절) — 안내는 title로 대체
  if (isSubscribed.value) await unsubscribe()
  else await subscribe()
}

// 예약 확정 시 [예약 달력] 조용히 새로고침용 SSE(2026-08-27, 스파이크 테스트 완료) — 레이아웃이
// 전 어드민 페이지 공통이라 여기서 한 번만 연결하고 페이지 이동엔 영향받지 않는다. 새 예약 접수는
// 이 채널을 안 타고 별도 웹 푸시로 처리한다(브라우저를 닫아도 받아야 하므로).
const reservationConfirmedTick = useState('sse:reservationConfirmedTick', () => 0)
if (import.meta.client) {
  const es = new EventSource(`${config.public.apiBase}/api/admin/events`)
  es.addEventListener('reservation_confirmed', () => { reservationConfirmedTick.value++ })
  onScopeDispose(() => es.close())
}

const NAV_ITEMS = [
  { to: '/admin', labelKey: 'admin.nav.dashboard', exact: true, icon: LayoutDashboard, roles: ['Admin', 'HospitalManager', 'Consultant'] },
  { to: '/admin/calendar', labelKey: 'admin.nav.calendar', icon: CalendarDays, roles: ['Admin', 'HospitalManager', 'Consultant'] },
  { to: '/admin/consultants', labelKey: 'admin.nav.consultants', icon: Users, roles: ['Admin', 'HospitalManager'] },
  { to: '/admin/procedures', labelKey: 'admin.nav.procedures', icon: Stethoscope, roles: ['Admin', 'HospitalManager'] },
  { to: '/admin/kpi', labelKey: 'admin.nav.kpi', icon: BarChart3, roles: ['Admin', 'HospitalManager'] },
  { to: '/admin/stats', labelKey: 'admin.nav.stats', icon: LineChart, roles: ['Admin', 'HospitalManager'] },
  { to: '/admin/users', labelKey: 'admin.nav.users', icon: UserCog, roles: ['Admin'] },
  { to: '/admin/audit-logs', labelKey: 'admin.nav.auditLogs', icon: FileClock, roles: ['Admin'] },
  { to: '/admin/referrals', labelKey: 'admin.nav.referrals', icon: Tag, roles: ['Admin'] },
]

const navLinks = computed(() => {
  const role = user.value?.role
  if (!role) return []
  return NAV_ITEMS.filter(i => i.roles.includes(role)).map(i => ({ ...i, label: t(i.labelKey) }))
})

function isActive(link: { to: string, exact?: boolean }) {
  // 예약 상세(/admin/reservations/[id])는 대시보드의 하위 페이지지만 exact 매칭 밖에 있어
  // 별도로 챙겨야 사이드바가 상세 화면에서도 [예약 대시보드]로 표시된다.
  if (link.to === '/admin') return route.path === '/admin' || route.path.startsWith('/admin/reservations/')
  return link.exact ? route.path === link.to : route.path === link.to || route.path.startsWith(`${link.to}/`)
}

// 화면 전환은 setLocale()로만(직접 대입 금지, 5-4절) + 계정 locale 서버 저장.
// 저장 실패해도 화면 언어 전환 자체는 막지 않는다(MeiyantongWeb 패턴과 동일).
async function onLocaleChange(e: Event) {
  const code = (e.target as HTMLSelectElement).value
  await setLocale(code)
  try {
    const updated = await authFetch<{ id: number, email: string, role: string, name: string, locale: string }>(
      `${config.public.apiBase}/api/auth/me/locale`,
      { method: 'PATCH', body: { locale: code } },
    )
    if (user.value) user.value.locale = updated.locale
  } catch { /* 계정 저장 실패는 무시 — 화면 언어 전환은 이미 완료됨 */ }
}

// 세션 만료·로그아웃으로 user가 null이 되는 순간 로그인 페이지로 이동한다.
// logout() 호출부(위 버튼)는 이동 책임을 지지 않는다 — 여기 watch가 유일한 이탈 경로다.
watch(user, (v) => {
  if (v === null) window.location.href = '/admin/login'
})
</script>

<style scoped>
/* 12-3절 — 768px 이하는 translateX(-100%)로 숨김, 열림 시 0. 768px 이상은 항상 표시(고정 사이드바). */
.admin-sidebar {
  transform: translateX(-100%);
}
.admin-sidebar--open {
  transform: translateX(0);
}
@media (min-width: 768px) {
  .admin-sidebar {
    transform: translateX(0);
  }
}
</style>
