<template>
  <div class="space-y-6">
    <h1 class="text-xl font-semibold text-foreground">{{ t('admin.auditLogs.title') }}</h1>

    <div class="flex flex-wrap items-end gap-4 rounded-md border border-border p-4">
      <div class="flex flex-col gap-1.5">
        <Label for="f-actor">{{ t('admin.auditLogs.filterActorLabel') }}</Label>
        <select id="f-actor" v-model="formActorId" class="h-9 w-56 rounded-md border border-input bg-background px-3 text-sm">
          <option value="">{{ t('admin.auditLogs.filterActorAll') }}</option>
          <option v-for="u in actors?.items" :key="u.id" :value="String(u.id)">{{ u.email }}</option>
        </select>
      </div>
      <div class="flex flex-col gap-1.5">
        <Label for="f-entity-type">{{ t('admin.auditLogs.filterEntityTypeLabel') }}</Label>
        <select id="f-entity-type" v-model="formEntityType" class="h-9 w-44 rounded-md border border-input bg-background px-3 text-sm">
          <option value="">{{ t('admin.auditLogs.filterAll') }}</option>
          <option v-for="et in ENTITY_TYPES" :key="et" :value="et">{{ et }}</option>
        </select>
      </div>
      <div class="flex flex-col gap-1.5">
        <Label for="f-action">{{ t('admin.auditLogs.filterActionLabel') }}</Label>
        <select id="f-action" v-model="formAction" class="h-9 w-44 rounded-md border border-input bg-background px-3 text-sm">
          <option value="">{{ t('admin.auditLogs.filterAll') }}</option>
          <option v-for="a in ACTIONS" :key="a" :value="a">{{ a }}</option>
        </select>
      </div>
      <div class="flex flex-col gap-1.5">
        <Label for="f-from">{{ t('admin.auditLogs.filterFromLabel') }}</Label>
        <Input id="f-from" v-model="formFrom" type="date" class="w-40" />
      </div>
      <div class="flex flex-col gap-1.5">
        <Label for="f-to">{{ t('admin.auditLogs.filterToLabel') }}</Label>
        <Input id="f-to" v-model="formTo" type="date" class="w-40" />
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

    <div class="flex items-center gap-3">
      <Button variant="outline" size="sm" :disabled="page <= 1" @click="goPage(page - 1)">{{ t('admin.reservations.prev') }}</Button>
      <span class="text-sm text-muted-foreground">{{ t('admin.reservations.pageInfo', { page, total: totalPages }) }}</span>
      <Button variant="outline" size="sm" :disabled="page >= totalPages" @click="goPage(page + 1)">{{ t('admin.reservations.next') }}</Button>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { AdminUser, AuditLogEntry, PagedResult } from '~/types/reservation'

definePageMeta({ middleware: 'admin', layout: 'admin', i18n: false })
useHead({ title: '로그(감사) | Admin', meta: [{ name: 'robots', content: 'noindex, nofollow' }] })

const { t } = useI18n()
const route = useRoute()

// 14-1절 RouteMap에 실제 등록된 entity_type·action 값만 노출(존재하지 않는 조합을 필터로 주면 항상 0건)
const ENTITY_TYPES = ['reservation', 'reservation_note', 'consultant', 'procedure', 'user']
const ACTIONS = ['create', 'update', 'soft_delete', 'note_add', 'note_update', 'status_change']

// 🔴 검색 입력을 반응형 query에 직접 물리지 말 것(12-4절) — [필터 적용] 클릭 시에만 route.query 반영
const query = computed(() => ({
  page: Number(route.query.page) || 1,
  pageSize: 20,
  actorId: route.query.actorId ? Number(route.query.actorId) : undefined,
  entityType: (route.query.entityType as string) || undefined,
  action: (route.query.action as string) || undefined,
  from: (route.query.from as string) || undefined,
  to: (route.query.to as string) || undefined,
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
const formFrom = ref(query.value.from ?? '')
const formTo = ref(query.value.to ?? '')
const formSearch = ref(query.value.search ?? '')

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
  formFrom.value = ''
  formTo.value = ''
  formSearch.value = ''
  navigateTo({ query: {} })
}
</script>
