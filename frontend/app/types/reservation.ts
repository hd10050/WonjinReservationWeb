export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export type ReservationStatus = 'New' | 'Consulting' | 'Confirmed' | 'Visited' | 'Cancelled'

export interface ReservationListItem {
  id: number
  code: string
  name: string
  wechatId: string
  status: ReservationStatus
  consultantId: number | null
  consultantName: string | null
  createdAt: string
  visitDate: string | null
}

export interface ReservationSummary {
  new: number
  consulting: number
  confirmed: number
  visitedThisMonth: number
}

export interface ReservationNote {
  id: number
  body: string
  authorUserId: number | null
  authorName: string
  createdAt: string
  updatedAt: string
  isEdited: boolean
}

export interface ReservationLog {
  id: number
  action: string
  note: string | null
  actorName: string
  createdAt: string
}

export interface ReservationNoteRevision {
  id: number
  body: string
  editedByName: string
  editedAt: string
}

export interface ReservationDetail {
  id: number
  code: string
  name: string
  birthDate: string
  gender: 'Female' | 'Male' | 'Other'
  wechatId: string
  preferredContactDate: string | null
  preferredContactTime: string | null
  locale: string
  status: ReservationStatus
  consultantId: number | null
  consultantName: string | null
  visitDate: string | null
  visitTime: string | null
  depositAmount: number | null
  depositCurrency: 'CNY' | 'KRW'
  depositPaid: boolean
  cancelReason: string | null
  utmSource: string
  utmMedium: string
  utmCampaign: string
  referralCode: string
  createdAt: string
  updatedAt: string
  consultingAt: string | null
  confirmedAt: string | null
  visitedAt: string | null
  cancelledAt: string | null
  procedureIds: number[]
  notes: ReservationNote[]
  logs: ReservationLog[]
}

export interface ReservationCalendarItem {
  id: number
  code: string
  name: string
  status: ReservationStatus
  visitDate: string
  visitTime: string | null
  consultantName: string | null
}

// 달력 그리드 배지용 — 날짜별 건수만(2026-08-27, 상세 목록과 분리)
export interface ReservationCalendarDayCount {
  visitDate: string
  count: number
}

export interface ConsultantLookup {
  id: number
  name: string
  isActive: boolean
  sortOrder: number
}

// D25(2026-08-28) — sortOrder 폐지, categoryId 추가(예약 상세 아코디언이 이 값으로 카테고리별 그룹핑).
export interface ProcedureLookup {
  id: number
  code: string
  categoryId: number
  nameZhCn: string
  nameZhTw: string
  nameEn: string
  nameKo: string
  isActive: boolean
}

// D25 — 시술 카테고리 마스터. 관리 탭 목록 + 예약 상세 시술 아코디언 그룹 헤더가 공유.
export interface CategoryLookup {
  id: number
  code: string
  nameZhCn: string
  nameZhTw: string
  nameEn: string
  nameKo: string
  isActive: boolean
}

// Phase 6 — 실장 KPI(11-4절). 활성 실장은 실적 0건이어도 0행으로 내려온다(11-6절 구간 0 채움).
export interface ConsultantKpi {
  consultantId: number
  consultantName: string
  assigned: number
  confirmed: number
  visited: number
  conversionRate: number
}

// Phase 6 — 예약 통계 주간 추이(D16). weekStart는 date(타임존 없음) 컬럼이라 KST 변환 불필요.
export interface WeeklyReservationStat {
  weekStart: string
  received: number
  confirmed: number
  visited: number
  cancelled: number
}

export interface ProcedureStat {
  procedureId: number
  nameZhCn: string
  nameZhTw: string
  nameEn: string
  nameKo: string
  count: number
}

export interface LocaleStat {
  locale: string
  count: number
}

// 담당 실장 축(11-4절) — 비활성 실장 제외, KPI와 달리 0행 채움 없음(실적 있는 실장만).
export interface ConsultantReservationStat {
  consultantId: number
  consultantName: string
  count: number
}

export interface ReservationStats {
  weekly: WeeklyReservationStat[]
  procedures: ProcedureStat[]
  locales: LocaleStat[]
  consultants: ConsultantReservationStat[]
}

// Phase 8 — 유입 경로 분석(D4·D5, 15-2절). 방문 기록(landing_daily_stats)이 있는 조합만 내려온다.
export interface ReferralStat {
  referralCode: string
  utmSource: string
  utmMedium: string
  utmCampaign: string
  visitCount: number
  reservationCount: number
  conversionRate: number
  confirmedCount: number
  confirmedConversionRate: number
}

// B안, 2026-08-27 신설 — 인플루언서 짧은 링크(/go/{code}) 매핑. code는 생성 후 변경 불가.
export interface InfluencerLink {
  id: number
  code: string
  displayName: string
  utmSource: string
  utmMedium: string
  utmCampaign: string
  locale: string
  isActive: boolean
  createdAt: string
}

export type AdminRole = 'Admin' | 'HospitalManager' | 'Consultant'

export interface AdminUser {
  id: number
  email: string
  role: AdminRole
  name: string
  locale: string
  isSuspended: boolean
  createdAt: string
}

export interface AuditLogEntry {
  id: number
  actorUserId: number | null
  actorEmail: string
  actorRole: string
  action: string
  entityType: string
  entityId: string | null
  summary: string
  ip: string | null
  statusCode: number
  createdAt: string
}
