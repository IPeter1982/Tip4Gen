import { useEffect, useState } from 'react'

type Health = {
  status: string
  timestamp: string
  environment: string
  version: string
}

export default function App() {
  const [health, setHealth] = useState<Health | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    fetch('/api/health')
      .then(async (r) => {
        if (!r.ok) throw new Error(`${r.status} ${r.statusText}`)
        setHealth(await r.json())
      })
      .catch((e: unknown) =>
        setError(e instanceof Error ? e.message : String(e)),
      )
  }, [])

  return (
    <main className="min-h-svh bg-stone-100 text-stone-900 flex items-center justify-center p-6">
      <div className="max-w-xl w-full space-y-6">
        <header>
          <p className="text-xs font-mono uppercase tracking-[0.2em] text-orange-600">
            Foci VB · Tippjáték
          </p>
          <h1 className="text-6xl font-black uppercase tracking-tight mt-2">
            Tip4Gen
          </h1>
        </header>

        <section className="border-2 border-stone-900 bg-white p-5">
          <p className="text-xs font-mono uppercase tracking-[0.15em] text-stone-500 mb-3">
            Backend health
          </p>
          {error && <p className="font-mono text-red-700">⚠ {error}</p>}
          {!error && !health && (
            <p className="font-mono text-stone-500">checking…</p>
          )}
          {health && (
            <pre className="font-mono text-sm whitespace-pre-wrap text-stone-800">
              {JSON.stringify(health, null, 2)}
            </pre>
          )}
        </section>

        <footer className="text-xs font-mono uppercase tracking-[0.2em] text-stone-500">
          v0.0.1 · Phase 0
        </footer>
      </div>
    </main>
  )
}
