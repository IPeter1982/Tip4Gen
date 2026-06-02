import { useAuth0 } from '@auth0/auth0-react'
import { Link } from 'react-router'
import { Radio } from 'lucide-react'
import { useMatches } from '../api/hooks'
import { isAuthConfigured } from '../auth/authConfig'
import type { MatchListItem } from '../api/types'
import { TeamLabel } from './TeamLabel'

export function LiveMatchBanner() {
  const { isAuthenticated } = useAuth0()
  const matches = useMatches('upcoming')

  if (!isAuthConfigured || !isAuthenticated) return null
  const live = (matches.data ?? []).filter((m) => m.status === 'Live')
  if (live.length === 0) return null

  return (
    <div className="border-b border-accent/30 bg-elevated">
      <div className="max-w-5xl mx-auto px-4 sm:px-6 py-2 flex items-center gap-3 flex-wrap">
        <span className="inline-flex items-center gap-1.5 text-[10px] font-mono uppercase tracking-[0.2em] font-bold text-live">
          <Radio size={12} className="live-dot" />
          ÉL
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
        className="hover:underline whitespace-nowrap flex items-center gap-2 text-fg-default"
      >
        <span className="font-bold"><TeamLabel team={match.homeTeam} /></span>
        <span className="px-2 py-0.5 rounded bg-sunken text-accent tabular-nums">{score}</span>
        <span className="font-bold"><TeamLabel team={match.awayTeam} /></span>
      </Link>
    </li>
  )
}
