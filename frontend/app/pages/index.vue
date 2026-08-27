<template>
  <div class="mx-auto max-w-3xl px-4 py-10">
    <section class="mb-10 text-center">
      <h1 class="text-2xl font-semibold text-foreground">{{ t('landing.hero.title') }}</h1>
      <p class="mt-2 text-muted-foreground">{{ t('landing.hero.subtitle') }}</p>
    </section>

    <Card>
      <CardHeader>
        <CardTitle>{{ t('landing.form.title') }}</CardTitle>
      </CardHeader>
      <CardContent>
        <!-- U2 — 완료 페이지 라우트를 따로 만들지 않고, 같은 화면에서 폼 자리를 완료 안내로 교체한다. -->
        <div v-if="successResult" class="flex flex-col gap-3">
          <p class="text-foreground">{{ t('landing.success.message') }}</p>
          <p class="text-sm text-muted-foreground">
            {{ t('landing.success.codeLabel') }}: <span class="font-mono font-semibold text-foreground">{{ successResult.code }}</span>
          </p>
          <p class="text-sm text-muted-foreground">
            {{ t('landing.success.wechatLabel') }}: <span class="font-semibold text-foreground">{{ successResult.wechatId }}</span>
          </p>
        </div>

        <!-- novalidate — 브라우저 기본 검증 팝업(브라우저/OS 언어를 따름)을 끄고 아래 커스텀 검증으로 대체한다. -->
        <form v-else class="flex flex-col gap-4" novalidate @submit.prevent="submit">
          <div class="flex flex-col gap-2">
            <Label for="name">{{ t('landing.form.name') }}</Label>
            <Input id="name" v-model="name" type="text" maxlength="50" required autocomplete="name" :aria-invalid="errors.name" />
            <p v-if="errors.name" class="text-sm text-destructive">{{ t('common.fieldRequired') }}</p>
          </div>

          <div class="flex flex-col gap-2">
            <Label for="birthDate">{{ t('landing.form.birthDate') }}</Label>
            <DatePicker id="birthDate" v-model="birthDate" :locale="inputLang" :invalid="errors.birthDate" />
            <p v-if="errors.birthDate" class="text-sm text-destructive">{{ t('common.fieldRequired') }}</p>
          </div>

          <div class="flex flex-col gap-2">
            <span class="text-sm leading-none font-medium">{{ t('landing.form.gender') }}</span>
            <div class="flex gap-4">
              <label class="flex items-center gap-2 text-sm">
                <input v-model="gender" type="radio" name="gender" value="Female" class="accent-primary" required>
                {{ t('landing.form.genderFemale') }}
              </label>
              <label class="flex items-center gap-2 text-sm">
                <input v-model="gender" type="radio" name="gender" value="Male" class="accent-primary">
                {{ t('landing.form.genderMale') }}
              </label>
              <label class="flex items-center gap-2 text-sm">
                <input v-model="gender" type="radio" name="gender" value="Other" class="accent-primary">
                {{ t('landing.form.genderOther') }}
              </label>
            </div>
            <p v-if="errors.gender" class="text-sm text-destructive">{{ t('common.fieldRequired') }}</p>
          </div>

          <div class="flex flex-col gap-2">
            <Label for="wechatId">{{ t('landing.form.wechatId') }}</Label>
            <Input id="wechatId" v-model="wechatId" type="text" maxlength="50" required autocomplete="off" :aria-invalid="errors.wechatId" />
            <p v-if="errors.wechatId" class="text-sm text-destructive">{{ t('common.fieldRequired') }}</p>
          </div>

          <div class="flex flex-col gap-2">
            <Label for="contactTime">{{ t('landing.form.contactTime') }}</Label>
            <TimePicker id="contactTime" v-model="contactTime" :locale="inputLang" :invalid="errors.contactTime" />
            <p v-if="errors.contactTime" class="text-sm text-destructive">{{ t('common.fieldRequired') }}</p>
          </div>

          <!-- honeypot(12-1절) — 사람에게는 보이지 않는 필드. 채워지면 봇으로 간주한다. -->
          <div class="absolute -left-[9999px]" aria-hidden="true">
            <label for="hpField">Website</label>
            <input id="hpField" v-model="honeypot" type="text" tabindex="-1" autocomplete="off">
          </div>

          <div class="flex flex-col gap-1">
            <label class="flex items-start gap-2 text-sm">
              <input v-model="consent" type="checkbox" class="mt-1 accent-primary" required>
              <!-- 🔴 태그 사이 줄바꿈이 공백 하나로 렌더링되어 "처리방침 에"처럼 어색한 공백이
                   생긴다(실측 확인) — 세 조각을 한 줄로 이어붙여 불필요한 공백을 없앤다. -->
              <span>{{ t('landing.form.consentPrefix') }}<NuxtLink :to="localePath('privacy')" class="underline" target="_blank">{{ t('landing.form.consentLink') }}</NuxtLink>{{ t('landing.form.consentSuffix') }}</span>
            </label>
            <p v-if="errors.consent" class="text-sm text-destructive">{{ t('common.fieldRequired') }}</p>
          </div>

          <p v-if="errorMessage" class="text-sm text-destructive">{{ errorMessage }}</p>

          <Button type="submit" :disabled="submitting">{{ t('landing.form.submit') }}</Button>
        </form>
      </CardContent>
    </Card>
  </div>
</template>

<script setup lang="ts">
// 랜딩(12-1절) — 헤더+히어로+예약 폼+푸터 중 히어로/폼/성공 안내. 헤더·푸터는 layouts/landing.vue.
// 히어로 문구는 최소 기능 설명이며, 실제 마케팅 카피·이미지(M6)는 범위 외(20장)로 보류된 상태다.
definePageMeta({ layout: 'landing' })

const { t, locale } = useI18n()
const localePath = useLocalePath()
const route = useRoute()
const config = useRuntimeConfig()

// DatePicker/TimePicker(9-2절①, D11 대체) — 팝업 캘린더 요일·월 이름까지 이 값으로 제어된다.
const inputLang = useInputLang()

useSeo({
  title: () => t('landing.hero.title'),
  description: () => t('landing.hero.subtitle'),
})

const name = ref('')
const birthDate = ref('')
const gender = ref('')
const wechatId = ref('')
const contactTime = ref('')
const consent = ref(false)
const honeypot = ref('')

const submitting = ref(false)
const errorMessage = ref('')
const successResult = ref<{ code: string; wechatId: string } | null>(null)

const utm = {
  source: (route.query.utm_source as string) || '',
  medium: (route.query.utm_medium as string) || '',
  campaign: (route.query.utm_campaign as string) || '',
  ref: (route.query.ref as string) || '',
}

// 15-1절 — 랜딩 SSR 시점에 프론트 서버가 내부 시크릿 헤더와 함께 방문을 기록한다.
// 🔴 await 하지 않는다(F6) — 방문 집계 실패·지연이 랜딩 렌더 응답 시간에 영향을 주면 안 된다.
if (import.meta.server) {
  $fetch(`${config.apiBaseInternal}/api/internal/landing-visit`, {
    method: 'POST',
    headers: { 'X-Internal-Secret': config.internalSecret as string },
    body: { referralCode: utm.ref, utmSource: utm.source, utmMedium: utm.medium, utmCampaign: utm.campaign },
    timeout: 2000,
  }).catch(() => {})
}

// 브라우저 기본 검증(novalidate로 비활성화, 위 템플릿 참고)을 대체하는 커스텀 검증 —
// 브라우저 기본 팝업은 페이지 로케일이 아니라 브라우저/OS 언어를 따라 메시지가 나오는 문제가 있었다.
const errors = reactive({
  name: false,
  birthDate: false,
  gender: false,
  wechatId: false,
  contactTime: false,
  consent: false,
})

function validate(): boolean {
  errors.name = !name.value.trim()
  errors.birthDate = !birthDate.value
  errors.gender = !gender.value
  errors.wechatId = !wechatId.value.trim()
  errors.contactTime = !contactTime.value
  errors.consent = !consent.value
  return !Object.values(errors).some(Boolean)
}

async function submit() {
  errorMessage.value = ''
  if (!validate()) return
  submitting.value = true
  try {
    const res = await $fetch<{ code: string; wechatId: string }>('/api/reservations', {
      method: 'POST',
      body: {
        name: name.value,
        birthDate: birthDate.value,
        gender: gender.value,
        wechatId: wechatId.value,
        preferredContactTime: `${contactTime.value}:00`,
        locale: locale.value,
        privacyConsent: consent.value,
        honeypot: honeypot.value,
        utmSource: utm.source,
        utmMedium: utm.medium,
        utmCampaign: utm.campaign,
        referralCode: utm.ref,
      },
    })
    successResult.value = res
  } catch (e: any) {
    const code = (e?.data?.code as string | undefined) ?? 'SUBMIT_FAILED'
    errorMessage.value = t(`errors.${code}`)
  } finally {
    submitting.value = false
  }
}
</script>
