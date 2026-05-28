import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import {
  useLongTipOutcomes,
  useNationalTeams,
  useSetLongTipOutcomes,
} from '../../api/hooks'
import { ApiError } from '../../api/errors'

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
        className="text-xs font-mono uppercase tracking-[0.15em] text-stone-500 hover:text-stone-900"
      >
        ← vissza
      </Link>

      <header>
        <p className="text-xs font-mono uppercase tracking-[0.2em] text-orange-600">Admin</p>
        <h1 className="text-4xl font-black uppercase tracking-tight mt-2">Hosszú tippek</h1>
        <p className="text-sm font-mono text-stone-600 mt-2">
          Add meg a torna győztesét és a gólkirályt. Ez vezérli a §9 holtverseny-szabályokat
          az egyéni ranglistán.
        </p>
      </header>

      {(outcomes.isLoading || teams.isLoading) && <p className="font-mono text-stone-500">betöltés…</p>}
      {outcomes.error && (
        <p className="border-2 border-red-700 bg-red-50 p-4 font-mono text-sm text-red-800">
          ⚠ {errorMessage(outcomes.error)}
        </p>
      )}

      {teams.data && (
        <form onSubmit={onSubmit} className="border-2 border-stone-900 bg-white p-5 space-y-4">
          <div>
            <label className="block text-xs font-mono uppercase tracking-[0.15em] text-stone-500 mb-1">
              Győztes csapat
            </label>
            <select
              value={winnerTeamId}
              onChange={(e) => setWinnerTeamId(e.target.value)}
              className="w-full border-2 border-stone-900 px-3 py-2 font-mono text-sm bg-white"
            >
              <option value="">— nincs megadva —</option>
              {teams.data.map((t) => (
                <option key={t.id} value={t.id}>
                  {t.name}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-xs font-mono uppercase tracking-[0.15em] text-stone-500 mb-1">
              Gólkirály neve
            </label>
            <input
              type="text"
              maxLength={120}
              value={topScorerName}
              onChange={(e) => setTopScorerName(e.target.value)}
              placeholder="pl. Lionel Messi"
              className="w-full border-2 border-stone-900 px-3 py-2 font-mono text-sm"
            />
            <p className="text-xs font-mono text-stone-500 mt-1">
              Pontos egyezés szükséges (kis/nagybetű nem számít). Max 120 karakter.
            </p>
          </div>

          <div>
            <label className="block text-xs font-mono uppercase tracking-[0.15em] text-stone-500 mb-1">
              Megjegyzés (audit log)
            </label>
            <input
              type="text"
              maxLength={500}
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              className="w-full border-2 border-stone-900 px-3 py-2 font-mono text-sm"
            />
          </div>

          {submitError && <p className="text-xs font-mono text-red-700">{submitError}</p>}
          {success && <p className="text-xs font-mono text-green-700">{success}</p>}

          <button
            type="submit"
            disabled={setOutcomes.isPending}
            className="w-full border-2 border-stone-900 bg-stone-900 text-white py-3 text-sm font-mono uppercase tracking-[0.2em] hover:bg-orange-600 hover:border-orange-600 disabled:opacity-40"
          >
            {setOutcomes.isPending ? 'mentés…' : 'Mentés'}
          </button>
        </form>
      )}
    </div>
  )
}
