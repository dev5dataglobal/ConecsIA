/**
 * Problem Details (RFC 9457) tal como los emite la API (ADR-020).
 * El frontend solo los presenta; nunca decide a partir de ellos permisos ni vigencias.
 */
export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  traceId?: string
  /** Código de motivo estable del negocio (p. ej. AUTORIZACION_NO_VIGENTE). */
  code?: string
  /** Errores de validación por campo. */
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  readonly status: number
  readonly problem: ProblemDetails

  constructor(status: number, problem: ProblemDetails) {
    super(problem.title ?? `HTTP ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function readErrors(value: unknown): Record<string, string[]> | undefined {
  if (!isRecord(value)) return undefined
  const result: Record<string, string[]> = {}
  for (const [field, messages] of Object.entries(value)) {
    if (Array.isArray(messages)) {
      result[field] = messages.filter((m): m is string => typeof m === 'string')
    }
  }
  return result
}

/** Convierte un cuerpo desconocido en ProblemDetails sin confiar en su forma. */
export function parseProblemDetails(body: unknown, status: number): ProblemDetails {
  if (!isRecord(body)) return { status }
  const text = (key: string) => (typeof body[key] === 'string' ? (body[key] as string) : undefined)
  return {
    type: text('type'),
    title: text('title'),
    status: typeof body.status === 'number' ? body.status : status,
    detail: text('detail'),
    instance: text('instance'),
    traceId: text('traceId'),
    code: text('code'),
    errors: readErrors(body.errors),
  }
}

/** Errores de un campo, buscando sin distinguir mayúsculas (la API usa camelCase). */
export function fieldErrors(problem: ProblemDetails, field: string): string[] {
  if (!problem.errors) return []
  const key = Object.keys(problem.errors).find((k) => k.toLowerCase() === field.toLowerCase())
  return key ? (problem.errors[key] ?? []) : []
}
