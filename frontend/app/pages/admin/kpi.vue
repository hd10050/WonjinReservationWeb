<template>
  <div class="space-y-6">
    <h1 class="text-xl font-semibold text-foreground">{{ t('admin.kpi.title') }}</h1>

    <Card>
      <CardContent class="flex flex-wrap items-end gap-4 pt-6">
        <div class="flex flex-col gap-1.5">
          <Label for="f-from">{{ t('admin.kpi.filterFrom') }}</Label>
          <Input id="f-from" v-model="formFrom" type="date" class="w-40" />
        </div>
        <div class="flex flex-col gap-1.5">
          <Label for="f-to">{{ t('admin.kpi.filterTo') }}</Label>
          <Input id="f-to" v-model="formTo" type="date" class="w-40" />
        </div>
        <Button @click="applyFilters">{{ t('admin.kpi.filterApply') }}</Button>
      </CardContent>
    </Card>

    <Card>
      <CardContent class="pt-6">
        <ClientOnly>
          <div style="height: 320px">
            <Bar :data="chartData" :options="chartOptions" />
          </div>
          <template #fallback>
            <div style="height: 320px" />
          </template>
        </ClientOnly>
      </CardContent>
    </Card>

    <div class="overflow-x-auto rounded-md border border-border">
      <table class="w-full text-sm">
        <thead class="bg-muted text-muted-foreground">
          <tr>
            <th class="px-3 py-2 text-left">{{ t('admin.kpi.colConsultant') }}</th>
            <th class="px-3 py-2 text-right">{{ t('admin.kpi.colAssigned') }}</th>
            <th class="px-3 py-2 text-right">{{ t('admin.kpi.colConfirmed') }}</th>
            <th class="px-3 py-2 text-right">{{ t('admin.kpi.colVisited') }}</th>
            <th class="px-3 py-2 text-right">{{ t('admin.kpi.colConversionRate') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!data?.length">
            <td colspan="5" class="p-6 text-center text-muted-foreground">{{ t('admin.kpi.empty') }}</td>
          </tr>
          <tr v-for="r in data" :key="r.consultantId" class="border-t border-border">
            <td class="px-3 py-2">{{ r.consultantName }}</td>
            <td class="px-3 py-2 text-right">{{ r.assigned }}</td>
            <td class="px-3 py-2 text-right">{{ r.confirmed }}</td>
            <td class="px-3 py-2 text-right">{{ r.visited }}</td>
            <td class="px-3 py-2 text-right">{{ r.conversionRate }}%</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup lang="ts">
import { Bar } from 'vue-chartjs'
import type { ConsultantKpi } from '~/types/reservation'
import { todayKst } from '~/utils/datetime'

definePageMeta({ middleware: 'admin', layout: 'admin', i18n: false })
useHead({ title: '실장 KPI | Admin', meta: [{ name: 'robots', content: 'noindex, nofollow' }] })

const { t } = useI18n()
const route = useRoute()

const defaultFrom = `${todayKst().slice(0, 7)}-01`
const defaultTo = todayKst()

// 🔴 검색 입력을 반응형 query에 직접 물리지 말 것(12-4절)과 동일 이유로 URL 쿼리를 computed로 감싼다.
const query = computed(() => ({
  from: (route.query.from as string) || defaultFrom,
  to: (route.query.to as string) || defaultTo,
}))

const { data } = await useApi<ConsultantKpi[]>('/api/admin/stats/consultants', { query })

const formFrom = ref(query.value.from)
const formTo = ref(query.value.to)

function applyFilters() {
  navigateTo({ query: { from: formFrom.value, to: formTo.value } })
}

// D20 Olive Garden Feast 팔레트를 재사용한다(D21) — 차트 전용 색상을 새로 만들지 않는다.
const COLOR_PRIMARY = '#606C38'
const COLOR_FOREGROUND = '#283618'
const COLOR_SECONDARY = '#DDA15E'

const chartData = computed(() => ({
  labels: (data.value ?? []).map(r => r.consultantName),
  datasets: [
    { label: t('admin.kpi.colAssigned'), data: (data.value ?? []).map(r => r.assigned), backgroundColor: COLOR_FOREGROUND },
    { label: t('admin.kpi.colConfirmed'), data: (data.value ?? []).map(r => r.confirmed), backgroundColor: COLOR_PRIMARY },
    { label: t('admin.kpi.colVisited'), data: (data.value ?? []).map(r => r.visited), backgroundColor: COLOR_SECONDARY },
  ],
}))
const chartOptions = { responsive: true, maintainAspectRatio: false }
</script>
