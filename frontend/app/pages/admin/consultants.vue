<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-semibold text-foreground">{{ t('admin.consultants.title') }}</h1>
      <div class="flex gap-2">
        <Button variant="outline" @click="downloadTemplate">{{ t('admin.consultants.bulk.templateButton') }}</Button>
        <Button variant="outline" @click="showBulk = !showBulk">{{ t('admin.consultants.bulk.button') }}</Button>
        <Button @click="startCreate">{{ t('admin.consultants.addButton') }}</Button>
      </div>
    </div>

    <Card v-if="showBulk">
      <CardHeader>
        <CardTitle>{{ t('admin.consultants.bulk.title') }}</CardTitle>
      </CardHeader>
      <CardContent class="space-y-4">
        <div class="flex flex-col gap-1.5">
          <Label for="f-bulk-file">{{ t('admin.consultants.bulk.fileLabel') }}</Label>
          <Input id="f-bulk-file" type="file" accept=".xlsx,.xls" class="w-auto" @change="onExcelSelected" />
        </div>

        <div v-if="bulkRows.length">
          <p v-if="bulkHasError" class="mb-2 text-sm font-medium text-destructive">
            {{ t('admin.consultants.bulk.errorBanner', { count: bulkErrorCount }) }}
          </p>
          <div class="overflow-x-auto rounded-md border border-border">
            <table class="w-full text-sm">
              <thead class="bg-muted text-muted-foreground">
                <tr>
                  <th class="px-3 py-2 text-left">{{ t('admin.consultants.bulk.colRow') }}</th>
                  <th class="px-3 py-2 text-left">{{ t('admin.consultants.colName') }}</th>
                  <th class="px-3 py-2 text-left">{{ t('admin.consultants.colSortOrder') }}</th>
                  <th class="px-3 py-2 text-left">{{ t('admin.consultants.bulk.colError') }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="row in bulkRows" :key="row.row" class="border-t border-border" :class="row.error ? 'bg-destructive/10' : ''">
                  <td class="px-3 py-2">{{ row.row }}</td>
                  <td class="px-3 py-2">{{ row.name }}</td>
                  <td class="px-3 py-2">{{ row.sortOrder }}</td>
                  <td class="px-3 py-2 text-destructive">{{ row.error }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <div class="flex items-center gap-3">
          <Button :disabled="!bulkRows.length || bulkHasError || bulkSubmitting" @click="submitBulk">
            {{ bulkHasError ? t('admin.consultants.bulk.submitDisabled', { count: bulkErrorCount }) : t('admin.consultants.bulk.submit') }}
          </Button>
          <Button variant="outline" @click="cancelBulk">{{ t('common.cancel') }}</Button>
          <span v-if="bulkSubmitError" class="text-sm text-destructive">{{ bulkSubmitError }}</span>
          <span v-if="bulkSuccessMessage" class="text-sm text-primary">{{ bulkSuccessMessage }}</span>
        </div>
      </CardContent>
    </Card>

    <Card>
      <CardContent class="flex flex-col gap-3">
        <div class="flex flex-wrap items-end gap-4">
          <div class="flex min-w-[200px] flex-1 flex-col gap-1.5">
            <Label for="f-search">{{ t('admin.consultants.filterSearch') }}</Label>
            <Input
              id="f-search" v-model="formSearch" maxlength="200"
              :placeholder="t('admin.consultants.filterSearchPlaceholder')"
              @keyup.enter="applySearch"
            />
          </div>
          <Button @click="applySearch">{{ t('admin.reservations.filterApply') }}</Button>
        </div>
        <div class="flex items-center gap-1.5">
          <Checkbox id="f-show-inactive" v-model="showInactive" />
          <Label for="f-show-inactive" class="text-sm font-normal text-muted-foreground">{{ t('admin.consultants.includeInactive') }}</Label>
        </div>
      </CardContent>
    </Card>

    <Card v-if="showForm">
      <CardHeader>
        <CardTitle>{{ editingId === null ? t('admin.consultants.formTitleCreate') : t('admin.consultants.formTitleEdit') }}</CardTitle>
      </CardHeader>
      <CardContent class="flex flex-wrap items-end gap-4">
        <div class="flex flex-col gap-1.5">
          <Label for="f-name">{{ t('admin.consultants.formNameLabel') }}</Label>
          <Input id="f-name" v-model="formName" maxlength="30" class="w-56" />
        </div>
        <div class="flex flex-col gap-1.5">
          <Label for="f-sort">{{ t('admin.consultants.formSortOrderLabel') }}</Label>
          <Input id="f-sort" v-model.number="formSortOrder" type="number" class="w-24" />
        </div>
        <div v-if="editingId !== null" class="flex items-center gap-1.5 pb-2">
          <Checkbox id="f-is-active" v-model="formIsActive" />
          <Label for="f-is-active" class="text-sm font-normal">{{ t('admin.consultants.formActiveLabel') }}</Label>
        </div>
        <Button :disabled="!formName.trim()" @click="submitForm">{{ t('common.save') }}</Button>
        <Button variant="outline" @click="cancelForm">{{ t('common.cancel') }}</Button>
        <span v-if="formError" class="text-sm text-destructive">{{ formError }}</span>
      </CardContent>
    </Card>

    <div class="overflow-x-auto rounded-md border border-border">
      <table class="w-full text-sm">
        <thead class="bg-muted text-muted-foreground">
          <tr>
            <th class="px-3 py-2 text-left">{{ t('admin.consultants.colName') }}</th>
            <th class="px-3 py-2 text-left">{{ t('admin.consultants.colSortOrder') }}</th>
            <th class="px-3 py-2 text-left">{{ t('admin.consultants.colActive') }}</th>
            <th class="px-3 py-2 text-left" />
          </tr>
        </thead>
        <tbody>
          <tr v-if="!consultants?.length">
            <td colspan="4" class="p-6 text-center text-muted-foreground">{{ t('admin.consultants.empty') }}</td>
          </tr>
          <tr v-for="c in consultants" :key="c.id" class="border-t border-border">
            <td class="px-3 py-2">{{ c.name }}</td>
            <td class="px-3 py-2">{{ c.sortOrder }}</td>
            <td class="px-3 py-2">{{ c.isActive ? t('admin.consultants.activeLabel') : t('admin.consultants.inactiveLabel') }}</td>
            <td class="px-3 py-2 text-right">
              <button type="button" class="text-sm underline" @click="startEdit(c)">{{ t('admin.consultants.edit') }}</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <Pagination :page="page" :total-pages="totalPages" @update:page="goPage" />
  </div>
</template>

<script setup lang="ts">
import type { ConsultantLookup, PagedResult } from '~/types/reservation'

definePageMeta({ middleware: 'admin', layout: 'admin', i18n: false })
useHead({ title: '실장 관리 | Admin', meta: [{ name: 'robots', content: 'noindex, nofollow' }] })

const { t } = useI18n()
const { authFetch } = useAuthFetch()
const route = useRoute()

// 6-2절 메뉴 매트릭스로 이미 Admin/HospitalManager만 이 경로에 도달한다(middleware/admin.ts) — 화면 안에서
// 역할별 버튼을 다시 가릴 필요가 없다. 실제 방어선은 컨트롤러 액션 레벨 Authorize(11-3절).
//
// 🔴 검색 입력을 반응형 query에 직접 물리지 말 것(12-4절) — URL 쿼리를 computed로 감싸 제출 시에만 반응.
// includeInactive도 page와 함께 URL 쿼리로 둔다(로컬 ref + 별도 watch로 "1페이지로 되돌리기"를 하면,
// ref 변경 시 useApi 쿼리 watcher와 그 watch가 서로 다른 타이밍에 반응해 "이전 페이지 값으로 1번 →
// 되돌린 1페이지 값으로 1번" 낭비 요청이 실제로 중복 발생한다(실측 확인). 토글을 이 computed
// getter/setter로 route.query에 직접 반영하면 navigateTo 1회 = 요청 1회로 끝난다.
const query = computed(() => ({
  page: Number(route.query.page) || 1,
  pageSize: 20,
  includeInactive: route.query.includeInactive === '1',
  search: (route.query.search as string) || undefined,
}))
const showInactive = computed({
  get: () => query.value.includeInactive,
  set: (v: boolean) => navigateTo({ query: { ...route.query, page: 1, includeInactive: v ? '1' : undefined } }),
})

const { data: consultantsPaged, refresh } = await useApi<PagedResult<ConsultantLookup>>('/api/admin/consultants', { query })

const consultants = computed(() => consultantsPaged.value?.items ?? [])
const page = computed(() => query.value.page)
const totalPages = computed(() => consultantsPaged.value ? Math.max(1, Math.ceil(consultantsPaged.value.total / consultantsPaged.value.pageSize)) : 1)

const formSearch = ref(query.value.search ?? '')
function applySearch() {
  navigateTo({ query: { ...route.query, page: 1, search: formSearch.value || undefined } })
}
function goPage(p: number) {
  navigateTo({ query: { ...route.query, page: p } })
}

const editingId = ref<number | null>(null)
const formName = ref('')
const formSortOrder = ref(0)
const formIsActive = ref(true)
const formError = ref('')
const showForm = ref(false)

function startCreate() {
  editingId.value = null
  formName.value = ''
  formSortOrder.value = 0
  formIsActive.value = true
  formError.value = ''
  showForm.value = true
}

function startEdit(c: ConsultantLookup) {
  editingId.value = c.id
  formName.value = c.name
  formSortOrder.value = c.sortOrder
  formIsActive.value = c.isActive
  formError.value = ''
  showForm.value = true
}

function cancelForm() {
  showForm.value = false
}

// 엑셀 일괄등록 — excel-bulk-upload-pattern-reference.md 레이어1~2. DB 제약과 1:1 일치하는 검증만
// 프론트에서 미리 계산(name 필수·30자), 최종 저장 게이트는 백엔드 /bulk가 담당(all-or-nothing).
interface ConsultantBulkRow {
  row: number
  name: string
  sortOrder: number
  error: string
}

const showBulk = ref(false)
const bulkRows = ref<ConsultantBulkRow[]>([])
const bulkSubmitting = ref(false)
const bulkSubmitError = ref('')
const bulkSuccessMessage = ref('')

const bulkErrorCount = computed(() => bulkRows.value.filter(r => r.error).length)
const bulkHasError = computed(() => bulkErrorCount.value > 0)

function fieldLabel(field?: string): string {
  return field === 'name' ? t('admin.consultants.formNameLabel') : (field ?? '')
}

function describeBulkError(code: string, field?: string, length?: number, max?: number): string {
  if (code === 'BULK_FIELD_REQUIRED') return t('errors.BULK_FIELD_REQUIRED', { field: fieldLabel(field) })
  if (code === 'BULK_FIELD_TOO_LONG') return t('errors.BULK_FIELD_TOO_LONG', { field: fieldLabel(field), length, max })
  return t('errors.UNKNOWN')
}

function validateBulkRow(name: string): string {
  const trimmed = name.trim()
  if (!trimmed) return describeBulkError('BULK_FIELD_REQUIRED', 'name')
  if (trimmed.length > 30) return describeBulkError('BULK_FIELD_TOO_LONG', 'name', trimmed.length, 30)
  return ''
}

// 동적 import — xlsx는 클릭 시점에만 필요한 클라이언트 전용 라이브러리라 최상단 정적 import 대신 여기서 로드한다.
// 빌드 산출물 확인 결과 xlsx는 별도 청크(chunks/_/xlsx.mjs, gzip 162KB)로 분리되며, SSR 렌더 경로(이 함수들은
// 브라우저 클릭에서만 실행됨)에서는 로드되지 않는다 — 단, Cloudflare Workers 배포 시 wrangler가 이 청크를
// 최종 워커 스크립트에 실제로 얼마나 포함하는지는 별도 확인 필요([미확인], 현재 gzip 총합 695KB로 한도 내 안전).
async function downloadTemplate() {
  const XLSX = await import('xlsx')
  const ws = XLSX.utils.aoa_to_sheet([[t('admin.consultants.colName'), t('admin.consultants.colSortOrder')]])
  ws['!cols'] = [{ wch: 24 }, { wch: 12 }]
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, 'template')
  XLSX.writeFile(wb, 'consultants_template.xlsx')
}

async function onExcelSelected(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (!file) return
  const XLSX = await import('xlsx')
  const data = await file.arrayBuffer()
  const wb = XLSX.read(data)
  const ws = wb.Sheets[wb.SheetNames[0]!]!
  // header:1 → 배열의 배열(aoa). 빈 행 제거는 원본 엑셀 행 번호(헤더=1행)를 먼저 붙인 뒤에 한다 —
  // 순서를 반대로 하면 필터 이후 배열 인덱스가 더 이상 실제 엑셀 행 번호와 일치하지 않는다.
  const aoa = XLSX.utils.sheet_to_json(ws, { header: 1 }) as unknown[][]

  bulkRows.value = aoa
    .map((r, idx) => ({ r, excelRow: idx + 1 }))
    .slice(1)
    .filter(x => x.r.some(c => String(c ?? '').trim()))
    .map(({ r, excelRow }) => {
      const name = String(r[0] ?? '').trim()
      const sortOrder = Number(r[1] ?? 0) || 0
      return { row: excelRow, name, sortOrder, error: validateBulkRow(name) }
    })
  bulkSubmitError.value = ''
  bulkSuccessMessage.value = ''
}

async function submitBulk() {
  if (!bulkRows.value.length || bulkHasError.value) return
  bulkSubmitError.value = ''
  bulkSuccessMessage.value = ''
  bulkSubmitting.value = true
  try {
    const res = await authFetch<{ successCount: number }>('/api/admin/consultants/bulk', {
      method: 'POST',
      // 필터링된 일부가 아니라 전체 행을 보낸다 — 버튼이 눌렸다는 것 자체가 전부 통과했다는 뜻.
      body: bulkRows.value.map(r => ({ row: r.row, name: r.name, sortOrder: r.sortOrder })),
    })
    bulkSuccessMessage.value = t('admin.consultants.bulk.successMessage', { count: res.successCount })
    bulkRows.value = []
    showBulk.value = false
    await refresh()
  }
  catch (e: any) {
    const code = e?.data?.code ?? 'UNKNOWN'
    if (code === 'BULK_VALIDATION_FAILED' && Array.isArray(e?.data?.rowErrors)) {
      const errorsByRow = new Map<number, string>()
      for (const err of e.data.rowErrors as { row: number, code: string, field?: string, length?: number, max?: number }[]) {
        const msg = describeBulkError(err.code, err.field, err.length, err.max)
        errorsByRow.set(err.row, errorsByRow.has(err.row) ? `${errorsByRow.get(err.row)} / ${msg}` : msg)
      }
      bulkRows.value = bulkRows.value.map(r => ({ ...r, error: errorsByRow.get(r.row) ?? r.error }))
    }
    else {
      bulkSubmitError.value = t(`errors.${code}`)
    }
  }
  finally {
    bulkSubmitting.value = false
  }
}

function cancelBulk() {
  showBulk.value = false
  bulkRows.value = []
  bulkSubmitError.value = ''
  bulkSuccessMessage.value = ''
}

async function submitForm() {
  formError.value = ''
  try {
    if (editingId.value === null) {
      await authFetch('/api/admin/consultants', {
        method: 'POST',
        body: { name: formName.value, sortOrder: formSortOrder.value },
      })
    }
    else {
      await authFetch(`/api/admin/consultants/${editingId.value}`, {
        method: 'PUT',
        body: { name: formName.value, sortOrder: formSortOrder.value, isActive: formIsActive.value },
      })
    }
    showForm.value = false
    await refresh()
  }
  catch (e: any) {
    formError.value = t(`errors.${e?.data?.code ?? 'UNKNOWN'}`)
  }
}
</script>
