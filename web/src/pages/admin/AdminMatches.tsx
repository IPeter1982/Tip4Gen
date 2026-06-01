import { useState } from 'react'
import { Link } from 'react-router'
import { useMatches } from '../../api/hooks'
import type { MatchListItem, MatchStatus } from '../../api/types'
import { STAGE_LABEL_HU, STATUS_LABEL_HU, formatBudapest } from '../../lib/format'

type Phase = 'upcoming' | 'past' | 'all'

const PHASE_LABEL: Record<Phase, string> = {
  upcoming: 'Mostani',
  past: 'Lejátszott',
  all: 'Mindegyik',
}

const STATUS_CLASS: Record<MatchStatus, string> = {
  Scheduled: 'text-stone-700',
  Live: 'text-orange-600',
  Finished: 'text-stone-900',
  Postponed: 'text-amber-700',
  Cancelled: 'text-red-700',
  Abandoned: 'text-red-700',
  Awarded: 'text-stone-900',
}

export function AdminMatches() {
  const [phase, setPhase] = useState<Phase>('upcoming')
  const matches = useMatches(phase)

  return (
    <div className="max-w-5xl mx-auto px-6 py-10 space-y-6">
      <header>
        <p className="text-xs font-mono uppercase tracking-[0.2em] text-orange-600">Admin</p>
        <h1 className="text-4xl font-black uppercase tracking-tight mt-2">Mérkőzések</h1>
      </header>

      <nav className="flex items-center gap-2 flex-wrap">
        {(['upcoming', 'past', 'all'] as Phase[]).map((p) => (
          <button
            key={p}
            type="button"
            onClick={() => setPhase(p)}
            className={`border-2 px-3 py-1 text-xs font-mono uppercase tracking-[0.15em] ${
              phase === p
                ? 'border-stone-900 bg-stone-900 text-white'
                : 'border-stone-900 bg-white hover:bg-stone-100'
            }`}
          >
            {PHASE_LABEL[p]}
          </button>
        ))}
        <Link
          to="/admin/audit"
          className="ml-auto border-2 border-stone-900 bg-white px-3 py-1 text-xs font-mono uppercase tracking-[0.15em] hover:bg-stone-100"
        >
          Audit log
        </Link>
        <Link
          to="/admin/long-tips"
          className="border-2 border-stone-900 bg-white px-3 py-1 text-xs font-mono uppercase tracking-[0.15em] hover:bg-stone-100"
        >
          Hosszú tippek
        </Link>
        <Link
          to="/admin/ai-avatar"
          className="border-2 border-stone-900 bg-white px-3 py-1 text-xs font-mono uppercase tracking-[0.15em] hover:bg-stone-100"
        >
          AI profilkép
        </Link>
      </nav>

      {matches.isLoading && <p className="font-mono text-stone-500">betöltés…</p>}
      {matches.error && (
        <p className="border-2 border-red-700 bg-red-50 p-4 font-mono text-sm text-red-800">
          ⚠ {matches.error instanceof Error ? matches.error.message : String(matches.error)}
        </p>
      )}

      {matches.data && (
        <section className="border-2 border-stone-900 bg-white overflow-x-auto">
          <table className="w-full min-w-[640px] text-sm font-mono">
            <thead className="bg-stone-100 text-left text-xs uppercase tracking-[0.15em] text-stone-600">
              <tr>
                <th className="px-3 py-2">Mikor</th>
                <th className="px-3 py-2">Szakasz</th>
                <th className="px-3 py-2">Mérkőzés</th>
                <th className="px-3 py-2">Eredmény</th>
                <th className="px-3 py-2">Állapot</th>
                <th className="px-3 py-2"></th>
              </tr>
            </thead>
            <tbody className="divide-y-2 divide-stone-200">
              {matches.data.map((m: MatchListItem) => (
                <tr key={m.id}>
                  <td className="px-3 py-2 text-stone-700">{formatBudapest(m.kickoffUtc)}</td>
                  <td className="px-3 py-2 text-stone-500 uppercase tracking-[0.1em] text-xs">
                    {STAGE_LABEL_HU[m.stage] ?? m.stage}
                    {m.groupCode ? ` ${m.groupCode}` : ''}
                  </td>
                  <td className="px-3 py-2">
                    {m.homeTeam.name} <span className="text-stone-400">vs</span> {m.awayTeam.name}
                  </td>
                  <td className="px-3 py-2 tabular-nums">
                    {m.homeGoals != null && m.awayGoals != null
                      ? `${m.homeGoals}–${m.awayGoals}`
                      : <span className="text-stone-400">—</span>}
                  </td>
                  <td className={`px-3 py-2 text-xs uppercase tracking-[0.15em] ${STATUS_CLASS[m.status]}`}>
                    {STATUS_LABEL_HU[m.status] ?? m.status}
                  </td>
                  <td className="px-3 py-2 text-right">
                    <Link
                      to={`/admin/matches/${m.id}`}
                      className="border-2 border-stone-900 px-3 py-1 text-xs uppercase tracking-[0.15em] hover:bg-stone-900 hover:text-white"
                    >
                      Szerkeszt
                    </Link>
                  </td>
                </tr>
              ))}
              {matches.data.length === 0 && (
                <tr>
                  <td colSpan={6} className="px-3 py-6 text-center text-stone-400">
                    nincs ilyen mérkőzés
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </section>
      )}
    </div>
  )
}
