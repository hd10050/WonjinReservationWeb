<template>
  <div class="space-y-6">
    <h1 class="text-xl font-semibold text-foreground">{{ t('admin.auditLogs.title') }}</h1>

    <Card>
      <CardContent class="flex flex-col gap-4">
        <div class="flex flex-wrap items-end gap-4">
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
            <Input id="f-search" v-model="formSearch" maxlength="200" class="w-56" @keyup.enter="applyFilters" />
          </div>
        </div>
        <!-- 🔴 필터 필드 줄과 별도 줄로 고정 — 같은 줄에 두면 필드 개수에 따라 줄바꿈 위치가 바뀌면서
             버튼이 위/아래로 오르내렸다(사용자 재현 보고). 항상 필터 아래 고정된 한 줄에 둔다. -->
        <div class="flex items-center gap-2">
          <Button size="sm" :disabled="rangeTooLong" @click="applyFilters">{{ t('admin.reservations.filterApply') }}</Button>
          <Button size="sm" variant="outline" @click="resetFilters">{{ t('admin.reservations.filterReset') }}</Button>
        </div>
        <p v-if="rangeTooLong" class="text-sm text-destructive">{{ t('admin.common.filterRangeError') }}</p>
      </CardContent>
    </Card>

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
import { clampDateRangeEnd, todayKst } from '~/utils/datetime'

definePageMeta({ middleware: 'admin', layout: 'admin', i18n: false })
useHead({ title: '로그(감사) | Admin', meta: [{ name: 'robots', content: 'noindex, nofollow' }] })

const { t } = useI18n()
const route = useRoute()
// layouts/admin.vue가 useOpsLocale()을 이미 호출해 locale이 계정 값으로 맞춰져 있다 — 여기선 재사용만.
const inputLang = useInputLang()

// 14-1절 RouteMap(api/Filters/AuditLogFilter.cs)에 실제 등록된 entity_type·action 값만 노출
// (존재하지 않는 조합을 필터로 주면 항상 0건). 🔴 2026-09-01 감사 — category·influencer_link
// 엔티티(D25/2026-08-27 추가)와 assign·restore·bulk_create 액션이 여기 반영이 안 돼 있었고,
// 이미 폐지된 soft_delete(D24)는 그대로 남아 골라도 항상 0건이었다. RouteMap과 다시 대조해 동기화.
const ENTITY_TYPES = ['reservation', 'reservation_note', 'consultant', 'procedure', 'category', 'user', 'influencer_link']
const ACTIONS = ['create', 'update', 'assign', 'restore', 'bulk_create', 'note_add', 'note_update', 'status_change']

// 🔴 버그(2026-08-27) — 다른 날짜 필터 페이지(대시보드·유입경로·통계·KPI)는 전부 당월 1일~현재를
// 기본값으로 두는데 이 페이지만 빠져 있어 날짜 필터가 빈 채로 시작했다 — 나머지와 동일하게 통일.
const defaultFrom = `${todayKst().slice(0, 7)}-01`
const defaultTo = todayKst()

// 🔴 검색 입력을 반응형 query에 직접 물리지 말 것(12-4절) — [필터 적용] 클릭 시에만 route.query 반영
// 🔴 조회 기간 상한(1년+1일)은 useDateRangeFilter가 필터 폼(UI)만 막는다 — URL 직접 조작·북마크는
// 폼을 거치지 않아 그 방어를 우회한다. 실제 조회에 쓰는 이 query 자체에서 clamp해 우회를 막는다.
const query = computed(() => {
  const from = (route.query.from as string) || defaultFrom
  return {
    page: Number(route.query.page) || 1,
    pageSize: 20,
    actorId: route.query.actorId ? Number(route.query.actorId) : undefined,
    entityType: (route.query.entityType as string) || undefined,
    action: (route.query.action as string) || undefined,
    from,
    to: clampDateRangeEnd(from, (route.query.to as string) || defaultTo),
    search: (route.query.search as string) || undefined,
  }
})
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
const { toMinValue, rangeTooLong } = useDateRangeFilter(formFrom, formTo)

function applyFilters() {
  if (rangeTooLong.value) return
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
