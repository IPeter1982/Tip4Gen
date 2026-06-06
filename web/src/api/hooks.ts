import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useApi } from '../auth/useApi'
import type {
  AdminAuditResponse,
  AdminTeamDeleteResponse,
  AdminTeamMemberRemoveResponse,
  AiMode,
  AiTipperManualRunResponse,
  IndividualLeaderboardRow,
  InviteView,
  LastImportInfo,
  LongTipOutcomes,
  LongTipsResponse,
  MatchCancelResponse,
  MatchListItem,
  MatchPostponeResponse,
  MatchSetResultResponse,
  MatchTipsResponse,
  MeResponse,
  NationalTeam,
  Player,
  PlayersImportResponse,
  PreferencesResponse,
  TeamAdminView,
  TeamLeaderboardRow,
  TeamMatchBreakdownView,
  TeamView,
  TipResponse,
  UserTipHistoryResponse,
} from './types'

type Phase = 'upcoming' | 'past' | 'all'

// /api/me cached at app level. isAdmin doesn't change mid-session, so
// staleTime: Infinity keeps the Topbar + RequireAdmin pulls instant.
export function useMe() {
  const api = useApi()
  return useQuery({
    queryKey: ['me'],
    queryFn: () => api.get<MeResponse>('/api/me'),
    staleTime: Infinity,
  })
}

export function useRenameMe() {
  const api = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (displayName: string) =>
      api.patch<MeResponse>('/api/me', { displayName }),
    onSuccess: (updated) => {
      qc.setQueryData(['me'], updated)
      // Renames are rare → invalidate every surface that renders the name.
      qc.invalidateQueries({ queryKey: ['team'] })
      qc.invalidateQueries({ queryKey: ['match-tips'] })
      qc.invalidateQueries({ queryKey: ['leaderboard'] })
      qc.invalidateQueries({ queryKey: ['admin', 'audit'] })
    },
  })
}

export function useSetAvatar() {
  const api = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (dataUrl: string) => api.put<MeResponse>('/api/me/avatar', { dataUrl }),
    onSuccess: (updated) => {
      qc.setQueryData(['me'], updated)
      qc.invalidateQueries({ queryKey: ['leaderboard'] })
    },
  })
}

export function useDeleteAvatar() {
  const api = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: () => api.del<MeResponse>('/api/me/avatar'),
    onSuccess: (updated) => {
      qc.setQueryData(['me'], updated)
      qc.invalidateQueries({ queryKey: ['leaderboard'] })
    },
  })
}

export function useSetAiAvatar() {
  const api = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ dataUrl, reason }: { dataUrl: string; reason?: string | null }) =>
      api.put<{ aiAvatarVersion: string | null }>('/api/admin/ai-avatar', { dataUrl, reason }),
    onSuccess: () => {
      // Re-fetch /api/me so every Avatar reading me.data?.aiAvatarVersion refreshes.
      qc.invalidateQueries({ queryKey: ['me'] })
      qc.invalidateQueries({ queryKey: ['team'] })
      qc.invalidateQueries({ queryKey: ['leaderboard'] })
      qc.invalidateQueries({ queryKey: ['admin', 'audit'] })
    },
  })
}

export function useDeleteAiAvatar() {
  const api = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (reason?: string | null) => {
      const qs = reason ? `?reason=${encodeURIComponent(reason)}` : ''
      return api.del<{ aiAvatarVersion: string | null }>(`/api/admin/ai-avatar${qs}`)
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['me'] })
      qc.invalidateQueries({ queryKey: ['team'] })
      qc.invalidateQueries({ queryKey: ['leaderboard'] })
      qc.invalidateQueries({ queryKey: ['admin', 'audit'] })
    },
  })
}

// 30s polling on live-changing queries (replaces the deferred SignalR work per
// Phase 10's cut-OK alternative). refetchIntervalInBackground stays default
// (false), so a backgrounded tab pauses polling.
const LIVE_REFETCH_MS = 30_000

export function useMatches(phase: Phase = 'upcoming') {
  const api = useApi()
  return useQuery({
    queryKey: ['matches', phase],
    queryFn: () => api.get<MatchListItem[]>(`/api/matches?phase=${phase}`),
    refetchInterval: LIVE_REFETCH_MS,
    refetchOnWindowFocus: true,
  })
}

export function useMatch(matchId: string | undefined) {
  const api = useApi()
  return useQuery({
    queryKey: ['match', matchId],
    queryFn: () => api.get<MatchListItem>(`/api/matches/${matchId}`),
    enabled: !!matchId,
    refetchInterval: LIVE_REFETCH_MS,
    refetchOnWindowFocus: true,
  })
}

export function useMatchTips(matchId: string | undefined) {
  const api = useApi()
  return useQuery({
    queryKey: ['match-tips', matchId],
    queryFn: () => api.get<MatchTipsResponse>(`/api/matches/${matchId}/tips`),
    enabled: !!matchId,
    refetchInterval: LIVE_REFETCH_MS,
    refetchOnWindowFocus: true,
  })
}

export function useNationalTeams() {
  const api = useApi()
  return useQuery({
    queryKey: ['national-teams'],
    queryFn: () => api.get<NationalTeam[]>('/api/national-teams'),
    staleTime: 10 * 60_000,
  })
}

// ~600 rows on a typical squad import; cache for 10 min like national teams.
// Client-side filtered in PlayerSelect, so no server-side search endpoint needed.
export function usePlayers() {
  const api = useApi()
  return useQuery({
    queryKey: ['players'],
    queryFn: () => api.get<Player[]>('/api/players'),
    staleTime: 10 * 60_000,
  })
}

export function useLongTips() {
  const api = useApi()
  return useQuery({
    queryKey: ['long-tips'],
    queryFn: () => api.get<LongTipsResponse>('/api/long-tips'),
  })
}

export type SubmitTipInput = {
  matchId: string
  homeGoals: number
  awayGoals: number
  joker: boolean
}

export function useSubmitTip() {
  const api = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ matchId, ...body }: SubmitTipInput) =>
      api.put<TipResponse>(`/api/tips/${matchId}`, body),
    onSuccess: (_data, vars) => {
      qc.invalidateQueries({ queryKey: ['matches'] })
      qc.invalidateQueries({ queryKey: ['match', vars.matchId] })
    },
  })
}

export type SubmitLongTipsInput = {
  winnerTeamId?: string | null
  topScorerPlayerId?: string | null
}

export function useSubmitLongTips() {
  const api = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: SubmitLongTipsInput) => api.put<LongTipsResponse>('/api/long-tips', body),
    onSuccess: (data) => {
      qc.setQueryData(['long-tips'], data)
    },
  })
}

// ----- Teams -----

export const TEAM_ME_KEY = ['team', 'me'] as const
export const TEAMS_ALL_KEY = ['teams', 'all'] as const

export function useMyTeam() {
  const api = useApi()
  return useQuery({
    queryKey: TEAM_ME_KEY,
    // GET /api/teams/me returns 204 when the user isn't in a team; useApi.get
    // resolves that as undefined, which we normalize to null so the query has
    // a stable cached value instead of forcing every caller to handle both.
    queryFn: async () => {
      const team = await api.get<TeamView | undefined>('/api/teams/me')
      return team ?? null
    },
  })
}

export function useAllTeams() {
  const api = useApi()
  return useQuery({
    queryKey: TEAMS_ALL_KEY,
    queryFn: () => api.get<TeamView[]>('/api/teams'),
  })
}

export function useJoinTeamDirect() {
  const api = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (teamId: string) => api.post<TeamView>(`/api/teams/${teamId}/join`),
    onSuccess: (team) => {
      qc.setQueryData(TEAM_ME_KEY, team)
      qc.invalidateQueries({ queryKey: TEAMS_ALL_KEY })
    },
  })
}

export function useCreateTeam() {
  const api = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (name: string) => api.post<TeamView>('/api/teams', { name }),
    onSuccess: (team) => {
      qc.setQueryData(TEAM_ME_KEY, team)
    },
  })
}

export type PatchTeamInput = {
  teamId: string
  name?: string
  aiMode?: AiMode
  clearAiMode?: boolean
}

export function usePatchTeam() {
  const api = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ teamId, ...body }: PatchTeamInput) =>
      api.patch<TeamView>(`/api/teams/${teamId}`, body),
    onSuccess: (team) => qc.setQueryData(TEAM_ME_KEY, team),
  })
}

export function useLeaveTeam() {
  const api = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (teamId: string) => api.post<void>(`/api/teams/${teamId}/leave`),
    onSuccess: () => qc.setQueryData(TEAM_ME_KEY, null),
  })
}

export type AddAiMemberInput = {
  teamId: string
  displayName: string
  mode: AiMode
}

export function useAddAiMember() {
  const api = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ teamId, ...body }: AddAiMemberInput) =>
      api.post<TeamView>(`/api/teams/${teamId}/ai-member`, body),
    onSuccess: (team) => qc.setQueryData(TEAM_ME_KEY, team),
  })
}

export function useCreateInvite() {
  const api = useApi()
  return useMutation({
    mutationFn: (teamId: string) => api.post<InviteView>(`/api/teams/${teamId}/invites`),
  })
}

export function useJoinTeam() {
  const api = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (token: string) => api.post<TeamView>(`/api/teams/join/${token}`),
    onSuccess: (team) => qc.setQueryData(TEAM_ME_KEY, team),
  })
}

export function useSetTeamAvatar() {
  const api = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ teamId, dataUrl }: { teamId: string; dataUrl: string }) =>
      api.put<TeamView>(`/api/teams/${teamId}/avatar`, { dataUrl }),
    onSuccess: (team) => {
      qc.setQueryData(TEAM_ME_KEY, team)
      qc.invalidateQueries({ queryKey: ['leaderboard', 'teams'] })
    },
  })
}

export function useDeleteTeamAvatar() {
  const api = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (teamId: string) => api.del<TeamView>(`/api/teams/${teamId}/avatar`),
    onSuccess: (team) => {
      qc.setQueryData(TEAM_ME_KEY, team)
      qc.invalidateQueries({ queryKey: ['leaderboard', 'teams'] })
    },
  })
}

export function useTeamMatchBreakdown(teamId: string | undefined, matchId: string | undefined) {
  const api = useApi()
  return useQuery({
    queryKey: ['team', teamId, 'match', matchId, 'breakdown'],
    queryFn: () =>
      api.get<TeamMatchBreakdownView>(`/api/teams/${teamId}/matches/${matchId}/breakdown`),
    enabled: !!teamId && !!matchId,
  })
}

// ----- Leaderboard -----

export function useIndividualLeaderboard() {
  const api = useApi()
  return useQuery({
    queryKey: ['leaderboard', 'users'],
    queryFn: () => api.get<IndividualLeaderboardRow[]>('/api/leaderboard/users'),
    refetchInterval: LIVE_REFETCH_MS,
    refetchOnWindowFocus: true,
  })
}

export function useTeamLeaderboard() {
  const api = useApi()
  return useQuery({
    queryKey: ['leaderboard', 'teams'],
    queryFn: () => api.get<TeamLeaderboardRow[]>('/api/leaderboard/teams'),
    refetchInterval: LIVE_REFETCH_MS,
    refetchOnWindowFocus: true,
  })
}

export function useUserTipHistory(userId: string | undefined) {
  const api = useApi()
  return useQuery({
    queryKey: ['user-tip-history', userId],
    queryFn: () => api.get<UserTipHistoryResponse>(`/api/users/${userId}/tips`),
    enabled: !!userId,
  })
}

// ----- Admin (Phase 8) -----

export type SetMatchResultInput = {
  matchId: string
  homeGoals: number
  awayGoals: number
  status: 'Finished' | 'Awarded'
  reason?: string | null
}

export function useSetMatchResult() {
  const api = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ matchId, ...body }: SetMatchResultInput) =>
      api.put<MatchSetResultResponse>(`/api/admin/matches/${matchId}/result`, body),
    onSuccess: (_data, vars) => {
      qc.invalidateQueries({ queryKey: ['matches'] })
      qc.invalidateQueries({ queryKey: ['match', vars.matchId] })
      qc.invalidateQueries({ queryKey: ['match-tips', vars.matchId] })
      qc.invalidateQueries({ queryKey: ['leaderboard'] })
      qc.invalidateQueries({ queryKey: ['admin', 'audit'] })
    },
  })
}

export type CancelMatchInput = { matchId: string; reason?: string | null }

export function useCancelMatch() {
  const api = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ matchId, reason }: CancelMatchInput) =>
      api.post<MatchCancelResponse>(`/api/admin/matches/${matchId}/cancel`, { reason }),
    onSuccess: (_data, vars) => {
      qc.invalidateQueries({ queryKey: ['matches'] })
      qc.invalidateQueries({ queryKey: ['match', vars.matchId] })
      qc.invalidateQueries({ queryKey: ['leaderboard'] })
      qc.invalidateQueries({ queryKey: ['admin', 'audit'] })
    },
  })
}

export type PostponeMatchInput = { matchId: string; newKickoffUtc: string; reason?: string | null }

export function usePostponeMatch() {
  const api = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ matchId, ...body }: PostponeMatchInput) =>
      api.post<MatchPostponeResponse>(`/api/admin/matches/${matchId}/postpone`, body),
    onSuccess: (_data, vars) => {
      qc.invalidateQueries({ queryKey: ['matches'] })
      qc.invalidateQueries({ queryKey: ['match', vars.matchId] })
      qc.invalidateQueries({ queryKey: ['admin', 'audit'] })
    },
  })
}

export type RunAiTipperInput = { matchId: string; reason?: string | null }

export function useRunAiTipperForMatch() {
  const api = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ matchId, reason }: RunAiTipperInput) =>
      api.post<AiTipperManualRunResponse>(`/api/admin/ai-tipper/run/${matchId}`, { reason }),
    onSuccess: (_data, vars) => {
      // New tips landed → invalidate match-tips (post-deadline panel) + audit log.
      qc.invalidateQueries({ queryKey: ['match-tips', vars.matchId] })
      qc.invalidateQueries({ queryKey: ['admin', 'audit'] })
    },
  })
}

export function useAdminAuditLog(matchId?: string, take = 50, skip = 0) {
  const api = useApi()
  return useQuery({
    queryKey: ['admin', 'audit', { matchId: matchId ?? null, take, skip }],
    queryFn: () => {
      const params = new URLSearchParams()
      if (matchId) params.set('matchId', matchId)
      params.set('take', String(take))
      params.set('skip', String(skip))
      return api.get<AdminAuditResponse>(`/api/admin/audit?${params.toString()}`)
    },
  })
}

// ----- Preferences (Phase 9) -----

export function usePreferences() {
  const api = useApi()
  return useQuery({
    queryKey: ['me', 'preferences'],
    queryFn: () => api.get<PreferencesResponse>('/api/me/preferences'),
  })
}

export function useSetPreferences() {
  const api = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (emailRemindersEnabled: boolean) =>
      api.put<PreferencesResponse>('/api/me/preferences', { emailRemindersEnabled }),
    onSuccess: (data) => {
      qc.setQueryData(['me', 'preferences'], data)
    },
  })
}

export function useLongTipOutcomes() {
  const api = useApi()
  return useQuery({
    queryKey: ['admin', 'long-tips', 'outcomes'],
    queryFn: () => api.get<LongTipOutcomes>('/api/admin/long-tips/outcomes'),
  })
}

export type SetLongTipOutcomesInput = {
  winnerTeamId?: string | null
  topScorerPlayerId?: string | null
  reason?: string | null
}

export function useSetLongTipOutcomes() {
  const api = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: SetLongTipOutcomesInput) =>
      api.put<LongTipOutcomes>('/api/admin/long-tips/outcomes', body),
    onSuccess: (data) => {
      qc.setQueryData(['admin', 'long-tips', 'outcomes'], data)
      qc.invalidateQueries({ queryKey: ['leaderboard'] })
      qc.invalidateQueries({ queryKey: ['admin', 'audit'] })
    },
  })
}

// ----- Admin: Players importer -----

export function useImportPlayers() {
  const api = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: () => api.post<PlayersImportResponse>('/api/admin/players/import', {}),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['players'] })
      qc.invalidateQueries({ queryKey: ['admin', 'players', 'last-import'] })
      qc.invalidateQueries({ queryKey: ['admin', 'audit'] })
    },
  })
}

export function useLastPlayersImport() {
  const api = useApi()
  return useQuery({
    queryKey: ['admin', 'players', 'last-import'],
    queryFn: () => api.get<LastImportInfo | null>('/api/admin/players/last-import'),
    staleTime: 0,
  })
}

// ----- Admin: Teams -----

export const ADMIN_TEAMS_KEY = ['admin', 'teams'] as const

export function useAdminTeams() {
  const api = useApi()
  return useQuery({
    queryKey: ADMIN_TEAMS_KEY,
    queryFn: () => api.get<TeamAdminView[]>('/api/admin/teams'),
  })
}

export type RenameAdminTeamInput = { teamId: string; name: string; reason?: string | null }

export function useRenameAdminTeam() {
  const api = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ teamId, name, reason }: RenameAdminTeamInput) =>
      api.put<TeamAdminView>(`/api/admin/teams/${teamId}/name`, { name, reason }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ADMIN_TEAMS_KEY })
      qc.invalidateQueries({ queryKey: ['leaderboard', 'teams'] })
      qc.invalidateQueries({ queryKey: ['teams', 'all'] })
      qc.invalidateQueries({ queryKey: ['team', 'me'] })
      qc.invalidateQueries({ queryKey: ['admin', 'audit'] })
    },
  })
}

export type DeleteAdminTeamInput = { teamId: string; reason?: string | null }

export function useDeleteAdminTeam() {
  const api = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ teamId, reason }: DeleteAdminTeamInput) => {
      const qs = reason ? `?reason=${encodeURIComponent(reason)}` : ''
      return api.del<AdminTeamDeleteResponse>(`/api/admin/teams/${teamId}${qs}`)
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ADMIN_TEAMS_KEY })
      qc.invalidateQueries({ queryKey: ['leaderboard', 'teams'] })
      qc.invalidateQueries({ queryKey: ['teams', 'all'] })
      qc.invalidateQueries({ queryKey: ['team', 'me'] })
      qc.invalidateQueries({ queryKey: ['admin', 'audit'] })
    },
  })
}

export type RemoveAdminTeamMemberInput = {
  teamId: string
  memberId: string
  reason?: string | null
}

export function useRemoveAdminTeamMember() {
  const api = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ teamId, memberId, reason }: RemoveAdminTeamMemberInput) => {
      const qs = reason ? `?reason=${encodeURIComponent(reason)}` : ''
      return api.del<AdminTeamMemberRemoveResponse>(
        `/api/admin/teams/${teamId}/members/${memberId}${qs}`,
      )
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ADMIN_TEAMS_KEY })
      qc.invalidateQueries({ queryKey: ['leaderboard', 'teams'] })
      qc.invalidateQueries({ queryKey: ['leaderboard', 'users'] })
      qc.invalidateQueries({ queryKey: ['teams', 'all'] })
      qc.invalidateQueries({ queryKey: ['team', 'me'] })
      qc.invalidateQueries({ queryKey: ['admin', 'audit'] })
    },
  })
}
