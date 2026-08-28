<template>
  <!-- 공개 랜딩 헤더 — layouts/landing.vue와 관리자 로그인 페이지가 공유(12-2절, 2026-08-28).
       언어 선택은 @select-locale 이벤트만 올리고 실제 동작은 사용처가 정한다:
       랜딩은 switchLocalePath 네비게이션, 로그인(i18n:false)은 setLocale + wj_lang 쿠키.
       🔴 overlay prop(2026-08-28, 랜딩 비주얼 리디자인 5절) — 풀블리드 히어로가 있는 페이지(홈·카테고리
       목록)에서만 true. login.vue는 이 prop을 넘기지 않아 기본값 false로 기존 동작(고정 흰 헤더) 그대로 —
       공유 컴포넌트 회귀 없음.
       🔴 overlay=true일 때 position은 스크롤 여부와 무관하게 항상 fixed로 고정한다 — 스크롤 시
       absolute→sticky처럼 position 자체를 바꾸면 헤더가 문서 흐름에 갑자기 끼어들며 히어로 아래
       콘텐츠가 헤더 높이만큼 밀리는 레이아웃 점프가 생긴다(실제 확인). fixed는 처음부터 흐름 밖이라
       배경·글자색만 바뀌어도 점프가 없다 — 참고 사이트(k-wonjin.co.kr)도 스크롤 후 네브바가 같은
       자리에 고정된 채 배경만 불투명해지는 동일 패턴(ref-wonjin-2/3 스크린샷으로 확인). -->
  <header
    class="z-40 transition-colors duration-300"
    :class="props.overlay
      ? ['fixed inset-x-0 top-0', isTransparent ? 'border-transparent bg-transparent' : 'border-b bg-card']
      : 'border-b bg-card'"
  >
    <div class="mx-auto flex max-w-6xl items-center justify-between gap-4 px-4 py-3 sm:px-6">
      <NuxtLink :to="localePath('index')" class="flex shrink-0 items-center">
        <img
          src="/logo.svg"
          :alt="t('common.appName')"
          class="h-9 w-auto transition-[filter] duration-300 sm:h-12"
          :class="{ 'brightness-0 invert': isTransparent }"
        >
      </NuxtLink>

      <!-- 🔴 375px 모바일 헤더 깨짐 대응(landing.vue 최종 리뷰 이력): 홈·문의하기 텍스트 링크는
           모바일에서 숨기고(로고 클릭·FAB으로 대체 가능), 로고 축소(h-9→sm:h-12), 언어버튼 국가명도
           모바일에선 숨김. 셋 중 하나만 빼도 다시 깨지니 함께 유지할 것. -->
      <nav class="flex flex-1 items-center justify-center gap-4 text-sm font-medium">
        <NuxtLink :to="localePath('index')" class="hidden sm:inline" :class="navLinkClass">{{ t('landing.nav.home') }}</NuxtLink>
        <DropdownMenuRoot>
          <DropdownMenuTrigger class="flex items-center gap-1 aria-expanded:opacity-100" :class="navLinkClass">
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
        <NuxtLink :to="localePath('inquiry')" class="hidden sm:inline" :class="navLinkClass">{{ t('landing.nav.inquiry') }}</NuxtLink>
      </nav>

      <DropdownMenuRoot>
        <DropdownMenuTrigger
          :aria-label="currentLocaleName"
          class="flex shrink-0 items-center gap-1 rounded-full border px-3 py-1.5 text-xs font-medium transition-colors aria-expanded:opacity-100"
          :class="isTransparent
            ? 'border-white/40 text-white hover:border-white hover:text-white'
            : 'text-muted-foreground hover:border-primary hover:text-foreground'"
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

const props = withDefaults(defineProps<{ overlay?: boolean }>(), { overlay: false })
const emit = defineEmits<{ (e: "selectLocale", code: string): void }>()

const { t, locale, locales } = useI18n()
const localePath = useLocalePath()
const currentLocaleName = computed(() => locales.value.find(l => l.code === locale.value)?.name ?? locale.value)

// overlay 모드에서 스크롤 40px 이후엔 일반 흰 헤더로 전환(히어로를 벗어난 뒤엔 텍스트가
// 콘텐츠 위에 겹치므로). overlay=false(로그인 페이지)에서는 스크롤 리스너 자체를 달지 않는다.
const scrolled = ref(false)
const isTransparent = computed(() => props.overlay && !scrolled.value)

if (import.meta.client) {
  onMounted(() => {
    if (!props.overlay) return
    function onScroll() { scrolled.value = window.scrollY > 40 }
    onScroll()
    window.addEventListener('scroll', onScroll, { passive: true })
    onUnmounted(() => window.removeEventListener('scroll', onScroll))
  })
}

const navLinkClass = computed(() => isTransparent.value
  ? 'text-white/90 hover:text-white'
  : 'text-muted-foreground hover:text-foreground')
</script>
