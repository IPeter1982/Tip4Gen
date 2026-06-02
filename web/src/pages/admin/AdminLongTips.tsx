import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import {
  useLongTipOutcomes,
  useNationalTeams,
  useSetLongTipOutcomes,
} from '../../api/hooks'
import { ApiError } from '../../api/errors'
import { TeamSelect } from '../../components/TeamSelect'

function errorMessage(e: unknown): string {
  if (e instanceof ApiError) return e.message
  if (e instanceof Error) return e.message
  return String(e)
}

export function AdminLongTips() {
  const outcomes = useLongTipOutcomes()
  const teams = useNationalTeams()
  const setOutcomes = useSetLongTipOutcomes()

  const [winnerTeamId, setWinnerTeamId] = useState<string>('')
  const [topScorerName, setTopScorerName] = useState<string>('')
  const [reason, setReason] = useState<string>('')
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  useEffect(() => {
    if (outcomes.data) {
      setWinnerTeamId(outcomes.data.winnerTeamId ?? '')
      setTopScorerName(outcomes.data.topScorerName ?? '')
    }
  }, [outcomes.data])

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSubmitError(null)
    setSuccess(null)
    try {
      const r = await setOutcomes.mutateAsync({
        winnerTeamId: winnerTeamId || null,
        topScorerName: topScorerName.trim() || null,
        reason: reason.trim() || null,
      })
      setSuccess(
        `Mentve. Győztes: ${r.winnerTeamName ?? '—'}, gólkirály: ${r.topScorerName ?? '—'}.`,
      )
      setReason('')
    } catch (err) {
      setSubmitError(errorMessage(err))
    }
  }

  return (
    <div className="max-w-xl mx-auto px-6 py-10 space-y-6">
      <Link
        to="/admin"
        className="text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle hover:text-accent"
      >
        ← vissza
      </Link>

      <header>
        <p className="text-xs font-mono uppercase tracking-[0.2em] text-accent">Admin</p>
        <h1 className="text-4xl font-black uppercase tracking-tight mt-2">Hosszú tippek</h1>
        <p className="text-sm font-mono text-fg-muted mt-2">
          Add meg a torna győztesét és a gólkirályt. Ez vezérli a §9 holtverseny-szabályokat
          az egyéni ranglistán.
        </p>
      </header>

      {(outcomes.isLoading || teams.isLoading) && <p className="font-mono text-fg-subtle">betöltés…</p>}
      {outcomes.error && (
        <p className="border-2 border-danger bg-danger/10 p-4 font-mono text-sm text-danger">
          ⚠ {errorMessage(outcomes.error)}
        </p>
      )}

      {teams.data && (
        <form onSubmit={onSubmit} className="border-2 border-border-strong bg-elevated p-5 space-y-4">
          <div>
            <label className="block text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle mb-1">
              Győztes csapat
            </label>
            <TeamSelect
              teams={teams.data}
              value={winnerTeamId || null}
              onChange={(id) => setWinnerTeamId(id ?? '')}
              placeholder="— nincs megadva —"
            />
          </div>

          <div>
            <label className="block text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle mb-1">
              Gólkirály neve
            </label>
            <input
              type="text"
              maxLength={120}
              value={topScorerName}
              onChange={(e) => setTopScorerName(e.target.value)}
              placeholder="pl. Lionel Messi"
              className="w-full border-2 border-border-strong px-3 py-2 font-mono text-sm"
            />
            <p className="text-xs font-mono text-fg-subtle mt-1">
              Pontos egyezés szükséges (kis/nagybetű nem számít). Max 120 karakter.
            </p>
          </div>

          <div>
            <label className="block text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle mb-1">
              Megjegyzés (audit log)
            </label>
            <input
              type="text"
              maxLength={500}
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              className="w-full border-2 border-border-strong px-3 py-2 font-mono text-sm"
            />
          </div>

          {submitError && <p className="text-xs font-mono text-danger">{submitError}</p>}
          {success && <p className="text-xs font-mono text-success">{success}</p>}

          <button
            type="submit"
            disabled={setOutcomes.isPending}
            className="w-full border-2 border-accent bg-accent text-on-accent py-3 text-sm font-mono uppercase tracking-[0.2em] hover:bg-accent-strong hover:border-accent disabled:opacity-40"
          >
            {setOutcomes.isPending ? 'mentés…' : 'Mentés'}
          </button>
        </form>
      )}
    </div>
  )
}
