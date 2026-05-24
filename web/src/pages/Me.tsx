import { useEffect, useState } from 'react'
import { useApi } from '../auth/useApi'

type Me = {
  id: string
  displayName: string
  auth0Sub: string
  createdAt: string
  isAdmin: boolean
}

export function Me() {
  const { fetchJson } = useApi()
  const [me, setMe] = useState<Me | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    fetchJson<Me>('/api/me')
      .then(setMe)
      .catch((e: unknown) => setError(e instanceof Error ? e.message : String(e)))
  }, [fetchJson])

  return (
    <div className="max-w-xl mx-auto px-6 py-10 space-y-6">
      <header>
        <p className="text-xs font-mono uppercase tracking-[0.2em] text-orange-600">Profil</p>
        <h1 className="text-4xl font-black uppercase tracking-tight mt-2">
          {me?.displayName ?? '…'}
        </h1>
      </header>

      <section className="border-2 border-stone-900 bg-white p-5">
        <p className="text-xs font-mono uppercase tracking-[0.15em] text-stone-500 mb-3">
          /api/me
        </p>
        {error && <p className="font-mono text-red-700">⚠ {error}</p>}
        {!error && !me && <p className="font-mono text-stone-500">betöltés…</p>}
        {me && (
          <pre className="font-mono text-sm whitespace-pre-wrap text-stone-800">
            {JSON.stringify(me, null, 2)}
          </pre>
        )}
      </section>
    </div>
  )
}
