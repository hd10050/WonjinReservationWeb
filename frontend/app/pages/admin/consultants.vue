<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-semibold text-foreground">{{ t('admin.consultants.title') }}</h1>
      <Button @click="startCreate">{{ t('admin.consultants.addButton') }}</Button>
    </div>

    <label class="flex items-center gap-1.5 text-sm text-muted-foreground">
      <input v-model="showInactive" type="checkbox">
      {{ t('admin.consultants.includeInactive') }}
    </label>

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
        <label v-if="editingId !== null" class="flex items-center gap-1.5 pb-2 text-sm">
          <input v-model="formIsActive" type="checkbox">
          {{ t('admin.consultants.formActiveLabel') }}
        </label>
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
  </div>
</template>

<script setup lang="ts">
import type { ConsultantLookup } from '~/types/reservation'

definePageMeta({ middleware: 'admin', layout: 'admin', i18n: false })
useHead({ title: '실장 관리 | Admin', meta: [{ name: 'robots', content: 'noindex, nofollow' }] })

const { t } = useI18n()
const { authFetch } = useAuthFetch()

// 6-2절 메뉴 매트릭스로 이미 Admin/HospitalManager만 이 경로에 도달한다(middleware/admin.ts) — 화면 안에서
// 역할별 버튼을 다시 가릴 필요가 없다. 실제 방어선은 컨트롤러 액션 레벨 Authorize(11-3절).
const showInactive = ref(false)
const { data: consultants, refresh } = await useApi<ConsultantLookup[]>('/api/admin/consultants', {
  query: () => ({ includeInactive: showInactive.value }),
})

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
