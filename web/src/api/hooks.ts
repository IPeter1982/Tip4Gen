import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useApi } from '../auth/useApi'
import type {
  LongTipsResponse,
  MatchListItem,
  MatchTipsResponse,
  NationalTeam,
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
