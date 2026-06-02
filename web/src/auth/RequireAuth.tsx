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
      <main className="min-h-svh bg-surface text-fg-default flex items-center justify-center p-6">
        <div className="max-w-xl rounded-2xl border border-warning/40 bg-warning/10 p-6">
          <p className="text-xs font-mono uppercase tracking-[0.15em] text-warning mb-2">
            Auth0 nincs konfigurálva
          </p>
          <p className="font-mono text-sm text-fg-default">
            Másold a <code>web/.env.local.example</code> fájlt <code>web/.env.local</code>-ra, töltsd ki a VITE_AUTH0_* értékeket, majd indítsd újra a dev szervert.
          </p>
        </div>
      </main>
    )
  }

  if (isLoading || !isAuthenticated) {
    return (
      <main className="min-h-svh bg-surface flex items-center justify-center">
        <p className="font-mono text-fg-subtle">bejelentkezés…</p>
      </main>
    )
  }

  return <>{children}</>
}
