import { useAuth0 } from '@auth0/auth0-react'
import type { ReactNode } from 'react'
import { useEffect } from 'react'
import { isAuthConfigured } from './authConfig'

export function RequireAuth({ children }: { children: ReactNode }) {
  const { isAuthenticated, isLoading, loginWithRedirect } = useAuth0()

  useEffect(() => {
    if (!isAuthConfigured || isLoading) return
    if (!isAuthenticated) loginWithRedirect()
  }, [isAuthConfigured, isAuthenticated, isLoading, loginWithRedirect])

  if (!isAuthConfigured) {
    return (
      <main className="min-h-svh bg-stone-100 text-stone-900 flex items-center justify-center p-6">
        <div className="max-w-xl border-2 border-stone-900 bg-white p-6">
          <p className="text-xs font-mono uppercase tracking-[0.15em] text-orange-600 mb-2">
            Auth0 nincs konfigurálva
          </p>
          <p className="font-mono text-sm">
            Másold a <code>web/.env.local.example</code> fájlt <code>web/.env.local</code>-ra, töltsd ki a VITE_AUTH0_* értékeket, majd indítsd újra a dev szervert.
          </p>
        </div>
      </main>
    )
  }

  if (isLoading || !isAuthenticated) {
    return (
      <main className="min-h-svh bg-stone-100 flex items-center justify-center">
        <p className="font-mono text-stone-500">bejelentkezés…</p>
      </main>
    )
  }

  return <>{children}</>
}
