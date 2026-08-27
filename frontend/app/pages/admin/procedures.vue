<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-semibold text-foreground">{{ t('admin.procedures.title') }}</h1>
      <div class="flex gap-2">
        <Button variant="outline" @click="downloadTemplate">{{ t('admin.procedures.bulk.templateButton') }}</Button>
        <Button variant="outline" @click="showBulk = !showBulk">{{ t('admin.procedures.bulk.button') }}</Button>
        <Button @click="startCreate">{{ t('admin.procedures.addButton') }}</Button>
      </div>
    </div>

    <Card v-if="showBulk">
      <CardHeader>
        <CardTitle>{{ t('admin.procedures.bulk.title') }}</CardTitle>
      </CardHeader>
      <CardContent class="space-y-4">
        <div class="flex flex-col gap-1.5">
          <Label for="f-bulk-file">{{ t('admin.procedures.bulk.fileLabel') }}</Label>
          <Input id="f-bulk-file" type="file" accept=".xlsx,.xls" class="w-auto" @change="onExcelSelected" />
        </div>

        <div v-if="bulkRows.length">
          <p v-if="bulkHasError" class="mb-2 text-sm font-medium text-destructive">
            {{ t('admin.procedures.bulk.errorBanner', { count: bulkErrorCount }) }}
          </p>
          <div class="overflow-x-auto rounded-md border border-border">
            <table class="w-full text-sm">
              <thead class="bg-muted text-muted-foreground">
                <tr>
                  <th class="px-3 py-2 text-left">{{ t('admin.procedures.bulk.colRow') }}</th>
                  <th class="px-3 py-2 text-left">{{ t('admin.procedures.colCode') }}</th>
                  <th class="px-3 py-2 text-left">简体中文</th>
                  <th class="px-3 py-2 text-left">繁體中文</th>
                  <th class="px-3 py-2 text-left">English</th>
                  <th class="px-3 py-2 text-left">한국어</th>
                  <th class="px-3 py-2 text-left">{{ t('admin.procedures.colSortOrder') }}</th>
                  <th class="px-3 py-2 text-left">{{ t('admin.procedures.bulk.colError') }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="row in bulkRows" :key="row.row" class="border-t border-border" :class="row.error ? 'bg-destructive/10' : ''">
                  <td class="px-3 py-2">{{ row.row }}</td>
                  <td class="px-3 py-2">{{ row.code }}</td>
                  <td class="px-3 py-2">{{ row.nameZhCn }}</td>
                  <td class="px-3 py-2">{{ row.nameZhTw }}</td>
                  <td class="px-3 py-2">{{ row.nameEn }}</td>
                  <td class="px-3 py-2">{{ row.nameKo }}</td>
                  <td class="px-3 py-2">{{ row.sortOrder }}</td>
                  <td class="px-3 py-2 text-destructive">{{ row.error }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <div class="flex items-center gap-3">
          <Button :disabled="!bulkRows.length || bulkHasError || bulkSubmitting" @click="submitBulk">
            {{ bulkHasError ? t('admin.procedures.bulk.submitDisabled', { count: bulkErrorCount }) : t('admin.procedures.bulk.submit') }}
          </Button>
          <Button variant="outline" @click="cancelBulk">{{ t('common.cancel') }}</Button>
          <span v-if="bulkSubmitError" class="text-sm text-destructive">{{ bulkSubmitError }}</span>
          <span v-if="bulkSuccessMessage" class="text-sm text-primary">{{ bulkSuccessMessage }}</span>
        </div>
      </CardContent>
    </Card>

    <div class="flex items-center gap-1.5">
      <Checkbox id="f-show-inactive" v-model="showInactive" />
      <Label for="f-show-inactive" class="text-sm font-normal text-muted-foreground">{{ t('admin.procedures.includeInactive') }}</Label>
    </div>

    <Card v-if="showForm">
      <CardHeader>
        <CardTitle>{{ editingId === null ? t('admin.procedures.formTitleCreate') : t('admin.procedures.formTitleEdit') }}</CardTitle>
      </CardHeader>
      <CardContent class="space-y-4">
        <div class="flex flex-wrap items-end gap-4">
          <div class="flex flex-col gap-1.5">
            <Label for="f-code">{{ t('admin.procedures.formCodeLabel') }}</Label>
            <Input id="f-code" v-model="formCode" maxlength="30" class="w-40" />
          </div>
          <div class="flex flex-col gap-1.5">
            <Label for="f-sort">{{ t('admin.procedures.formSortOrderLabel') }}</Label>
            <Input id="f-sort" v-model.number="formSortOrder" type="number" class="w-24" />
          </div>
          <div v-if="editingId !== null" class="flex items-center gap-1.5 pb-2">
            <Checkbox id="f-is-active" v-model="formIsActive" />
            <Label for="f-is-active" class="text-sm font-normal">{{ t('admin.procedures.formActiveLabel') }}</Label>
          </div>
        </div>

        <!-- 12-7절 "4언어 탭 입력 폼" -->
        <div>
          <div class="flex gap-1 border-b border-border" role="tablist">
            <button
              v-for="tab in NAME_TABS" :key="tab.locale" type="button" role="tab"
              class="px-3 py-1.5 text-sm"
              :class="activeTab === tab.locale ? 'border-b-2 border-primary font-medium text-foreground' : 'text-muted-foreground'"
              @click="activeTab = tab.locale"
            >
              {{ tab.label }}
            </button>
          </div>
          <div class="flex flex-col gap-1.5 pt-3">
            <Label :for="`f-name-${activeTab}`">{{ t('admin.procedures.formNameLabel') }} — {{ activeTabLabel }}</Label>
            <Input :id="`f-name-${activeTab}`" v-model="formNames[activeTab]" maxlength="50" class="w-72" />
          </div>
        </div>

        <div class="flex items-center gap-3">
          <Button :disabled="!canSubmit" @click="submitForm">{{ t('common.save') }}</Button>
          <Button variant="outline" @click="cancelForm">{{ t('common.cancel') }}</Button>
          <span v-if="formError" class="text-sm text-destructive">{{ formError }}</span>
        </div>
      </CardContent>
    </Card>

    <div class="overflow-x-auto rounded-md border border-border">
      <table class="w-full text-sm">
        <thead class="bg-muted text-muted-foreground">
          <tr>
            <th class="px-3 py-2 text-left">{{ t('admin.procedures.colCode') }}</th>
            <th class="px-3 py-2 text-left">{{ t('admin.procedures.colName') }}</th>
            <th class="px-3 py-2 text-left">{{ t('admin.procedures.colSortOrder') }}</th>
            <th class="px-3 py-2 text-left">{{ t('admin.procedures.colActive') }}</th>
            <th class="px-3 py-2 text-left" />
          </tr>
        </thead>
        <tbody>
          <tr v-if="!procedures?.length">
            <td colspan="5" class="p-6 text-center text-muted-foreground">{{ t('admin.procedures.empty') }}</td>
          </tr>
          <tr v-for="p in procedures" :key="p.id" class="border-t border-border">
            <td class="px-3 py-2">{{ p.code }}</td>
            <td class="px-3 py-2">{{ procedureName(p) }}</td>
            <td class="px-3 py-2">{{ p.sortOrder }}</td>
            <td class="px-3 py-2">{{ p.isActive ? t('admin.procedures.activeLabel') : t('admin.procedures.inactiveLabel') }}</td>
            <td class="px-3 py-2 text-right">
              <button type="button" class="text-sm underline" @click="startEdit(p)">{{ t('admin.procedures.edit') }}</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { ProcedureLookup } from '~/types/reservation'

definePageMeta({ middleware: 'admin', layout: 'admin', i18n: false })
useHead({ title: '시술·수술 관리 | Admin', meta: [{ name: 'robots', content: 'noindex, nofollow' }] })

const { t, locale } = useI18n()
const { authFetch } = useAuthFetch()

type NameLocale = 'zh-CN' | 'zh-TW' | 'en' | 'ko'
const NAME_TABS: { locale: NameLocale, label: string }[] = [
  { locale: 'zh-CN', label: '简体中文' },
  { locale: 'zh-TW', label: '繁體中文' },
  { locale: 'en', label: 'English' },
  { locale: 'ko', label: '한국어' },
]

// 6-2절 메뉴 매트릭스로 이미 Admin/HospitalManager만 이 경로에 도달한다(middleware/admin.ts) — 화면 안에서
// 역할별 버튼을 다시 가릴 필요가 없다. 실제 방어선은 컨트롤러 액션 레벨 Authorize(11-3절).
const showInactive = ref(false)
const { data: procedures, refresh } = await useApi<ProcedureLookup[]>('/api/admin/procedures', {
  query: () => ({ includeInactive: showInactive.value }),
})

function procedureName(p: ProcedureLookup): string {
  const map: Record<string, string> = { 'zh-CN': p.nameZhCn, 'zh-TW': p.nameZhTw, en: p.nameEn, ko: p.nameKo }
  return map[locale.value] ?? p.nameKo
}

const editingId = ref<number | null>(null)
const formCode = ref('')
const formSortOrder = ref(0)
const formIsActive = ref(true)
const formNames = ref<Record<NameLocale, string>>({ 'zh-CN': '', 'zh-TW': '', en: '', ko: '' })
const activeTab = ref<NameLocale>('zh-CN')
const activeTabLabel = computed(() => NAME_TABS.find(tab => tab.locale === activeTab.value)?.label ?? '')
const formError = ref('')
const showForm = ref(false)

const canSubmit = computed(() =>
  formCode.value.trim() && NAME_TABS.every(tab => formNames.value[tab.locale].trim()))

function startCreate() {
  editingId.value = null
  formCode.value = ''
  formSortOrder.value = 0
  formIsActive.value = true
  formNames.value = { 'zh-CN': '', 'zh-TW': '', en: '', ko: '' }
  activeTab.value = 'zh-CN'
  formError.value = ''
  showForm.value = true
}

function startEdit(p: ProcedureLookup) {
  editingId.value = p.id
  formCode.value = p.code
  formSortOrder.value = p.sortOrder
  formIsActive.value = p.isActive
  formNames.value = { 'zh-CN': p.nameZhCn, 'zh-TW': p.nameZhTw, en: p.nameEn, ko: p.nameKo }
  activeTab.value = 'zh-CN'
  formError.value = ''
  showForm.value = true
}

function cancelForm() {
  showForm.value = false
}

// 엑셀 일괄등록 — excel-bulk-upload-pattern-reference.md 레이어1~2. DB 제약과 1:1 일치하는 검증만
// 프론트에서 미리 계산(code 필수·30자, 4언어명 필수·50자, 엑셀 내부 code 중복). 기존 DB와의 중복은
// 전체 목록을 프론트가 들고 있지 않으므로(비활성 미포함 필터 등) 신뢰할 수 없어 백엔드 /bulk가 전담한다.
interface ProcedureBulkRow {
  row: number
  code: string
  nameZhCn: string
  nameZhTw: string
  nameEn: string
  nameKo: string
  sortOrder: number
  error: string
}

const showBulk = ref(false)
const bulkRows = ref<ProcedureBulkRow[]>([])
const bulkSubmitting = ref(false)
const bulkSubmitError = ref('')
const bulkSuccessMessage = ref('')

const bulkErrorCount = computed(() => bulkRows.value.filter(r => r.error).length)
const bulkHasError = computed(() => bulkErrorCount.value > 0)

function fieldLabel(field?: string): string {
  const map: Record<string, string> = {
    code: t('admin.procedures.formCodeLabel'),
    nameZhCn: '简体中文',
    nameZhTw: '繁體中文',
    nameEn: 'English',
    nameKo: '한국어',
  }
  return map[field ?? ''] ?? (field ?? '')
}

function describeBulkError(code: string, field?: string, length?: number, max?: number): string {
  if (code === 'BULK_FIELD_REQUIRED') return t('errors.BULK_FIELD_REQUIRED', { field: fieldLabel(field) })
  if (code === 'BULK_FIELD_TOO_LONG') return t('errors.BULK_FIELD_TOO_LONG', { field: fieldLabel(field), length, max })
  if (code === 'BULK_CODE_DUPLICATE_IN_FILE') return t('errors.BULK_CODE_DUPLICATE_IN_FILE')
  if (code === 'BULK_CODE_DUPLICATE_EXISTING') return t('errors.BULK_CODE_DUPLICATE_EXISTING')
  return t('errors.UNKNOWN')
}

function validateField(value: string, field: string, max: number): string {
  const trimmed = value.trim()
  if (!trimmed) return describeBulkError('BULK_FIELD_REQUIRED', field)
  if (trimmed.length > max) return describeBulkError('BULK_FIELD_TOO_LONG', field, trimmed.length, max)
  return ''
}

function validateBulkRow(row: Omit<ProcedureBulkRow, 'row' | 'error'>): string {
  return validateField(row.code, 'code', 30)
    || validateField(row.nameZhCn, 'nameZhCn', 50)
    || validateField(row.nameZhTw, 'nameZhTw', 50)
    || validateField(row.nameEn, 'nameEn', 50)
    || validateField(row.nameKo, 'nameKo', 50)
}

// 동적 import — xlsx는 클릭 시점에만 필요한 클라이언트 전용 라이브러리라 최상단 정적 import 대신 여기서 로드한다.
// 빌드 산출물 확인 결과 xlsx는 별도 청크(chunks/_/xlsx.mjs, gzip 162KB)로 분리되며, SSR 렌더 경로(이 함수들은
// 브라우저 클릭에서만 실행됨)에서는 로드되지 않는다 — 단, Cloudflare Workers 배포 시 wrangler가 이 청크를
// 최종 워커 스크립트에 실제로 얼마나 포함하는지는 별도 확인 필요([미확인], 현재 gzip 총합 695KB로 한도 내 안전).
async function downloadTemplate() {
  const XLSX = await import('xlsx')
  const ws = XLSX.utils.aoa_to_sheet([[
    t('admin.procedures.formCodeLabel'), '简体中文', '繁體中文', 'English', '한국어', t('admin.procedures.colSortOrder'),
  ]])
  ws['!cols'] = [{ wch: 16 }, { wch: 24 }, { wch: 24 }, { wch: 24 }, { wch: 24 }, { wch: 12 }]
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, 'template')
  XLSX.writeFile(wb, 'procedures_template.xlsx')
}

async function onExcelSelected(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (!file) return
  const XLSX = await import('xlsx')
  const data = await file.arrayBuffer()
  const wb = XLSX.read(data)
  const ws = wb.Sheets[wb.SheetNames[0]!]!
  const aoa = XLSX.utils.sheet_to_json(ws, { header: 1 }) as unknown[][]

  const parsed = aoa
    .map((r, idx) => ({ r, excelRow: idx + 1 }))
    .slice(1)
    .filter(x => x.r.some(c => String(c ?? '').trim()))
    .map(({ r, excelRow }) => ({
      row: excelRow,
      code: String(r[0] ?? '').trim(),
      nameZhCn: String(r[1] ?? '').trim(),
      nameZhTw: String(r[2] ?? '').trim(),
      nameEn: String(r[3] ?? '').trim(),
      nameKo: String(r[4] ?? '').trim(),
      sortOrder: Number(r[5] ?? 0) || 0,
    }))

  // 엑셀 내부 code 중복 — 서버 조회 없이 배치 자체에서 계산(레이어1 이중 방어, 백엔드와 동일 규칙).
  const codeCounts = new Map<string, number>()
  for (const p of parsed) if (p.code) codeCounts.set(p.code, (codeCounts.get(p.code) ?? 0) + 1)

  bulkRows.value = parsed.map((p) => {
    const baseError = validateBulkRow(p)
    const dupError = !baseError && p.code && (codeCounts.get(p.code) ?? 0) > 1
      ? describeBulkError('BULK_CODE_DUPLICATE_IN_FILE')
      : ''
    return { ...p, error: baseError || dupError }
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
    const res = await authFetch<{ successCount: number }>('/api/admin/procedures/bulk', {
      method: 'POST',
      body: bulkRows.value.map(r => ({
        row: r.row, code: r.code, nameZhCn: r.nameZhCn, nameZhTw: r.nameZhTw, nameEn: r.nameEn, nameKo: r.nameKo, sortOrder: r.sortOrder,
      })),
    })
    bulkSuccessMessage.value = t('admin.procedures.bulk.successMessage', { count: res.successCount })
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
  const body = {
    code: formCode.value,
    nameZhCn: formNames.value['zh-CN'],
    nameZhTw: formNames.value['zh-TW'],
    nameEn: formNames.value.en,
    nameKo: formNames.value.ko,
    sortOrder: formSortOrder.value,
  }
  try {
    if (editingId.value === null) {
      await authFetch('/api/admin/procedures', { method: 'POST', body })
    }
    else {
      await authFetch(`/api/admin/procedures/${editingId.value}`, {
        method: 'PUT',
        body: { ...body, isActive: formIsActive.value },
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
