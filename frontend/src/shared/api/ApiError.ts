import type { ErroDeCampo } from '@/shared/api/types'

export interface ApiErrorInit {
  status: number
  code: string
  title: string
  detail: string
  meta?: Record<string, unknown> | null
  errors?: ErroDeCampo[] | null
  traceId?: string
  retryAfterSegundos?: number
}

export class ApiError extends Error {
  readonly status: number
  readonly code: string
  readonly title: string
  readonly detail: string
  readonly meta: Record<string, unknown>
  readonly errors: ErroDeCampo[]
  readonly traceId?: string
  readonly retryAfterSegundos?: number

  constructor(init: ApiErrorInit) {
    super(init.detail)
    this.name = 'ApiError'
    this.status = init.status
    this.code = init.code
    this.title = init.title
    this.detail = init.detail
    this.meta = init.meta ?? {}
    this.errors = init.errors ?? []
    this.traceId = init.traceId
    this.retryAfterSegundos = init.retryAfterSegundos
  }
}
