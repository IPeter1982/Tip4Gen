import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { z } from 'zod'
import { useLongTips, useNationalTeams, useSubmitLongTips } from '../api/hooks'
import { ApiError } from '../api/errors'
import { TeamSelect } from '../components/TeamSelect'
import { formatBudapest } from '../lib/format'

const schema = z
  .object({
    winnerTeamId: z.string().optional(),
    topScorerName: z
      .string()
      .max(120, 'Maximum 120 karakter')
      .optional(),
  })
  .refine(
    (v) => (v.winnerTeamId && v.winnerTeamId.length > 0) || (v.topScorerName && v.topScorerName.trim().length > 0),
    { message: 'Adj meg legalább egy tippet.', path: ['root'] },
  )

type FormValues = z.infer<typeof schema>

function reasonMessage(reason: string): string {
  switch (reason) {
    case 'Locked':
      return 'A hosszú távú tippek lezárultak (kezdő mérkőzés rajt).'
    case 'NothingProvided':
      return 'Adj meg legalább egy tippet (győztes vagy gólkirály).'
    case 'PlayerNameTooLong':
      return 'A gólkirály neve túl hosszú (max 120 karakter).'
    case 'PlayerNameBlank':
      return 'A gólkirály neve nem lehet üres.'
    default:
      return 'Nem sikerült elmenteni a tippet.'
  }
}

export function LongTips() {
  const longTips = useLongTips()
  const teams = useNationalTeams()
  const submit = useSubmitLongTips()

  const {
    register,
    handleSubmit,
    setError,
    reset,
    control,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { winnerTeamId: '', topScorerName: '' },
  })

  // Hydrate the form when the GET resolves.
  useEffect(() => {
    if (longTips.data) {
      reset({
        winnerTeamId: longTips.data.winnerTeamId ?? '',
        topScorerName: longTips.data.topScorerName ?? '',
      })
    }
  }, [longTips.data, reset])

  const locked = longTips.data?.locked ?? false

  const onValid = async (values: FormValues) => {
    try {
      await submit.mutateAsync({
        winnerTeamId: values.winnerTeamId ? values.winnerTeamId : null,
        topScorerName: values.topScorerName?.trim() ? values.topScorerName.trim() : null,
      })
    } catch (e) {
      if (e instanceof ApiError && e.reason) {
        setError('root', { message: reasonMessage(e.reason) })
      } else if (e instanceof ApiError) {
        setError('root', { message: e.message })
      } else {
        setError('root', { message: e instanceof Error ? e.message : String(e) })
      }
    }
  }

  return (
    <div className="max-w-xl mx-auto px-6 py-10 space-y-6">
      <header>
        <p className="text-xs font-mono uppercase tracking-[0.2em] text-accent">
          Hosszú távú tippek
        </p>
        <h1 className="text-4xl font-black uppercase tracking-tight mt-2">
          Győztes &amp; Gólkirály
        </h1>
      </header>

      {longTips.isLoading && <p className="font-mono text-fg-subtle">betöltés…</p>}
      {longTips.error && (
        <p className="border border-danger bg-danger/10 p-4 font-mono text-sm text-danger">
          ⚠ {longTips.error instanceof Error ? longTips.error.message : String(longTips.error)}
        </p>
      )}

      {longTips.data && (
        <>
          <section
            className={`border p-4 font-mono text-xs uppercase tracking-[0.15em] ${
              locked
                ? 'border-accent bg-accent text-on-accent'
                : 'border-border-subtle bg-elevated text-fg-muted'
            }`}
          >
            zár:{' '}
            <span className={locked ? 'text-accent-soft' : 'text-fg-default'}>
              {formatBudapest(longTips.data.lockUtc)}
            </span>
            {locked && <span className="ml-3">· LEZÁRVA</span>}
          </section>

          <form onSubmit={handleSubmit(onValid)} className="space-y-5">
            <section className="border border-border-strong bg-elevated p-5 space-y-3">
              <label
                htmlFor="winnerTeamId"
                className="block text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle"
              >
                Győztes csapat
              </label>
              <Controller
                control={control}
                name="winnerTeamId"
                render={({ field }) => (
                  <TeamSelect
                    id="winnerTeamId"
                    teams={teams.data ?? []}
                    value={field.value || null}
                    onChange={(id) => field.onChange(id ?? '')}
                    disabled={locked || teams.isLoading}
                  />
                )}
              />
              {longTips.data.winnerSubmittedAt && (
                <p className="text-xs font-mono text-fg-subtle">
                  utolsó módosítás: {formatBudapest(longTips.data.winnerSubmittedAt)}
                </p>
              )}
            </section>

            <section className="border border-border-strong bg-elevated p-5 space-y-3">
              <label
                htmlFor="topScorerName"
                className="block text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle"
              >
                Gólkirály neve
              </label>
              <input
                id="topScorerName"
                type="text"
                disabled={locked}
                maxLength={120}
                placeholder="pl. Kylian Mbappe"
                {...register('topScorerName')}
                className="w-full border border-border-strong px-3 py-2 font-mono disabled:bg-sunken"
              />
              {errors.topScorerName && (
                <p className="text-xs font-mono text-danger">{errors.topScorerName.message}</p>
              )}
              {longTips.data.topScorerSubmittedAt && (
                <p className="text-xs font-mono text-fg-subtle">
                  utolsó módosítás: {formatBudapest(longTips.data.topScorerSubmittedAt)}
                </p>
              )}
            </section>

            {errors.root && (
              <p className="text-xs font-mono text-danger">{errors.root.message}</p>
            )}

            <button
              type="submit"
              disabled={locked || isSubmitting || submit.isPending}
              className="w-full border border-accent bg-accent text-on-accent py-3 text-sm font-mono uppercase tracking-[0.2em] hover:bg-accent-strong hover:border-accent disabled:opacity-40 disabled:cursor-not-allowed"
            >
              {submit.isPending || isSubmitting ? 'küldés…' : 'Mentés'}
            </button>
          </form>
        </>
      )}
    </div>
  )
}
