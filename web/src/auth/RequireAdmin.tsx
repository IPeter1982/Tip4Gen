import type { ReactNode } from 'react'
import { useMe } from '../api/hooks'

/// Gate for admin-only routes. Render *inside* a RequireAuth, since this hook
/// presumes an authenticated session — calling /api/me without a token 401s.
export function RequireAdmin({ children }: { children: ReactNode }) {
  const me = useMe()

  if (me.isLoading) {
    return (
      <main className="min-h-svh bg-stone-100 flex items-center justify-center">
        <p className="font-mono text-stone-500">jogosultság ellenőrzése…</p>
      </main>
    )
  }

  if (me.error || !me.data?.isAdmin) {
    return (
      <main className="min-h-svh bg-stone-100 flex items-center justify-center p-6">
        <div className="max-w-md border-2 border-stone-900 bg-white p-6 space-y-2">
          <p className="text-xs font-mono uppercase tracking-[0.2em] text-red-700">403</p>
          <h1 className="text-2xl font-black uppercase tracking-tight">Nincs jogosultság</h1>
          <p className="font-mono text-sm text-stone-700">
            Ez az oldal csak admin felhasználók számára elérhető.
          </p>
        </div>
      </main>
    )
  }

  return <>{children}</>
}
