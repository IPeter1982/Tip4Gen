import { Link, useSearchParams } from 'react-router'
import {
  AlertTriangle,
  BarChart3,
  Crosshair,
  Flame,
  Medal,
  Sparkles,
  Trophy,
} from 'lucide-react'
import { useIndividualLeaderboard, useTeamLeaderboard } from '../api/hooks'
import type { IndividualLeaderboardRow, TeamLeaderboardRow } from '../api/types'
import { Avatar } from '../components/Avatar'
import { TeamAvatar } from '../components/TeamAvatar'

type Tab = 'users' | 'teams'

const TABS: { value: Tab; label: string }[] = [
  { value: 'users', label: 'Egyéni' },
  { value: 'teams', label: 'Csapat' },
]

export function Leaderboard() {
  const [params, setParams] = useSearchParams()
  const tab = (params.get('tab') as Tab) || 'users'

  return (
    <div className="max-w-3xl mx-auto px-6 py-10 space-y-8">
      <header>
        <p className="inline-flex items-center gap-1.5 text-xs font-mono uppercase tracking-[0.2em] text-accent">
          <BarChart3 size={14} />
          Ranglista
        </p>
        <h1 className="text-4xl font-bold tracking-tight mt-2 text-fg-default">Állás</h1>
      </header>

      <div className="inline-flex p-1 rounded-full bg-sunken">
        {TABS.map((t) => (
          <button
            key={t.value}
            type="button"
            onClick={() => setParams({ tab: t.value })}
            className={`px-4 py-1.5 text-xs font-mono uppercase tracking-[0.15em] rounded-full transition ${
              tab === t.value
                ? 'bg-accent text-on-accent shadow'
                : 'text-fg-muted hover:text-fg-default'
            }`}
          >
            {t.label}
          </button>
        ))}
      </div>

      {tab === 'users' ? <UsersBoard /> : <TeamsBoard />}
    </div>
  )
}

const MEDAL_COLOR = ['text-amber-400', 'text-zinc-300', 'text-orange-700'] as const
const PODIUM_HEIGHT = ['h-40', 'h-32', 'h-28'] as const
const PODIUM_ORDER = [1, 0, 2] as const // 2nd, 1st, 3rd

function UsersBoard() {
  const { data, isLoading, error } = useIndividualLeaderboard()

  if (isLoading) return <p className="font-mono text-fg-subtle">betöltés…</p>
  if (error) return <ErrorBox e={error} />
  if (!data || data.length === 0) return <EmptyBox text="Még nincs pontozás." />

  const podium = data.slice(0, 3)
  const rest = data.slice(3)

  return (
    <div className="space-y-6">
      {podium.length > 0 && (
        <div className="grid grid-cols-3 gap-3 items-end">
          {PODIUM_ORDER.map((idx) => {
            const row = podium[idx]
            if (!row) return <div key={`empty-${idx}`} />
            return <PodiumTile key={row.userId} row={row} place={idx} />
          })}
        </div>
      )}

      {rest.length > 0 && (
        <ul className="space-y-2">
          {rest.map((row) => (
            <UserRowCard key={row.userId} row={row} />
          ))}
        </ul>
      )}
    </div>
  )
}

function PodiumTile({ row, place }: { row: IndividualLeaderboardRow; place: 0 | 1 | 2 }) {
  const isFirst = place === 0
  return (
    <Link
      to={`/leaderboard/user/${row.userId}`}
      className={`group flex flex-col items-center justify-end gap-2 rounded-2xl border bg-elevated p-3 transition hover:-translate-y-0.5 ${
        PODIUM_HEIGHT[place]
      } ${isFirst ? 'border-accent glow-accent' : row.isMe ? 'border-accent' : 'border-border-subtle'}`}
    >
      <Trophy size={isFirst ? 28 : 22} className={MEDAL_COLOR[place]} />
      <Avatar
        userId={row.userId}
        displayName={row.displayName}
        version={row.avatarVersion}
        size={isFirst ? 44 : 36}
      />
      <span className="text-xs font-mono truncate max-w-full text-fg-default">{row.displayName}</span>
      <span className={`tabular-nums font-bold ${isFirst ? 'text-xl text-accent' : 'text-base text-fg-default'}`}>
        {row.totalPoints}
      </span>
      {row.longestStreak >= 2 && (
        <span className="inline-flex items-center gap-0.5 text-[10px] font-mono text-fg-subtle">
          <Flame size={10} className="text-warning" />
          {row.longestStreak}
        </span>
      )}
    </Link>
  )
}

function UserRowCard({ row }: { row: IndividualLeaderboardRow }) {
  return (
    <li
      className={`flex items-center gap-3 rounded-xl border bg-elevated p-3 transition hover:border-accent/60 ${
        row.isMe ? 'border-accent glow-accent' : 'border-border-subtle'
      }`}
    >
      <span className="w-8 shrink-0 text-center text-sm font-mono font-bold tabular-nums text-fg-subtle">
        {row.rank}
      </span>
      <Link
        to={`/leaderboard/user/${row.userId}`}
        className="flex items-center gap-2 min-w-0 flex-1 hover:underline"
      >
        <Avatar
          userId={row.userId}
          displayName={row.displayName}
          version={row.avatarVersion}
          size={28}
        />
        <span className="truncate text-sm text-fg-default">{row.displayName}</span>
      </Link>
      {row.isMe && (
        <span className="text-[10px] font-mono uppercase tracking-[0.15em] text-accent">én</span>
      )}
      {row.winnerCorrect === true && (
        <span className="inline-flex items-center gap-1 text-[10px] font-mono uppercase tracking-[0.15em] bg-warning/15 text-warning px-1.5 py-0.5 rounded">
          <Trophy size={10} />
          győztes
        </span>
      )}
      {row.topScorerCorrect === true && (
        <span className="inline-flex items-center gap-1 text-[10px] font-mono uppercase tracking-[0.15em] bg-warning/15 text-warning px-1.5 py-0.5 rounded">
          <Crosshair size={10} />
          gólkirály
        </span>
      )}
      {row.longestStreak >= 2 && (
        <span className="hidden sm:inline-flex items-center gap-1 text-xs font-mono text-fg-subtle">
          <Flame size={12} className="text-warning" />
          {row.longestStreak}
        </span>
      )}
      <span className="ml-2 px-3 py-1 rounded-lg bg-sunken text-base font-mono font-bold tabular-nums text-fg-default shrink-0">
        {row.totalPoints}
      </span>
    </li>
  )
}

function TeamsBoard() {
  const { data, isLoading, error } = useTeamLeaderboard()

  if (isLoading) return <p className="font-mono text-fg-subtle">betöltés…</p>
  if (error) return <ErrorBox e={error} />
  if (!data || data.length === 0) {
    return <EmptyBox text="Még nincsenek lezárt csapatok." />
  }

  return (
    <div className="space-y-3">
      {data.map((row) => (
        <TeamRow key={row.teamId} row={row} />
      ))}
    </div>
  )
}

function TeamRow({ row }: { row: TeamLeaderboardRow }) {
  const isPodium = row.rank <= 3
  const medalColor = isPodium ? MEDAL_COLOR[row.rank - 1] : 'text-fg-subtle'
  return (
    <article
      className={`rounded-2xl border bg-elevated transition ${
        row.isMyTeam ? 'border-accent glow-accent' : 'border-border-subtle'
      }`}
    >
      <header className="flex items-center justify-between px-4 py-3 border-b border-border-subtle gap-3">
        <div className="flex items-center gap-3 min-w-0">
          <span className="inline-flex items-center gap-1 text-xl font-bold tabular-nums shrink-0 text-fg-default">
            {isPodium && <Medal size={18} className={medalColor} />}
            {row.rank}.
          </span>
          <TeamAvatar
            teamId={row.teamId}
            teamName={row.teamName}
            version={row.teamAvatarVersion}
            size={32}
          />
          <span className="text-base font-bold truncate text-fg-default">{row.teamName}</span>
          {row.isMyTeam && (
            <span className="text-[10px] font-mono uppercase tracking-[0.15em] bg-accent-soft text-accent-strong px-2 py-0.5 shrink-0 rounded">
              csapatom
            </span>
          )}
        </div>
        <span className="px-3 py-1 rounded-lg bg-sunken text-xl font-mono tabular-nums font-bold shrink-0 text-fg-default">
          {row.totalPoints}
        </span>
      </header>
      <ul className="px-4 py-3 text-xs font-mono text-fg-muted grid grid-cols-2 gap-x-4 gap-y-1.5">
        {row.members.map((m) => (
          <li key={m.memberId} className="flex items-center justify-between gap-2">
            <span className="flex items-center gap-2 truncate min-w-0">
              <Avatar
                userId={m.userId}
                displayName={m.displayName}
                version={m.avatarVersion}
                isAi={m.isAi}
                size={20}
              />
              <span className="truncate text-fg-default">{m.displayName}</span>
              {m.isAi && (
                <span className="inline-flex items-center gap-0.5 text-[10px] uppercase text-fg-subtle">
                  <Sparkles size={10} />
                  AI
                </span>
              )}
            </span>
            <span className="tabular-nums text-fg-subtle shrink-0">{m.points}</span>
          </li>
        ))}
      </ul>
    </article>
  )
}

function ErrorBox({ e }: { e: unknown }) {
  return (
    <p className="rounded-xl border border-danger/40 bg-danger/10 p-4 font-mono text-sm text-danger flex items-center gap-2">
      <AlertTriangle size={16} />
      {e instanceof Error ? e.message : String(e)}
    </p>
  )
}

function EmptyBox({ text }: { text: string }) {
  return (
    <p className="rounded-2xl border border-border-subtle bg-elevated p-8 text-center font-mono text-fg-subtle">
      {text}
    </p>
  )
}
