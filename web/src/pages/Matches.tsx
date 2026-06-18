import { useEffect, useMemo, useState, type ComponentType } from 'react'
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router'
import { Switch } from '@headlessui/react'
import {
  AlertTriangle,
  CheckCircle2,
  Clock,
  Goal,
  Radio,
  RefreshCw,
  Star,
  Target,
  Timer,
  Trophy,
  type LucideProps,
} from 'lucide-react'
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

const HIDE_TIPPED_STORAGE_KEY = 'tip4gen.matches.hideTipped'

const PHASES: { value: Phase; label: string }[] = [
  { value: 'upcoming', label: 'Soron következő' },
  { value: 'past', label: 'Lejátszott' },
  { value: 'all', label: 'Összes' },
]

type StateChip = {
  label: string
  cls: string
  Icon: ComponentType<LucideProps>
  pulse?: boolean
}

function tipStateChip(match: MatchListItem, now: Date): StateChip {
  if (match.status === 'Finished' || match.status === 'Awarded') {
    return { label: 'lejátszott', cls: 'bg-sunken text-fg-muted', Icon: CheckCircle2 }
  }
  if (match.status === 'Cancelled' || match.status === 'Abandoned') {
    return { label: STATUS_LABEL_HU[match.status], cls: 'bg-danger/15 text-danger', Icon: AlertTriangle }
  }
  if (match.status === 'Live') {
    return { label: 'él', cls: 'bg-live-soft text-live', Icon: Radio, pulse: true }
  }
  if (match.status === 'Postponed') {
    return { label: 'halasztott', cls: 'bg-warning/15 text-warning', Icon: AlertTriangle }
  }
  const deadlinePassed = new Date(match.deadlineUtc).getTime() <= now.getTime()
  return deadlinePassed
    ? { label: 'lezárva', cls: 'bg-sunken text-fg-muted', Icon: Clock }
    : { label: 'nyitva', cls: 'bg-accent-soft text-accent-strong', Icon: Clock }
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
  const location = useLocation()
  const navigate = useNavigate()
  const tipSearch = location.search

  const [now, setNow] = useState(() => new Date())
  useEffect(() => {
    const id = setInterval(() => setNow(new Date()), 1000)
    return () => clearInterval(id)
  }, [])

  const [hideTipped, setHideTipped] = useState<boolean>(() => {
    if (typeof window === 'undefined') return false
    return localStorage.getItem(HIDE_TIPPED_STORAGE_KEY) === '1'
  })
  useEffect(() => {
    localStorage.setItem(HIDE_TIPPED_STORAGE_KEY, hideTipped ? '1' : '0')
  }, [hideTipped])

  const visible = useMemo(() => {
    if (!data) return []
    if (phase !== 'upcoming' || !hideTipped) return data
    return data.filter((m) => m.myTip === null)
  }, [data, phase, hideTipped])

  const grouped = useMemo(() => {
    const out = new Map<string, MatchListItem[]>()
    for (const m of visible) {
      const key = formatBudapestDate(m.kickoffUtc)
      const list = out.get(key) ?? []
      list.push(m)
      out.set(key, list)
    }
    return [...out.entries()]
  }, [visible])

  const [flashId, setFlashId] = useState<string | null>(null)
  useEffect(() => {
    const focusId = (location.state as { focusMatchId?: string } | null)?.focusMatchId
    if (!focusId || !data) return
    if (!data.some((m) => m.id === focusId)) return

    const raf = requestAnimationFrame(() => {
      document.getElementById(`match-${focusId}`)?.scrollIntoView({ block: 'center', behavior: 'auto' })
    })
    setFlashId(focusId)
    const timeout = window.setTimeout(() => setFlashId(null), 1500)
    navigate(location.pathname + location.search, { replace: true, state: null })

    return () => {
      cancelAnimationFrame(raf)
      window.clearTimeout(timeout)
    }
  }, [data, location.state, location.pathname, location.search, navigate])

  return (
    <div className="max-w-3xl mx-auto px-6 py-10 space-y-8">
      <header className="flex items-end justify-between gap-4 flex-wrap">
        <div>
          <p className="inline-flex items-center gap-1.5 text-xs font-mono uppercase tracking-[0.2em] text-accent">
            <Goal size={14} />
            Mérkőzések
          </p>
          <h1 className="text-4xl font-bold tracking-tight mt-2 text-fg-default">Tippelés</h1>
        </div>
        <button
          type="button"
          onClick={() => refetch()}
          className="inline-flex items-center gap-1.5 text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle hover:text-accent transition"
        >
          <RefreshCw size={12} className={isFetching ? 'animate-spin' : ''} />
          {isFetching ? 'frissítés…' : 'frissítés'}
        </button>
      </header>

      <div className="inline-flex p-1 rounded-full bg-sunken">
        {PHASES.map((p) => (
          <button
            key={p.value}
            type="button"
            onClick={() => setParams({ phase: p.value })}
            className={`px-4 py-1.5 text-xs font-mono uppercase tracking-[0.15em] rounded-full transition ${
              phase === p.value
                ? 'bg-accent text-on-accent shadow'
                : 'text-fg-muted hover:text-fg-default'
            }`}
          >
            {p.label}
          </button>
        ))}
      </div>

      {phase === 'upcoming' && (
        <div className="flex items-center gap-2.5">
          <Switch
            checked={hideTipped}
            onChange={setHideTipped}
            className={`relative inline-flex h-5 w-9 items-center rounded-full transition ${
              hideTipped ? 'bg-accent' : 'bg-sunken'
            }`}
          >
            <span
              className={`inline-block h-3.5 w-3.5 transform rounded-full bg-elevated transition ${
                hideTipped ? 'translate-x-[18px]' : 'translate-x-[2px]'
              }`}
            />
          </Switch>
          <span className="text-xs font-mono uppercase tracking-[0.15em] text-fg-muted">
            Tippelt mérkőzések elrejtése
          </span>
        </div>
      )}

      {error && (
        <p className="rounded-xl border border-danger/40 bg-danger/10 p-4 font-mono text-sm text-danger flex items-center gap-2">
          <AlertTriangle size={16} />
          {error instanceof Error ? error.message : String(error)}
        </p>
      )}

      {isLoading && <p className="font-mono text-fg-subtle">betöltés…</p>}

      {!isLoading && grouped.length === 0 && (
        <p className="rounded-2xl border border-border-subtle bg-elevated p-8 text-center font-mono text-fg-subtle">
          {phase === 'upcoming' && hideTipped
            ? 'Minden közelgő mérkőzésre tippeltél már.'
            : 'Nincs megjeleníthető mérkőzés ebben a nézetben.'}
        </p>
      )}

      <div className="space-y-8">
        {grouped.map(([date, matches]) => (
          <section key={date} className="space-y-3">
            <h2 className="text-xs font-mono uppercase tracking-[0.2em] text-fg-subtle pl-1">{date}</h2>
            <ul className="space-y-3">
              {matches.map((m) => (
                <MatchRow
                  key={m.id}
                  match={m}
                  now={now}
                  flashFocus={flashId === m.id}
                  tipSearch={tipSearch}
                />
              ))}
            </ul>
          </section>
        ))}
      </div>
    </div>
  )
}

function MatchRow({
  match,
  now,
  flashFocus,
  tipSearch,
}: {
  match: MatchListItem
  now: Date
  flashFocus: boolean
  tipSearch: string
}) {
  const chip = tipStateChip(match, now)
  const deadlinePassed = new Date(match.deadlineUtc).getTime() <= now.getTime()
  const kickoff = formatBudapestTime(match.kickoffUtc)
  const finished = match.status === 'Finished' || match.status === 'Awarded'
  const canTip = !deadlinePassed && match.status === 'Scheduled'
  const isLive = match.status === 'Live'
  const highlight = isLive || flashFocus

  return (
    <li
      id={`match-${match.id}`}
      className={`rounded-2xl border bg-elevated transition hover:-translate-y-0.5 hover:border-accent/60 ${
        highlight ? 'border-accent glow-accent' : 'border-border-subtle'
      }`}
    >
      <Link to={`/matches/${match.id}/tip${tipSearch}`} className="block p-4 sm:p-5">
        <div className="flex items-center gap-3 text-[11px] font-mono uppercase tracking-[0.15em] text-fg-subtle">
          <span className="inline-flex items-center gap-1">
            <Trophy size={12} />
            {stageLabel(match)}
          </span>
          <span>·</span>
          <span>{kickoff}</span>
          <span className={`ml-auto inline-flex items-center gap-1 px-2 py-0.5 rounded-full ${chip.cls}`}>
            <chip.Icon size={12} className={chip.pulse ? 'live-dot' : ''} />
            {chip.label}
          </span>
        </div>

        <div className="mt-4 grid grid-cols-[1fr_auto_1fr] items-center gap-3 sm:gap-4">
          <div className="text-right text-sm sm:text-base font-bold text-fg-default truncate">
            <TeamLabel team={match.homeTeam} />
          </div>
          <div className="px-3 sm:px-4 py-2 rounded-xl bg-sunken text-2xl sm:text-3xl font-mono font-bold tabular-nums text-fg-default min-w-[88px] text-center">
            {finished
              ? `${match.homeGoals ?? '-'} : ${match.awayGoals ?? '-'}`
              : '— : —'}
          </div>
          <div className="text-left text-sm sm:text-base font-bold text-fg-default truncate">
            <TeamLabel team={match.awayTeam} />
          </div>
        </div>

        <div className="mt-4 flex items-center justify-between gap-3 text-xs font-mono text-fg-subtle">
          {match.myTip ? (
            <span className="inline-flex items-center gap-1.5">
              <Target size={14} className="text-accent" />
              <span className="text-fg-default">
                {match.myTip.homeGoals}–{match.myTip.awayGoals}
              </span>
              {match.myTip.joker && (
                <span className="inline-flex items-center gap-1 ml-1 px-1.5 py-0.5 rounded bg-accent-soft text-accent-strong">
                  <Star size={10} className="fill-current" />
                  Joker
                </span>
              )}
            </span>
          ) : (
            <span className="italic">még nincs tipp</span>
          )}
          {canTip && (
            <span className="inline-flex items-center gap-1.5">
              <Timer size={14} />
              <span className="text-fg-default tabular-nums">{formatCountdown(match.deadlineUtc, now)}</span>
            </span>
          )}
        </div>
      </Link>
    </li>
  )
}
