import { useSearchParams } from 'react-router'
import { useIndividualLeaderboard, useTeamLeaderboard } from '../api/hooks'
import type { IndividualLeaderboardRow, TeamLeaderboardRow } from '../api/types'
import { Avatar } from '../components/Avatar'

type Tab = 'users' | 'teams'

const TABS: { value: Tab; label: string }[] = [
  { value: 'users', label: 'Egyéni' },
  { value: 'teams', label: 'Csapat' },
]

export function Leaderboard() {
  const [params, setParams] = useSearchParams()
  const tab = (params.get('tab') as Tab) || 'users'

  return (
    <div className="max-w-3xl mx-auto px-6 py-10 space-y-6">
      <header>
        <p className="text-xs font-mono uppercase tracking-[0.2em] text-orange-600">Ranglista</p>
        <h1 className="text-4xl font-black uppercase tracking-tight mt-2">Állás</h1>
      </header>

      <div className="flex gap-2 flex-wrap">
        {TABS.map((t) => (
          <button
            key={t.value}
            type="button"
            onClick={() => setParams({ tab: t.value })}
            className={`border-2 px-3 py-1 text-xs font-mono uppercase tracking-[0.15em] ${
              tab === t.value
                ? 'border-stone-900 bg-stone-900 text-white'
                : 'border-stone-300 text-stone-600 hover:border-stone-900'
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

function UsersBoard() {
  const { data, isLoading, error } = useIndividualLeaderboard()

  if (isLoading) return <p className="font-mono text-stone-500">betöltés…</p>
  if (error) return <ErrorBox e={error} />
  if (!data || data.length === 0) return <EmptyBox text="Még nincs pontozás." />

  return (
    <div className="border-2 border-stone-900 bg-white">
      <table className="w-full text-sm font-mono">
        <thead>
          <tr className="border-b-2 border-stone-900 bg-stone-50">
            <Th className="w-12 text-center">#</Th>
            <Th>Játékos</Th>
            <Th className="text-right">Pont</Th>
            <Th className="text-right hidden sm:table-cell" title="10 pontos találatok">10p</Th>
            <Th className="text-right hidden sm:table-cell" title="Leghosszabb 3+ pontos sorozat">Sorozat</Th>
          </tr>
        </thead>
        <tbody>
          {data.map((row) => (
            <UserRow key={row.userId} row={row} />
          ))}
        </tbody>
      </table>
    </div>
  )
}

function UserRow({ row }: { row: IndividualLeaderboardRow }) {
  const cls = row.isMe ? 'bg-orange-50 border-l-4 border-l-orange-600' : ''
  return (
    <tr className={`border-b border-stone-200 last:border-b-0 ${cls}`}>
      <Td className="text-center font-bold">{row.rank}</Td>
      <Td>
        <span className="flex items-center gap-2 flex-wrap">
          <Avatar
            userId={row.userId}
            displayName={row.displayName}
            version={row.avatarVersion}
            size={28}
          />
          <span>{row.displayName}</span>
          {row.isMe && (
            <span className="text-[10px] uppercase tracking-[0.15em] text-orange-700">én</span>
          )}
          {row.winnerCorrect === true && (
            <span className="text-[10px] uppercase tracking-[0.15em] bg-yellow-100 text-yellow-800 px-1.5 py-0.5">
              győztes ✓
            </span>
          )}
          {row.topScorerCorrect === true && (
            <span className="text-[10px] uppercase tracking-[0.15em] bg-yellow-100 text-yellow-800 px-1.5 py-0.5">
              gólkirály ✓
            </span>
          )}
        </span>
      </Td>
      <Td className="text-right tabular-nums font-bold">{row.totalPoints}</Td>
      <Td className="text-right tabular-nums hidden sm:table-cell text-stone-600">{row.exactCount}</Td>
      <Td className="text-right tabular-nums hidden sm:table-cell text-stone-600">{row.longestStreak}</Td>
    </tr>
  )
}

function TeamsBoard() {
  const { data, isLoading, error } = useTeamLeaderboard()

  if (isLoading) return <p className="font-mono text-stone-500">betöltés…</p>
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
  const cls = row.isMyTeam ? 'border-orange-600 ring-2 ring-orange-200' : 'border-stone-900'
  return (
    <article className={`border-2 ${cls} bg-white`}>
      <header className="flex items-center justify-between px-4 py-3 border-b-2 border-stone-200">
        <div className="flex items-center gap-3">
          <span className="text-xl font-black tabular-nums">{row.rank}.</span>
          <span className="text-base font-bold">{row.teamName}</span>
          {row.isMyTeam && (
            <span className="text-[10px] uppercase tracking-[0.15em] bg-orange-100 text-orange-800 px-2 py-0.5">
              csapatom
            </span>
          )}
        </div>
        <span className="text-2xl font-mono tabular-nums font-bold">{row.totalPoints}</span>
      </header>
      <ul className="px-4 py-2 text-xs font-mono text-stone-600 grid grid-cols-2 gap-x-4 gap-y-1">
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
              <span className="truncate">{m.displayName}</span>
              {m.isAi && <span className="text-[10px] uppercase text-stone-400">AI</span>}
            </span>
            <span className="tabular-nums text-stone-500 shrink-0">{m.points}</span>
          </li>
        ))}
      </ul>
    </article>
  )
}

function Th({ children, className, title }: { children: React.ReactNode; className?: string; title?: string }) {
  return (
    <th
      title={title}
      className={`px-3 py-2 text-left text-xs uppercase tracking-[0.15em] text-stone-500 ${className ?? ''}`}
    >
      {children}
    </th>
  )
}

function Td({ children, className }: { children: React.ReactNode; className?: string }) {
  return <td className={`px-3 py-2 ${className ?? ''}`}>{children}</td>
}

function ErrorBox({ e }: { e: unknown }) {
  return (
    <p className="border-2 border-red-700 bg-red-50 p-4 font-mono text-sm text-red-800">
      ⚠ {e instanceof Error ? e.message : String(e)}
    </p>
  )
}

function EmptyBox({ text }: { text: string }) {
  return (
    <p className="border-2 border-stone-300 bg-white p-6 text-center font-mono text-stone-500">
      {text}
    </p>
  )
}
