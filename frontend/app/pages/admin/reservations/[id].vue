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
        </CardContent>
      </Card>

      <!-- 담당 실장 배정 -->
      <Card>
        <CardHeader><CardTitle>{{ t('admin.reservationDetail.consultant') }}</CardTitle></CardHeader>
        <CardContent class="flex flex-wrap items-end gap-3">
          <div class="flex flex-col gap-1.5">
            <Label for="f-assign">{{ t('admin.reservationDetail.consultant') }}</Label>
            <NativeSelect id="f-assign" v-model="assignConsultantId" :disabled="!canWrite || isAssignLocked" class="w-56">
              <NativeSelectOption value="">{{ t('admin.reservationDetail.consultantPlaceholder') }}</NativeSelectOption>
              <NativeSelectOption v-for="c in assignableConsultants" :key="c.id" :value="String(c.id)">
                {{ c.name }}{{ c.isActive ? '' : ` (${t('admin.reservationDetail.inactive')})` }}
              </NativeSelectOption>
            </NativeSelect>
          </div>
          <Button :disabled="!canWrite || isAssignLocked || !assignConsultantId" @click="submitAssign">{{ t('admin.reservationDetail.assign') }}</Button>
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
                <span class="flex items-center gap-2">
                  <button v-if="n.isEdited" type="button" class="underline" @click="toggleRevisions(n.id)">{{ t('admin.reservationDetail.noteHistory') }}</button>
                  <button v-if="canEditNote(n) && editingNoteId !== n.id && !isCancelled" type="button" class="underline" @click="startEditNote(n)">{{ t('admin.reservationDetail.editNote') }}</button>
                </span>
              </div>
              <div v-if="openRevisionsForNoteId === n.id" class="mb-2 space-y-2 rounded-md bg-muted/50 p-2">
                <p v-if="revisionsLoading" class="text-xs text-muted-foreground">{{ t('common.loading') }}</p>
                <div v-for="rev in noteRevisions" :key="rev.id" class="text-xs">
                  <div class="mb-0.5 text-muted-foreground">{{ rev.editedByName }} · {{ formatKst(rev.editedAt) }}</div>
                  <p class="whitespace-pre-wrap">{{ rev.body }}</p>
                </div>
              </div>
              <template v-if="editingNoteId === n.id">
                <Textarea v-model="editingNoteBody" maxlength="2000" rows="3" />
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
            <Textarea
              id="f-note" v-model="noteBody" maxlength="2000" rows="3" :disabled="!canWrite || isNotesLocked"
              :placeholder="t('admin.reservationDetail.noteBodyPlaceholder')"
            />
            <div class="flex items-center gap-3">
              <Button :disabled="!canWrite || isNotesLocked || !noteBody.trim()" @click="submitNote">{{ t('admin.reservationDetail.addNote') }}</Button>
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
              <DatePicker id="f-visit-date" v-model="visitDate" :locale="inputLang" :disabled="!canWrite || isVisitInfoLocked" class="w-40" />
            </div>
            <div class="flex flex-col gap-1.5">
              <Label for="f-visit-time">{{ t('admin.reservationDetail.visitTime') }} ({{ t('admin.reservationDetail.preferredContactTimeHint') }})</Label>
              <TimePicker id="f-visit-time" v-model="visitTime" :locale="inputLang" :disabled="!canWrite || isVisitInfoLocked" />
            </div>
          </div>

          <div>
            <p class="mb-1.5 text-sm font-medium">{{ t('admin.reservationDetail.procedures') }}</p>
            <div class="flex flex-wrap gap-x-4 gap-y-2">
              <div v-for="p in visibleProcedures" :key="p.id" class="flex items-center gap-1.5">
                <Checkbox
                  :id="`f-procedure-${p.id}`"
                  :model-value="selectedProcedureIds.includes(p.id)"
                  :disabled="!canWrite || isVisitInfoLocked"
                  @update:model-value="(checked) => toggleProcedure(p.id, checked)"
                />
                <Label :for="`f-procedure-${p.id}`" class="text-sm font-normal">{{ procedureName(p) }}{{ p.isActive ? '' : ` (${t('admin.reservationDetail.inactive')})` }}</Label>
              </div>
            </div>
          </div>

          <div class="flex flex-wrap items-end gap-4">
            <div class="flex flex-col gap-1.5">
              <Label for="f-deposit-currency">{{ t('admin.reservationDetail.depositCurrency') }}</Label>
              <NativeSelect id="f-deposit-currency" v-model="depositCurrency" :disabled="!canWrite || isVisitInfoLocked || depositMode === 'waived'">
                <NativeSelectOption value="CNY">CNY</NativeSelectOption>
                <NativeSelectOption value="KRW">KRW</NativeSelectOption>
              </NativeSelect>
            </div>
            <div class="flex flex-col gap-1.5">
              <Label for="f-deposit-amount">{{ t('admin.reservationDetail.depositAmount') }}</Label>
              <Input id="f-deposit-amount" v-model.number="depositAmount" type="number" min="0" max="9999999999.99" :disabled="!canWrite || isVisitInfoLocked || depositMode === 'waived'" class="w-32" />
            </div>
            <div class="flex items-center gap-6 pb-2">
              <label class="flex items-center gap-1.5 text-sm">
                <input type="radio" name="deposit-mode" :checked="depositMode === 'paid'" :disabled="!canWrite || isVisitInfoLocked" @click="toggleDepositMode('paid')">
                {{ t('admin.reservationDetail.depositPaid') }}
              </label>
              <label class="flex items-center gap-1.5 text-sm">
                <input type="radio" name="deposit-mode" :checked="depositMode === 'waived'" :disabled="!canWrite || isVisitInfoLocked" @click="toggleDepositMode('waived')">
                {{ t('admin.reservationDetail.depositWaived') }}
              </label>
            </div>
          </div>

          <div class="flex items-center gap-3">
            <Button :disabled="!canWrite || isVisitInfoLocked || saving" @click="submitSave">{{ t('common.save') }}</Button>
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

      <!-- 액션: 상태 전이 — 방문완료 후에는 상담 기록 외 전 구역이 잠기므로 이 카드 자체를 숨긴다(#14) -->
      <Card v-if="!isVisited">
        <CardHeader><CardTitle>{{ t('admin.reservationDetail.actions') }}</CardTitle></CardHeader>
        <CardContent class="space-y-3">
          <div v-if="canWrite" class="flex flex-wrap items-center gap-3">
            <Button v-if="isAssigned && detail.status === 'Confirmed'" @click="markVisited">
              {{ t('admin.reservationDetail.markVisited') }}
            </Button>
            <Button
              v-if="['New', 'Consulting', 'Confirmed'].includes(detail.status)"
              variant="outline" @click="showCancelForm = !showCancelForm"
            >
              {{ t('admin.reservationDetail.cancelReservation') }}
            </Button>
            <Button v-if="isCancelled && user?.role === 'Admin'" @click="submitRestore">
              {{ t('admin.reservationDetail.restoreReservation') }}
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
        </CardContent>
      </Card>
    </template>
  </div>
</template>

<script setup lang="ts">
import type { ConsultantLookup, ProcedureLookup, ReservationDetail, ReservationNote, ReservationNoteRevision } from '~/types/reservation'

definePageMeta({ middleware: 'admin', layout: 'admin', i18n: false })

const { t, locale } = useI18n()
const { user } = useAuth()
const { authFetch } = useAuthFetch()
const route = useRoute()
// layouts/admin.vue가 useOpsLocale()을 이미 호출해 locale이 계정 값으로 맞춰져 있다 — 여기선 재사용만.
const inputLang = useInputLang()

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
const isCancelled = computed(() => detail.value?.status === 'Cancelled')
const isVisited = computed(() => detail.value?.status === 'Visited')
// 담당 실장 배정 섹션 — 미배정 상태에서 바로 이 컨트롤로 최초 배정을 하므로 !isAssigned로는 잠그지 않는다.
const isAssignLocked = computed(() => isCancelled.value || isVisited.value)
// 방문일시·시술·예약금 섹션 — 미배정이거나 취소·방문완료면 잠근다(11-2절, 담당 실장 배정 전과 동일 취급).
const isVisitInfoLocked = computed(() => !isAssigned.value || isCancelled.value || isVisited.value)
// 상담 기록 섹션 — 취소면 잠그지만 방문완료는 예외로 계속 허용한다(#14, 사후 상담 기록 목적).
const isNotesLocked = computed(() => !isAssigned.value || isCancelled.value)
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
const selectedProcedureIds = ref<number[]>([...(detail.value?.procedureIds ?? [])])
// reka-ui Checkbox는 네이티브 체크박스와 달리 배열 v-model을 지원하지 않아 체크 상태를 직접 토글한다.
function toggleProcedure(id: number, checked: boolean | 'indeterminate') {
  if (checked) {
    if (!selectedProcedureIds.value.includes(id)) selectedProcedureIds.value.push(id)
  } else {
    selectedProcedureIds.value = selectedProcedureIds.value.filter(x => x !== id)
  }
}

// 입금 확인 라디오 3상태(#13) — 미확인/입금확인/예약금없음(면제). 예약금없음도 내부적으로는
// depositPaid=true로 전송하되 금액이 null인 것으로 구분한다(백엔드도 동일 규칙으로 판별).
type DepositMode = 'unpaid' | 'paid' | 'waived'
function deriveDepositMode(paid: boolean, amount: number | null): DepositMode {
  if (!paid) return 'unpaid'
  return amount === null ? 'waived' : 'paid'
}
const depositMode = ref<DepositMode>(deriveDepositMode(detail.value?.depositPaid ?? false, detail.value?.depositAmount ?? null))
function toggleDepositMode(mode: 'paid' | 'waived') {
  // 이미 선택된 라디오를 다시 클릭하면 미확인 상태로 되돌린다(네이티브 radio는 스스로 해제가 안 되므로).
  depositMode.value = depositMode.value === mode ? 'unpaid' : mode
  if (depositMode.value === 'waived') depositAmount.value = null
}

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
        depositAmount: depositMode.value === 'waived' ? null : depositAmount.value,
        depositCurrency: depositCurrency.value,
        depositPaid: depositMode.value !== 'unpaid',
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

// 상담 기록 수정 이력(#5) — 클릭 시에만 불러온다(목록에 있는 모든 노트의 이력을 미리 로드할 필요 없음).
const openRevisionsForNoteId = ref<number | null>(null)
const noteRevisions = ref<ReservationNoteRevision[]>([])
const revisionsLoading = ref(false)
async function toggleRevisions(noteId: number) {
  if (openRevisionsForNoteId.value === noteId) {
    openRevisionsForNoteId.value = null
    return
  }
  openRevisionsForNoteId.value = noteId
  revisionsLoading.value = true
  try {
    noteRevisions.value = await authFetch<ReservationNoteRevision[]>(`/api/admin/reservations/${id.value}/notes/${noteId}/revisions`)
  } finally {
    revisionsLoading.value = false
  }
}

// 배정 드롭다운 — 이미 배정된 실장이 있으면 플레이스홀더 대신 그 실장이 바로 선택된 상태로 보여준다(#3).
const assignConsultantId = ref(detail.value?.consultantId != null ? String(detail.value.consultantId) : '')
const assignError = ref('')
async function submitAssign() {
  assignError.value = ''
  try {
    await authFetch(`/api/admin/reservations/${id.value}/consultant`, { method: 'PATCH', body: { consultantId: Number(assignConsultantId.value) } })
    await refresh()
    assignConsultantId.value = detail.value?.consultantId != null ? String(detail.value.consultantId) : ''
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

// 취소된 예약 복구(#10) — 어드민 전용. 되돌릴 수 없는 상태 전이들과 동일하게 확인을 거친다(12-5절).
async function submitRestore() {
  if (!confirm(t('admin.reservationDetail.restoreConfirm'))) return
  statusError.value = ''
  try {
    await authFetch(`/api/admin/reservations/${id.value}/restore`, { method: 'POST' })
    await refresh()
  } catch (e: any) {
    statusError.value = t(`errors.${errCode(e)}`)
  }
}
</script>
