<template>
  <div class="space-y-6">
    <h1 class="text-xl font-semibold text-foreground">{{ t('admin.procedures.title') }}</h1>

    <!-- 탭 2개 — 별도 메뉴를 늘리지 않고 한 화면 안에서 전환(D25) -->
    <div class="flex gap-1 border-b border-border" role="tablist">
      <button
        v-for="tab in MAIN_TABS" :key="tab.key" type="button" role="tab"
        class="px-4 py-2 text-sm"
        :class="mainTab === tab.key ? 'border-b-2 border-primary font-medium text-foreground' : 'text-muted-foreground'"
        @click="mainTab = tab.key"
      >
        {{ t(tab.labelKey) }}
      </button>
    </div>

    <!-- ============================ 카테고리 관리 탭 ============================ -->
    <section v-show="mainTab === 'categories'" class="space-y-6">
      <div class="flex justify-end gap-2">
        <Button variant="outline" @click="downloadCategoryTemplate">{{ t('admin.categories.bulk.templateButton') }}</Button>
        <Button variant="outline" @click="catShowBulk = !catShowBulk">{{ t('admin.categories.bulk.button') }}</Button>
        <Button @click="catStartCreate">{{ t('admin.categories.addButton') }}</Button>
      </div>

      <Card v-if="catShowBulk">
        <CardHeader><CardTitle>{{ t('admin.categories.bulk.title') }}</CardTitle></CardHeader>
        <CardContent class="space-y-4">
          <div class="flex flex-col gap-1.5">
            <Label for="cat-bulk-file">{{ t('admin.categories.bulk.fileLabel') }}</Label>
            <Input id="cat-bulk-file" type="file" accept=".xlsx,.xls" class="w-auto" @change="onCategoryExcelSelected" />
          </div>

          <div v-if="catBulkRows.length">
            <p v-if="catBulkErrorCount > 0" class="mb-2 text-sm font-medium text-destructive">
              {{ t('admin.categories.bulk.errorBanner', { count: catBulkErrorCount }) }}
            </p>
            <div class="overflow-x-auto rounded-md border border-border">
              <table class="w-full text-sm">
                <thead class="bg-muted text-muted-foreground">
                  <tr>
                    <th class="px-3 py-2 text-left">{{ t('admin.categories.bulk.colRow') }}</th>
                    <th class="px-3 py-2 text-left">{{ t('admin.categories.colCode') }}</th>
                    <th class="px-3 py-2 text-left">简体中文</th>
                    <th class="px-3 py-2 text-left">繁體中文</th>
                    <th class="px-3 py-2 text-left">English</th>
                    <th class="px-3 py-2 text-left">한국어</th>
                    <th class="px-3 py-2 text-left">{{ t('admin.categories.bulk.colError') }}</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="row in catBulkRows" :key="row.row" class="border-t border-border" :class="row.error ? 'bg-destructive/10' : ''">
                    <td class="px-3 py-2">{{ row.row }}</td>
                    <td class="px-3 py-2">{{ row.code }}</td>
                    <td class="px-3 py-2">{{ row.nameZhCn }}</td>
                    <td class="px-3 py-2">{{ row.nameZhTw }}</td>
                    <td class="px-3 py-2">{{ row.nameEn }}</td>
                    <td class="px-3 py-2">{{ row.nameKo }}</td>
                    <td class="px-3 py-2 text-destructive">{{ row.error }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <div class="flex items-center gap-3">
            <Button :disabled="!catBulkRows.length || catBulkErrorCount > 0 || catBulkSubmitting" @click="submitCategoryBulk">
              {{ catBulkErrorCount > 0 ? t('admin.categories.bulk.submitDisabled', { count: catBulkErrorCount }) : t('admin.categories.bulk.submit') }}
            </Button>
            <Button variant="outline" @click="cancelCategoryBulk">{{ t('common.cancel') }}</Button>
            <span v-if="catBulkSubmitError" class="text-sm text-destructive">{{ catBulkSubmitError }}</span>
            <span v-if="catBulkSuccessMessage" class="text-sm text-primary">{{ catBulkSuccessMessage }}</span>
          </div>
        </CardContent>
      </Card>

      <div class="flex flex-wrap items-end gap-4">
        <div class="flex items-center gap-1.5">
          <Checkbox id="cat-show-inactive" v-model="catShowInactive" />
          <Label for="cat-show-inactive" class="text-sm font-normal text-muted-foreground">{{ t('admin.categories.includeInactive') }}</Label>
        </div>
        <div class="flex min-w-[200px] flex-1 flex-col gap-1.5">
          <Label for="cat-search">{{ t('admin.categories.filterSearch') }}</Label>
          <Input
            id="cat-search" v-model="catFormSearch" maxlength="200"
            :placeholder="t('admin.categories.filterSearchPlaceholder')"
            @keyup.enter="applyCategorySearch"
          />
        </div>
        <Button @click="applyCategorySearch">{{ t('admin.reservations.filterApply') }}</Button>
      </div>

      <Card v-if="catShowForm">
        <CardHeader>
          <CardTitle>{{ catEditingId === null ? t('admin.categories.formTitleCreate') : t('admin.categories.formTitleEdit') }}</CardTitle>
        </CardHeader>
        <CardContent class="space-y-4">
          <div class="flex flex-wrap items-end gap-4">
            <div class="flex flex-col gap-1.5">
              <Label for="cat-code">{{ t('admin.categories.formCodeLabel') }}</Label>
              <Input id="cat-code" v-model="catFormCode" maxlength="30" class="w-40" />
            </div>
            <div v-if="catEditingId !== null" class="flex items-center gap-1.5 pb-2">
              <Checkbox id="cat-is-active" v-model="catFormIsActive" />
              <Label for="cat-is-active" class="text-sm font-normal">{{ t('admin.categories.formActiveLabel') }}</Label>
            </div>
          </div>

          <div>
            <div class="flex gap-1 border-b border-border" role="tablist">
              <button
                v-for="tab in NAME_TABS" :key="tab.locale" type="button" role="tab"
                class="px-3 py-1.5 text-sm"
                :class="catActiveNameTab === tab.locale ? 'border-b-2 border-primary font-medium text-foreground' : 'text-muted-foreground'"
                @click="catActiveNameTab = tab.locale"
              >
                {{ tab.label }}
              </button>
            </div>
            <div class="flex flex-col gap-1.5 pt-3">
              <Label :for="`cat-name-${catActiveNameTab}`">{{ t('admin.categories.formNameLabel') }} — {{ nameTabLabel(catActiveNameTab) }}</Label>
              <Input :id="`cat-name-${catActiveNameTab}`" v-model="catFormNames[catActiveNameTab]" maxlength="50" class="w-72" />
            </div>
          </div>

          <div class="flex items-center gap-3">
            <Button :disabled="!catCanSubmit" @click="submitCategoryForm">{{ t('common.save') }}</Button>
            <Button variant="outline" @click="catShowForm = false">{{ t('common.cancel') }}</Button>
            <span v-if="catFormError" class="text-sm text-destructive">{{ catFormError }}</span>
          </div>
        </CardContent>
      </Card>

      <div class="overflow-x-auto rounded-md border border-border">
        <table class="w-full text-sm">
          <thead class="bg-muted text-muted-foreground">
            <tr>
              <th class="px-3 py-2 text-left">{{ t('admin.categories.colCode') }}</th>
              <th class="px-3 py-2 text-left">{{ t('admin.categories.colName') }}</th>
              <th class="px-3 py-2 text-left">{{ t('admin.categories.colActive') }}</th>
              <th class="px-3 py-2 text-left" />
            </tr>
          </thead>
          <tbody>
            <tr v-if="!categories.length">
              <td colspan="4" class="p-6 text-center text-muted-foreground">{{ t('admin.categories.empty') }}</td>
            </tr>
            <tr v-for="c in categories" :key="c.id" class="border-t border-border">
              <td class="px-3 py-2">{{ c.code }}</td>
              <td class="px-3 py-2">{{ nameOf(c) }}</td>
              <td class="px-3 py-2">{{ c.isActive ? t('admin.categories.activeLabel') : t('admin.categories.inactiveLabel') }}</td>
              <td class="px-3 py-2 text-right">
                <button type="button" class="text-sm underline" @click="catStartEdit(c)">{{ t('admin.categories.edit') }}</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <Pagination :page="catPage" :total-pages="catTotalPages" @update:page="goCategoryPage" />
    </section>

    <!-- ============================ 시술·수술 관리 탭 ============================ -->
    <section v-show="mainTab === 'procedures'" class="space-y-6">
      <div class="flex justify-end gap-2">
        <Button variant="outline" @click="downloadProcedureTemplate">{{ t('admin.procedures.bulk.templateButton') }}</Button>
        <Button variant="outline" @click="procShowBulk = !procShowBulk">{{ t('admin.procedures.bulk.button') }}</Button>
        <Button :disabled="!activeCategories.length" @click="procStartCreate">{{ t('admin.procedures.addButton') }}</Button>
      </div>

      <p v-if="!activeCategories.length" class="rounded-md border border-border bg-muted/50 px-4 py-3 text-sm text-muted-foreground">
        {{ t('admin.procedures.noCategoryHint') }}
      </p>

      <Card v-if="procShowBulk">
        <CardHeader><CardTitle>{{ t('admin.procedures.bulk.title') }}</CardTitle></CardHeader>
        <CardContent class="space-y-4">
          <div class="flex flex-col gap-1.5">
            <Label for="proc-bulk-file">{{ t('admin.procedures.bulk.fileLabel') }}</Label>
            <Input id="proc-bulk-file" type="file" accept=".xlsx,.xls" class="w-auto" @change="onProcedureExcelSelected" />
          </div>

          <div v-if="procBulkRows.length">
            <p v-if="procBulkErrorCount > 0" class="mb-2 text-sm font-medium text-destructive">
              {{ t('admin.procedures.bulk.errorBanner', { count: procBulkErrorCount }) }}
            </p>
            <div class="overflow-x-auto rounded-md border border-border">
              <table class="w-full text-sm">
                <thead class="bg-muted text-muted-foreground">
                  <tr>
                    <th class="px-3 py-2 text-left">{{ t('admin.procedures.bulk.colRow') }}</th>
                    <th class="px-3 py-2 text-left">{{ t('admin.procedures.colCode') }}</th>
                    <th class="px-3 py-2 text-left">{{ t('admin.procedures.bulk.colCategoryCode') }}</th>
                    <th class="px-3 py-2 text-left">简体中文</th>
                    <th class="px-3 py-2 text-left">繁體中文</th>
                    <th class="px-3 py-2 text-left">English</th>
                    <th class="px-3 py-2 text-left">한국어</th>
                    <th class="px-3 py-2 text-left">{{ t('admin.procedures.bulk.colError') }}</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="row in procBulkRows" :key="row.row" class="border-t border-border" :class="row.error ? 'bg-destructive/10' : ''">
                    <td class="px-3 py-2">{{ row.row }}</td>
                    <td class="px-3 py-2">{{ row.code }}</td>
                    <td class="px-3 py-2">{{ row.categoryCode }}</td>
                    <td class="px-3 py-2">{{ row.nameZhCn }}</td>
                    <td class="px-3 py-2">{{ row.nameZhTw }}</td>
                    <td class="px-3 py-2">{{ row.nameEn }}</td>
                    <td class="px-3 py-2">{{ row.nameKo }}</td>
                    <td class="px-3 py-2 text-destructive">{{ row.error }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <div class="flex items-center gap-3">
            <Button :disabled="!procBulkRows.length || procBulkErrorCount > 0 || procBulkSubmitting" @click="submitProcedureBulk">
              {{ procBulkErrorCount > 0 ? t('admin.procedures.bulk.submitDisabled', { count: procBulkErrorCount }) : t('admin.procedures.bulk.submit') }}
            </Button>
            <Button variant="outline" @click="cancelProcedureBulk">{{ t('common.cancel') }}</Button>
            <span v-if="procBulkSubmitError" class="text-sm text-destructive">{{ procBulkSubmitError }}</span>
            <span v-if="procBulkSuccessMessage" class="text-sm text-primary">{{ procBulkSuccessMessage }}</span>
          </div>
        </CardContent>
      </Card>

      <div class="flex flex-wrap items-end gap-4">
        <div class="flex items-center gap-1.5">
          <Checkbox id="proc-show-inactive" v-model="procShowInactive" />
          <Label for="proc-show-inactive" class="text-sm font-normal text-muted-foreground">{{ t('admin.procedures.includeInactive') }}</Label>
        </div>
        <div class="flex min-w-[200px] flex-1 flex-col gap-1.5">
          <Label for="proc-search">{{ t('admin.procedures.filterSearch') }}</Label>
          <Input
            id="proc-search" v-model="procFormSearch" maxlength="200"
            :placeholder="t('admin.procedures.filterSearchPlaceholder')"
            @keyup.enter="applyProcedureSearch"
          />
        </div>
        <Button @click="applyProcedureSearch">{{ t('admin.reservations.filterApply') }}</Button>
      </div>

      <Card v-if="procShowForm">
        <CardHeader>
          <CardTitle>{{ procEditingId === null ? t('admin.procedures.formTitleCreate') : t('admin.procedures.formTitleEdit') }}</CardTitle>
        </CardHeader>
        <CardContent class="space-y-4">
          <div class="flex flex-wrap items-end gap-4">
            <div class="flex flex-col gap-1.5">
              <Label for="proc-code">{{ t('admin.procedures.formCodeLabel') }}</Label>
              <Input id="proc-code" v-model="procFormCode" maxlength="30" class="w-40" />
            </div>
            <div class="flex flex-col gap-1.5">
              <Label for="proc-category">{{ t('admin.procedures.formCategoryLabel') }}</Label>
              <NativeSelect id="proc-category" v-model="procFormCategoryId" class="w-56">
                <NativeSelectOption value="">{{ t('admin.procedures.categoryPlaceholder') }}</NativeSelectOption>
                <NativeSelectOption v-for="c in procFormCategoryOptions" :key="c.id" :value="String(c.id)">
                  {{ nameOf(c) }}{{ c.isActive ? '' : ` (${t('admin.procedures.inactiveLabel')})` }}
                </NativeSelectOption>
              </NativeSelect>
            </div>
            <div v-if="procEditingId !== null" class="flex items-center gap-1.5 pb-2">
              <Checkbox id="proc-is-active" v-model="procFormIsActive" />
              <Label for="proc-is-active" class="text-sm font-normal">{{ t('admin.procedures.formActiveLabel') }}</Label>
            </div>
          </div>

          <div>
            <div class="flex gap-1 border-b border-border" role="tablist">
              <button
                v-for="tab in NAME_TABS" :key="tab.locale" type="button" role="tab"
                class="px-3 py-1.5 text-sm"
                :class="procActiveNameTab === tab.locale ? 'border-b-2 border-primary font-medium text-foreground' : 'text-muted-foreground'"
                @click="procActiveNameTab = tab.locale"
              >
                {{ tab.label }}
              </button>
            </div>
            <div class="flex flex-col gap-1.5 pt-3">
              <Label :for="`proc-name-${procActiveNameTab}`">{{ t('admin.procedures.formNameLabel') }} — {{ nameTabLabel(procActiveNameTab) }}</Label>
              <Input :id="`proc-name-${procActiveNameTab}`" v-model="procFormNames[procActiveNameTab]" maxlength="50" class="w-72" />
            </div>
          </div>

          <div class="flex items-center gap-3">
            <Button :disabled="!procCanSubmit" @click="submitProcedureForm">{{ t('common.save') }}</Button>
            <Button variant="outline" @click="procShowForm = false">{{ t('common.cancel') }}</Button>
            <span v-if="procFormError" class="text-sm text-destructive">{{ procFormError }}</span>
          </div>
        </CardContent>
      </Card>

      <div class="overflow-x-auto rounded-md border border-border">
        <table class="w-full text-sm">
          <thead class="bg-muted text-muted-foreground">
            <tr>
              <th class="px-3 py-2 text-left">{{ t('admin.procedures.colCode') }}</th>
              <th class="px-3 py-2 text-left">{{ t('admin.procedures.colCategory') }}</th>
              <th class="px-3 py-2 text-left">{{ t('admin.procedures.colName') }}</th>
              <th class="px-3 py-2 text-left">{{ t('admin.procedures.colActive') }}</th>
              <th class="px-3 py-2 text-left" />
            </tr>
          </thead>
          <tbody>
            <tr v-if="!procedures.length">
              <td colspan="5" class="p-6 text-center text-muted-foreground">{{ t('admin.procedures.empty') }}</td>
            </tr>
            <tr v-for="p in procedures" :key="p.id" class="border-t border-border">
              <td class="px-3 py-2">{{ p.code }}</td>
              <td class="px-3 py-2">{{ categoryNameById(p.categoryId) }}</td>
              <td class="px-3 py-2">{{ nameOf(p) }}</td>
              <td class="px-3 py-2">{{ p.isActive ? t('admin.procedures.activeLabel') : t('admin.procedures.inactiveLabel') }}</td>
              <td class="px-3 py-2 text-right">
                <button type="button" class="text-sm underline" @click="procStartEdit(p)">{{ t('admin.procedures.edit') }}</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <Pagination :page="procPage" :total-pages="procTotalPages" @update:page="goProcedurePage" />
    </section>
  </div>
</template>

<script setup lang="ts">
import type { CategoryLookup, PagedResult, ProcedureLookup } from '~/types/reservation'

definePageMeta({ middleware: 'admin', layout: 'admin', i18n: false })
useHead({ title: '시술·수술 관리 | Admin', meta: [{ name: 'robots', content: 'noindex, nofollow' }] })

const { t, locale } = useI18n()
const { authFetch } = useAuthFetch()
const route = useRoute()

type NameLocale = 'zh-CN' | 'zh-TW' | 'en' | 'ko'
const NAME_TABS: { locale: NameLocale, label: string }[] = [
  { locale: 'zh-CN', label: '简体中文' },
  { locale: 'zh-TW', label: '繁體中文' },
  { locale: 'en', label: 'English' },
  { locale: 'ko', label: '한국어' },
]
function nameTabLabel(l: NameLocale): string {
  return NAME_TABS.find(tab => tab.locale === l)?.label ?? ''
}

// 카테고리·시술 공통 — 현재 UI 로케일의 이름을 반환(D25, "나열하는 모든 곳 이름 오름차순"의 표시측).
// 정렬 자체는 백엔드가 locale 파라미터로 이미 처리하므로 여기선 표시만.
type NamedRow = { nameZhCn: string, nameZhTw: string, nameEn: string, nameKo: string }
function nameOf(row: NamedRow): string {
  const map: Record<string, string> = { 'zh-CN': row.nameZhCn, 'zh-TW': row.nameZhTw, en: row.nameEn, ko: row.nameKo }
  return map[locale.value] ?? row.nameKo
}

const MAIN_TABS = [
  { key: 'categories', labelKey: 'admin.procedures.tabCategories' },
  { key: 'procedures', labelKey: 'admin.procedures.tabProcedures' },
] as const
type MainTab = typeof MAIN_TABS[number]['key']
const mainTab = computed<MainTab>({
  get: () => (route.query.tab === 'categories' ? 'categories' : 'procedures'),
  set: (v: MainTab) => navigateTo({ query: { ...route.query, tab: v } }),
})

// ─────────────────────────── 카테고리 관리 탭 ───────────────────────────
// 🔴 검색 입력을 반응형 query에 직접 물리지 말 것(12-4절, procedures.vue 기존 패턴 유지). page·includeInactive도
// URL 쿼리로 두고 computed getter/setter로 navigateTo 1회 = 요청 1회가 되게 한다. 탭별로 쿼리 키를 분리해
// (c* / p*) 탭 전환 시 서로의 페이지·검색이 섞이지 않게 한다.
const catQuery = computed(() => ({
  page: Number(route.query.cPage) || 1,
  pageSize: 20,
  includeInactive: route.query.cInactive === '1',
  search: (route.query.cSearch as string) || undefined,
  locale: locale.value,
}))
const catShowInactive = computed({
  get: () => catQuery.value.includeInactive,
  set: (v: boolean) => navigateTo({ query: { ...route.query, cPage: 1, cInactive: v ? '1' : undefined } }),
})
const { data: categoriesPaged, refresh: refreshCategories } = await useApi<PagedResult<CategoryLookup>>('/api/admin/categories', { query: catQuery })
const categories = computed(() => categoriesPaged.value?.items ?? [])
const catPage = computed(() => catQuery.value.page)
const catTotalPages = computed(() => categoriesPaged.value ? Math.max(1, Math.ceil(categoriesPaged.value.total / categoriesPaged.value.pageSize)) : 1)

const catFormSearch = ref(catQuery.value.search ?? '')
function applyCategorySearch() {
  navigateTo({ query: { ...route.query, cPage: 1, cSearch: catFormSearch.value || undefined } })
}
function goCategoryPage(p: number) {
  navigateTo({ query: { ...route.query, cPage: p } })
}

const catEditingId = ref<number | null>(null)
const catFormCode = ref('')
const catFormIsActive = ref(true)
const catFormNames = ref<Record<NameLocale, string>>({ 'zh-CN': '', 'zh-TW': '', en: '', ko: '' })
const catActiveNameTab = ref<NameLocale>('zh-CN')
const catFormError = ref('')
const catShowForm = ref(false)
const catCanSubmit = computed(() =>
  !!catFormCode.value.trim() && NAME_TABS.every(tab => catFormNames.value[tab.locale].trim()))

function catStartCreate() {
  catEditingId.value = null
  catFormCode.value = ''
  catFormIsActive.value = true
  catFormNames.value = { 'zh-CN': '', 'zh-TW': '', en: '', ko: '' }
  catActiveNameTab.value = 'zh-CN'
  catFormError.value = ''
  catShowForm.value = true
}
function catStartEdit(c: CategoryLookup) {
  catEditingId.value = c.id
  catFormCode.value = c.code
  catFormIsActive.value = c.isActive
  catFormNames.value = { 'zh-CN': c.nameZhCn, 'zh-TW': c.nameZhTw, en: c.nameEn, ko: c.nameKo }
  catActiveNameTab.value = 'zh-CN'
  catFormError.value = ''
  catShowForm.value = true
}
async function submitCategoryForm() {
  catFormError.value = ''
  const body = {
    code: catFormCode.value,
    nameZhCn: catFormNames.value['zh-CN'],
    nameZhTw: catFormNames.value['zh-TW'],
    nameEn: catFormNames.value.en,
    nameKo: catFormNames.value.ko,
  }
  try {
    if (catEditingId.value === null) {
      await authFetch('/api/admin/categories', { method: 'POST', body })
    } else {
      await authFetch(`/api/admin/categories/${catEditingId.value}`, { method: 'PUT', body: { ...body, isActive: catFormIsActive.value } })
    }
    catShowForm.value = false
    await Promise.all([refreshCategories(), refreshCategoryOptions()])
  } catch (e: any) {
    catFormError.value = t(`errors.${e?.data?.code ?? 'UNKNOWN'}`)
  }
}

// 카테고리 엑셀 일괄등록 — excel-bulk-upload-pattern-reference.md 레이어1~2(레이어3은 백엔드 /bulk).
interface CategoryBulkRow { row: number, code: string, nameZhCn: string, nameZhTw: string, nameEn: string, nameKo: string, error: string }
const catShowBulk = ref(false)
const catBulkRows = ref<CategoryBulkRow[]>([])
const catBulkSubmitting = ref(false)
const catBulkSubmitError = ref('')
const catBulkSuccessMessage = ref('')
const catBulkErrorCount = computed(() => catBulkRows.value.filter(r => r.error).length)

function fieldLabel(field?: string): string {
  const map: Record<string, string> = {
    code: t('admin.procedures.formCodeLabel'),
    categoryCode: t('admin.procedures.bulk.colCategoryCode'),
    nameZhCn: '简体中文', nameZhTw: '繁體中文', nameEn: 'English', nameKo: '한국어',
  }
  return map[field ?? ''] ?? (field ?? '')
}
function describeBulkError(code: string, field?: string, length?: number, max?: number): string {
  if (code === 'BULK_FIELD_REQUIRED') return t('errors.BULK_FIELD_REQUIRED', { field: fieldLabel(field) })
  if (code === 'BULK_FIELD_TOO_LONG') return t('errors.BULK_FIELD_TOO_LONG', { field: fieldLabel(field), length, max })
  if (code === 'BULK_CODE_DUPLICATE_IN_FILE') return t('errors.BULK_CODE_DUPLICATE_IN_FILE')
  if (code === 'BULK_CODE_DUPLICATE_EXISTING') return t('errors.BULK_CODE_DUPLICATE_EXISTING')
  if (code === 'BULK_CATEGORY_NOT_FOUND') return t('errors.BULK_CATEGORY_NOT_FOUND')
  return t('errors.UNKNOWN')
}
function validateNameFields(r: { nameZhCn: string, nameZhTw: string, nameEn: string, nameKo: string }): string {
  const check = (v: string, f: string) => {
    const trimmed = v.trim()
    if (!trimmed) return describeBulkError('BULK_FIELD_REQUIRED', f)
    if (trimmed.length > 50) return describeBulkError('BULK_FIELD_TOO_LONG', f, trimmed.length, 50)
    return ''
  }
  return check(r.nameZhCn, 'nameZhCn') || check(r.nameZhTw, 'nameZhTw') || check(r.nameEn, 'nameEn') || check(r.nameKo, 'nameKo')
}
function validateCode(code: string): string {
  const trimmed = code.trim()
  if (!trimmed) return describeBulkError('BULK_FIELD_REQUIRED', 'code')
  if (trimmed.length > 30) return describeBulkError('BULK_FIELD_TOO_LONG', 'code', trimmed.length, 30)
  return ''
}

async function downloadCategoryTemplate() {
  const XLSX = await import('xlsx')
  const ws = XLSX.utils.aoa_to_sheet([[t('admin.categories.formCodeLabel'), '简体中文', '繁體中文', 'English', '한국어']])
  ws['!cols'] = [{ wch: 16 }, { wch: 24 }, { wch: 24 }, { wch: 24 }, { wch: 24 }]
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, 'template')
  XLSX.writeFile(wb, 'categories_template.xlsx')
}
async function onCategoryExcelSelected(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (!file) return
  const XLSX = await import('xlsx')
  const wb = XLSX.read(await file.arrayBuffer())
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
    }))
  const codeCounts = new Map<string, number>()
  for (const p of parsed) if (p.code) codeCounts.set(p.code, (codeCounts.get(p.code) ?? 0) + 1)
  catBulkRows.value = parsed.map((p) => {
    const baseError = validateCode(p.code) || validateNameFields(p)
    const dupError = !baseError && p.code && (codeCounts.get(p.code) ?? 0) > 1 ? describeBulkError('BULK_CODE_DUPLICATE_IN_FILE') : ''
    return { ...p, error: baseError || dupError }
  })
  catBulkSubmitError.value = ''
  catBulkSuccessMessage.value = ''
}
function applyRowErrors<T extends { row: number, error: string }>(rows: T[], e: any): T[] {
  const errorsByRow = new Map<number, string>()
  for (const err of e.data.rowErrors as { row: number, code: string, field?: string, length?: number, max?: number }[]) {
    const msg = describeBulkError(err.code, err.field, err.length, err.max)
    errorsByRow.set(err.row, errorsByRow.has(err.row) ? `${errorsByRow.get(err.row)} / ${msg}` : msg)
  }
  return rows.map(r => ({ ...r, error: errorsByRow.get(r.row) ?? r.error }))
}
async function submitCategoryBulk() {
  if (!catBulkRows.value.length || catBulkErrorCount.value > 0) return
  catBulkSubmitError.value = ''
  catBulkSuccessMessage.value = ''
  catBulkSubmitting.value = true
  try {
    const res = await authFetch<{ successCount: number }>('/api/admin/categories/bulk', {
      method: 'POST',
      body: catBulkRows.value.map(r => ({ row: r.row, code: r.code, nameZhCn: r.nameZhCn, nameZhTw: r.nameZhTw, nameEn: r.nameEn, nameKo: r.nameKo })),
    })
    catBulkSuccessMessage.value = t('admin.categories.bulk.successMessage', { count: res.successCount })
    catBulkRows.value = []
    catShowBulk.value = false
    await Promise.all([refreshCategories(), refreshCategoryOptions()])
  } catch (e: any) {
    const code = e?.data?.code ?? 'UNKNOWN'
    if (code === 'BULK_VALIDATION_FAILED' && Array.isArray(e?.data?.rowErrors)) {
      catBulkRows.value = applyRowErrors(catBulkRows.value, e)
    } else {
      catBulkSubmitError.value = t(`errors.${code}`)
    }
  } finally {
    catBulkSubmitting.value = false
  }
}
function cancelCategoryBulk() {
  catShowBulk.value = false
  catBulkRows.value = []
  catBulkSubmitError.value = ''
  catBulkSuccessMessage.value = ''
}

// ─────────────────────────── 시술·수술 관리 탭 ───────────────────────────
const procQuery = computed(() => ({
  page: Number(route.query.pPage) || 1,
  pageSize: 20,
  includeInactive: route.query.pInactive === '1',
  search: (route.query.pSearch as string) || undefined,
  locale: locale.value,
}))
const procShowInactive = computed({
  get: () => procQuery.value.includeInactive,
  set: (v: boolean) => navigateTo({ query: { ...route.query, pPage: 1, pInactive: v ? '1' : undefined } }),
})
const { data: proceduresPaged, refresh: refreshProcedures } = await useApi<PagedResult<ProcedureLookup>>('/api/admin/procedures', { query: procQuery })
const procedures = computed(() => proceduresPaged.value?.items ?? [])
const procPage = computed(() => procQuery.value.page)
const procTotalPages = computed(() => proceduresPaged.value ? Math.max(1, Math.ceil(proceduresPaged.value.total / proceduresPaged.value.pageSize)) : 1)

const procFormSearch = ref(procQuery.value.search ?? '')
function applyProcedureSearch() {
  navigateTo({ query: { ...route.query, pPage: 1, pSearch: procFormSearch.value || undefined } })
}
function goProcedurePage(p: number) {
  navigateTo({ query: { ...route.query, pPage: p } })
}

// 시술 폼의 카테고리 select·엑셀 카테고리코드 검증에 쓸 "전체 카테고리" 목록(비활성 포함 — 비활성
// 카테고리에 속한 시술을 편집할 때 그 카테고리가 옵션에서 사라지면 저장 시 조용히 바뀌므로, 8-3절
// 비활성 시술 함정과 동일하게 유지한다). 정렬 기준 로케일도 함께 넘겨 이름 오름차순으로 받는다.
const { data: allCategoriesPaged, refresh: refreshCategoryOptions }
  = await useApi<PagedResult<CategoryLookup>>('/api/admin/categories', { query: computed(() => ({ includeInactive: true, pageSize: 100, locale: locale.value })) })
const allCategories = computed(() => allCategoriesPaged.value?.items ?? [])
const activeCategories = computed(() => allCategories.value.filter(c => c.isActive))
const categoryNameMap = computed(() => new Map(allCategories.value.map(c => [c.id, nameOf(c)])))
function categoryNameById(id: number): string {
  return categoryNameMap.value.get(id) ?? `#${id}`
}

const procEditingId = ref<number | null>(null)
const procFormCode = ref('')
const procFormCategoryId = ref('')
const procFormIsActive = ref(true)
const procFormNames = ref<Record<NameLocale, string>>({ 'zh-CN': '', 'zh-TW': '', en: '', ko: '' })
const procActiveNameTab = ref<NameLocale>('zh-CN')
const procFormError = ref('')
const procShowForm = ref(false)
// 활성 카테고리 + (편집 중이면) 현재 소속 카테고리가 비활성이어도 옵션에 남긴다.
const procFormCategoryOptions = computed(() => {
  const current = procFormCategoryId.value ? Number(procFormCategoryId.value) : null
  return allCategories.value.filter(c => c.isActive || c.id === current)
})
const procCanSubmit = computed(() =>
  !!procFormCode.value.trim() && !!procFormCategoryId.value
  && NAME_TABS.every(tab => procFormNames.value[tab.locale].trim()))

function procStartCreate() {
  procEditingId.value = null
  procFormCode.value = ''
  procFormCategoryId.value = ''
  procFormIsActive.value = true
  procFormNames.value = { 'zh-CN': '', 'zh-TW': '', en: '', ko: '' }
  procActiveNameTab.value = 'zh-CN'
  procFormError.value = ''
  procShowForm.value = true
}
function procStartEdit(p: ProcedureLookup) {
  procEditingId.value = p.id
  procFormCode.value = p.code
  procFormCategoryId.value = String(p.categoryId)
  procFormIsActive.value = p.isActive
  procFormNames.value = { 'zh-CN': p.nameZhCn, 'zh-TW': p.nameZhTw, en: p.nameEn, ko: p.nameKo }
  procActiveNameTab.value = 'zh-CN'
  procFormError.value = ''
  procShowForm.value = true
}
async function submitProcedureForm() {
  procFormError.value = ''
  const body = {
    code: procFormCode.value,
    categoryId: Number(procFormCategoryId.value),
    nameZhCn: procFormNames.value['zh-CN'],
    nameZhTw: procFormNames.value['zh-TW'],
    nameEn: procFormNames.value.en,
    nameKo: procFormNames.value.ko,
  }
  try {
    if (procEditingId.value === null) {
      await authFetch('/api/admin/procedures', { method: 'POST', body })
    } else {
      await authFetch(`/api/admin/procedures/${procEditingId.value}`, { method: 'PUT', body: { ...body, isActive: procFormIsActive.value } })
    }
    procShowForm.value = false
    await refreshProcedures()
  } catch (e: any) {
    procFormError.value = t(`errors.${e?.data?.code ?? 'UNKNOWN'}`)
  }
}

// 시술 엑셀 일괄등록 — 카테고리는 카테고리 코드로 지정(D25). 레이어1에서 이미 로드한 전체 카테고리
// 목록으로 코드 존재를 즉시 대조(서버 왕복 없음), 레이어3은 백엔드 /bulk가 재검증한다.
interface ProcedureBulkRow {
  row: number, code: string, categoryCode: string
  nameZhCn: string, nameZhTw: string, nameEn: string, nameKo: string, error: string
}
const procShowBulk = ref(false)
const procBulkRows = ref<ProcedureBulkRow[]>([])
const procBulkSubmitting = ref(false)
const procBulkSubmitError = ref('')
const procBulkSuccessMessage = ref('')
const procBulkErrorCount = computed(() => procBulkRows.value.filter(r => r.error).length)

async function downloadProcedureTemplate() {
  const XLSX = await import('xlsx')
  const ws = XLSX.utils.aoa_to_sheet([[
    t('admin.procedures.formCodeLabel'), t('admin.procedures.bulk.colCategoryCode'), '简体中文', '繁體中文', 'English', '한국어',
  ]])
  ws['!cols'] = [{ wch: 16 }, { wch: 16 }, { wch: 24 }, { wch: 24 }, { wch: 24 }, { wch: 24 }]
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, 'template')
  XLSX.writeFile(wb, 'procedures_template.xlsx')
}
async function onProcedureExcelSelected(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (!file) return
  const XLSX = await import('xlsx')
  const wb = XLSX.read(await file.arrayBuffer())
  const ws = wb.Sheets[wb.SheetNames[0]!]!
  const aoa = XLSX.utils.sheet_to_json(ws, { header: 1 }) as unknown[][]
  const parsed = aoa
    .map((r, idx) => ({ r, excelRow: idx + 1 }))
    .slice(1)
    .filter(x => x.r.some(c => String(c ?? '').trim()))
    .map(({ r, excelRow }) => ({
      row: excelRow,
      code: String(r[0] ?? '').trim(),
      categoryCode: String(r[1] ?? '').trim(),
      nameZhCn: String(r[2] ?? '').trim(),
      nameZhTw: String(r[3] ?? '').trim(),
      nameEn: String(r[4] ?? '').trim(),
      nameKo: String(r[5] ?? '').trim(),
    }))
  const codeCounts = new Map<string, number>()
  for (const p of parsed) if (p.code) codeCounts.set(p.code, (codeCounts.get(p.code) ?? 0) + 1)
  const knownCategoryCodes = new Set(allCategories.value.map(c => c.code))
  procBulkRows.value = parsed.map((p) => {
    let error = validateCode(p.code)
      || (p.categoryCode ? '' : describeBulkError('BULK_FIELD_REQUIRED', 'categoryCode'))
      || validateNameFields(p)
    if (!error && p.code && (codeCounts.get(p.code) ?? 0) > 1) error = describeBulkError('BULK_CODE_DUPLICATE_IN_FILE')
    if (!error && p.categoryCode && !knownCategoryCodes.has(p.categoryCode)) error = describeBulkError('BULK_CATEGORY_NOT_FOUND')
    return { ...p, error }
  })
  procBulkSubmitError.value = ''
  procBulkSuccessMessage.value = ''
}
async function submitProcedureBulk() {
  if (!procBulkRows.value.length || procBulkErrorCount.value > 0) return
  procBulkSubmitError.value = ''
  procBulkSuccessMessage.value = ''
  procBulkSubmitting.value = true
  try {
    const res = await authFetch<{ successCount: number }>('/api/admin/procedures/bulk', {
      method: 'POST',
      body: procBulkRows.value.map(r => ({
        row: r.row, code: r.code, categoryCode: r.categoryCode,
        nameZhCn: r.nameZhCn, nameZhTw: r.nameZhTw, nameEn: r.nameEn, nameKo: r.nameKo,
      })),
    })
    procBulkSuccessMessage.value = t('admin.procedures.bulk.successMessage', { count: res.successCount })
    procBulkRows.value = []
    procShowBulk.value = false
    await refreshProcedures()
  } catch (e: any) {
    const code = e?.data?.code ?? 'UNKNOWN'
    if (code === 'BULK_VALIDATION_FAILED' && Array.isArray(e?.data?.rowErrors)) {
      procBulkRows.value = applyRowErrors(procBulkRows.value, e)
    } else {
      procBulkSubmitError.value = t(`errors.${code}`)
    }
  } finally {
    procBulkSubmitting.value = false
  }
}
function cancelProcedureBulk() {
  procShowBulk.value = false
  procBulkRows.value = []
  procBulkSubmitError.value = ''
  procBulkSuccessMessage.value = ''
}
</script>
