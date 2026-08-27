<template>
  <div class="space-y-6">
    <h1 class="text-xl font-semibold text-foreground">{{ t('admin.referrals.title') }}</h1>

    <Card>
      <CardContent class="flex flex-wrap items-end gap-4">
        <div class="flex flex-col gap-1.5">
          <Label for="f-from">{{ t('admin.referrals.filterFrom') }}</Label>
          <DatePicker id="f-from" v-model="formFrom" :locale="inputLang" />
        </div>
        <div class="flex flex-col gap-1.5">
          <Label for="f-to">{{ t('admin.referrals.filterTo') }}</Label>
          <DatePicker id="f-to" v-model="formTo" :locale="inputLang" />
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
import type { ReferralStat } from '~/types/reservation'
import { todayKst } from '~/utils/datetime'

definePageMeta({ middleware: 'admin', layout: 'admin', i18n: false })
useHead({ title: '유입 경로 분석 | Admin', meta: [{ name: 'robots', content: 'noindex, nofollow' }] })

const { t } = useI18n()
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
</script>
