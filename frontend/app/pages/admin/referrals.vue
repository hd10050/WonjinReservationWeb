<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-semibold text-foreground">{{ t('admin.referrals.title') }}</h1>
      <Button variant="outline" @click="showLinks = !showLinks">{{ t('admin.referrals.influencerLinks.toggleButton') }}</Button>
    </div>

    <Card v-if="showLinks">
      <CardHeader>
        <CardTitle>{{ t('admin.referrals.influencerLinks.title') }}</CardTitle>
      </CardHeader>
      <CardContent class="space-y-4">
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-1.5">
            <Checkbox id="f-il-show-inactive" v-model="showInactive" />
            <Label for="f-il-show-inactive" class="text-sm font-normal text-muted-foreground">{{ t('admin.consultants.includeInactive') }}</Label>
          </div>
          <Button @click="startCreate">{{ t('admin.referrals.influencerLinks.addButton') }}</Button>
        </div>

        <Card v-if="showForm">
          <CardHeader>
            <CardTitle>{{ editingId === null ? t('admin.referrals.influencerLinks.formTitleCreate') : t('admin.referrals.influencerLinks.formTitleEdit') }}</CardTitle>
          </CardHeader>
          <CardContent class="flex flex-wrap items-end gap-4">
            <div class="flex flex-col gap-1.5">
              <Label for="f-il-code">{{ t('admin.referrals.influencerLinks.formCodeLabel') }}</Label>
              <Input id="f-il-code" v-model="formCode" :disabled="editingId !== null" maxlength="50" class="w-40" />
            </div>
            <div class="flex flex-col gap-1.5">
              <Label for="f-il-name">{{ t('admin.referrals.influencerLinks.formDisplayNameLabel') }}</Label>
              <Input id="f-il-name" v-model="formDisplayName" maxlength="100" class="w-56" />
            </div>
            <div class="flex flex-col gap-1.5">
              <Label for="f-il-source">{{ t('admin.referrals.colUtmSource') }}</Label>
              <Input id="f-il-source" v-model="formUtmSource" maxlength="100" class="w-36" />
            </div>
            <div class="flex flex-col gap-1.5">
              <Label for="f-il-medium">{{ t('admin.referrals.colUtmMedium') }}</Label>
              <Input id="f-il-medium" v-model="formUtmMedium" maxlength="100" class="w-36" />
            </div>
            <div class="flex flex-col gap-1.5">
              <Label for="f-il-campaign">{{ t('admin.referrals.colUtmCampaign') }}</Label>
              <Input id="f-il-campaign" v-model="formUtmCampaign" maxlength="100" class="w-36" />
            </div>
            <div class="flex flex-col gap-1.5">
              <Label for="f-il-locale">{{ t('admin.referrals.influencerLinks.formLocaleLabel') }}</Label>
              <NativeSelect id="f-il-locale" v-model="formLocale" class="w-28">
                <NativeSelectOption v-for="loc in LOCALES" :key="loc" :value="loc">{{ localeName(loc) }}</NativeSelectOption>
              </NativeSelect>
            </div>
            <div v-if="editingId !== null" class="flex items-center gap-1.5 pb-2">
              <Checkbox id="f-il-active" v-model="formIsActive" />
              <Label for="f-il-active" class="text-sm font-normal">{{ t('admin.consultants.formActiveLabel') }}</Label>
            </div>
            <Button :disabled="!formCode.trim() || !formDisplayName.trim()" @click="submitForm">{{ t('common.save') }}</Button>
            <Button variant="outline" @click="cancelForm">{{ t('common.cancel') }}</Button>
            <span v-if="formError" class="text-sm text-destructive">{{ formError }}</span>
          </CardContent>
        </Card>

        <div class="overflow-x-auto rounded-md border border-border">
          <table class="w-full text-sm">
            <thead class="bg-muted text-muted-foreground">
              <tr>
                <th class="px-3 py-2 text-left">{{ t('admin.referrals.influencerLinks.colUrl') }}</th>
                <th class="px-3 py-2 text-left">{{ t('admin.referrals.influencerLinks.formDisplayNameLabel') }}</th>
                <th class="px-3 py-2 text-left">{{ t('admin.referrals.colUtmSource') }}</th>
                <th class="px-3 py-2 text-left">{{ t('admin.referrals.colUtmMedium') }}</th>
                <th class="px-3 py-2 text-left">{{ t('admin.referrals.colUtmCampaign') }}</th>
                <th class="px-3 py-2 text-left">{{ t('admin.referrals.influencerLinks.formLocaleLabel') }}</th>
                <th class="px-3 py-2 text-left">{{ t('admin.consultants.colActive') }}</th>
                <th class="px-3 py-2 text-left" />
              </tr>
            </thead>
            <tbody>
              <tr v-if="!links?.items?.length">
                <td colspan="8" class="p-6 text-center text-muted-foreground">{{ t('admin.referrals.influencerLinks.empty') }}</td>
              </tr>
              <tr v-for="l in links?.items" :key="l.id" class="border-t border-border">
                <td class="px-3 py-2">
                  <div class="flex items-center gap-2">
                    <code class="text-xs">{{ shortUrl(l.code) }}</code>
                    <button type="button" class="text-sm underline shrink-0" @click="copyUrl(l.code)">
                      {{ copiedCode === l.code ? t('admin.referrals.influencerLinks.copied') : t('admin.referrals.influencerLinks.copy') }}
                    </button>
                  </div>
                </td>
                <td class="px-3 py-2">{{ l.displayName }}</td>
                <td class="px-3 py-2">{{ l.utmSource || '—' }}</td>
                <td class="px-3 py-2">{{ l.utmMedium || '—' }}</td>
                <td class="px-3 py-2">{{ l.utmCampaign || '—' }}</td>
                <td class="px-3 py-2">{{ l.locale }}</td>
                <td class="px-3 py-2">{{ l.isActive ? t('admin.consultants.activeLabel') : t('admin.consultants.inactiveLabel') }}</td>
                <td class="px-3 py-2 text-right">
                  <button type="button" class="text-sm underline" @click="startEdit(l)">{{ t('admin.consultants.edit') }}</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </CardContent>
    </Card>

    <Card>
      <CardContent class="flex flex-wrap items-end gap-4">
        <div class="flex flex-col gap-1.5">
          <Label for="f-from">{{ t('admin.referrals.filterFrom') }}</Label>
          <DatePicker id="f-from" v-model="formFrom" :locale="inputLang" class="w-40" />
        </div>
        <div class="flex flex-col gap-1.5">
          <Label for="f-to">{{ t('admin.referrals.filterTo') }}</Label>
          <DatePicker id="f-to" v-model="formTo" :locale="inputLang" class="w-40" />
        </div>
        <Button @click="applyFilters">{{ t('admin.referrals.filterApply') }}</Button>
      </CardContent>
    </Card>

    <div class="overflow-x-auto rounded-md border border-border">
      <table class="w-full text-sm">
        <thead class="bg-muted text-muted-foreground">
          <tr>
            <th class="px-3 py-2 text-left">{{ t('admin.referrals.colReferralCode') }}</th>
            <th class="px-3 py-2 text-left">{{ t('admin.referrals.colUtmSource') }}</th>
            <th class="px-3 py-2 text-left">{{ t('admin.referrals.colUtmMedium') }}</th>
            <th class="px-3 py-2 text-left">{{ t('admin.referrals.colUtmCampaign') }}</th>
            <th class="px-3 py-2 text-right">{{ t('admin.referrals.colVisitCount') }}</th>
            <th class="px-3 py-2 text-right">{{ t('admin.referrals.colReservationCount') }}</th>
            <th class="px-3 py-2 text-right">{{ t('admin.referrals.colConversionRate') }}</th>
            <th class="px-3 py-2 text-right">{{ t('admin.referrals.colConfirmedCount') }}</th>
            <th class="px-3 py-2 text-right">{{ t('admin.referrals.colConfirmedConversionRate') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!data?.length">
            <td colspan="9" class="p-6 text-center text-muted-foreground">{{ t('admin.referrals.empty') }}</td>
          </tr>
          <tr
            v-for="r in data"
            :key="`${r.referralCode}|${r.utmSource}|${r.utmMedium}|${r.utmCampaign}`"
            class="border-t border-border"
          >
            <td class="px-3 py-2">{{ r.referralCode || '—' }}</td>
            <td class="px-3 py-2">{{ r.utmSource || '—' }}</td>
            <td class="px-3 py-2">{{ r.utmMedium || '—' }}</td>
            <td class="px-3 py-2">{{ r.utmCampaign || '—' }}</td>
            <td class="px-3 py-2 text-right">{{ r.visitCount }}</td>
            <td class="px-3 py-2 text-right">{{ r.reservationCount }}</td>
            <td class="px-3 py-2 text-right">{{ r.conversionRate }}%</td>
            <td class="px-3 py-2 text-right">{{ r.confirmedCount }}</td>
            <td class="px-3 py-2 text-right">{{ r.confirmedConversionRate }}%</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { InfluencerLink, PagedResult, ReferralStat } from '~/types/reservation'
import { todayKst } from '~/utils/datetime'

definePageMeta({ middleware: 'admin', layout: 'admin', i18n: false })
useHead({ title: '유입 경로 분석 | Admin', meta: [{ name: 'robots', content: 'noindex, nofollow' }] })

const { t, locales } = useI18n()
const { authFetch } = useAuthFetch()
const route = useRoute()
// layouts/admin.vue가 useOpsLocale()을 이미 호출해 locale이 계정 값으로 맞춰져 있다 — 여기선 재사용만.
const inputLang = useInputLang()

const defaultFrom = `${todayKst().slice(0, 7)}-01`
const defaultTo = todayKst()

// 🔴 검색 입력을 반응형 query에 직접 물리지 말 것(12-4절)과 동일 이유로 URL 쿼리를 computed로 감싼다.
const query = computed(() => ({
  from: (route.query.from as string) || defaultFrom,
  to: (route.query.to as string) || defaultTo,
}))

const { data } = await useApi<ReferralStat[]>('/api/admin/stats/referrals', { query })

const formFrom = ref(query.value.from)
const formTo = ref(query.value.to)

function applyFilters() {
  navigateTo({ query: { from: formFrom.value, to: formTo.value } })
}

// 인플루언서 링크 관리(B안, 2026-08-27 신설) — 짧은 URL(/go/{code}) 매핑 CRUD. consultants.vue의
// 폼 CRUD 패턴을 그대로 따른다(startCreate/startEdit/submitForm).
const LOCALES = ['zh-CN', 'zh-TW', 'en', 'ko'] as const
// 관리자 상단바 언어 선택(layouts/admin.vue)·로그인 화면과 동일하게 useI18n().locales의 친화적
// 이름을 재사용한다 — 원본 코드값("zh-CN" 등)을 select에 그대로 노출하지 않는다.
function localeName(code: string): string {
  return locales.value.find(l => l.code === code)?.name ?? code
}
const showLinks = ref(false)
const showInactive = ref(false)
const { data: links, refresh: refreshLinks } = await useApi<PagedResult<InfluencerLink>>('/api/admin/influencer-links', {
  query: () => ({ includeInactive: showInactive.value, pageSize: 100 }),
})

const editingId = ref<number | null>(null)
const formCode = ref('')
const formDisplayName = ref('')
const formUtmSource = ref('')
const formUtmMedium = ref('influencer')
const formUtmCampaign = ref('')
const formLocale = ref<typeof LOCALES[number]>('zh-CN')
const formIsActive = ref(true)
const formError = ref('')
const showForm = ref(false)

function startCreate() {
  editingId.value = null
  formCode.value = ''
  formDisplayName.value = ''
  formUtmSource.value = ''
  formUtmMedium.value = 'influencer'
  formUtmCampaign.value = ''
  formLocale.value = 'zh-CN'
  formIsActive.value = true
  formError.value = ''
  showForm.value = true
}

function startEdit(l: InfluencerLink) {
  editingId.value = l.id
  formCode.value = l.code
  formDisplayName.value = l.displayName
  formUtmSource.value = l.utmSource
  formUtmMedium.value = l.utmMedium
  formUtmCampaign.value = l.utmCampaign
  formLocale.value = l.locale as typeof LOCALES[number]
  formIsActive.value = l.isActive
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
      await authFetch('/api/admin/influencer-links', {
        method: 'POST',
        body: {
          code: formCode.value.trim(),
          displayName: formDisplayName.value.trim(),
          utmSource: formUtmSource.value,
          utmMedium: formUtmMedium.value,
          utmCampaign: formUtmCampaign.value,
          locale: formLocale.value,
        },
      })
    }
    else {
      await authFetch(`/api/admin/influencer-links/${editingId.value}`, {
        method: 'PUT',
        body: {
          displayName: formDisplayName.value.trim(),
          utmSource: formUtmSource.value,
          utmMedium: formUtmMedium.value,
          utmCampaign: formUtmCampaign.value,
          locale: formLocale.value,
          isActive: formIsActive.value,
        },
      })
    }
    showForm.value = false
    await refreshLinks()
  }
  catch (e: any) {
    formError.value = t(`errors.${e?.data?.code ?? 'UNKNOWN'}`)
  }
}

function shortUrl(code: string): string {
  if (import.meta.server) return `/go/${code}`
  return `${window.location.origin}/go/${code}`
}

const copiedCode = ref('')
async function copyUrl(code: string) {
  try {
    await navigator.clipboard.writeText(shortUrl(code))
    copiedCode.value = code
    setTimeout(() => { if (copiedCode.value === code) copiedCode.value = '' }, 1500)
  }
  catch {
    // 클립보드 API 미지원 환경 — 조용히 무시(URL은 이미 화면에 텍스트로 표시되어 있어 수동 복사 가능)
  }
}
</script>
