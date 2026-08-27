<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-semibold text-foreground">{{ t('admin.users.title') }}</h1>
      <Button @click="startCreate">{{ t('admin.users.addButton') }}</Button>
    </div>

    <Card>
      <CardContent class="flex flex-wrap items-end gap-4">
        <div class="flex flex-col gap-1.5">
          <Label for="f-role-filter">{{ t('admin.users.filterRoleLabel') }}</Label>
          <NativeSelect id="f-role-filter" v-model="roleFilter" class="w-56" @change="applyRoleFilter">
            <NativeSelectOption value="">{{ t('admin.users.filterRoleAll') }}</NativeSelectOption>
            <NativeSelectOption value="Admin">{{ t('admin.users.roleAdmin') }}</NativeSelectOption>
            <NativeSelectOption value="HospitalManager">{{ t('admin.users.roleHospitalManager') }}</NativeSelectOption>
            <NativeSelectOption value="Consultant">{{ t('admin.users.roleConsultant') }}</NativeSelectOption>
          </NativeSelect>
        </div>
      </CardContent>
    </Card>

    <Card v-if="showCreateForm">
      <CardHeader>
        <CardTitle>{{ t('admin.users.formTitleCreate') }}</CardTitle>
      </CardHeader>
      <CardContent class="flex flex-wrap items-end gap-4">
        <div class="flex flex-col gap-1.5">
          <Label for="f-email">{{ t('admin.users.formEmailLabel') }}</Label>
          <Input id="f-email" v-model="createEmail" type="email" maxlength="254" class="w-64" />
        </div>
        <div class="flex flex-col gap-1.5">
          <Label for="f-password">{{ t('admin.users.formPasswordLabel') }}</Label>
          <Input id="f-password" v-model="createPassword" type="password" minlength="8" maxlength="64" class="w-48" />
        </div>
        <div class="flex flex-col gap-1.5">
          <Label for="f-name">{{ t('admin.users.formNameLabel') }}</Label>
          <Input id="f-name" v-model="createName" maxlength="30" class="w-40" />
        </div>
        <div class="flex flex-col gap-1.5">
          <Label for="f-create-role">{{ t('admin.users.formRoleLabel') }}</Label>
          <NativeSelect id="f-create-role" v-model="createRole" class="w-44">
            <NativeSelectOption value="Admin">{{ t('admin.users.roleAdmin') }}</NativeSelectOption>
            <NativeSelectOption value="HospitalManager">{{ t('admin.users.roleHospitalManager') }}</NativeSelectOption>
            <NativeSelectOption value="Consultant">{{ t('admin.users.roleConsultant') }}</NativeSelectOption>
          </NativeSelect>
        </div>
        <Button :disabled="!canSubmitCreate" @click="submitCreate">{{ t('common.save') }}</Button>
        <Button variant="outline" @click="showCreateForm = false">{{ t('common.cancel') }}</Button>
        <span v-if="createError" class="text-sm text-destructive">{{ createError }}</span>
      </CardContent>
    </Card>

    <Card v-if="editingId !== null">
      <CardHeader>
        <CardTitle>{{ t('admin.users.formTitleEdit') }}</CardTitle>
      </CardHeader>
      <CardContent class="flex flex-wrap items-end gap-4">
        <div class="flex flex-col gap-1.5">
          <Label for="f-edit-role">{{ t('admin.users.formRoleLabel') }}</Label>
          <NativeSelect id="f-edit-role" v-model="editRole" class="w-44">
            <NativeSelectOption value="Admin">{{ t('admin.users.roleAdmin') }}</NativeSelectOption>
            <NativeSelectOption value="HospitalManager">{{ t('admin.users.roleHospitalManager') }}</NativeSelectOption>
            <NativeSelectOption value="Consultant">{{ t('admin.users.roleConsultant') }}</NativeSelectOption>
          </NativeSelect>
        </div>
        <div class="flex items-center gap-1.5 pb-2">
          <Checkbox id="f-edit-suspended" v-model="editSuspended" />
          <Label for="f-edit-suspended" class="text-sm font-normal">{{ t('admin.users.formSuspendedLabel') }}</Label>
        </div>
        <Button @click="submitEdit">{{ t('common.save') }}</Button>
        <Button variant="outline" @click="editingId = null">{{ t('common.cancel') }}</Button>
        <span v-if="editError" class="text-sm text-destructive">{{ editError }}</span>
      </CardContent>
    </Card>

    <div class="overflow-x-auto rounded-md border border-border">
      <table class="w-full text-sm">
        <thead class="bg-muted text-muted-foreground">
          <tr>
            <th class="px-3 py-2 text-left">{{ t('admin.users.colEmail') }}</th>
            <th class="px-3 py-2 text-left">{{ t('admin.users.colName') }}</th>
            <th class="px-3 py-2 text-left">{{ t('admin.users.colRole') }}</th>
            <th class="px-3 py-2 text-left">{{ t('admin.users.colStatus') }}</th>
            <th class="px-3 py-2 text-left">{{ t('admin.users.colCreatedAt') }}</th>
            <th class="px-3 py-2 text-left" />
          </tr>
        </thead>
        <tbody>
          <tr v-if="!data?.items.length">
            <td colspan="6" class="p-6 text-center text-muted-foreground">{{ t('admin.users.empty') }}</td>
          </tr>
          <tr v-for="u in data?.items" :key="u.id" class="border-t border-border">
            <td class="px-3 py-2">{{ u.email }}</td>
            <td class="px-3 py-2">{{ u.name }}</td>
            <td class="px-3 py-2">{{ roleLabel(u.role) }}</td>
            <td class="px-3 py-2">{{ u.isSuspended ? t('admin.users.suspendedLabel') : t('admin.users.activeLabel') }}</td>
            <td class="px-3 py-2">{{ formatDate(u.createdAt) }}</td>
            <td class="px-3 py-2 text-right">
              <button v-if="u.id !== currentUserId" type="button" class="text-sm underline" @click="startEdit(u)">{{ t('admin.users.edit') }}</button>
              <span v-else class="text-xs text-muted-foreground">{{ t('admin.users.self') }}</span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <Pagination :page="page" :total-pages="totalPages" @update:page="goPage" />
  </div>
</template>

<script setup lang="ts">
import type { AdminRole, AdminUser, PagedResult } from '~/types/reservation'

definePageMeta({ middleware: 'admin', layout: 'admin', i18n: false })
useHead({ title: '계정 관리 | Admin', meta: [{ name: 'robots', content: 'noindex, nofollow' }] })

const { t } = useI18n()
const { authFetch } = useAuthFetch()
const { user } = useAuth()
const route = useRoute()

// 6-2절 매트릭스로 이미 Admin만 이 경로에 도달한다(middleware/admin.ts) — 컨트롤러 액션 레벨
// Authorize(Roles="Admin")가 실제 방어선(11-5절).
const currentUserId = computed(() => user.value?.id ?? null)

const query = computed(() => ({
  page: Number(route.query.page) || 1,
  pageSize: 20,
  role: (route.query.role as string) || undefined,
}))
const { data, refresh } = await useApi<PagedResult<AdminUser>>('/api/admin/users', { query })

const page = computed(() => query.value.page)
const totalPages = computed(() => data.value ? Math.max(1, Math.ceil(data.value.total / data.value.pageSize)) : 1)
function goPage(p: number) {
  navigateTo({ query: { ...route.query, page: p } })
}

const roleFilter = ref(query.value.role ?? '')
function applyRoleFilter() {
  navigateTo({ query: { ...route.query, role: roleFilter.value || undefined, page: 1 } })
}

function roleLabel(role: string) {
  if (role === 'Admin') return t('admin.users.roleAdmin')
  if (role === 'HospitalManager') return t('admin.users.roleHospitalManager')
  return t('admin.users.roleConsultant')
}
function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('ko-KR', { timeZone: 'Asia/Seoul' })
}

// ── 계정 발급 ──────────────────────────────────────────────
const showCreateForm = ref(false)
const createEmail = ref('')
const createPassword = ref('')
const createName = ref('')
const createRole = ref<AdminRole>('Consultant')
const createError = ref('')
const canSubmitCreate = computed(() =>
  createEmail.value.trim() && createPassword.value.length >= 8 && createName.value.trim())

function startCreate() {
  createEmail.value = ''
  createPassword.value = ''
  createName.value = ''
  createRole.value = 'Consultant'
  createError.value = ''
  showCreateForm.value = true
  editingId.value = null
}

async function submitCreate() {
  createError.value = ''
  try {
    await authFetch('/api/admin/users', {
      method: 'POST',
      body: { email: createEmail.value.trim(), password: createPassword.value, role: createRole.value, name: createName.value.trim() },
    })
    showCreateForm.value = false
    await refresh()
  }
  catch (e: any) {
    createError.value = t(`errors.${e?.data?.code ?? 'UNKNOWN'}`)
  }
}

// ── 역할 변경·정지 ─────────────────────────────────────────
const editingId = ref<number | null>(null)
const editRole = ref<AdminRole>('Consultant')
const editSuspended = ref(false)
const editOriginalSuspended = ref(false)
const editError = ref('')

function startEdit(u: AdminUser) {
  editingId.value = u.id
  editRole.value = u.role
  editSuspended.value = u.isSuspended
  editOriginalSuspended.value = u.isSuspended
  editError.value = ''
  showCreateForm.value = false
}

async function submitEdit() {
  if (editingId.value === null) return
  // 16장 보안 체크리스트 — 파괴적 액션(정지)에 확인 UI 필수. 활성→정지로 새로 전환하는 경우에만 확인.
  if (editSuspended.value && !editOriginalSuspended.value && !confirm(t('admin.users.suspendConfirm'))) return
  editError.value = ''
  try {
    await authFetch(`/api/admin/users/${editingId.value}`, {
      method: 'PATCH',
      body: { role: editRole.value, isSuspended: editSuspended.value },
    })
    editingId.value = null
    await refresh()
  }
  catch (e: any) {
    editError.value = t(`errors.${e?.data?.code ?? 'UNKNOWN'}`)
  }
}
</script>
