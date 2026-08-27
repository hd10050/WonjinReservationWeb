<template>
  <div class="space-y-6">
    <h1 class="text-xl font-semibold text-foreground">{{ t('admin.auditLogs.title') }}</h1>

    <div class="flex flex-wrap items-end gap-4 rounded-md border border-border p-4">
      <div class="flex flex-col gap-1.5">
        <Label for="f-actor">{{ t('admin.auditLogs.filterActorLabel') }}</Label>
        <NativeSelect id="f-actor" v-model="formActorId" class="w-56">
          <NativeSelectOption value="">{{ t('admin.auditLogs.filterActorAll') }}</NativeSelectOption>
          <NativeSelectOption v-for="u in actors?.items" :key="u.id" :value="String(u.id)">{{ u.email }}</NativeSelectOption>
        </NativeSelect>
      </div>
      <div class="flex flex-col gap-1.5">
        <Label for="f-entity-type">{{ t('admin.auditLogs.filterEntityTypeLabel') }}</Label>
        <NativeSelect id="f-entity-type" v-model="formEntityType" class="w-44">
          <NativeSelectOption value="">{{ t('admin.auditLogs.filterAll') }}</NativeSelectOption>
          <NativeSelectOption v-for="et in ENTITY_TYPES" :key="et" :value="et">{{ et }}</NativeSelectOption>
        </NativeSelect>
      </div>
      <div class="flex flex-col gap-1.5">
        <Label for="f-action">{{ t('admin.auditLogs.filterActionLabel') }}</Label>
        <NativeSelect id="f-action" v-model="formAction" class="w-44">
          <NativeSelectOption value="">{{ t('admin.auditLogs.filterAll') }}</NativeSelectOption>
          <NativeSelectOption v-for="a in ACTIONS" :key="a" :value="a">{{ a }}</NativeSelectOption>
        </NativeSelect>
      </div>
      <div class="flex flex-col gap-1.5">
        <Label for="f-from">{{ t('admin.auditLogs.filterFromLabel') }}</Label>
        <DatePicker id="f-from" v-model="formFrom" :locale="inputLang" />
      </div>
      <div class="flex flex-col gap-1.5">
        <Label for="f-to">{{ t('admin.auditLogs.filterToLabel') }}</Label>
        <DatePicker id="f-to" v-model="formTo" :locale="inputLang" :min-value="toMinValue" />
      </div>
      <div class="flex flex-col gap-1.5">
        <Label for="f-search">{{ t('admin.auditLogs.filterSearchLabel') }}</Label>
        <Input id="f-search" v-model="formSearch" maxlength="200" class="w-56" />
      </div>
      <Button size="sm" @click="applyFilters">{{ t('admin.reservations.filterApply') }}</Button>
      <Button size="sm" variant="outline" @click="resetFilters">{{ t('admin.reservations.filterReset') }}</Button>
    </div>

    <div class="overflow-x-auto rounded-md border border-border">
      <table class="w-full text-sm">
        <thead class="bg-muted text-muted-foreground">
          <tr>
            <th class="px-3 py-2 text-left">{{ t('admin.auditLogs.colCreatedAt') }}</th>
            <th class="px-3 py-2 text-left">{{ t('admin.auditLogs.colActor') }}</th>
            <th class="px-3 py-2 text-left">{{ t('admin.auditLogs.colAction') }}</th>
            <th class="px-3 py-2 text-left">{{ t('admin.auditLogs.colEntity') }}</th>
            <th class="px-3 py-2 text-left">{{ t('admin.auditLogs.colSummary') }}</th>
            <th class="px-3 py-2 text-left">{{ t('admin.auditLogs.colStatusCode') }}</th>
            <th class="px-3 py-2 text-left">{{ t('admin.auditLogs.colIp') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!data?.items.length">
            <td colspan="7" class="p-6 text-center text-muted-foreground">{{ t('admin.auditLogs.empty') }}</td>
          </tr>
          <tr v-for="log in data?.items" :key="log.id" class="border-t border-border">
            <td class="px-3 py-2 whitespace-nowrap">{{ formatDateTime(log.createdAt) }}</td>
            <td class="px-3 py-2">{{ log.actorEmail }} ({{ log.actorRole }})</td>
            <td class="px-3 py-2">{{ log.action }}</td>
            <td class="px-3 py-2">{{ log.entityType }}<span v-if="log.entityId">#{{ log.entityId }}</span></td>
            <td class="px-3 py-2">{{ log.summary }}</td>
            <td class="px-3 py-2" :class="log.statusCode >= 400 ? 'text-destructive' : ''">{{ log.statusCode }}</td>
            <td class="px-3 py-2">{{ log.ip ?? '-' }}</td>
          </tr>
        </tbody>
      </table>
    </div>

    <Pagination :page="page" :total-pages="totalPages" @update:page="goPage" />
  </div>
</template>

<script setup lang="ts">
import type { AdminUser, AuditLogEntry, PagedResult } from '~/types/reservation'
import { todayKst } from '~/utils/datetime'

definePageMeta({ middleware: 'admin', layout: 'admin', i18n: false })
useHead({ title: '로그(감사) | Admin', meta: [{ name: 'robots', content: 'noindex, nofollow' }] })

const { t } = useI18n()
const route = useRoute()
// layouts/admin.vue가 useOpsLocale()을 이미 호출해 locale이 계정 값으로 맞춰져 있다 — 여기선 재사용만.
const inputLang = useInputLang()

// 14-1절 RouteMap에 실제 등록된 entity_type·action 값만 노출(존재하지 않는 조합을 필터로 주면 항상 0건)
const ENTITY_TYPES = ['reservation', 'reservation_note', 'consultant', 'procedure', 'user']
const ACTIONS = ['create', 'update', 'soft_delete', 'note_add', 'note_update', 'status_change']

// 🔴 버그(2026-08-27) — 다른 날짜 필터 페이지(대시보드·유입경로·통계·KPI)는 전부 당월 1일~현재를
// 기본값으로 두는데 이 페이지만 빠져 있어 날짜 필터가 빈 채로 시작했다 — 나머지와 동일하게 통일.
const defaultFrom = `${todayKst().slice(0, 7)}-01`
const defaultTo = todayKst()

// 🔴 검색 입력을 반응형 query에 직접 물리지 말 것(12-4절) — [필터 적용] 클릭 시에만 route.query 반영
const query = computed(() => ({
  page: Number(route.query.page) || 1,
  pageSize: 20,
  actorId: route.query.actorId ? Number(route.query.actorId) : undefined,
  entityType: (route.query.entityType as string) || undefined,
  action: (route.query.action as string) || undefined,
  from: (route.query.from as string) || defaultFrom,
  to: (route.query.to as string) || defaultTo,
  search: (route.query.search as string) || undefined,
}))
const { data } = await useApi<PagedResult<AuditLogEntry>>('/api/admin/audit-logs', { query })
// 행위자 필터 드롭다운용 — 계정 수가 적어 페이지 1개(최대 100)로 충분(9-1절 규모 전제와 동일)
const { data: actors } = await useApi<PagedResult<AdminUser>>('/api/admin/users', { query: () => ({ page: 1, pageSize: 100 }) })

const page = computed(() => query.value.page)
const totalPages = computed(() => data.value ? Math.max(1, Math.ceil(data.value.total / data.value.pageSize)) : 1)
function goPage(p: number) {
  navigateTo({ query: { ...route.query, page: p } })
}
function formatDateTime(iso: string) {
  return new Date(iso).toLocaleString('ko-KR', { timeZone: 'Asia/Seoul' })
}

const formActorId = ref(query.value.actorId ? String(query.value.actorId) : '')
const formEntityType = ref(query.value.entityType ?? '')
const formAction = ref(query.value.action ?? '')
const formFrom = ref(query.value.from)
const formTo = ref(query.value.to)
const formSearch = ref(query.value.search ?? '')
const { toMinValue } = useDateRangeFilter(formFrom, formTo)

function applyFilters() {
  navigateTo({
    query: {
      page: 1,
      actorId: formActorId.value || undefined,
      entityType: formEntityType.value || undefined,
      action: formAction.value || undefined,
      from: formFrom.value || undefined,
      to: formTo.value || undefined,
      search: formSearch.value || undefined,
    },
  })
}
function resetFilters() {
  formActorId.value = ''
  formEntityType.value = ''
  formAction.value = ''
  formFrom.value = defaultFrom
  formTo.value = defaultTo
  formSearch.value = ''
  navigateTo({ query: {} })
}
</script>
