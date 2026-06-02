import type { ReactNode } from 'react'
import { useMe } from '../api/hooks'

export function RequireAdmin({ children }: { children: ReactNode }) {
  const me = useMe()

  if (me.isLoading) {
    return (
      <main className="min-h-svh bg-surface flex items-center justify-center">
        <p className="font-mono text-fg-subtle">jogosultság ellenőrzése…</p>
      </main>
    )
  }

  if (me.error || !me.data?.isAdmin) {
    return (
      <main className="min-h-svh bg-surface flex items-center justify-center p-6">
        <div className="max-w-md rounded-2xl border border-danger/40 bg-elevated p-6 space-y-2">
          <p className="text-xs font-mono uppercase tracking-[0.2em] text-danger">403</p>
          <h1 className="text-2xl font-bold tracking-tight text-fg-default">Nincs jogosultság</h1>
          <p className="font-mono text-sm text-fg-muted">
            Ez az oldal csak admin felhasználók számára elérhető.
          </p>
        </div>
      </main>
    )
  }

  return <>{children}</>
}
