export type ProblemDetails = {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  reason?: string
} & Record<string, unknown>

export class ApiError extends Error {
  status: number
  problem?: ProblemDetails
  reason?: string

  constructor(status: number, message: string, problem?: ProblemDetails) {
    super(message)
    this.status = status
    this.problem = problem
    this.reason = typeof problem?.reason === 'string' ? problem.reason : undefined
  }
}
