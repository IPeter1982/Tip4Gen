import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate, useParams } from 'react-router'
import { z } from 'zod'
import {
  AlertTriangle,
  ArrowLeft,
  CheckCircle2,
  Clock,
  Send,
  Sparkles,
  Star,
  Target,
  Timer,
  Trophy,
  XCircle,
} from 'lucide-react'
import { useMatch, useMatchTips, useSubmitTip } from '../api/hooks'
import { ApiError } from '../api/errors'
import type { MatchListItem, MatchTip, UserTipScore } from '../api/types'
import { TeamLabel } from '../components/TeamLabel'
import {
  CATEGORY_LABEL_HU,
  STAGE_LABEL_HU,
  formatBudapest,
  formatCountdown,
} from '../lib/format'

const schema = z.object({
  homeGoals: z
    .number({ message: 'Adj meg egy számot' })
    .int('Egész szám legyen')
    .min(0, 'Nem lehet negatív')
    .max(15, 'Maximum 15'),
  awayGoals: z
    .number({ message: 'Adj meg egy számot' })
    .int('Egész szám legyen')
    .min(0, 'Nem lehet negatív')
    .max(15, 'Maximum 15'),
  joker: z.boolean(),
})

type FormValues = z.infer<typeof schema>

function isKnockout(stage: string): boolean {
  return stage !== 'Group'
}

function reasonToFieldError(reason: string): {
  field?: keyof FormValues
  message: string
} {
  switch (reason) {
    case 'DeadlinePassed':
      return { message: 'A tippelési határidő lejárt.' }
    case 'ScoreOutOfRange':
      return { field: 'homeGoals', message: 'Az érvényes tartomány 0–15 gól.' }
    case 'JokerNotAllowedOnKnockoutMatch':
      return { field: 'joker', message: 'Joker csak csoportmérkőzésen használható.' }
    case 'JokerQuotaExceeded':
      return { field: 'joker', message: 'Már elhasználtad mind a 3 jokered.' }
    default:
      return { message: 'A tipp elutasítva.' }
  }
}

export function TipSubmit() {
  const { matchId } = useParams()
  const navigate = useNavigate()
  const { data: match, isLoading, error } = useMatch(matchId)
  const submit = useSubmitTip()

  return (
    <div className="max-w-2xl mx-auto px-6 py-10 space-y-6">
      <Link
        to="/matches"
        className="inline-flex items-center gap-1.5 text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle hover:text-accent transition"
      >
        <ArrowLeft size={14} />
        vissza
      </Link>

      {isLoading && <p className="font-mono text-fg-subtle">betöltés…</p>}
      {error && (
        <p className="rounded-xl border border-danger/40 bg-danger/10 p-4 font-mono text-sm text-danger flex items-center gap-2">
          <AlertTriangle size={16} />
          {error instanceof Error ? error.message : String(error)}
        </p>
      )}

      {match && (
        <>
          <TipForm
            match={match}
            submitting={submit.isPending}
            onSubmit={async (values) => {
              await submit.mutateAsync({ matchId: match.id, ...values })
              navigate('/matches')
            }}
          />
          <AllTipsPanel matchId={match.id} />
        </>
      )}
    </div>
  )
}

function AllTipsPanel({ matchId }: { matchId: string }) {
  const { data, isLoading } = useMatchTips(matchId)

  if (isLoading || !data) return null
  if (!data.deadlinePassed) {
    return (
      <p className="text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle">
        A többiek tippjei a határidő után láthatóak.
      </p>
    )
  }
  if (data.tipCount === 0) {
    return (
      <p className="text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle">
        Nem érkezett tipp erre a meccsre.
      </p>
    )
  }

  return (
    <section className="rounded-2xl border border-border-subtle bg-elevated p-5 space-y-3">
      <h2 className="text-xs font-mono uppercase tracking-[0.2em] text-fg-subtle inline-flex items-center gap-1.5">
        <Target size={12} className="text-accent" />
        Mindenki tippje
      </h2>
      <ul className="divide-y divide-border-subtle">
        {data.tips.map((tip) => (
          <li key={tipKey(tip)} className="py-3 space-y-1.5">
            <div className="flex items-center gap-2 flex-wrap font-mono text-sm">
              <span className="flex-1 truncate text-fg-default">{tip.displayName}</span>
              {tip.isAi && (
                <span className="inline-flex items-center gap-1 bg-sunken text-fg-muted px-2 py-0.5 text-[10px] uppercase tracking-[0.15em] rounded">
                  <Sparkles size={10} />
                  AI
                </span>
              )}
              {tip.isAiFallback && (
                <span className="px-2 py-0.5 text-[10px] uppercase tracking-[0.15em] rounded border border-border-strong text-fg-muted">
                  Fallback
                </span>
              )}
              {tip.joker && (
                <span className="inline-flex items-center gap-1 px-2 py-0.5 text-[10px] uppercase tracking-[0.15em] rounded bg-accent-soft text-accent-strong">
                  <Star size={10} className="fill-current" />
                  Joker
                </span>
              )}
              <span className="tabular-nums font-bold text-fg-default px-2 py-0.5 rounded bg-sunken">
                {tip.homeGoals}:{tip.awayGoals}
              </span>
            </div>
            {tip.score && <TipPointsLine score={tip.score} />}
            {tip.reasoning && (
              <p className="text-xs italic text-fg-subtle pl-1">{tip.reasoning}</p>
            )}
          </li>
        ))}
      </ul>
    </section>
  )
}

function tipKey(tip: MatchTip): string {
  return tip.userId ?? tip.teamMemberId ?? tip.displayName
}

function TipPointsLine({ score }: { score: UserTipScore }) {
  const isZero = score.finalPoints === 0
  const label = CATEGORY_LABEL_HU[score.category] ?? score.category
  return (
    <p className="text-xs font-mono pl-1 flex items-center gap-2 flex-wrap">
      {isZero ? (
        <XCircle size={14} className="text-fg-subtle" />
      ) : (
        <CheckCircle2 size={14} className="text-success" />
      )}
      <span
        className={
          isZero
            ? 'text-fg-subtle'
            : 'text-success font-bold tabular-nums'
        }
      >
        {isZero ? '0 pt' : `+${score.finalPoints} pt`}
      </span>
      <span className="text-fg-subtle">
        {label} · ×{score.multiplier}
        {score.jokerApplied && ' · ×2 (joker)'}
      </span>
    </p>
  )
}

function TipForm({
  match,
  submitting,
  onSubmit,
}: {
  match: MatchListItem
  submitting: boolean
  onSubmit: (values: FormValues) => Promise<void>
}) {
  const knockout = isKnockout(match.stage)
  const [now, setNow] = useState(() => new Date())
  useEffect(() => {
    const id = setInterval(() => setNow(new Date()), 1000)
    return () => clearInterval(id)
  }, [])
  const deadlineMs = new Date(match.deadlineUtc).getTime()
  const deadlinePassed = deadlineMs <= now.getTime()
  const formDisabled = deadlinePassed || match.status !== 'Scheduled'

  const {
    register,
    handleSubmit,
    setError,
    setValue,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      homeGoals: match.myTip?.homeGoals ?? 0,
      awayGoals: match.myTip?.awayGoals ?? 0,
      joker: match.myTip?.joker ?? false,
    },
  })

  const joker = watch('joker')

  const onValid = async (values: FormValues) => {
    try {
      await onSubmit(values)
    } catch (e) {
      if (e instanceof ApiError && e.reason) {
        const mapped = reasonToFieldError(e.reason)
        if (mapped.field) {
          setError(mapped.field, { message: mapped.message })
        } else {
          setError('root', { message: mapped.message })
        }
      } else {
        setError('root', { message: e instanceof Error ? e.message : String(e) })
      }
    }
  }

  return (
    <form onSubmit={handleSubmit(onValid)} className="space-y-5">
      {/* Hero scoreboard card */}
      <section className="rounded-3xl border border-border-strong bg-elevated p-6 sm:p-8 space-y-6 shadow-xl">
        <header className="flex items-start justify-between gap-3 flex-wrap text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle">
          <span className="inline-flex items-center gap-1.5 text-accent">
            <Trophy size={12} />
            {STAGE_LABEL_HU[match.stage] ?? match.stage}
            {match.groupCode ? ` ${match.groupCode}` : ''}
          </span>
          <span className="inline-flex items-center gap-1.5">
            <Clock size={12} />
            {formatBudapest(match.kickoffUtc)} CET
          </span>
        </header>

        <div className="grid grid-cols-[1fr_auto_1fr] items-center gap-3 sm:gap-4">
          <div className="text-right text-sm sm:text-base font-bold text-fg-default truncate">
            <TeamLabel team={match.homeTeam} size="md" />
          </div>
          <div className="flex items-center gap-2 sm:gap-3">
            <ScoreInput
              ariaLabel={`${match.homeTeam.name} gólok`}
              disabled={formDisabled}
              {...register('homeGoals', { valueAsNumber: true })}
            />
            <span className="text-3xl sm:text-5xl font-bold text-fg-subtle">:</span>
            <ScoreInput
              ariaLabel={`${match.awayTeam.name} gólok`}
              disabled={formDisabled}
              {...register('awayGoals', { valueAsNumber: true })}
            />
          </div>
          <div className="text-left text-sm sm:text-base font-bold text-fg-default truncate">
            <TeamLabel team={match.awayTeam} size="md" />
          </div>
        </div>

        {(errors.homeGoals || errors.awayGoals) && (
          <p className="text-center text-xs font-mono text-danger">
            {errors.homeGoals?.message ?? errors.awayGoals?.message}
          </p>
        )}

        <div className="flex items-center justify-center text-xs font-mono text-fg-subtle">
          <Timer size={14} className="mr-1.5" />
          határidő:&nbsp;
          <span className={deadlinePassed ? 'text-danger' : 'text-fg-default tabular-nums'}>
            {deadlinePassed ? 'lejárt' : formatCountdown(match.deadlineUtc, now)}
          </span>
        </div>

        {/* Joker as pill button */}
        <div className="flex justify-center">
          <input type="hidden" {...register('joker')} />
          <button
            type="button"
            disabled={formDisabled || knockout}
            onClick={() => setValue('joker', !joker, { shouldDirty: true })}
            aria-pressed={joker}
            className={`inline-flex items-center gap-2 rounded-full border px-4 py-2 text-xs font-mono uppercase tracking-[0.15em] transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring-focus disabled:opacity-40 disabled:cursor-not-allowed ${
              joker
                ? 'bg-accent-soft text-accent-strong border-accent shadow-[0_0_18px_-4px_var(--color-accent-glow)]'
                : 'bg-elevated border-border-strong text-fg-muted hover:border-accent hover:text-accent'
            }`}
            title={knockout ? 'Joker csak csoportmérkőzésen' : joker ? 'Joker aktiválva' : 'Joker bekapcsolása'}
          >
            <Star size={14} className={joker ? 'fill-current' : ''} />
            Joker {joker ? 'aktív' : ''}
          </button>
        </div>
        {knockout && (
          <p className="text-center text-[11px] font-mono text-fg-subtle -mt-2">
            Joker csak csoportmérkőzésen használható.
          </p>
        )}
        {errors.joker && (
          <p className="text-center text-xs font-mono text-danger">{errors.joker.message}</p>
        )}

        {errors.root && (
          <p className="text-center text-xs font-mono text-danger">{errors.root.message}</p>
        )}

        <button
          type="submit"
          disabled={formDisabled || isSubmitting || submitting}
          className="w-full inline-flex items-center justify-center gap-2 rounded-xl bg-accent text-on-accent py-3 text-sm font-mono uppercase tracking-[0.2em] font-bold transition hover:bg-accent-strong disabled:opacity-40 disabled:cursor-not-allowed focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring-focus"
        >
          <Send size={16} />
          {submitting || isSubmitting
            ? 'küldés…'
            : match.myTip
              ? 'Tipp módosítása'
              : 'Tipp beküldése'}
        </button>
      </section>
    </form>
  )
}

type ScoreInputProps = React.InputHTMLAttributes<HTMLInputElement> & {
  ariaLabel: string
}

function ScoreInput({ ariaLabel, ...inputProps }: ScoreInputProps) {
  return (
    <input
      type="number"
      min={0}
      max={15}
      step={1}
      inputMode="numeric"
      aria-label={ariaLabel}
      {...inputProps}
      className="w-20 h-20 sm:w-24 sm:h-24 rounded-2xl bg-sunken text-center text-4xl sm:text-5xl font-mono font-bold tabular-nums text-fg-default focus:outline-none focus:ring-4 focus:ring-ring-focus focus:bg-elevated disabled:opacity-60 transition"
    />
  )
}
