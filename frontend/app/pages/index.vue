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

        <form v-else class="flex flex-col gap-4" @submit.prevent="submit">
          <div class="flex flex-col gap-2">
            <Label for="name">{{ t('landing.form.name') }}</Label>
            <Input id="name" v-model="name" type="text" maxlength="50" required autocomplete="name" />
          </div>

          <div class="flex flex-col gap-2">
            <Label for="birthDate">{{ t('landing.form.birthDate') }}</Label>
            <Input id="birthDate" v-model="birthDate" type="date" required :lang="inputLang" />
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
          </div>

          <div class="flex flex-col gap-2">
            <Label for="wechatId">{{ t('landing.form.wechatId') }}</Label>
            <Input id="wechatId" v-model="wechatId" type="text" maxlength="50" required autocomplete="off" />
          </div>

          <div class="flex flex-col gap-2">
            <Label for="contactTime">{{ t('landing.form.contactTime') }}</Label>
            <Input id="contactTime" v-model="contactTime" type="time" required :lang="inputLang" />
          </div>

          <!-- honeypot(12-1절) — 사람에게는 보이지 않는 필드. 채워지면 봇으로 간주한다. -->
          <div class="absolute -left-[9999px]" aria-hidden="true">
            <label for="hpField">Website</label>
            <input id="hpField" v-model="honeypot" type="text" tabindex="-1" autocomplete="off">
          </div>

          <label class="flex items-start gap-2 text-sm">
            <input v-model="consent" type="checkbox" class="mt-1 accent-primary" required>
            <!-- 🔴 태그 사이 줄바꿈이 공백 하나로 렌더링되어 "처리방침 에"처럼 어색한 공백이
                 생긴다(실측 확인) — 세 조각을 한 줄로 이어붙여 불필요한 공백을 없앤다. -->
            <span>{{ t('landing.form.consentPrefix') }}<NuxtLink :to="localePath('privacy')" class="underline" target="_blank">{{ t('landing.form.consentLink') }}</NuxtLink>{{ t('landing.form.consentSuffix') }}</span>
          </label>

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

const { t, locale, locales } = useI18n()
const localePath = useLocalePath()
const route = useRoute()
const config = useRuntimeConfig()

// 🔴 네이티브 <input type="date">/<input type="time">의 표시 형식(연월일 순서·오전/오후 표기)은
// 브라우저가 이 lang 속성으로 판단한다(<html lang>만으로는 개별 입력 요소까지 안 이어지는
// 경우가 있어 명시적으로 지정) — nuxt.config.ts의 locales[].language(BCP-47 태그)를 그대로 쓴다.
// 단, 팝업 달력 자체의 요일·월 이름은 이 속성과 무관하게 브라우저/OS 자체 언어를 따르는
// 네이티브 위젯이라 웹페이지 코드로 제어할 수 없다(브라우저 공통의 잘 알려진 한계 — 이 프로젝트는
// D11에 따라 별도 JS 날짜선택 라이브러리를 쓰지 않기로 했으므로 이 잔여 한계는 감수한다).
const inputLang = computed(() => locales.value.find(l => l.code === locale.value)?.language ?? locale.value)

useHead({ title: () => `${t('landing.hero.title')} - ${t('common.appName')}` })

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

async function submit() {
  errorMessage.value = ''
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
