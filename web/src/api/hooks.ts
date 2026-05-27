import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useApi } from '../auth/useApi'
import type {
  AiMode,
  IndividualLeaderboardRow,
  InviteView,
  LongTipsResponse,
  MatchListItem,
  MatchTipsResponse,
  NationalTeam,
  TeamLeaderboardRow,
  TeamMatchBreakdownView,
  TeamView,
  TipResponse,
} from './types'

type Phase = 'upcoming' | 'past' | 'all'

export function useMatches(phase: Phase = 'upcoming') {
  const api = useApi()
  return useQuery({
    queryKey: ['matches', phase],
    queryFn: () => api.get<MatchListItem[]>(`/api/matches?phase=${phase}`),
  })
}

export function useMatch(matchId: string | undefined) {
  const api = useApi()
  return useQuery({
    queryKey: ['match', matchId],
    queryFn: () => api.get<MatchListItem>(`/api/matches/${matchId}`),
    enabled: !!matchId,
  })
}

export function useMatchTips(matchId: string | undefined) {
  const api = useApi()
  return useQuery({
    queryKey: ['match-tips', matchId],
    queryFn: () => api.get<MatchTipsResponse>(`/api/matches/${matchId}/tips`),
    enabled: !!matchId,
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
  topScorerName?: string | null
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
  })
}

export function useTeamLeaderboard() {
  const api = useApi()
  return useQuery({
    queryKey: ['leaderboard', 'teams'],
    queryFn: () => api.get<TeamLeaderboardRow[]>('/api/leaderboard/teams'),
  })
}
