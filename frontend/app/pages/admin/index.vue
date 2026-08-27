<template>
  <div class="space-y-6">
    <h1 class="text-xl font-semibold text-foreground">{{ t('admin.reservations.title') }}</h1>

    <div class="grid grid-cols-2 gap-4 md:grid-cols-4">
      <Card v-for="c in cards" :key="c.key">
        <CardHeader>
          <CardDescription>{{ c.label }}</CardDescription>
        </CardHeader>
        <CardContent>
          <p class="text-3xl font-bold text-primary">{{ c.value }}</p>
        </CardContent>
      </Card>
    </div>

    <Card>
      <CardContent class="flex flex-wrap items-end gap-4">
        <div class="flex flex-col gap-1.5">
          <Label for="f-status">{{ t('admin.reservations.filterStatus') }}</Label>
          <NativeSelect id="f-status" v-model="formStatus">
            <NativeSelectOption value="">{{ t('admin.reservations.filterStatusAll') }}</NativeSelectOption>
            <NativeSelectOption v-for="s in STATUSES" :key="s" :value="s">{{ t(`status.${s}`) }}</NativeSelectOption>
          </NativeSelect>
        </div>
        <div class="flex flex-col gap-1.5">
          <Label for="f-consultant">{{ t('admin.reservations.filterConsultant') }}</Label>
          <NativeSelect id="f-consultant" v-model="formConsultantId">
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
          <DatePicker id="f-from" v-model="formFrom" :locale="inputLang" class="w-40" />
        </div>
        <div class="flex flex-col gap-1.5">
          <Label for="f-to">{{ t('admin.reservations.filterTo') }}</Label>
          <DatePicker id="f-to" v-model="formTo" :locale="inputLang" class="w-40" />
        </div>
        <div class="flex min-w-[200px] flex-1 flex-col gap-1.5">
          <Label for="f-search">{{ t('admin.reservations.filterSearch') }}</Label>
          <Input
            id="f-search" v-model="formSearch"
            :placeholder="t('admin.reservations.filterSearchPlaceholder')"
            @keyup.enter="applyFilters"
          />
        </div>
        <Button @click="applyFilters">{{ t('admin.reservations.filterApply') }}</Button>
        <Button variant="outline" @click="resetFilters">{{ t('admin.reservations.filterReset') }}</Button>
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

definePageMeta({ middleware: 'admin', layout: 'admin', i18n: false })
useHead({ title: '예약 대시보드 | Admin', meta: [{ name: 'robots', content: 'noindex, nofollow' }] })

const { t } = useI18n()
const route = useRoute()
// layouts/admin.vue가 useOpsLocale()을 이미 호출해 locale이 계정 값으로 맞춰져 있다 — 여기선 재사용만.
const inputLang = useInputLang()

const STATUSES: string[] = ['New', 'Consulting', 'Confirmed', 'Visited', 'Cancelled']

// 🔴 검색 입력을 반응형 query에 직접 물리지 말 것(12-4절) — URL 쿼리를 computed로 감싸 제출 시에만 반응
const query = computed(() => ({
  page: Number(route.query.page) || 1,
  pageSize: 20,
  status: (route.query.status as string) || undefined,
  consultantId: route.query.consultantId ? Number(route.query.consultantId) : undefined,
  from: (route.query.from as string) || undefined,
  to: (route.query.to as string) || undefined,
  search: (route.query.search as string) || undefined,
}))

const { data: summary } = await useApi<ReservationSummary>('/api/admin/reservations/summary')
const { data } = await useApi<PagedResult<ReservationListItem>>('/api/admin/reservations', { query })
// 8-4절/12-4절 — 대시보드 필터는 기본 활성 실장만, "비활성 포함" 체크 시 퇴사자도 필터 대상에 노출
const showInactiveConsultants = ref(false)
const { data: consultants } = await useApi<ConsultantLookup[]>('/api/admin/consultants', {
  query: () => ({ includeInactive: showInactiveConsultants.value }),
})

const page = computed(() => query.value.page)
const totalPages = computed(() => data.value ? Math.max(1, Math.ceil(data.value.total / data.value.pageSize)) : 1)

const cards = computed(() => [
  { key: 'new', label: t('admin.reservations.cardNew'), value: summary.value?.new ?? 0 },
  { key: 'consulting', label: t('admin.reservations.cardConsulting'), value: summary.value?.consulting ?? 0 },
  { key: 'confirmed', label: t('admin.reservations.cardConfirmed'), value: summary.value?.confirmed ?? 0 },
  { key: 'visited', label: t('admin.reservations.cardVisitedThisMonth'), value: summary.value?.visitedThisMonth ?? 0 },
])

// 폼 로컬 상태 — [필터 적용] 클릭 시에만 route.query로 반영한다(타이핑 중 재조회 방지)
const formStatus = ref(query.value.status ?? '')
const formConsultantId = ref(query.value.consultantId ? String(query.value.consultantId) : '')
const formFrom = ref(query.value.from ?? '')
const formTo = ref(query.value.to ?? '')
const formSearch = ref(query.value.search ?? '')

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
  formFrom.value = ''
  formTo.value = ''
  formSearch.value = ''
  navigateTo({ query: {} })
}
function goPage(p: number) {
  navigateTo({ query: { ...route.query, page: p } })
}
function goDetail(id: number) {
  navigateTo(`/admin/reservations/${id}`)
}
</script>
