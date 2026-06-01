import { useAuth0 } from '@auth0/auth0-react'
import { Link } from 'react-router'
import { useMatches } from '../api/hooks'
import { isAuthConfigured } from '../auth/authConfig'
import type { MatchListItem } from '../api/types'
import { TeamLabel } from './TeamLabel'

export function LiveMatchBanner() {
  const { isAuthenticated } = useAuth0()
  // 'upcoming' is Scheduled+Live on the backend; we filter to Live here.
  // The query is shared (TanStack dedupes by key) with the Matches page.
  const matches = useMatches('upcoming')

  if (!isAuthConfigured || !isAuthenticated) return null
  const live = (matches.data ?? []).filter((m) => m.status === 'Live')
  if (live.length === 0) return null

  return (
    <div className="border-b-2 border-stone-900 bg-orange-500 text-stone-900">
      <div className="max-w-5xl mx-auto px-4 sm:px-6 py-2 flex items-center gap-3 flex-wrap">
        <span className="text-[10px] font-mono uppercase tracking-[0.2em] font-bold animate-pulse">
          ● ÉL
        </span>
        <ul className="flex items-center gap-x-4 gap-y-1 flex-wrap text-xs font-mono">
          {live.map((m) => (
            <LiveRow key={m.id} match={m} />
          ))}
        </ul>
      </div>
    </div>
  )
}

function LiveRow({ match }: { match: MatchListItem }) {
  const score =
    match.homeGoals !== null && match.awayGoals !== null
      ? `${match.homeGoals}–${match.awayGoals}`
      : '— : —'
  return (
    <li>
      <Link
        to={`/matches/${match.id}/tip`}
        className="hover:underline whitespace-nowrap"
      >
        <span className="font-bold"><TeamLabel team={match.homeTeam} /></span>
        <span className="mx-2 tabular-nums">{score}</span>
        <span className="font-bold"><TeamLabel team={match.awayTeam} /></span>
      </Link>
    </li>
  )
}
