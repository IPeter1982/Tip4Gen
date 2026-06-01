import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate, useParams } from 'react-router'
import { z } from 'zod'
import { useMatch, useMatchTips, useSubmitTip } from '../api/hooks'
import { ApiError } from '../api/errors'
import type { MatchListItem, MatchTip } from '../api/types'
import { TeamLabel } from '../components/TeamLabel'
import {
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
    <div className="max-w-xl mx-auto px-6 py-10 space-y-6">
      <Link
        to="/matches"
        className="text-xs font-mono uppercase tracking-[0.15em] text-stone-500 hover:text-stone-900"
      >
        ← vissza
      </Link>

      {isLoading && <p className="font-mono text-stone-500">betöltés…</p>}
      {error && (
        <p className="border-2 border-red-700 bg-red-50 p-4 font-mono text-sm text-red-800">
          ⚠ {error instanceof Error ? error.message : String(error)}
        </p>
      )}

      {match && (
        <>
          <TipForm
            match={match}
            submitting={submit.isPending}
            onSubmit={async (values) => {
              try {
                await submit.mutateAsync({ matchId: match.id, ...values })
                navigate('/matches')
              } catch (e) {
                throw e
              }
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
      <p className="text-xs font-mono uppercase tracking-[0.15em] text-stone-500">
        A többiek tippjei a határidő után láthatóak.
      </p>
    )
  }
  if (data.tipCount === 0) {
    return (
      <p className="text-xs font-mono uppercase tracking-[0.15em] text-stone-500">
        Nem érkezett tipp erre a meccsre.
      </p>
    )
  }

  return (
    <section className="border-2 border-stone-900 bg-white p-5 space-y-3">
      <h2 className="text-xs font-mono uppercase tracking-[0.2em] text-stone-500">
        Mindenki tippje
      </h2>
      <ul className="divide-y-2 divide-stone-200">
        {data.tips.map((tip) => (
          <li key={tipKey(tip)} className="py-2 space-y-1">
            <div className="flex items-center gap-3 font-mono text-sm">
              <span className="flex-1 truncate">{tip.displayName}</span>
              {tip.isAi && (
                <span className="bg-stone-900 text-white px-2 py-0.5 text-xs uppercase tracking-[0.15em]">
                  AI
                </span>
              )}
              {tip.isAiFallback && (
                <span className="border-2 border-stone-400 text-stone-600 px-2 py-0.5 text-xs uppercase tracking-[0.15em]">
                  Fallback
                </span>
              )}
              {tip.joker && (
                <span className="border-2 border-orange-600 text-orange-700 px-2 py-0.5 text-xs uppercase tracking-[0.15em]">
                  Joker
                </span>
              )}
              <span className="tabular-nums font-semibold">
                {tip.homeGoals}:{tip.awayGoals}
              </span>
            </div>
            {tip.reasoning && (
              <p className="text-xs italic text-stone-600 pl-1">{tip.reasoning}</p>
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
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      homeGoals: match.myTip?.homeGoals ?? 0,
      awayGoals: match.myTip?.awayGoals ?? 0,
      joker: match.myTip?.joker ?? false,
    },
  })

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
      <header className="border-2 border-stone-900 bg-white p-5">
        <p className="text-xs font-mono uppercase tracking-[0.15em] text-stone-500">
          {STAGE_LABEL_HU[match.stage] ?? match.stage}
          {match.groupCode ? ` ${match.groupCode}` : ''}
        </p>
        <h1 className="text-2xl font-black uppercase tracking-tight mt-2 flex items-center gap-2 flex-wrap">
          <TeamLabel team={match.homeTeam} size="md" />
          <span className="text-stone-400">vs</span>
          <TeamLabel team={match.awayTeam} size="md" />
        </h1>
        <p className="text-xs font-mono text-stone-500 mt-2">
          {formatBudapest(match.kickoffUtc)} CET
        </p>
        <p className="text-xs font-mono text-stone-500 mt-1">
          határidő:{' '}
          <span className={deadlinePassed ? 'text-red-700' : 'text-stone-900'}>
            {deadlinePassed ? 'lejárt' : formatCountdown(match.deadlineUtc, now)}
          </span>
        </p>
      </header>

      <section className="border-2 border-stone-900 bg-white p-5 space-y-5">
        <div className="flex items-end justify-between gap-4">
          <ScoreInput
            label={<TeamLabel team={match.homeTeam} />}
            disabled={formDisabled}
            error={errors.homeGoals?.message}
            {...register('homeGoals', { valueAsNumber: true })}
          />
          <span className="text-3xl font-black pb-3">:</span>
          <ScoreInput
            label={<TeamLabel team={match.awayTeam} />}
            disabled={formDisabled}
            error={errors.awayGoals?.message}
            {...register('awayGoals', { valueAsNumber: true })}
          />
        </div>

        <label className="flex items-center gap-3 cursor-pointer">
          <input
            type="checkbox"
            disabled={formDisabled || knockout}
            {...register('joker')}
            className="size-5 border-2 border-stone-900"
          />
          <span className="text-sm font-mono uppercase tracking-[0.15em]">
            Joker
            {knockout && (
              <span className="ml-2 text-stone-400 normal-case">
                (csak csoportmérkőzésen)
              </span>
            )}
          </span>
        </label>
        {errors.joker && (
          <p className="text-xs font-mono text-red-700">{errors.joker.message}</p>
        )}

        {errors.root && (
          <p className="text-xs font-mono text-red-700">{errors.root.message}</p>
        )}

        <button
          type="submit"
          disabled={formDisabled || isSubmitting || submitting}
          className="w-full border-2 border-stone-900 bg-stone-900 text-white py-3 text-sm font-mono uppercase tracking-[0.2em] hover:bg-orange-600 hover:border-orange-600 disabled:opacity-40 disabled:cursor-not-allowed"
        >
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
  label: React.ReactNode
  error?: string
}

const ScoreInput = function ScoreInput({ label, error, ...inputProps }: ScoreInputProps) {
  return (
    <div className="flex-1">
      <label className="block text-xs font-mono uppercase tracking-[0.15em] text-stone-500 mb-1">
        {label}
      </label>
      <input
        type="number"
        min={0}
        max={15}
        step={1}
        inputMode="numeric"
        {...inputProps}
        className="w-full border-2 border-stone-900 px-3 py-2 text-2xl font-mono tabular-nums focus:outline-none focus:bg-orange-50 disabled:bg-stone-100"
      />
      {error && <p className="text-xs font-mono text-red-700 mt-1">{error}</p>}
    </div>
  )
}
