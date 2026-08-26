<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-semibold text-foreground">{{ t('admin.procedures.title') }}</h1>
      <Button @click="startCreate">{{ t('admin.procedures.addButton') }}</Button>
    </div>

    <label class="flex items-center gap-1.5 text-sm text-muted-foreground">
      <input v-model="showInactive" type="checkbox">
      {{ t('admin.procedures.includeInactive') }}
    </label>

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
          <label v-if="editingId !== null" class="flex items-center gap-1.5 pb-2 text-sm">
            <input v-model="formIsActive" type="checkbox">
            {{ t('admin.procedures.formActiveLabel') }}
          </label>
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
