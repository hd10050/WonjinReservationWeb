<template>
  <!-- 공개 랜딩 헤더 — layouts/landing.vue와 관리자 로그인 페이지가 공유(12-2절, 2026-08-28).
       언어 선택은 @select-locale 이벤트만 올리고 실제 동작은 사용처가 정한다:
       랜딩은 switchLocalePath 네비게이션, 로그인(i18n:false)은 setLocale + wj_lang 쿠키. -->
  <header class="border-b bg-card">
    <div class="mx-auto flex max-w-3xl items-center justify-between gap-4 px-4 py-3">
      <NuxtLink :to="localePath('index')" class="flex shrink-0 items-center">
        <img src="/logo.svg" :alt="t('common.appName')" class="h-9 w-auto sm:h-12">
      </NuxtLink>

      <!-- 🔴 375px 모바일 헤더 깨짐 대응(landing.vue 최종 리뷰 이력): 홈·문의하기 텍스트 링크는
           모바일에서 숨기고(로고 클릭·FAB으로 대체 가능), 로고 축소(h-9→sm:h-12), 언어버튼 국가명도
           모바일에선 숨김. 셋 중 하나만 빼도 다시 깨지니 함께 유지할 것. -->
      <nav class="flex flex-1 items-center justify-center gap-4 text-sm font-medium">
        <NuxtLink :to="localePath('index')" class="hidden text-muted-foreground hover:text-foreground sm:inline">{{ t('landing.nav.home') }}</NuxtLink>
        <DropdownMenuRoot>
          <DropdownMenuTrigger class="flex items-center gap-1 text-muted-foreground hover:text-foreground aria-expanded:text-foreground">
            {{ t('landing.nav.procedures') }}
            <ChevronDown class="size-3.5" />
          </DropdownMenuTrigger>
          <DropdownMenuPortal>
            <DropdownMenuContent :side-offset="8" align="center" class="z-50 max-h-[70vh] min-w-40 overflow-y-auto rounded-lg border bg-card p-1 text-sm shadow-md">
              <DropdownMenuItem
                v-for="category in PROCEDURE_CATEGORIES"
                :key="category.slug"
                as-child
                class="block cursor-pointer rounded-md px-3 py-1.5 text-foreground outline-none data-[highlighted]:bg-accent data-[highlighted]:text-accent-foreground"
              >
                <NuxtLink :to="localePath({ name: 'procedures-category', params: { category: category.slug } })">
                  {{ category.name[locale as Locale] }}
                </NuxtLink>
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenuPortal>
        </DropdownMenuRoot>
        <NuxtLink :to="localePath('inquiry')" class="hidden text-muted-foreground hover:text-foreground sm:inline">{{ t('landing.nav.inquiry') }}</NuxtLink>
      </nav>

      <DropdownMenuRoot>
        <DropdownMenuTrigger
          :aria-label="currentLocaleName"
          class="flex shrink-0 items-center gap-1 rounded-full border px-3 py-1.5 text-xs font-medium text-muted-foreground transition-colors hover:border-primary hover:text-foreground aria-expanded:border-primary aria-expanded:text-foreground"
        >
          <Globe class="size-3.5" />
          <span class="hidden sm:inline">{{ currentLocaleName }}</span>
          <ChevronDown class="size-3.5" />
        </DropdownMenuTrigger>
        <DropdownMenuPortal>
          <DropdownMenuContent :side-offset="8" align="end" class="z-50 min-w-32 rounded-lg border bg-card p-1 text-sm shadow-md">
            <DropdownMenuItem
              v-for="loc in locales"
              :key="loc.code"
              class="block w-full cursor-pointer rounded-md px-3 py-1.5 text-foreground outline-none data-[highlighted]:bg-accent data-[highlighted]:text-accent-foreground"
              :class="{ 'font-semibold': loc.code === locale }"
              @select="emit('selectLocale', loc.code)"
            >
              {{ loc.name }}
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenuPortal>
      </DropdownMenuRoot>
    </div>
  </header>
</template>

<script setup lang="ts">
import { ChevronDown, Globe } from "@lucide/vue"
import { PROCEDURE_CATEGORIES, type Locale } from "~/data/procedures"
import {
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuPortal,
  DropdownMenuRoot,
  DropdownMenuTrigger,
} from "reka-ui"

const emit = defineEmits<{ (e: "selectLocale", code: string): void }>()

const { t, locale, locales } = useI18n()
const localePath = useLocalePath()
const currentLocaleName = computed(() => locales.value.find(l => l.code === locale.value)?.name ?? locale.value)
</script>
