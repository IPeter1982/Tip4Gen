export type MeResponse = {
  id: string
  displayName: string
  auth0Sub: string
  createdAt: string
  isAdmin: boolean
  avatarVersion: string | null
  aiAvatarVersion: string | null
}

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

export type Player = {
  id: string
  name: string
  teamId: string
  teamName: string
  teamCode: string | null
}

export type LongTipsResponse = {
  winnerTeamId: string | null
  winnerTeamName: string | null
  topScorerPlayerId: string | null
  topScorerPlayerName: string | null
  topScorerTeamCode: string | null
  winnerSubmittedAt: string | null
  topScorerSubmittedAt: string | null
  lockUtc: string
  locked: boolean
}

export type MatchTip = {
  userId: string | null
  teamMemberId: string | null
  displayName: string
  isAi: boolean
  isAiFallback: boolean
  reasoning: string | null
  submittedAt: string
  updatedAt: string | null
  homeGoals: number | null
  awayGoals: number | null
  joker: boolean | null
  score: UserTipScore | null
}

export type MatchTipsResponse = {
  matchId: string
  deadlineUtc: string
  deadlinePassed: boolean
  tipCount: number
  tips: MatchTip[]
}

export type TeamStatus = 'Forming' | 'Locked' | 'Disqualified'
export type AiMode = 'Conservative' | 'Balanced' | 'Bold'

export type TeamMemberView = {
  id: string
  userId: string | null
  displayName: string
  avatarVersion: string | null
  isAi: boolean
  joinedAt: string
}

export type TeamView = {
  id: string
  name: string
  status: TeamStatus
  aiMode: AiMode | null
  createdAt: string
  avatarVersion: string | null
  members: TeamMemberView[]
}

export type InviteView = {
  token: string
  expiresAt: string
}

export type MemberBreakdownView = {
  memberId: string
  userId: string | null
  displayName: string
  isAi: boolean
  points: number
}

export type TeamMatchBreakdownView = {
  teamId: string
  teamName: string
  teamAvatarVersion: string | null
  matchId: string
  totalPoints: number
  members: MemberBreakdownView[]
}

export type IndividualLeaderboardRow = {
  rank: number
  userId: string
  displayName: string
  avatarVersion: string | null
  totalPoints: number
  exactCount: number
  winnerCorrect: boolean | null
  topScorerCorrect: boolean | null
  longestStreak: number
  isMe: boolean
}

export type TeamLeaderboardMember = {
  memberId: string
  userId: string | null
  displayName: string
  avatarVersion: string | null
  isAi: boolean
  points: number
}

export type TeamLeaderboardRow = {
  rank: number
  teamId: string
  teamName: string
  teamAvatarVersion: string | null
  totalPoints: number
  members: TeamLeaderboardMember[]
  isMyTeam: boolean
}

// ----- User tip history (drilldown from individual leaderboard) -----

export type ScoreCategoryName =
  | 'Nothing'
  | 'OneTeamGoals'
  | 'Winner'
  | 'WinnerAndGoalDiff'
  | 'Exact'

export type UserTipScore = {
  category: ScoreCategoryName
  basePoints: number
  multiplier: number
  jokerApplied: boolean
  finalPoints: number
}

export type UserTipDetail = {
  homeGoals: number
  awayGoals: number
  joker: boolean
  submittedAt: string
  score: UserTipScore | null
}

export type UserTipHistoryItem = {
  matchId: string
  stage: Stage
  groupCode: string | null
  roundLabel: string | null
  homeTeam: TeamSummary
  awayTeam: TeamSummary
  kickoffUtc: string
  status: MatchStatus
  homeGoals: number | null
  awayGoals: number | null
  tip: UserTipDetail
}

export type UserTipHistoryResponse = {
  userId: string
  displayName: string
  avatarVersion: string | null
  totalPoints: number
  items: UserTipHistoryItem[]
}

// ----- Preferences (Phase 9) -----

export type PreferencesResponse = {
  emailRemindersEnabled: boolean
  hasEmail: boolean
}

// ----- Admin (Phase 8) -----

export type AdminAuditAction =
  | 'MatchSetResult'
  | 'MatchCancel'
  | 'MatchPostpone'
  | 'MatchRescore'
  | 'LongTipOutcomesSet'
  | 'AiAvatarSet'
  | 'AiAvatarDeleted'
  | 'AiTipperManualRun'
  | 'PlayersImported'
  | 'TeamRenamed'
  | 'TeamDeleted'
  | 'TeamMemberRemoved'

export type AdminAuditRow = {
  id: string
  action: AdminAuditAction
  entityType: string
  entityId: string | null
  adminUserId: string
  adminDisplayName: string
  beforeJson: string | null
  afterJson: string | null
  reason: string | null
  occurredAt: string
}

export type AdminAuditResponse = {
  total: number
  take: number
  skip: number
  rows: AdminAuditRow[]
}

export type MatchSetResultResponse = {
  matchId: string
  tipsScored: number
  totalPoints: number
}

export type MatchCancelResponse = {
  matchId: string
  scoredTipsCleared: number
  jokersRefunded: number
}

export type MatchPostponeResponse = {
  matchId: string
  newKickoffUtc: string
  newDeadlineUtc: string
}

export type AiTipperManualRunResponse = {
  aiMembers: number
  attempted: number
  written: number
  fallbacks: number
  skipped: number
}

export type LongTipOutcomes = {
  winnerTeamId: string | null
  winnerTeamName: string | null
  topScorerPlayerId: string | null
  topScorerPlayerName: string | null
  topScorerTeamCode: string | null
}

export type PlayersImportResponse = {
  added: number
  skipped: number
  unmatchedTeams: number
  totalAfter: number
  durationMs: number
}

export type LastImportInfo = {
  occurredAt: string
  afterJson: string | null
}

// ----- Admin: Teams -----

export type TeamAdminMemberView = {
  id: string
  userId: string | null
  displayName: string
  avatarVersion: string | null
  isAi: boolean
  joinedAt: string
}

export type TeamAdminView = {
  id: string
  name: string
  status: TeamStatus
  aiMode: AiMode | null
  createdAt: string
  updatedAt: string
  avatarVersion: string | null
  memberCount: number
  humanMemberCount: number
  aiMemberCount: number
  members: TeamAdminMemberView[]
}

export type AdminTeamDeleteResponse = {
  teamId: string
  name: string
  membersRemoved: number
}

export type AdminTeamMemberRemoveResponse = {
  teamId: string
  removedMemberId: string
  teamCascadeDeleted: boolean
  statusRevertedToForming: boolean
  team: TeamAdminView | null
}
