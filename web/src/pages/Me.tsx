import { useMe } from '../api/hooks'

export function Me() {
  const { data: me, error, isLoading } = useMe()

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
        {error && (
          <p className="font-mono text-red-700">
            ⚠ {error instanceof Error ? error.message : String(error)}
          </p>
        )}
        {!error && isLoading && <p className="font-mono text-stone-500">betöltés…</p>}
        {me && (
          <pre className="font-mono text-sm whitespace-pre-wrap text-stone-800">
            {JSON.stringify(me, null, 2)}
          </pre>
        )}
      </section>
    </div>
  )
}
