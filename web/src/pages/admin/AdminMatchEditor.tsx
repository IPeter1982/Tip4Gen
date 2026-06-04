import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useParams } from 'react-router'
import { z } from 'zod'
import { ApiError } from '../../api/errors'
import {
  useCancelMatch,
  useMatch,
  usePostponeMatch,
  useRunAiTipperForMatch,
  useSetMatchResult,
} from '../../api/hooks'
import { ConfirmDialog } from '../../components/ConfirmDialog'
import { TeamLabel } from '../../components/TeamLabel'
import { STAGE_LABEL_HU, STATUS_LABEL_HU, formatBudapest } from '../../lib/format'

const resultSchema = z.object({
  homeGoals: z.number().int().min(0).max(15),
  awayGoals: z.number().int().min(0).max(15),
  status: z.enum(['Finished', 'Awarded']),
  reason: z.string().max(500).optional().nullable(),
})
type ResultValues = z.infer<typeof resultSchema>

const postponeSchema = z.object({
  newKickoffLocal: z.string().min(1, 'Adj meg új kezdési időpontot'),
  reason: z.string().max(500).optional().nullable(),
})
type PostponeValues = z.infer<typeof postponeSchema>

function errorMessage(e: unknown): string {
  if (e instanceof ApiError) return e.message
  if (e instanceof Error) return e.message
  return String(e)
}

export function AdminMatchEditor() {
  const { matchId } = useParams()
  const { data: match, isLoading, error } = useMatch(matchId)

  return (
    <div className="max-w-2xl mx-auto px-6 py-10 space-y-6">
      <Link
        to="/admin"
        className="text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle hover:text-accent"
      >
        ← vissza
      </Link>

      {isLoading && <p className="font-mono text-fg-subtle">betöltés…</p>}
      {error && (
        <p className="border-2 border-danger bg-danger/10 p-4 font-mono text-sm text-danger">
          ⚠ {errorMessage(error)}
        </p>
      )}

      {match && (
        <>
          <header className="border-2 border-border-strong bg-elevated p-5">
            <p className="text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle">
              {STAGE_LABEL_HU[match.stage] ?? match.stage}
              {match.groupCode ? ` ${match.groupCode}` : ''}
              {' · '}
              <span className="text-fg-default">{STATUS_LABEL_HU[match.status] ?? match.status}</span>
            </p>
            <h1 className="text-2xl font-black uppercase tracking-tight mt-2 flex items-center gap-2 flex-wrap">
              <TeamLabel team={match.homeTeam} size="md" />
              <span className="text-fg-subtle">vs</span>
              <TeamLabel team={match.awayTeam} size="md" />
            </h1>
            <p className="text-xs font-mono text-fg-subtle mt-2">
              {formatBudapest(match.kickoffUtc)} CET
            </p>
            {match.homeGoals != null && match.awayGoals != null && (
              <p className="font-mono text-2xl mt-2 tabular-nums">
                {match.homeGoals} – {match.awayGoals}
              </p>
            )}
          </header>

          <ResultPanel matchId={match.id} initial={{
            homeGoals: match.homeGoals ?? 0,
            awayGoals: match.awayGoals ?? 0,
            status: match.status === 'Awarded' ? 'Awarded' : 'Finished',
          }} />
          <PostponePanel matchId={match.id} currentKickoffUtc={match.kickoffUtc} />
          <AiTipperPanel
            matchId={match.id}
            eligible={match.status === 'Scheduled' && new Date(match.kickoffUtc).getTime() > Date.now()}
            status={match.status}
          />
          <CancelPanel matchId={match.id} disabled={match.status === 'Cancelled'} />
        </>
      )}
    </div>
  )
}

// ----------------------------------------------------------------------------
// Result
// ----------------------------------------------------------------------------

function ResultPanel({
  matchId,
  initial,
}: {
  matchId: string
  initial: { homeGoals: number; awayGoals: number; status: 'Finished' | 'Awarded' }
}) {
  const setResult = useSetMatchResult()
  const [success, setSuccess] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<ResultValues>({
    resolver: zodResolver(resultSchema),
    defaultValues: {
      homeGoals: initial.homeGoals,
      awayGoals: initial.awayGoals,
      status: initial.status,
      reason: '',
    },
  })

  const onValid = async (values: ResultValues) => {
    setSuccess(null)
    try {
      const r = await setResult.mutateAsync({ matchId, ...values })
      setSuccess(`Mentve. ${r.tipsScored} tipp értékelve, összesen ${r.totalPoints} pont.`)
    } catch (e) {
      setError('root', { message: errorMessage(e) })
    }
  }

  return (
    <section className="border-2 border-border-strong bg-elevated p-5 space-y-4">
      <h2 className="text-xs font-mono uppercase tracking-[0.2em] text-fg-subtle">Eredmény</h2>
      <form onSubmit={handleSubmit(onValid)} className="space-y-4">
        <div className="flex items-end gap-3">
          <div className="flex-1">
            <label className="block text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle mb-1">
              Hazai
            </label>
            <input
              type="number"
              min={0}
              max={15}
              {...register('homeGoals', { valueAsNumber: true })}
              className="w-full border-2 border-border-strong px-3 py-2 text-2xl font-mono tabular-nums"
            />
            {errors.homeGoals && <p className="text-xs text-danger mt-1">{errors.homeGoals.message}</p>}
          </div>
          <span className="text-3xl font-black pb-3">:</span>
          <div className="flex-1">
            <label className="block text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle mb-1">
              Vendég
            </label>
            <input
              type="number"
              min={0}
              max={15}
              {...register('awayGoals', { valueAsNumber: true })}
              className="w-full border-2 border-border-strong px-3 py-2 text-2xl font-mono tabular-nums"
            />
            {errors.awayGoals && <p className="text-xs text-danger mt-1">{errors.awayGoals.message}</p>}
          </div>
        </div>

        <fieldset className="space-y-1">
          <legend className="text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle mb-1">
            Eredmény típusa
          </legend>
          <label className="flex items-center gap-2 font-mono text-sm">
            <input type="radio" value="Finished" {...register('status')} />
            Hivatalos (lejátszott)
          </label>
          <label className="flex items-center gap-2 font-mono text-sm">
            <input type="radio" value="Awarded" {...register('status')} />
            FIFA-megítélt (félbeszakadt vagy átadott meccs)
          </label>
        </fieldset>

        <div>
          <label className="block text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle mb-1">
            Megjegyzés (audit log)
          </label>
          <input
            type="text"
            maxLength={500}
            {...register('reason')}
            className="w-full border-2 border-border-strong px-3 py-2 font-mono text-sm"
          />
        </div>

        {errors.root && <p className="text-xs font-mono text-danger">{errors.root.message}</p>}
        {success && <p className="text-xs font-mono text-success">{success}</p>}

        <button
          type="submit"
          disabled={isSubmitting || setResult.isPending}
          className="w-full border-2 border-accent bg-accent text-on-accent py-3 text-sm font-mono uppercase tracking-[0.2em] hover:bg-accent-strong hover:border-accent disabled:opacity-40"
        >
          {isSubmitting || setResult.isPending ? 'mentés…' : 'Eredmény mentése'}
        </button>
      </form>
    </section>
  )
}

// ----------------------------------------------------------------------------
// Postpone
// ----------------------------------------------------------------------------

function PostponePanel({ matchId, currentKickoffUtc }: { matchId: string; currentKickoffUtc: string }) {
  const postpone = usePostponeMatch()
  const [pending, setPending] = useState<PostponeValues | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<PostponeValues>({
    resolver: zodResolver(postponeSchema),
    defaultValues: { newKickoffLocal: localDateTimeFromUtc(currentKickoffUtc), reason: '' },
  })

  const confirm = async () => {
    if (!pending) return
    setSubmitError(null)
    try {
      const newKickoffUtc = new Date(pending.newKickoffLocal).toISOString()
      const r = await postpone.mutateAsync({ matchId, newKickoffUtc, reason: pending.reason })
      setSuccess(`Új kezdés: ${formatBudapest(r.newKickoffUtc)}. Új határidő: ${formatBudapest(r.newDeadlineUtc)}.`)
    } catch (e) {
      setSubmitError(errorMessage(e))
    } finally {
      setPending(null)
    }
  }

  return (
    <section className="border-2 border-border-strong bg-elevated p-5 space-y-4">
      <h2 className="text-xs font-mono uppercase tracking-[0.2em] text-fg-subtle">Halasztás</h2>
      <form onSubmit={handleSubmit((v) => setPending(v))} className="space-y-3">
        <div>
          <label className="block text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle mb-1">
            Új kezdés (Budapest)
          </label>
          <input
            type="datetime-local"
            {...register('newKickoffLocal')}
            className="w-full border-2 border-border-strong px-3 py-2 font-mono text-sm"
          />
          {errors.newKickoffLocal && <p className="text-xs text-danger mt-1">{errors.newKickoffLocal.message}</p>}
        </div>
        <div>
          <label className="block text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle mb-1">
            Megjegyzés (audit log)
          </label>
          <input
            type="text"
            maxLength={500}
            {...register('reason')}
            className="w-full border-2 border-border-strong px-3 py-2 font-mono text-sm"
          />
        </div>
        {submitError && <p className="text-xs font-mono text-danger">{submitError}</p>}
        {success && <p className="text-xs font-mono text-success">{success}</p>}
        <button
          type="submit"
          disabled={postpone.isPending}
          className="w-full border-2 border-border-strong bg-elevated py-3 text-sm font-mono uppercase tracking-[0.2em] hover:bg-sunken disabled:opacity-40"
        >
          {postpone.isPending ? 'halasztás…' : 'Halasztás'}
        </button>
      </form>

      <ConfirmDialog
        open={pending !== null}
        title="Halasztás megerősítése"
        body={
          pending
            ? `Új kezdés: ${formatLocalDateTime(pending.newKickoffLocal)}. A tippek érvényben maradnak, a határidő az új kezdés −1h. A jokerek NEM kerülnek vissza.`
            : ''
        }
        confirmLabel="Halasztás"
        onConfirm={confirm}
        onCancel={() => setPending(null)}
      />
    </section>
  )
}

// ----------------------------------------------------------------------------
// Cancel
// ----------------------------------------------------------------------------

function CancelPanel({ matchId, disabled }: { matchId: string; disabled: boolean }) {
  const cancel = useCancelMatch()
  const [open, setOpen] = useState(false)
  const [reason, setReason] = useState('')
  const [success, setSuccess] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)

  const confirm = async () => {
    setSubmitError(null)
    try {
      const r = await cancel.mutateAsync({ matchId, reason })
      setSuccess(
        `Lemondva. ${r.scoredTipsCleared} értékelt tipp törölve, ${r.jokersRefunded} joker visszaadva.`,
      )
    } catch (e) {
      setSubmitError(errorMessage(e))
    } finally {
      setOpen(false)
    }
  }

  return (
    <section className="border-2 border-border-strong bg-elevated p-5 space-y-3">
      <h2 className="text-xs font-mono uppercase tracking-[0.2em] text-fg-subtle">Lemondás</h2>
      <p className="text-xs font-mono text-fg-default">
        A pontok törlődnek, a meccsen feltett jokerek visszakerülnek a felhasználók kvótájába. A tipp-sorok
        megmaradnak (történeti céllal).
      </p>
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
        type="button"
        onClick={() => setOpen(true)}
        disabled={disabled || cancel.isPending}
        className="w-full border-2 border-danger bg-elevated text-danger py-3 text-sm font-mono uppercase tracking-[0.2em] hover:bg-danger hover:text-on-accent disabled:opacity-40 disabled:cursor-not-allowed"
      >
        {disabled ? 'Már lemondva' : cancel.isPending ? 'lemondás…' : 'Mérkőzés lemondása'}
      </button>

      <ConfirmDialog
        open={open}
        title="Lemondás megerősítése"
        body="Biztosan lemondod a meccset? A pontok törlődnek, a jokerek visszakerülnek. Ez visszafordítható: új eredmény beírásával újraértékelhető."
        destructive
        confirmLabel="Lemondás"
        onConfirm={confirm}
        onCancel={() => setOpen(false)}
      />
    </section>
  )
}

// ----------------------------------------------------------------------------
// AI tipper force-run
// ----------------------------------------------------------------------------

function AiTipperPanel({
  matchId,
  eligible,
  status,
}: {
  matchId: string
  eligible: boolean
  status: string
}) {
  const run = useRunAiTipperForMatch()
  const [open, setOpen] = useState(false)
  const [reason, setReason] = useState('')
  const [success, setSuccess] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)

  const confirm = async () => {
    setSubmitError(null)
    setSuccess(null)
    try {
      const r = await run.mutateAsync({ matchId, reason: reason.trim() || null })
      setSuccess(
        `Kész. ${r.aiMembers} AI tag · próbálkozás: ${r.attempted} · sikeres: ${r.written} · 1–1 fallback: ${r.fallbacks} · kihagyva (már volt tipp): ${r.skipped}.`,
      )
    } catch (e) {
      setSubmitError(errorMessage(e))
    } finally {
      setOpen(false)
    }
  }

  return (
    <section className="border-2 border-border-strong bg-elevated p-5 space-y-3">
      <h2 className="text-xs font-mono uppercase tracking-[0.2em] text-fg-subtle">AI tippelők kényszerítése</h2>
      <p className="text-xs font-mono text-fg-default">
        Az ütemezett ablak (T-2h / T-90m / T-1h) megkerülésével azonnal lefuttatja az AI tippet minden Locked
        csapat AI tagjára. Sikeres OpenAI-hívásnál valós tipp, hiba esetén determinisztikus 1–1 fallback kerül
        be. Csak Scheduled státuszú, jövőbeli kezdésű meccsen elérhető.
      </p>
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
        type="button"
        onClick={() => setOpen(true)}
        disabled={!eligible || run.isPending}
        className="w-full border-2 border-accent bg-elevated text-accent py-3 text-sm font-mono uppercase tracking-[0.2em] hover:bg-accent hover:text-on-accent disabled:opacity-40 disabled:cursor-not-allowed"
      >
        {!eligible
          ? `Nem futtatható (${STATUS_LABEL_HU[status] ?? status})`
          : run.isPending
          ? 'futtatás…'
          : 'AI tippelés kényszerítése'}
      </button>

      <ConfirmDialog
        open={open}
        title="AI tippelés kényszerítése"
        body="Minden Locked csapat AI tagjára azonnal lefut a tippelés. Akinek már van tippje erre a meccsre, nem írunk felül. OpenAI hiba esetén 1–1 fallback kerül be. Folytatod?"
        confirmLabel="Futtatás"
        onConfirm={confirm}
        onCancel={() => setOpen(false)}
      />
    </section>
  )
}

// ----------------------------------------------------------------------------
// Helpers
// ----------------------------------------------------------------------------

/// Convert ISO UTC timestamp → yyyy-MM-ddTHH:mm string for <input type="datetime-local">.
/// Renders in Budapest local time so the admin types/sees Hungarian wall-clock time.
function localDateTimeFromUtc(iso: string): string {
  const d = new Date(iso)
  const fmt = new Intl.DateTimeFormat('sv-SE', {
    timeZone: 'Europe/Budapest',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
  // sv-SE returns "YYYY-MM-DD HH:mm"; datetime-local wants "YYYY-MM-DDTHH:mm".
  return fmt.format(d).replace(' ', 'T')
}

function formatLocalDateTime(local: string): string {
  // The form value is local wall-clock time (browser timezone, which we expect to be Budapest).
  // Format it for display with the same shape as formatBudapest for consistency.
  return new Date(local).toLocaleString('hu-HU', {
    timeZone: 'Europe/Budapest',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}
