import { useAuth0 } from '@auth0/auth0-react'
import { useCallback } from 'react'
import { isAuthConfigured } from './authConfig'

export function useApi() {
  const { getAccessTokenSilently, isAuthenticated } = useAuth0()

  const fetchJson = useCallback(
    async <T,>(path: string, init: RequestInit = {}): Promise<T> => {
      const headers = new Headers(init.headers)
      if (isAuthConfigured && isAuthenticated) {
        const token = await getAccessTokenSilently()
        headers.set('Authorization', `Bearer ${token}`)
      }
      const res = await fetch(path, { ...init, headers })
      if (!res.ok) {
        throw new Error(`${res.status} ${res.statusText}`)
      }
      return res.json() as Promise<T>
    },
    [getAccessTokenSilently, isAuthenticated],
  )

  return { fetchJson }
}
