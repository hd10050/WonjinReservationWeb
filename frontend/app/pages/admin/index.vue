<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-semibold text-foreground">{{ t('admin.reservations.title') }}</h1>
      <Button
        variant="ghost" size="icon"
        :aria-label="t('admin.reservations.refresh')"
        :title="t('admin.reservations.refresh')"
        @click="refreshAll"
      >
        <RefreshCw class="size-4" :class="{ 'animate-spin': dataPending }" />
      </Button>
    </div>

    <div class="grid grid-cols-2 gap-4 md:grid-cols-4">
      <Card
        v-for="c in cards" :key="c.key"
        class="cursor-pointer transition-colors hover:bg-accent/50"
        @click="filterByStatus(c.status)"
      >
        <CardHeader>
          <CardDescription>{{ c.label }}</CardDescription>
        </CardHeader>
        <CardContent>
          <p class="text-3xl font-bold text-primary">{{ c.value }}</p>
        </CardContent>
      </Card>
    </div>

    <Card>
      <CardContent class="flex flex-wrap items-start gap-4">
        <div class="flex flex-col gap-1.5">
          <Label for="f-status">{{ t('admin.reservations.filterStatus') }}</Label>
          <NativeSelect id="f-status" v-model="formStatus">
            <NativeSelectOption value="">{{ t('admin.reservations.filterStatusAll') }}</NativeSelectOption>
            <NativeSelectOption v-for="s in STATUSES" :key="s" :value="s">{{ t(`status.${s}`) }}</NativeSelectOption>
          </NativeSelect>
        </div>
        <div class="flex flex-col gap-1.5">
          <Label for="f-consultant">{{ t('admin.reservations.filterConsultant') }}</Label>
          <NativeSelect id="f-consultant" v-model="formConsultantId" class="min-w-[160px]">
            <NativeSelectOption value="">{{ t('admin.reservations.filterConsultantAll') }}</NativeSelectOption>
            <NativeSelectOption v-for="c in consultants" :key="c.id" :value="String(c.id)">
              {{ c.name }}{{ c.isActive ? '' : ` (${t('admin.reservationDetail.inactive')})` }}
            </NativeSelectOption>
          </NativeSelect>
          <div class="flex items-center gap-1.5">
            <Checkbox id="f-show-inactive-consultants" v-model="showInactiveConsultants" />
            <Label for="f-show-inactive-consultants" class="text-xs font-normal text-muted-foreground">{{ t('admin.reservations.filterIncludeInactive') }}</Label>
          </div>
        </div>
        <div class="flex flex-col gap-1.5">
          <Label for="f-from">{{ t('admin.reservations.filterFrom') }}</Label>
          <DatePicker id="f-from" v-model="formFrom" :locale="inputLang" />
        </div>
        <div class="flex flex-col gap-1.5">
          <Label for="f-to">{{ t('admin.reservations.filterTo') }}</Label>
          <DatePicker id="f-to" v-model="formTo" :locale="inputLang" />
        </div>
        <div class="flex min-w-[200px] flex-1 flex-col gap-1.5">
          <Label for="f-search">{{ t('admin.reservations.filterSearch') }}</Label>
          <Input
            id="f-search" v-model="formSearch"
            :placeholder="t('admin.reservations.filterSearchPlaceholder')"
            @keyup.enter="applyFilters"
          />
        </div>
        <div class="flex flex-col gap-1.5">
          <Label class="invisible">{{ t('admin.reservations.filterApply') }}</Label>
          <Button @click="applyFilters">{{ t('admin.reservations.filterApply') }}</Button>
        </div>
        <div class="flex flex-col gap-1.5">
          <Label class="invisible">{{ t('admin.reservations.filterReset') }}</Label>
          <Button variant="outline" @click="resetFilters">{{ t('admin.reservations.filterReset') }}</Button>
        </div>
      </CardContent>
    </Card>

    <div class="overflow-x-auto rounded-md border border-border">
      <table class="w-full text-sm">
        <thead class="bg-muted text-muted-foreground">
          <tr>
            <th class="px-3 py-2 text-left">{{ t('admin.reservations.colCode') }}</th>
            <th class="px-3 py-2 text-left">{{ t('admin.reservations.colName') }}</th>
            <th class="px-3 py-2 text-left">{{ t('admin.reservations.colWechatId') }}</th>
            <th class="px-3 py-2 text-left">{{ t('admin.reservations.colStatus') }}</th>
            <th class="px-3 py-2 text-left">{{ t('admin.reservations.colConsultant') }}</th>
            <th class="px-3 py-2 text-left">{{ t('admin.reservations.colCreatedAt') }}</th>
            <th class="px-3 py-2 text-left">{{ t('admin.reservations.colVisitDate') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!data?.items.length">
            <td colspan="7" class="p-6 text-center text-muted-foreground">{{ t('admin.reservations.empty') }}</td>
          </tr>
          <tr
            v-for="r in data?.items" :key="r.id"
            class="cursor-pointer border-t border-border hover:bg-accent"
            @click="goDetail(r.id)"
          >
            <td class="px-3 py-2">{{ r.code }}</td>
            <td class="px-3 py-2">{{ r.name }}</td>
            <td class="px-3 py-2">{{ r.wechatId }}</td>
            <td class="px-3 py-2">{{ t(`status.${r.status}`) }}</td>
            <td class="px-3 py-2">{{ r.consultantName ?? t('admin.reservations.unassigned') }}</td>
            <td class="px-3 py-2">{{ formatKst(r.createdAt) }}</td>
            <td class="px-3 py-2">{{ r.visitDate ?? '-' }}</td>
          </tr>
        </tbody>
      </table>
    </div>

    <Pagination :page="page" :total-pages="totalPages" @update:page="goPage" />
  </div>
</template>

<script setup lang="ts">
import type { ConsultantLookup, PagedResult, ReservationListItem, ReservationSummary } from '~/types/reservation'
import { RefreshCw } from '@lucide/vue'

definePageMeta({ middleware: 'admin', layout: 'admin', i18n: false })
useHead({ title: '예약 대시보드 | Admin', meta: [{ name: 'robots', content: 'noindex, nofollow' }] })

const { t } = useI18n()
const route = useRoute()
// layouts/admin.vue가 useOpsLocale()을 이미 호출해 locale이 계정 값으로 맞춰져 있다 — 여기선 재사용만.
const inputLang = useInputLang()

const STATUSES: string[] = ['New', 'Consulting', 'Confirmed', 'Visited', 'Cancelled']

// 접수일 필터 기본값 = 당월 1일~현재(KST, 2026-08-27) — todayKst()는 YYYY-MM-DD 고정 길이라 앞 8자
// + '01'로 당월 1일을 얻는다(day 필드 별도 계산 불필요, utils/datetime.ts 기존 패턴 재사용)
const defaultFrom = `${todayKst().slice(0, 8)}01`
const defaultTo = todayKst()

// 🔴 검색 입력을 반응형 query에 직접 물리지 말 것(12-4절) — URL 쿼리를 computed로 감싸 제출 시에만 반응
const query = computed(() => ({
  page: Number(route.query.page) || 1,
  pageSize: 20,
  status: (route.query.status as string) || undefined,
  consultantId: route.query.consultantId ? Number(route.query.consultantId) : undefined,
  from: (route.query.from as string) || defaultFrom,
  to: (route.query.to as string) || defaultTo,
  search: (route.query.search as string) || undefined,
}))

// 8-4절/12-4절 — 대시보드 필터는 기본 활성 실장만, "비활성 포함" 체크 시 퇴사자도 필터 대상에 노출
const showInactiveConsultants = ref(false)

// 🔴 성능(2026-08-27, "로그인이 느림" 조사) — 아래 3개 API는 서로 의존성이 없는데
// 각각 await로 순차 실행되고 있었다. 로그인 직후 가장 먼저 뜨는 이 페이지가 왕복을
// 3번 직렬로 기다려 체감 지연의 직접 원인이었음 — Promise.all로 동시 시작해 왕복 1회로 단축.
const [
  { data: summary, refresh: refreshSummary },
  { data, refresh: refreshData, pending: dataPending },
  { data: consultants },
] = await Promise.all([
  useApi<ReservationSummary>('/api/admin/reservations/summary'),
  useApi<PagedResult<ReservationListItem>>('/api/admin/reservations', { query }),
  useApi<ConsultantLookup[]>('/api/admin/consultants', {
    query: () => ({ includeInactive: showInactiveConsultants.value }),
  }),
])

const page = computed(() => query.value.page)
const totalPages = computed(() => data.value ? Math.max(1, Math.ceil(data.value.total / data.value.pageSize)) : 1)

const cards = computed(() => [
  { key: 'new', label: t('admin.reservations.cardNew'), value: summary.value?.new ?? 0, status: 'New' },
  { key: 'consulting', label: t('admin.reservations.cardConsulting'), value: summary.value?.consulting ?? 0, status: 'Consulting' },
  { key: 'confirmed', label: t('admin.reservations.cardConfirmed'), value: summary.value?.confirmed ?? 0, status: 'Confirmed' },
  { key: 'visited', label: t('admin.reservations.cardVisitedThisMonth'), value: summary.value?.visitedThisMonth ?? 0, status: 'Visited' },
])

// 폼 로컬 상태 — [필터 적용] 클릭 시에만 route.query로 반영한다(타이핑 중 재조회 방지)
const formStatus = ref(query.value.status ?? '')
const formConsultantId = ref(query.value.consultantId ? String(query.value.consultantId) : '')
const formFrom = ref(query.value.from)
const formTo = ref(query.value.to)
const formSearch = ref(query.value.search ?? '')

// 🔴 버그(2026-08-27) — "비활성 포함" 체크 후 비활성 실장을 선택하고 체크를 다시 해제하면 목록이
// 활성 실장만으로 교체돼 선택돼 있던 값이 <select>에 더 이상 없는 옵션이 된다. 네이티브 select는
// 이때 화면만 빈 값처럼 보일 뿐 formConsultantId ref 자체는 사라진 값을 계속 들고 있어(다음
// [필터 적용] 클릭 시 그 stale id로 조회됨) — 목록이 바뀔 때마다 현재 선택값이 여전히 유효한지
// 확인해, 없으면 "전체"로 리셋한다(비활성 토글뿐 아니라 목록이 바뀌는 모든 경우에 적용되는 근본 수정).
watch(consultants, (list) => {
  if (formConsultantId.value && !(list ?? []).some(c => String(c.id) === formConsultantId.value))
    formConsultantId.value = ''
})

function applyFilters() {
  navigateTo({
    query: {
      page: 1,
      status: formStatus.value || undefined,
      consultantId: formConsultantId.value || undefined,
      from: formFrom.value || undefined,
      to: formTo.value || undefined,
      search: formSearch.value || undefined,
    },
  })
}
function resetFilters() {
  formStatus.value = ''
  formConsultantId.value = ''
  formFrom.value = defaultFrom
  formTo.value = defaultTo
  formSearch.value = ''
  navigateTo({ query: {} })
}
function goPage(p: number) {
  navigateTo({ query: { ...route.query, page: p } })
}
function goDetail(id: number) {
  navigateTo(`/admin/reservations/${id}`)
}
// 상단 카드 클릭 → 그 카드의 상태로 필터 세팅 후 검색(날짜 범위는 접수일 기준이라 건드리지 않음 —
// "이번 달 방문 완료" 카드는 방문일(visitedAt) 기준 집계라 접수일 필터와 무관, 그대로 두는 게 맞음)
function filterByStatus(status: string) {
  formStatus.value = status
  navigateTo({ query: { ...route.query, page: 1, status } })
}
async function refreshAll() {
  await Promise.all([refreshData(), refreshSummary()])
}
</script>
