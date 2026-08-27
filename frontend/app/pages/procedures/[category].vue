<!-- frontend/app/pages/procedures/[category].vue -->
<template>
  <div v-if="category">
    <section
      class="relative flex min-h-80 items-end bg-cover bg-center text-background"
      :style="{ backgroundImage: `linear-gradient(to top, rgba(0,0,0,.6), rgba(0,0,0,.25)), url(/img/hero/${category.heroImages[0]})` }"
    >
      <div class="mx-auto w-full max-w-3xl px-4 pb-10">
        <component :is="categoryIcon(category.icon)" class="mb-3 size-8" />
        <h1 class="text-3xl font-bold">{{ category.name[locale as Locale] }}</h1>
        <p class="mt-3 max-w-xl text-background/90">{{ category.intro[locale as Locale] }}</p>
      </div>
    </section>

    <section class="mx-auto max-w-3xl px-4 py-10">
      <h2 class="mb-6 text-xl font-semibold text-foreground">
        {{ t('procedures.concernHeading', { category: category.name[locale as Locale] }) }}
      </h2>

      <ul class="divide-y divide-border">
        <li v-for="(item, i) in category.items" :key="item.slug">
          <NuxtLink
            :to="localePath({ name: 'procedures-category-procedure', params: { category: category.slug, procedure: item.slug } })"
            class="flex flex-col gap-4 py-6 sm:flex-row sm:items-center"
            :class="{ 'sm:flex-row-reverse': i % 2 === 1 }"
          >
            <img
              :src="`/img/${item.imageCategory ?? category.slug}/${item.image}`"
              :alt="item.name[locale as Locale]"
              class="h-48 w-full rounded-lg object-cover sm:w-64 sm:shrink-0"
            >
            <div class="flex flex-1 flex-col gap-2">
              <ul class="space-y-1 text-sm text-muted-foreground">
                <li v-for="(concern, ci) in item.concerns[locale as Locale]" :key="ci">{{ concern }}</li>
              </ul>
              <h3 class="text-lg font-semibold text-foreground">{{ item.name[locale as Locale] }}</h3>
            </div>
          </NuxtLink>
        </li>
      </ul>

      <div v-if="category.otherItems.length" class="mt-10 rounded-lg border bg-muted/30 p-5">
        <h3 class="mb-3 text-sm font-semibold text-muted-foreground">{{ t('procedures.otherHeading') }}</h3>
        <div class="flex flex-wrap gap-2">
          <NuxtLink
            v-for="other in category.otherItems"
            :key="other.slug"
            :to="localePath({ name: 'procedures-category-procedure', params: { category: category.slug, procedure: other.slug } })"
            class="rounded-full border px-3 py-1.5 text-sm text-foreground hover:border-primary"
          >
            {{ other.name[locale as Locale] }}
          </NuxtLink>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import * as icons from '@lucide/vue'
import { findCategory, type Locale } from '~/data/procedures'

definePageMeta({ layout: 'landing' })

const route = useRoute()
const { t, locale } = useI18n()
const localePath = useLocalePath()

const category = computed(() => findCategory(route.params.category as string))

if (!category.value) {
  throw createError({ statusCode: 404, statusMessage: 'Category not found' })
}

function categoryIcon(name: string) {
  return (icons as Record<string, unknown>)[name]
}

useSeo({
  title: () => category.value?.name[locale.value as Locale] ?? '',
  description: () => category.value?.intro[locale.value as Locale] ?? '',
})
</script>
