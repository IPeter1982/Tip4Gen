import { useAuth0 } from '@auth0/auth0-react'
import { useCallback, useMemo } from 'react'
import { ApiError, type ProblemDetails } from '../api/errors'
import { isAuthConfigured } from './authConfig'

type RequestOptions = Omit<RequestInit, 'body'> & { body?: unknown }

async function readError(res: Response): Promise<ApiError> {
  const text = await res.text()
  if (!text) return new ApiError(res.status, `${res.status} ${res.statusText}`)
  try {
    const parsed = JSON.parse(text) as ProblemDetails
    const msg = parsed.detail || parsed.title || `${res.status} ${res.statusText}`
    return new ApiError(res.status, msg, parsed)
  } catch {
    return new ApiError(res.status, text || `${res.status} ${res.statusText}`)
  }
}

export function useApi() {
  const { getAccessTokenSilently, isAuthenticated } = useAuth0()

  const request = useCallback(
    async <T,>(path: string, init: RequestOptions = {}): Promise<T> => {
      const headers = new Headers(init.headers)
      if (init.body !== undefined && !headers.has('Content-Type')) {
        headers.set('Content-Type', 'application/json')
      }
      if (isAuthConfigured && isAuthenticated) {
        const token = await getAccessTokenSilently()
        headers.set('Authorization', `Bearer ${token}`)
      }
      const body = init.body === undefined ? undefined : JSON.stringify(init.body)
      const res = await fetch(path, { ...init, headers, body })
      if (!res.ok) throw await readError(res)
      if (res.status === 204) return undefined as T
      const text = await res.text()
      return text ? (JSON.parse(text) as T) : (undefined as T)
    },
    [getAccessTokenSilently, isAuthenticated],
  )

  return useMemo(() => ({
    get: <T,>(path: string) => request<T>(path),
    put: <T,>(path: string, body?: unknown) => request<T>(path, { method: 'PUT', body }),
    post: <T,>(path: string, body?: unknown) => request<T>(path, { method: 'POST', body }),
    del: <T,>(path: string) => request<T>(path, { method: 'DELETE' }),
    // back-compat for existing call sites
    fetchJson: <T,>(path: string, init: RequestInit = {}) => request<T>(path, init),
  }), [request])
}
