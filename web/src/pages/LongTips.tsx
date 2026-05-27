import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { useLongTips, useNationalTeams, useSubmitLongTips } from '../api/hooks'
import { ApiError } from '../api/errors'
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
        <p className="text-xs font-mono uppercase tracking-[0.2em] text-orange-600">
          Hosszú távú tippek
        </p>
        <h1 className="text-4xl font-black uppercase tracking-tight mt-2">
          Győztes &amp; Gólkirály
        </h1>
      </header>

      {longTips.isLoading && <p className="font-mono text-stone-500">betöltés…</p>}
      {longTips.error && (
        <p className="border-2 border-red-700 bg-red-50 p-4 font-mono text-sm text-red-800">
          ⚠ {longTips.error instanceof Error ? longTips.error.message : String(longTips.error)}
        </p>
      )}

      {longTips.data && (
        <>
          <section
            className={`border-2 p-4 font-mono text-xs uppercase tracking-[0.15em] ${
              locked
                ? 'border-stone-900 bg-stone-900 text-white'
                : 'border-stone-300 bg-white text-stone-600'
            }`}
          >
            zár:{' '}
            <span className={locked ? 'text-orange-300' : 'text-stone-900'}>
              {formatBudapest(longTips.data.lockUtc)}
            </span>
            {locked && <span className="ml-3">· LEZÁRVA</span>}
          </section>

          <form onSubmit={handleSubmit(onValid)} className="space-y-5">
            <section className="border-2 border-stone-900 bg-white p-5 space-y-3">
              <label
                htmlFor="winnerTeamId"
                className="block text-xs font-mono uppercase tracking-[0.15em] text-stone-500"
              >
                Győztes csapat
              </label>
              <select
                id="winnerTeamId"
                disabled={locked || teams.isLoading}
                {...register('winnerTeamId')}
                className="w-full border-2 border-stone-900 px-3 py-2 font-mono disabled:bg-stone-100"
              >
                <option value="">— válassz csapatot —</option>
                {teams.data?.map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.name}
                    {t.code ? ` (${t.code})` : ''}
                  </option>
                ))}
              </select>
              {longTips.data.winnerSubmittedAt && (
                <p className="text-xs font-mono text-stone-500">
                  utolsó módosítás: {formatBudapest(longTips.data.winnerSubmittedAt)}
                </p>
              )}
            </section>

            <section className="border-2 border-stone-900 bg-white p-5 space-y-3">
              <label
                htmlFor="topScorerName"
                className="block text-xs font-mono uppercase tracking-[0.15em] text-stone-500"
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
                className="w-full border-2 border-stone-900 px-3 py-2 font-mono disabled:bg-stone-100"
              />
              {errors.topScorerName && (
                <p className="text-xs font-mono text-red-700">{errors.topScorerName.message}</p>
              )}
              {longTips.data.topScorerSubmittedAt && (
                <p className="text-xs font-mono text-stone-500">
                  utolsó módosítás: {formatBudapest(longTips.data.topScorerSubmittedAt)}
                </p>
              )}
            </section>

            {errors.root && (
              <p className="text-xs font-mono text-red-700">{errors.root.message}</p>
            )}

            <button
              type="submit"
              disabled={locked || isSubmitting || submit.isPending}
              className="w-full border-2 border-stone-900 bg-stone-900 text-white py-3 text-sm font-mono uppercase tracking-[0.2em] hover:bg-orange-600 hover:border-orange-600 disabled:opacity-40 disabled:cursor-not-allowed"
            >
              {submit.isPending || isSubmitting ? 'küldés…' : 'Mentés'}
            </button>
          </form>
        </>
      )}
    </div>
  )
}
