import { useMemo } from 'react'
import { Link, useParams } from 'react-router'
import { useUserTipHistory } from '../api/hooks'
import type { UserTipHistoryItem, UserTipHistoryResponse } from '../api/types'
import { Avatar } from '../components/Avatar'
import { TeamLabel } from '../components/TeamLabel'
import {
  CATEGORY_LABEL_HU,
  STAGE_LABEL_HU,
  STATUS_LABEL_HU,
  formatBudapestDate,
  formatBudapestTime,
} from '../lib/format'

function stageLabel(item: UserTipHistoryItem): string {
  const base = STAGE_LABEL_HU[item.stage] ?? item.stage
  if (item.stage === 'Group' && item.groupCode) return `${base} ${item.groupCode}`
  if (item.stage === 'Group' && item.roundLabel) return `${base} · ${item.roundLabel}`
  return base
}

function statusChip(status: UserTipHistoryItem['status']): { label: string; cls: string } {
  if (status === 'Finished' || status === 'Awarded') {
    return { label: 'lejátszott', cls: 'bg-sunken text-fg-default' }
  }
  if (status === 'Cancelled' || status === 'Abandoned') {
    return { label: STATUS_LABEL_HU[status], cls: 'bg-danger/15 text-danger' }
  }
  return { label: STATUS_LABEL_HU[status] ?? status, cls: 'bg-sunken text-fg-default' }
}

export function UserTips() {
  const { userId } = useParams<{ userId: string }>()
  const { data, isLoading, error } = useUserTipHistory(userId)

  const grouped = useMemo(() => {
    const out = new Map<string, UserTipHistoryItem[]>()
    for (const it of data?.items ?? []) {
      const key = formatBudapestDate(it.kickoffUtc)
      const list = out.get(key) ?? []
      list.push(it)
      out.set(key, list)
    }
    return [...out.entries()]
  }, [data])

  return (
    <div className="max-w-3xl mx-auto px-6 py-10 space-y-6">
      <div>
        <Link
          to="/leaderboard?tab=users"
          className="text-xs font-mono uppercase tracking-[0.2em] text-fg-subtle hover:text-accent"
        >
          ← Ranglista
        </Link>
      </div>

      {error && (
        <p className="border border-danger bg-danger/10 p-4 font-mono text-sm text-danger">
          ⚠ {error instanceof Error ? error.message : String(error)}
        </p>
      )}

      {isLoading && <p className="font-mono text-fg-subtle">betöltés…</p>}

      {data && <UserHeader data={data} />}

      {data && grouped.length === 0 && (
        <p className="border border-border-subtle bg-elevated p-6 text-center font-mono text-fg-subtle">
          Még nincs lezárt tipp.
        </p>
      )}

      <div className="space-y-6">
        {grouped.map(([date, items]) => (
          <section key={date} className="space-y-2">
            <h2 className="text-xs font-mono uppercase tracking-[0.2em] text-fg-subtle">{date}</h2>
            <ul className="space-y-2">
              {items.map((it) => (
                <MatchTipRow key={it.matchId} item={it} />
              ))}
            </ul>
          </section>
        ))}
      </div>
    </div>
  )
}

function UserHeader({ data }: { data: UserTipHistoryResponse }) {
  return (
    <header className="flex items-center justify-between gap-4 flex-wrap">
      <div className="flex items-center gap-4 min-w-0">
        <Avatar
          userId={data.userId}
          displayName={data.displayName}
          version={data.avatarVersion}
          size={56}
        />
        <div className="min-w-0">
          <p className="text-xs font-mono uppercase tracking-[0.2em] text-accent">Ranglista · Tippek</p>
          <h1 className="text-3xl sm:text-4xl font-black uppercase tracking-tight mt-1 truncate">
            {data.displayName}
          </h1>
        </div>
      </div>
      <div className="text-right">
        <p className="text-xs font-mono uppercase tracking-[0.2em] text-fg-subtle">Összpont</p>
        <p className="text-4xl font-black font-mono tabular-nums">{data.totalPoints}</p>
      </div>
    </header>
  )
}

function MatchTipRow({ item }: { item: UserTipHistoryItem }) {
  const chip = statusChip(item.status)
  const kickoff = formatBudapestTime(item.kickoffUtc)
  const hasResult = item.homeGoals !== null && item.awayGoals !== null
  const { tip } = item

  return (
    <li className="border border-border-strong bg-elevated">
      <Link to={`/matches/${item.matchId}/tip`} className="block p-4 hover:bg-sunken">
        <div className="flex items-center gap-3 text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle">
          <span>{stageLabel(item)}</span>
          <span>·</span>
          <span>{kickoff}</span>
          <span className={`ml-auto px-2 py-0.5 ${chip.cls}`}>{chip.label}</span>
        </div>

        <div className="mt-3 flex items-center justify-between gap-4">
          <div className="flex-1 min-w-0">
            <div className="text-base font-bold"><TeamLabel team={item.homeTeam} /></div>
            <div className="text-base font-bold"><TeamLabel team={item.awayTeam} /></div>
          </div>
          <div className="text-2xl font-mono tabular-nums text-fg-default shrink-0">
            {hasResult ? `${item.homeGoals} : ${item.awayGoals}` : '— : —'}
          </div>
        </div>

        <div className="mt-3 flex items-center justify-between gap-3 text-xs font-mono text-fg-subtle">
          <span>
            tipp:{' '}
            <span className="text-fg-default">
              {tip.homeGoals}–{tip.awayGoals}
            </span>
            {tip.joker && (
              <span className="ml-2 bg-accent-soft text-accent-strong px-1.5 py-0.5">JOKER</span>
            )}
          </span>
          <TipPoints item={item} />
        </div>
      </Link>
    </li>
  )
}

function TipPoints({ item }: { item: UserTipHistoryItem }) {
  const score = item.tip.score
  if (score === null) {
    return <span className="text-fg-subtle">—</span>
  }
  if (score.finalPoints === 0) {
    return (
      <span className="text-right">
        <span className="text-fg-subtle font-bold">0 pt</span>
        <span className="block text-[10px] text-fg-subtle">
          {CATEGORY_LABEL_HU[score.category] ?? score.category}
        </span>
      </span>
    )
  }
  return (
    <span className="text-right">
      <span className="text-success font-bold tabular-nums">+{score.finalPoints} pt</span>
      <span className="block text-[10px] text-fg-subtle">
        {CATEGORY_LABEL_HU[score.category] ?? score.category} · ×{score.multiplier}
        {score.jokerApplied && ' · ×2 (joker)'}
      </span>
    </span>
  )
}
