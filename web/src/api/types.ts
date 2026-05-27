export type Stage = 'Group' | 'R32' | 'R16' | 'QF' | 'SF' | 'Bronze' | 'Final'

export type MatchStatus =
  | 'Scheduled'
  | 'Live'
  | 'Finished'
  | 'Postponed'
  | 'Cancelled'
  | 'Abandoned'
  | 'Awarded'

export type TeamSummary = { id: string; name: string; code: string | null }

export type MyTip = {
  homeGoals: number
  awayGoals: number
  joker: boolean
  submittedAt: string
  updatedAt: string
}

export type MatchListItem = {
  id: string
  stage: Stage
  groupCode: string | null
  roundLabel: string | null
  homeTeam: TeamSummary
  awayTeam: TeamSummary
  kickoffUtc: string
  deadlineUtc: string
  status: MatchStatus
  homeGoals: number | null
  awayGoals: number | null
  myTip: MyTip | null
}

export type TipResponse = {
  id: string
  matchId: string
  homeGoals: number
  awayGoals: number
  joker: boolean
  submittedAt: string
  updatedAt: string
  deadlineUtc: string
}

export type NationalTeam = { id: string; name: string; code: string | null }

export type LongTipsResponse = {
  winnerTeamId: string | null
  winnerTeamName: string | null
  topScorerName: string | null
  winnerSubmittedAt: string | null
  topScorerSubmittedAt: string | null
  lockUtc: string
  locked: boolean
}

export type MatchTip = {
  userId: string
  displayName: string
  submittedAt: string
  updatedAt: string | null
  homeGoals: number | null
  awayGoals: number | null
  joker: boolean | null
}

export type MatchTipsResponse = {
  matchId: string
  deadlineUtc: string
  deadlinePassed: boolean
  tipCount: number
  tips: MatchTip[]
}
