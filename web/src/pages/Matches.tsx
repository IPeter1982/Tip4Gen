import { useEffect, useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { useMatches } from '../api/hooks'
import type { MatchListItem } from '../api/types'
import { TeamLabel } from '../components/TeamLabel'
import {
  STAGE_LABEL_HU,
  STATUS_LABEL_HU,
  formatBudapestDate,
  formatBudapestTime,
  formatCountdown,
} from '../lib/format'

type Phase = 'upcoming' | 'past' | 'all'

const PHASES: { value: Phase; label: string }[] = [
  { value: 'upcoming', label: 'Soron következő' },
  { value: 'past', label: 'Lejátszott' },
  { value: 'all', label: 'Összes' },
]

function tipStateChip(match: MatchListItem, now: Date): { label: string; cls: string } {
  if (match.status === 'Finished' || match.status === 'Awarded') {
    return { label: 'lejátszott', cls: 'bg-stone-200 text-stone-700' }
  }
  if (match.status === 'Cancelled' || match.status === 'Abandoned') {
    return { label: STATUS_LABEL_HU[match.status], cls: 'bg-red-100 text-red-800' }
  }
  if (match.status === 'Live') {
    return { label: 'él', cls: 'bg-orange-100 text-orange-800' }
  }
  if (match.status === 'Postponed') {
    return { label: 'halasztott', cls: 'bg-yellow-100 text-yellow-800' }
  }
  const deadlinePassed = new Date(match.deadlineUtc).getTime() <= now.getTime()
  return deadlinePassed
    ? { label: 'lezárva', cls: 'bg-stone-200 text-stone-700' }
    : { label: 'nyitva', cls: 'bg-green-100 text-green-800' }
}

function stageLabel(match: MatchListItem): string {
  const base = STAGE_LABEL_HU[match.stage] ?? match.stage
  if (match.stage === 'Group' && match.groupCode) return `${base} ${match.groupCode}`
  if (match.stage === 'Group' && match.roundLabel) return `${base} · ${match.roundLabel}`
  return base
}

export function Matches() {
  const [params, setParams] = useSearchParams()
  const phase = (params.get('phase') as Phase) || 'upcoming'
  const { data, isLoading, error, refetch, isFetching } = useMatches(phase)

  // Drive countdowns once per second.
  const [now, setNow] = useState(() => new Date())
  useEffect(() => {
    const id = setInterval(() => setNow(new Date()), 1000)
    return () => clearInterval(id)
  }, [])

  const grouped = useMemo(() => {
    const out = new Map<string, MatchListItem[]>()
    for (const m of data ?? []) {
      const key = formatBudapestDate(m.kickoffUtc)
      const list = out.get(key) ?? []
      list.push(m)
      out.set(key, list)
    }
    return [...out.entries()]
  }, [data])

  return (
    <div className="max-w-3xl mx-auto px-6 py-10 space-y-6">
      <header className="flex items-end justify-between gap-4 flex-wrap">
        <div>
          <p className="text-xs font-mono uppercase tracking-[0.2em] text-orange-600">Mérkőzések</p>
          <h1 className="text-4xl font-black uppercase tracking-tight mt-2">Tippelés</h1>
        </div>
        <button
          type="button"
          onClick={() => refetch()}
          className="text-xs font-mono uppercase tracking-[0.15em] text-stone-500 hover:text-stone-900"
        >
          {isFetching ? 'frissítés…' : 'frissítés ↻'}
        </button>
      </header>

      <div className="flex gap-2 flex-wrap">
        {PHASES.map((p) => (
          <button
            key={p.value}
            type="button"
            onClick={() => setParams({ phase: p.value })}
            className={`border-2 px-3 py-1 text-xs font-mono uppercase tracking-[0.15em] ${
              phase === p.value
                ? 'border-stone-900 bg-stone-900 text-white'
                : 'border-stone-300 text-stone-600 hover:border-stone-900'
            }`}
          >
            {p.label}
          </button>
        ))}
      </div>

      {error && (
        <p className="border-2 border-red-700 bg-red-50 p-4 font-mono text-sm text-red-800">
          ⚠ {error instanceof Error ? error.message : String(error)}
        </p>
      )}

      {isLoading && <p className="font-mono text-stone-500">betöltés…</p>}

      {!isLoading && grouped.length === 0 && (
        <p className="border-2 border-stone-300 bg-white p-6 text-center font-mono text-stone-500">
          Nincs megjeleníthető mérkőzés ebben a nézetben.
        </p>
      )}

      <div className="space-y-6">
        {grouped.map(([date, matches]) => (
          <section key={date} className="space-y-2">
            <h2 className="text-xs font-mono uppercase tracking-[0.2em] text-stone-500">{date}</h2>
            <ul className="space-y-2">
              {matches.map((m) => (
                <MatchRow key={m.id} match={m} now={now} />
              ))}
            </ul>
          </section>
        ))}
      </div>
    </div>
  )
}

function MatchRow({ match, now }: { match: MatchListItem; now: Date }) {
  const chip = tipStateChip(match, now)
  const deadlinePassed = new Date(match.deadlineUtc).getTime() <= now.getTime()
  const kickoff = formatBudapestTime(match.kickoffUtc)
  const finished = match.status === 'Finished' || match.status === 'Awarded'
  const canTip = !deadlinePassed && match.status === 'Scheduled'

  return (
    <li className="border-2 border-stone-900 bg-white">
      <Link
        to={`/matches/${match.id}/tip`}
        className="block p-4 hover:bg-stone-50"
      >
        <div className="flex items-center gap-3 text-xs font-mono uppercase tracking-[0.15em] text-stone-500">
          <span>{stageLabel(match)}</span>
          <span>·</span>
          <span>{kickoff}</span>
          <span className={`ml-auto px-2 py-0.5 ${chip.cls}`}>{chip.label}</span>
        </div>

        <div className="mt-3 flex items-center justify-between gap-4">
          <div className="flex-1">
            <div className="text-base font-bold"><TeamLabel team={match.homeTeam} /></div>
            <div className="text-base font-bold"><TeamLabel team={match.awayTeam} /></div>
          </div>
          <div className="text-2xl font-mono tabular-nums text-stone-900">
            {finished
              ? `${match.homeGoals ?? '-'} : ${match.awayGoals ?? '-'}`
              : '— : —'}
          </div>
        </div>

        <div className="mt-3 flex items-center justify-between text-xs font-mono text-stone-500">
          {match.myTip ? (
            <span>
              tippem:{' '}
              <span className="text-stone-900">
                {match.myTip.homeGoals}–{match.myTip.awayGoals}
              </span>
              {match.myTip.joker && (
                <span className="ml-2 bg-orange-100 text-orange-800 px-1.5 py-0.5">JOKER</span>
              )}
            </span>
          ) : (
            <span className="italic">még nincs tipp</span>
          )}
          {canTip && (
            <span>
              határidő: <span className="text-stone-900">{formatCountdown(match.deadlineUtc, now)}</span>
            </span>
          )}
        </div>
      </Link>
    </li>
  )
}
