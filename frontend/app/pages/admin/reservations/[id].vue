<template>
  <div class="space-y-6">
    <button type="button" class="text-sm text-muted-foreground hover:text-foreground" @click="navigateTo('/admin')">
      ← {{ t('admin.reservationDetail.backToList') }}
    </button>

    <div v-if="!detail" class="text-muted-foreground">{{ t('common.loading') }}</div>

    <template v-else>
      <h1 class="text-xl font-semibold text-foreground">
        {{ detail.name }} · {{ detail.code }} · {{ t(`status.${detail.status}`) }}
      </h1>

      <div v-if="!isAssigned" class="rounded-md border border-destructive bg-destructive/10 px-4 py-3 text-sm text-destructive">
        {{ t('admin.reservationDetail.notAssignedBanner') }}
      </div>

      <!-- 1. 고객 정보(읽기 전용) -->
      <Card>
        <CardHeader><CardTitle>{{ t('admin.reservationDetail.customerInfo') }}</CardTitle></CardHeader>
        <CardContent class="grid grid-cols-2 gap-x-6 gap-y-3 text-sm md:grid-cols-3">
          <div><span class="text-muted-foreground">{{ t('admin.reservations.colName') }}: </span>{{ detail.name }}</div>
          <div><span class="text-muted-foreground">{{ t('admin.reservations.colCode') }}: </span>{{ detail.code }}</div>
          <div><span class="text-muted-foreground">{{ t('admin.reservationDetail.birthDate') }}: </span>{{ detail.birthDate }} ({{ calculateAge(detail.birthDate) }}{{ t('admin.reservationDetail.age') }})</div>
          <div><span class="text-muted-foreground">{{ t('admin.reservationDetail.gender') }}: </span>{{ t(`admin.reservationDetail.gender${detail.gender}`) }}</div>
          <div><span class="text-muted-foreground">{{ t('admin.reservationDetail.wechatId') }}: </span>{{ detail.wechatId }}</div>
          <div>
            <span class="text-muted-foreground">{{ t('admin.reservationDetail.preferredContactTime') }} ({{ t('admin.reservationDetail.preferredContactTimeHint') }}): </span>
            {{ detail.preferredContactTime.slice(0, 5) }}
          </div>
          <div><span class="text-muted-foreground">{{ t('admin.reservationDetail.locale') }}: </span>{{ detail.locale }}</div>
          <div><span class="text-muted-foreground">{{ t('admin.reservationDetail.receivedAt') }}: </span>{{ formatKst(detail.createdAt) }}</div>
          <div class="md:col-span-3"><span class="text-muted-foreground">{{ t('admin.reservationDetail.referralSource') }}: </span>{{ [detail.utmSource, detail.utmMedium, detail.utmCampaign, detail.referralCode].filter(Boolean).join(' / ') || '-' }}</div>
        </CardContent>
      </Card>

      <!-- 담당 실장 배정 -->
      <Card>
        <CardHeader><CardTitle>{{ t('admin.reservationDetail.consultant') }}</CardTitle></CardHeader>
        <CardContent class="flex flex-wrap items-end gap-3">
          <div class="flex flex-col gap-1.5">
            <Label for="f-assign">{{ t('admin.reservationDetail.consultant') }}</Label>
            <select id="f-assign" v-model="assignConsultantId" :disabled="!canWrite" class="h-9 w-56 rounded-md border border-input bg-transparent px-3 text-sm">
              <option value="">{{ t('admin.reservationDetail.consultantPlaceholder') }}</option>
              <option v-for="c in assignableConsultants" :key="c.id" :value="String(c.id)">
                {{ c.name }}{{ c.isActive ? '' : ` (${t('admin.reservationDetail.inactive')})` }}
              </option>
            </select>
          </div>
          <Button :disabled="!canWrite || !assignConsultantId" @click="submitAssign">{{ t('admin.reservationDetail.assign') }}</Button>
          <span v-if="assignError" class="text-sm text-destructive">{{ assignError }}</span>
        </CardContent>
      </Card>

      <!-- 2. 상담 기록(누적) -->
      <Card>
        <CardHeader><CardTitle>{{ t('admin.reservationDetail.notes') }}</CardTitle></CardHeader>
        <CardContent class="space-y-4">
          <p v-if="!detail.notes.length" class="text-sm text-muted-foreground">{{ t('admin.reservationDetail.notesEmpty') }}</p>
          <ul class="space-y-3">
            <li v-for="n in detail.notes" :key="n.id" class="rounded-md border border-border p-3">
              <div class="mb-1 flex items-center justify-between text-xs text-muted-foreground">
                <span>{{ n.authorName }} · {{ formatKst(n.createdAt) }}<template v-if="n.isEdited"> ({{ t('admin.reservationDetail.edited') }})</template></span>
                <button v-if="canEditNote(n) && editingNoteId !== n.id" type="button" class="underline" @click="startEditNote(n)">{{ t('admin.reservationDetail.editNote') }}</button>
              </div>
              <template v-if="editingNoteId === n.id">
                <textarea v-model="editingNoteBody" maxlength="2000" rows="3" class="w-full rounded-md border border-input bg-transparent p-2 text-sm" />
                <div class="mt-2 flex gap-2">
                  <Button size="sm" @click="saveEditNote(n.id)">{{ t('common.save') }}</Button>
                  <Button size="sm" variant="outline" @click="editingNoteId = null">{{ t('common.cancel') }}</Button>
                </div>
              </template>
              <p v-else class="whitespace-pre-wrap text-sm">{{ n.body }}</p>
            </li>
          </ul>

          <div class="flex flex-col gap-1.5 pt-2">
            <Label for="f-note">{{ t('admin.reservationDetail.noteBodyLabel') }}</Label>
            <textarea
              id="f-note" v-model="noteBody" maxlength="2000" rows="3" :disabled="!canWrite || !isAssigned"
              :placeholder="t('admin.reservationDetail.noteBodyPlaceholder')"
              class="w-full rounded-md border border-input bg-transparent p-2 text-sm disabled:cursor-not-allowed disabled:opacity-50"
            />
            <div class="flex items-center gap-3">
              <Button :disabled="!canWrite || !isAssigned || !noteBody.trim()" @click="submitNote">{{ t('admin.reservationDetail.addNote') }}</Button>
              <span v-if="noteError" class="text-sm text-destructive">{{ noteError }}</span>
            </div>
          </div>
        </CardContent>
      </Card>

      <!-- 3. 방문 예약 + 4. 시술 + 5. 예약금 -->
      <Card>
        <CardHeader><CardTitle>{{ t('admin.reservationDetail.visitInfo') }}</CardTitle></CardHeader>
        <CardContent class="space-y-4">
          <div class="flex flex-wrap gap-4">
            <div class="flex flex-col gap-1.5">
              <Label for="f-visit-date">{{ t('admin.reservationDetail.visitDate') }}</Label>
              <Input id="f-visit-date" v-model="visitDate" type="date" :disabled="!canWrite || !isAssigned" class="w-40" />
            </div>
            <div class="flex flex-col gap-1.5">
              <Label for="f-visit-time">{{ t('admin.reservationDetail.visitTime') }} ({{ t('admin.reservationDetail.preferredContactTimeHint') }})</Label>
              <Input id="f-visit-time" v-model="visitTime" type="time" :disabled="!canWrite || !isAssigned" class="w-32" />
            </div>
          </div>

          <div>
            <p class="mb-1.5 text-sm font-medium">{{ t('admin.reservationDetail.procedures') }}</p>
            <div class="flex flex-wrap gap-x-4 gap-y-2">
              <label v-for="p in visibleProcedures" :key="p.id" class="flex items-center gap-1.5 text-sm">
                <input type="checkbox" :value="p.id" v-model="selectedProcedureIds" :disabled="!canWrite || !isAssigned">
                {{ procedureName(p) }}{{ p.isActive ? '' : ` (${t('admin.reservationDetail.inactive')})` }}
              </label>
            </div>
          </div>

          <div class="flex flex-wrap items-end gap-4">
            <div class="flex flex-col gap-1.5">
              <Label for="f-deposit-currency">{{ t('admin.reservationDetail.depositCurrency') }}</Label>
              <select id="f-deposit-currency" v-model="depositCurrency" :disabled="!canWrite || !isAssigned" class="h-9 rounded-md border border-input bg-transparent px-3 text-sm">
                <option value="CNY">CNY</option>
                <option value="KRW">KRW</option>
              </select>
            </div>
            <div class="flex flex-col gap-1.5">
              <Label for="f-deposit-amount">{{ t('admin.reservationDetail.depositAmount') }}</Label>
              <Input id="f-deposit-amount" v-model.number="depositAmount" type="number" min="0" :disabled="!canWrite || !isAssigned" class="w-32" />
            </div>
            <label class="flex items-center gap-1.5 pb-2 text-sm">
              <input type="checkbox" v-model="depositPaid" :disabled="!canWrite || !isAssigned">
              {{ t('admin.reservationDetail.depositPaid') }}
            </label>
          </div>

          <div class="flex items-center gap-3">
            <Button :disabled="!canWrite || !isAssigned || saving" @click="submitSave">{{ t('common.save') }}</Button>
            <span v-if="saveError" class="text-sm text-destructive">{{ saveError }}</span>
          </div>
        </CardContent>
      </Card>

      <!-- 처리 이력 -->
      <Card>
        <CardHeader><CardTitle>{{ t('admin.reservationDetail.logs') }}</CardTitle></CardHeader>
        <CardContent>
          <ul class="space-y-1 text-sm">
            <li v-for="l in detail.logs" :key="l.id" class="flex gap-2">
              <span class="text-muted-foreground">{{ formatKst(l.createdAt) }}</span>
              <span>{{ t(`reservationLogAction.${l.action}`) }}</span>
              <span v-if="l.note" class="text-muted-foreground">— {{ l.note }}</span>
              <span class="text-muted-foreground">({{ l.actorName }})</span>
            </li>
          </ul>
        </CardContent>
      </Card>

      <!-- 액션: 상태 전이 · 삭제 -->
      <Card>
        <CardHeader><CardTitle>{{ t('admin.reservationDetail.actions') }}</CardTitle></CardHeader>
        <CardContent class="space-y-3">
          <div v-if="canWrite" class="flex flex-wrap items-center gap-3">
            <Button v-if="isAssigned && detail.status === 'Confirmed'" @click="markVisited">
              {{ t('admin.reservationDetail.markVisited') }}
            </Button>
            <Button
              v-if="isAssigned && ['New', 'Consulting', 'Confirmed'].includes(detail.status)"
              variant="outline" @click="showCancelForm = !showCancelForm"
            >
              {{ t('admin.reservationDetail.cancelReservation') }}
            </Button>
            <span v-if="statusError" class="text-sm text-destructive">{{ statusError }}</span>
          </div>

          <div v-if="canWrite && showCancelForm" class="flex flex-col gap-1.5">
            <Label for="f-cancel-reason">{{ t('admin.reservationDetail.cancelReasonLabel') }}</Label>
            <Input id="f-cancel-reason" v-model="cancelReason" maxlength="200" :placeholder="t('admin.reservationDetail.cancelReasonPlaceholder')" class="max-w-md" />
            <div class="flex gap-2">
              <Button variant="destructive" :disabled="!cancelReason.trim()" @click="submitCancel">{{ t('admin.reservationDetail.cancelReservation') }}</Button>
              <Button variant="outline" @click="showCancelForm = false">{{ t('common.cancel') }}</Button>
            </div>
          </div>

          <div v-if="canWrite && detail.notes.length === 0" class="border-t border-border pt-3">
            <Button variant="destructive" @click="submitDelete">{{ t('admin.reservationDetail.deleteReservation') }}</Button>
            <span v-if="deleteError" class="ml-3 text-sm text-destructive">{{ deleteError }}</span>
          </div>
        </CardContent>
      </Card>
    </template>
  </div>
</template>

<script setup lang="ts">
import type { ConsultantLookup, ProcedureLookup, ReservationDetail, ReservationNote } from '~/types/reservation'

definePageMeta({ middleware: 'admin', layout: 'admin', i18n: false })

const { t, locale } = useI18n()
const { user } = useAuth()
const { authFetch } = useAuthFetch()
const route = useRoute()

const id = computed(() => Number(route.params.id))

const { data: detail, refresh, error } = await useApi<ReservationDetail>(() => `/api/admin/reservations/${id.value}`)
const { data: consultantsRaw } = await useApi<ConsultantLookup[]>('/api/admin/consultants', { query: { includeInactive: true } })
const { data: proceduresRaw } = await useApi<ProcedureLookup[]>('/api/admin/procedures', { query: { includeInactive: true } })

if (error.value) {
  throw createError({ statusCode: (error.value as any)?.statusCode ?? 404, statusMessage: 'Not Found', fatal: true })
}

useHead(() => ({ title: `${detail.value?.name ?? ''} | Admin`, meta: [{ name: 'robots', content: 'noindex, nofollow' }] }))

// 쓰기 API는 Consultant·Admin만 허용한다(6-3절 원칙 2·11-2절 표) — 버튼만 숨기고 API를 안 잠그면
// 개발자 도구로 우회되므로 백엔드가 실제 방어선이지만, 프론트도 역할별로 같이 가려야 한다.
const canWrite = computed(() => user.value?.role === 'Admin' || user.value?.role === 'Consultant')
const isAssigned = computed(() => detail.value?.consultantId != null)
const assignableConsultants = computed(() =>
  (consultantsRaw.value ?? []).filter(c => c.isActive || c.id === detail.value?.consultantId))
// 8-3절 — 이미 선택된 비활성 시술은 목록에 남겨야 한다(빼면 편집 화면에서 확인·해제가 불가능해진다)
const visibleProcedures = computed(() =>
  (proceduresRaw.value ?? []).filter(p => p.isActive || detail.value?.procedureIds.includes(p.id)))

function procedureName(p: ProcedureLookup): string {
  const map: Record<string, string> = { 'zh-CN': p.nameZhCn, 'zh-TW': p.nameZhTw, en: p.nameEn, ko: p.nameKo }
  return map[locale.value] ?? p.nameKo
}

function errCode(e: any): string {
  return e?.data?.code ?? 'UNKNOWN'
}

// 방문일시·시술·예약금 — 최초 로드 시 한 번만 복사한다. 다른 액션(상담기록 추가 등)의 refresh()로
// 편집 중이던 값이 조용히 덮어써지지 않게 하기 위함(같은 데이터를 다시 불러와도 이 폼은 재동기화하지 않음).
const visitDate = ref(detail.value?.visitDate ?? '')
const visitTime = ref(detail.value?.visitTime?.slice(0, 5) ?? '')
const depositCurrency = ref(detail.value?.depositCurrency ?? 'CNY')
const depositAmount = ref<number | null>(detail.value?.depositAmount ?? null)
const depositPaid = ref(detail.value?.depositPaid ?? false)
const selectedProcedureIds = ref<number[]>([...(detail.value?.procedureIds ?? [])])

const saving = ref(false)
const saveError = ref('')
async function submitSave() {
  saveError.value = ''
  saving.value = true
  try {
    await authFetch(`/api/admin/reservations/${id.value}`, {
      method: 'PATCH',
      body: {
        visitDate: visitDate.value || null,
        visitTime: visitTime.value || null,
        procedureIds: selectedProcedureIds.value,
        depositAmount: depositAmount.value,
        depositCurrency: depositCurrency.value,
        depositPaid: depositPaid.value,
      },
    })
    await refresh()
  } catch (e: any) {
    saveError.value = t(`errors.${errCode(e)}`)
  } finally {
    saving.value = false
  }
}

const noteBody = ref('')
const noteError = ref('')
async function submitNote() {
  noteError.value = ''
  try {
    await authFetch(`/api/admin/reservations/${id.value}/notes`, { method: 'POST', body: { body: noteBody.value } })
    noteBody.value = ''
    await refresh()
  } catch (e: any) {
    noteError.value = t(`errors.${errCode(e)}`)
  }
}

const editingNoteId = ref<number | null>(null)
const editingNoteBody = ref('')
function startEditNote(n: ReservationNote) {
  editingNoteId.value = n.id
  editingNoteBody.value = n.body
}
async function saveEditNote(noteId: number) {
  await authFetch(`/api/admin/reservations/${id.value}/notes/${noteId}`, { method: 'PATCH', body: { body: editingNoteBody.value } })
  editingNoteId.value = null
  await refresh()
}
function canEditNote(n: ReservationNote): boolean {
  return user.value?.role === 'Admin' || n.authorUserId === user.value?.id
}

const assignConsultantId = ref('')
const assignError = ref('')
async function submitAssign() {
  assignError.value = ''
  try {
    await authFetch(`/api/admin/reservations/${id.value}/consultant`, { method: 'PATCH', body: { consultantId: Number(assignConsultantId.value) } })
    assignConsultantId.value = ''
    await refresh()
  } catch (e: any) {
    assignError.value = t(`errors.${errCode(e)}`)
  }
}

const statusError = ref('')
async function markVisited() {
  // Visited는 종결 상태(10장) — 취소·삭제와 동일하게 되돌릴 수 없는 액션이라 확인 UI를 거친다(12-5절·16장)
  if (!confirm(t('admin.reservationDetail.markVisitedConfirm'))) return
  statusError.value = ''
  try {
    await authFetch(`/api/admin/reservations/${id.value}/status`, { method: 'POST', body: { status: 'Visited' } })
    await refresh()
  } catch (e: any) {
    statusError.value = t(`errors.${errCode(e)}`)
  }
}

const showCancelForm = ref(false)
const cancelReason = ref('')
async function submitCancel() {
  statusError.value = ''
  try {
    await authFetch(`/api/admin/reservations/${id.value}/status`, { method: 'POST', body: { status: 'Cancelled', cancelReason: cancelReason.value } })
    showCancelForm.value = false
    cancelReason.value = ''
    await refresh()
  } catch (e: any) {
    statusError.value = t(`errors.${errCode(e)}`)
  }
}

const deleteError = ref('')
async function submitDelete() {
  if (!confirm(t('admin.reservationDetail.deleteConfirm'))) return
  try {
    await authFetch(`/api/admin/reservations/${id.value}`, { method: 'DELETE' })
    await navigateTo('/admin')
  } catch (e: any) {
    deleteError.value = t(`errors.${errCode(e)}`)
  }
}
</script>
