import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { Trophy } from 'lucide-react'
import { z } from 'zod'
import {
  useAllLongTips,
  useLongTips,
  useNationalTeams,
  usePlayers,
  useSubmitLongTips,
} from '../api/hooks'
import { ApiError } from '../api/errors'
import type { LongTipPublicEntry } from '../api/types'
import { Avatar } from '../components/Avatar'
import { PlayerSelect } from '../components/PlayerSelect'
import { TeamFlag } from '../components/TeamFlag'
import { TeamLabel } from '../components/TeamLabel'
import { TeamSelect } from '../components/TeamSelect'
import { formatBudapest } from '../lib/format'

const schema = z
  .object({
    winnerTeamId: z.string().optional(),
    topScorerPlayerId: z.string().optional(),
  })
  .refine(
    (v) =>
      (v.winnerTeamId && v.winnerTeamId.length > 0) ||
      (v.topScorerPlayerId && v.topScorerPlayerId.length > 0),
    { message: 'Adj meg legalább egy tippet.', path: ['root'] },
  )

type FormValues = z.infer<typeof schema>

function reasonMessage(reason: string): string {
  switch (reason) {
    case 'Locked':
      return 'A végső győztes tippek lezárultak (kezdő mérkőzés rajt).'
    case 'NothingProvided':
      return 'Adj meg legalább egy tippet (győztes vagy gólkirály).'
    case 'TopScorerPlayerNotFound':
      return 'A kiválasztott játékos már nem létezik. Frissítsd az oldalt.'
    case 'TeamNotFound':
      return 'A kiválasztott csapat már nem létezik. Frissítsd az oldalt.'
    default:
      return 'Nem sikerült elmenteni a tippet.'
  }
}

export function LongTips() {
  const longTips = useLongTips()
  const teams = useNationalTeams()
  const players = usePlayers()
  const submit = useSubmitLongTips()

  const {
    handleSubmit,
    setError,
    reset,
    control,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { winnerTeamId: '', topScorerPlayerId: '' },
  })

  // Hydrate the form when the GET resolves.
  useEffect(() => {
    if (longTips.data) {
      reset({
        winnerTeamId: longTips.data.winnerTeamId ?? '',
        topScorerPlayerId: longTips.data.topScorerPlayerId ?? '',
      })
    }
  }, [longTips.data, reset])

  const locked = longTips.data?.locked ?? false

  const onValid = async (values: FormValues) => {
    try {
      await submit.mutateAsync({
        winnerTeamId: values.winnerTeamId ? values.winnerTeamId : null,
        topScorerPlayerId: values.topScorerPlayerId ? values.topScorerPlayerId : null,
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
        <p className="inline-flex items-center gap-1.5 text-xs font-mono uppercase tracking-[0.2em] text-accent">
          <Trophy size={14} />
          Végső győztes
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
                htmlFor="topScorerPlayerId"
                className="block text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle"
              >
                Gólkirály
              </label>
              <Controller
                control={control}
                name="topScorerPlayerId"
                render={({ field }) => (
                  <PlayerSelect
                    id="topScorerPlayerId"
                    players={players.data ?? []}
                    value={field.value || null}
                    onChange={(id) => field.onChange(id ?? '')}
                    disabled={locked || players.isLoading}
                    placeholder={
                      players.isLoading ? 'betöltés…' : '— keress játékost (név vagy ország) —'
                    }
                  />
                )}
              />
              {longTips.data.topScorerPlayerName && (
                <p className="text-xs font-mono text-fg-subtle inline-flex items-center gap-1.5">
                  jelenlegi: <TeamFlag code={longTips.data.topScorerTeamCode} size="sm" />
                  <span className="text-fg-default">{longTips.data.topScorerPlayerName}</span>
                </p>
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

          {locked && <PublicLongTipsSection />}
        </>
      )}
    </div>
  )
}

function PublicLongTipsSection() {
  const all = useAllLongTips(true)

  return (
    <section className="space-y-3">
      <h2 className="text-xs font-mono uppercase tracking-[0.2em] text-fg-subtle">
        Mindenki tippje
      </h2>

      {all.isLoading && <p className="font-mono text-fg-subtle">betöltés…</p>}
      {all.error && (
        <p className="border border-danger bg-danger/10 p-4 font-mono text-sm text-danger">
          ⚠ {all.error instanceof Error ? all.error.message : String(all.error)}
        </p>
      )}

      {all.data && all.data.items.length === 0 && (
        <p className="border border-border-subtle bg-elevated p-6 text-center font-mono text-fg-subtle">
          Még senki nem tippelt.
        </p>
      )}

      {all.data && all.data.items.length > 0 && (
        <ul className="divide-y divide-border-subtle border border-border-subtle bg-elevated">
          {all.data.items.map((entry) => (
            <PublicLongTipRow key={entry.userId} entry={entry} />
          ))}
        </ul>
      )}
    </section>
  )
}

function PublicLongTipRow({ entry }: { entry: LongTipPublicEntry }) {
  return (
    <li className="flex items-center gap-3 flex-wrap p-3">
      <div className="flex items-center gap-2 min-w-0 flex-1">
        <Avatar
          userId={entry.userId}
          displayName={entry.displayName}
          version={entry.avatarVersion}
          size={28}
        />
        <span className="truncate text-sm text-fg-default">{entry.displayName}</span>
      </div>
      <div className="flex items-center gap-3 flex-wrap text-xs font-mono">
        <span className="inline-flex items-center gap-1.5">
          <span className="text-fg-subtle uppercase tracking-[0.15em]">győztes:</span>
          {entry.winnerTeamId && entry.winnerTeamName ? (
            <TeamLabel team={{ name: entry.winnerTeamName, code: entry.winnerTeamCode }} />
          ) : (
            <span className="text-fg-subtle italic">nincs tipp</span>
          )}
        </span>
        <span className="inline-flex items-center gap-1.5">
          <span className="text-fg-subtle uppercase tracking-[0.15em]">gólkirály:</span>
          {entry.topScorerPlayerId && entry.topScorerPlayerName ? (
            <>
              <TeamFlag code={entry.topScorerTeamCode} size="sm" />
              <span className="text-fg-default">{entry.topScorerPlayerName}</span>
            </>
          ) : (
            <span className="text-fg-subtle italic">nincs tipp</span>
          )}
        </span>
      </div>
    </li>
  )
}
