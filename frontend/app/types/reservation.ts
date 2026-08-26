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

export interface ReservationDetail {
  id: number
  code: string
  name: string
  birthDate: string
  gender: 'Female' | 'Male' | 'Other'
  wechatId: string
  preferredContactTime: string
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

export interface ConsultantLookup {
  id: number
  name: string
  isActive: boolean
  sortOrder: number
}

export interface ProcedureLookup {
  id: number
  nameZhCn: string
  nameZhTw: string
  nameEn: string
  nameKo: string
  isActive: boolean
  sortOrder: number
}
