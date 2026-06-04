import { useState } from 'react'
import { Link } from 'react-router'
import { ShieldCheck } from 'lucide-react'
import { useMatches } from '../../api/hooks'
import type { MatchListItem, MatchStatus } from '../../api/types'
import { STAGE_LABEL_HU, STATUS_LABEL_HU, formatBudapest } from '../../lib/format'
import { TeamLabel } from '../../components/TeamLabel'

type Phase = 'upcoming' | 'past' | 'all'

const PHASE_LABEL: Record<Phase, string> = {
  upcoming: 'Mostani',
  past: 'Lejátszott',
  all: 'Mindegyik',
}

const STATUS_CLASS: Record<MatchStatus, string> = {
  Scheduled: 'text-fg-default',
  Live: 'text-accent',
  Finished: 'text-fg-default',
  Postponed: 'text-warning',
  Cancelled: 'text-danger',
  Abandoned: 'text-danger',
  Awarded: 'text-fg-default',
}

export function AdminMatches() {
  const [phase, setPhase] = useState<Phase>('upcoming')
  const matches = useMatches(phase)

  return (
    <div className="max-w-5xl mx-auto px-6 py-10 space-y-6">
      <header>
        <p className="inline-flex items-center gap-1.5 text-xs font-mono uppercase tracking-[0.2em] text-accent">
          <ShieldCheck size={14} />
          Admin
        </p>
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
                ? 'border-accent bg-accent text-on-accent'
                : 'border-border-strong bg-elevated hover:bg-sunken'
            }`}
          >
            {PHASE_LABEL[p]}
          </button>
        ))}
        <Link
          to="/admin/audit"
          className="ml-auto border-2 border-border-strong bg-elevated px-3 py-1 text-xs font-mono uppercase tracking-[0.15em] hover:bg-sunken"
        >
          Audit log
        </Link>
        <Link
          to="/admin/long-tips"
          className="border-2 border-border-strong bg-elevated px-3 py-1 text-xs font-mono uppercase tracking-[0.15em] hover:bg-sunken"
        >
          Végső győztes
        </Link>
        <Link
          to="/admin/ai-avatar"
          className="border-2 border-border-strong bg-elevated px-3 py-1 text-xs font-mono uppercase tracking-[0.15em] hover:bg-sunken"
        >
          AI profilkép
        </Link>
      </nav>

      {matches.isLoading && <p className="font-mono text-fg-subtle">betöltés…</p>}
      {matches.error && (
        <p className="border-2 border-danger bg-danger/10 p-4 font-mono text-sm text-danger">
          ⚠ {matches.error instanceof Error ? matches.error.message : String(matches.error)}
        </p>
      )}

      {matches.data && (
        <section className="border-2 border-border-strong bg-elevated overflow-x-auto">
          <table className="w-full min-w-[640px] text-sm font-mono">
            <thead className="bg-sunken text-left text-xs uppercase tracking-[0.15em] text-fg-muted">
              <tr>
                <th className="px-3 py-2">Mikor</th>
                <th className="px-3 py-2">Szakasz</th>
                <th className="px-3 py-2">Mérkőzés</th>
                <th className="px-3 py-2">Eredmény</th>
                <th className="px-3 py-2">Állapot</th>
                <th className="px-3 py-2"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border-subtle">
              {matches.data.map((m: MatchListItem) => (
                <tr key={m.id}>
                  <td className="px-3 py-2 text-fg-default">{formatBudapest(m.kickoffUtc)}</td>
                  <td className="px-3 py-2 text-fg-subtle uppercase tracking-[0.1em] text-xs">
                    {STAGE_LABEL_HU[m.stage] ?? m.stage}
                    {m.groupCode ? ` ${m.groupCode}` : ''}
                  </td>
                  <td className="px-3 py-2">
                    <TeamLabel team={m.homeTeam} /> <span className="text-fg-subtle">vs</span> <TeamLabel team={m.awayTeam} />
                  </td>
                  <td className="px-3 py-2 tabular-nums">
                    {m.homeGoals != null && m.awayGoals != null
                      ? `${m.homeGoals}–${m.awayGoals}`
                      : <span className="text-fg-subtle">—</span>}
                  </td>
                  <td className={`px-3 py-2 text-xs uppercase tracking-[0.15em] ${STATUS_CLASS[m.status]}`}>
                    {STATUS_LABEL_HU[m.status] ?? m.status}
                  </td>
                  <td className="px-3 py-2 text-right">
                    <Link
                      to={`/admin/matches/${m.id}`}
                      className="border-2 border-border-strong px-3 py-1 text-xs uppercase tracking-[0.15em] hover:bg-accent hover:text-on-accent hover:border-accent"
                    >
                      Szerkeszt
                    </Link>
                  </td>
                </tr>
              ))}
              {matches.data.length === 0 && (
                <tr>
                  <td colSpan={6} className="px-3 py-6 text-center text-fg-subtle">
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
