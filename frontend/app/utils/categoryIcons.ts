// 카테고리 아이콘 11개를 명시적으로 임포트한다 — `import * as icons from '@lucide/vue'` +
// 동적 프로퍼티 접근은 트리셰이킹이 깨져 광고 랜딩 페이지 번들에 아이콘 1770개(gzip 158KB)가
// 전부 실리는 결함이 최종 리뷰에서 실측 확인됐다(청크 lucide-vue, index.vue·procedures/[category]가 preload).
import { Dna, Eye, Heart, HeartHandshake, PersonStanding, Scan, ScanFace, Sparkles, Stethoscope, TrendingUp, UserRound } from '@lucide/vue'
import type { Component } from 'vue'

export const CATEGORY_ICONS: Record<string, Component> = {
  Eye,
  ScanFace,
  Stethoscope,
  TrendingUp,
  Sparkles,
  Dna,
  Heart,
  Scan,
  PersonStanding,
  UserRound,
  HeartHandshake,
}
