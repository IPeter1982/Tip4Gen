import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect, useRef, useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { ApiError } from '../api/errors'
import {
  useAddAiMember,
  useCreateInvite,
  useCreateTeam,
  useDeleteTeamAvatar,
  useLeaveTeam,
  useLongTips,
  useMyTeam,
  usePatchTeam,
  useSetTeamAvatar,
} from '../api/hooks'
import type { AiMode, TeamView } from '../api/types'
import { Avatar } from '../components/Avatar'
import { TeamAvatar } from '../components/TeamAvatar'
import { formatBudapest, formatCountdown } from '../lib/format'
import { resizeToDataUrl } from '../lib/imageResize'

const AI_MODE_LABEL: Record<AiMode, string> = {
  Conservative: 'Óvatos',
  Balanced: 'Kiegyensúlyozott',
  Bold: 'Merész',
}

const STATUS_LABEL: Record<TeamView['status'], string> = {
  Forming: 'Alakuló',
  Locked: 'Lezárva',
  Disqualified: 'Kizárva',
}

function reasonMessage(reason: string): string {
  switch (reason) {
    case 'NameBlank':
      return 'A csapatnévnek nem szabad üresnek lennie.'
    case 'NameTooLong':
      return 'A csapatnév maximum 80 karakter lehet.'
    case 'TournamentStarted':
      return 'A torna már elkezdődött; a csapat-beállítások véglegesek.'
    case 'TeamLocked':
      return 'A csapat lezárult; ekkor már nem módosítható.'
    case 'TeamFull':
      return 'A csapat már elérte a 3 fős maximumot.'
    case 'AiSlotTaken':
      return 'A csapatban már van AI tag.'
    case 'UserAlreadyInTeam':
    case 'AlreadyInTeam':
      return 'Már tagja vagy egy másik csapatnak.'
    case 'AvatarMissing':
      return 'Nincs kép kiválasztva.'
    case 'AvatarUnsupportedFormat':
      return 'Csak JPEG, PNG vagy WebP képek tölthetők fel.'
    case 'AvatarTooLarge':
      return 'A kép maximum 50 KB lehet.'
    case 'AvatarInvalidDataUrl':
      return 'Érvénytelen kép-adat.'
    default:
      return 'A művelet nem hajtható végre.'
  }
}

function errorMessage(e: unknown): string {
  if (e instanceof ApiError && e.reason) return reasonMessage(e.reason)
  if (e instanceof ApiError) return e.message
  if (e instanceof Error) return e.message
  return String(e)
}

export function Team() {
  const myTeam = useMyTeam()

  return (
    <div className="max-w-2xl mx-auto px-6 py-10 space-y-6">
      <header>
        <p className="text-xs font-mono uppercase tracking-[0.2em] text-accent">Csapat</p>
        <h1 className="text-4xl font-black uppercase tracking-tight mt-2">
          Csapat-beállítások
        </h1>
      </header>

      {myTeam.isLoading && <p className="font-mono text-fg-subtle">betöltés…</p>}
      {myTeam.error && (
        <p className="border border-danger bg-danger/10 p-4 font-mono text-sm text-danger">
          ⚠ {errorMessage(myTeam.error)}
        </p>
      )}

      {!myTeam.isLoading && !myTeam.error && (
        myTeam.data ? <TeamPanel team={myTeam.data} /> : <CreateTeamPanel />
      )}
    </div>
  )
}

// ----------------------------------------------------------------------------
// Create team
// ----------------------------------------------------------------------------

const createSchema = z.object({
  name: z.string().trim().min(1, 'Adj meg egy nevet.').max(80, 'Maximum 80 karakter.'),
})
type CreateValues = z.infer<typeof createSchema>

function CreateTeamPanel() {
  const create = useCreateTeam()
  const longTips = useLongTips()
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<CreateValues>({
    resolver: zodResolver(createSchema),
    defaultValues: { name: '' },
  })

  const onValid = async (values: CreateValues) => {
    try {
      await create.mutateAsync(values.name.trim())
    } catch (e) {
      setError('root', { message: errorMessage(e) })
    }
  }

  const locked = longTips.data?.locked ?? false

  return (
    <section className="border border-border-strong bg-elevated p-6 space-y-5">
      <div className="space-y-2">
        <h2 className="text-2xl font-black uppercase tracking-tight">Új csapat</h2>
        <p className="font-mono text-sm text-fg-muted">
          Hozz létre egy csapatot, hívj meg 2 barátot, és opcionálisan adj hozzá egy AI tagot.
          Max. 3 fő/csapat. A csapatok akkor zárulnak le, amikor megtelnek (a torna kezdete után is csatlakozhatnak új tagok).
        </p>
        {longTips.data && (
          <p className="text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle">
            zár: <span className="text-fg-default">{formatBudapest(longTips.data.lockUtc)}</span>
            {locked && <span className="ml-2 text-danger">· LEZÁRVA</span>}
          </p>
        )}
      </div>

      <form onSubmit={handleSubmit(onValid)} className="space-y-4">
        <div className="space-y-2">
          <label
            htmlFor="name"
            className="block text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle"
          >
            Csapatnév
          </label>
          <input
            id="name"
            type="text"
            maxLength={80}
            disabled={locked}
            placeholder="pl. Pekka Pekkarinen"
            {...register('name')}
            className="w-full border border-border-strong px-3 py-2 font-mono disabled:bg-sunken"
          />
          {errors.name && (
            <p className="text-xs font-mono text-danger">{errors.name.message}</p>
          )}
        </div>

        {errors.root && (
          <p className="text-xs font-mono text-danger">{errors.root.message}</p>
        )}

        <button
          type="submit"
          disabled={locked || isSubmitting || create.isPending}
          className="w-full border border-accent bg-accent text-on-accent py-3 text-sm font-mono uppercase tracking-[0.2em] hover:bg-accent-strong hover:border-accent disabled:opacity-40 disabled:cursor-not-allowed"
        >
          {create.isPending || isSubmitting ? 'küldés…' : 'Csapat létrehozása'}
        </button>
      </form>
    </section>
  )
}

// ----------------------------------------------------------------------------
// Manage existing team
// ----------------------------------------------------------------------------

function TeamPanel({ team }: { team: TeamView }) {
  const longTips = useLongTips()
  const [now, setNow] = useState(() => new Date())
  useEffect(() => {
    const id = setInterval(() => setNow(new Date()), 1000)
    return () => clearInterval(id)
  }, [])

  const editable = team.status === 'Forming' && !(longTips.data?.locked ?? false)
  const hasAi = team.members.some((m) => m.isAi)
  const humanCount = team.members.filter((m) => !m.isAi).length

  return (
    <div className="space-y-6">
      <LockBanner status={team.status} lockUtc={longTips.data?.lockUtc} now={now} />

      <MembersPanel team={team} editable={editable} />

      {editable && !hasAi && (
        <AddAiPanel teamId={team.id} />
      )}

      {editable && (
        <RenamePanel team={team} />
      )}

      {hasAi && (
        <AiModePanel team={team} editable={editable} />
      )}

      {editable && <InvitePanel teamId={team.id} />}

      {editable && <LeavePanel teamId={team.id} humanCount={humanCount} />}
    </div>
  )
}

function LockBanner({
  status,
  lockUtc,
  now,
}: {
  status: TeamView['status']
  lockUtc?: string
  now: Date
}) {
  if (status === 'Disqualified') {
    return (
      <section className="border border-danger bg-danger/10 p-4 font-mono text-sm text-danger">
        ⚠ A csapat <strong>kizárva</strong> a csapat-versenyből. Az egyéni tippek továbbra is érvényesek.
      </section>
    )
  }
  if (status === 'Locked') {
    return (
      <section className="border border-border-strong bg-accent text-on-accent p-4 font-mono text-xs uppercase tracking-[0.15em]">
        állapot: <span className="text-accent-soft">{STATUS_LABEL[status]}</span>
        <span className="ml-3">· a csapat összetétele végleges</span>
      </section>
    )
  }
  if (!lockUtc) return null
  const countdown = formatCountdown(lockUtc, now)
  return (
    <section className="border border-border-strong bg-elevated p-4 font-mono text-xs uppercase tracking-[0.15em] text-fg-muted">
      csapat zárul: <span className="text-fg-default">{formatBudapest(lockUtc)}</span>
      <span className="ml-3">
        · hátralévő idő: <span className="text-fg-default">{countdown}</span>
      </span>
    </section>
  )
}

function MembersPanel({ team, editable }: { team: TeamView; editable: boolean }) {
  return (
    <section className="border border-border-strong bg-elevated p-5 space-y-3">
      <div className="flex items-center justify-between gap-3">
        <div className="flex items-center gap-3 min-w-0">
          <TeamAvatar
            teamId={team.id}
            teamName={team.name}
            version={team.avatarVersion}
            size={48}
          />
          <h2 className="text-2xl font-black uppercase tracking-tight truncate">{team.name}</h2>
        </div>
        <span className="text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle shrink-0">
          {STATUS_LABEL[team.status]} · {team.members.length}/3
        </span>
      </div>
      <ul className="divide-y divide-border-subtle">
        {team.members.map((m) => (
          <li key={m.id} className="py-2 flex items-center gap-3 font-mono text-sm">
            <Avatar
              userId={m.userId}
              displayName={m.displayName}
              version={m.avatarVersion}
              isAi={m.isAi}
              size={32}
            />
            <span className="flex-1 truncate">{m.displayName}</span>
            {m.isAi && (
              <span className="bg-accent text-on-accent px-2 py-0.5 text-xs uppercase tracking-[0.15em]">
                AI
              </span>
            )}
          </li>
        ))}
        {Array.from({ length: 3 - team.members.length }).map((_, i) => (
          <li key={`empty-${i}`} className="py-2 font-mono text-sm italic text-fg-subtle">
            — üres hely —
          </li>
        ))}
      </ul>
      {!editable && team.status === 'Forming' && (
        <p className="text-xs font-mono text-fg-subtle">
          A torna már elkezdődött, a tagság véglegesnek tekinthető.
        </p>
      )}
    </section>
  )
}

const renameSchema = z.object({
  name: z.string().trim().min(1, 'Adj meg egy nevet.').max(80, 'Maximum 80 karakter.'),
})
type RenameValues = z.infer<typeof renameSchema>

function RenamePanel({ team }: { team: TeamView }) {
  const patch = usePatchTeam()
  const {
    register,
    handleSubmit,
    setError,
    reset,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<RenameValues>({
    resolver: zodResolver(renameSchema),
    defaultValues: { name: team.name },
  })

  useEffect(() => {
    reset({ name: team.name })
  }, [team.name, reset])

  const onValid = async (values: RenameValues) => {
    try {
      await patch.mutateAsync({ teamId: team.id, name: values.name.trim() })
    } catch (e) {
      setError('root', { message: errorMessage(e) })
    }
  }

  return (
    <section className="border border-border-subtle bg-elevated p-5 space-y-4">
      <h3 className="text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle">
        Név és profilkép
      </h3>
      <form onSubmit={handleSubmit(onValid)} className="flex gap-2 items-start">
        <div className="flex-1 space-y-1">
          <label
            htmlFor={`team-name-${team.id}`}
            className="block text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle"
          >
            Csapatnév
          </label>
          <input
            id={`team-name-${team.id}`}
            type="text"
            maxLength={80}
            {...register('name')}
            className="w-full border border-border-strong px-3 py-2 font-mono"
          />
          {errors.name && (
            <p className="text-xs font-mono text-danger">{errors.name.message}</p>
          )}
          {errors.root && (
            <p className="text-xs font-mono text-danger">{errors.root.message}</p>
          )}
        </div>
        <button
          type="submit"
          disabled={!isDirty || isSubmitting || patch.isPending}
          className="self-end border border-accent bg-accent text-on-accent px-4 py-2 text-xs font-mono uppercase tracking-[0.15em] hover:bg-accent-strong hover:border-accent disabled:opacity-40 disabled:cursor-not-allowed"
        >
          mentés
        </button>
      </form>
      <TeamAvatarPanel team={team} />
    </section>
  )
}

function TeamAvatarPanel({ team }: { team: TeamView }) {
  const setAvatar = useSetTeamAvatar()
  const deleteAvatar = useDeleteTeamAvatar()
  const inputRef = useRef<HTMLInputElement | null>(null)
  const [preview, setPreview] = useState<string | null>(null)
  const [err, setErr] = useState<string | null>(null)
  const busy = setAvatar.isPending || deleteAvatar.isPending

  const onFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    e.target.value = ''
    if (!file) return
    setErr(null)
    try {
      const dataUrl = await resizeToDataUrl(file)
      setPreview(dataUrl)
    } catch (e2) {
      setErr(e2 instanceof Error ? e2.message : 'Nem sikerült a kép feldolgozása.')
    }
  }

  const onSave = async () => {
    if (!preview) return
    setErr(null)
    try {
      await setAvatar.mutateAsync({ teamId: team.id, dataUrl: preview })
      setPreview(null)
    } catch (e) {
      setErr(errorMessage(e))
    }
  }

  const onDelete = async () => {
    setErr(null)
    try {
      await deleteAvatar.mutateAsync(team.id)
    } catch (e) {
      setErr(errorMessage(e))
    }
  }

  return (
    <div className="space-y-2 border-t border-border-subtle pt-4">
      <span className="block text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle">
        Profilkép
      </span>
      <div className="flex items-center gap-4">
        {preview ? (
          <img
            src={preview}
            alt=""
            style={{ width: 80, height: 80 }}
            className="rounded-full border border-accent object-cover bg-elevated shrink-0"
          />
        ) : (
          <TeamAvatar
            teamId={team.id}
            teamName={team.name}
            version={team.avatarVersion}
            size={80}
          />
        )}
        <div className="flex-1 min-w-0 space-y-2">
          <input
            ref={inputRef}
            type="file"
            accept="image/jpeg,image/png,image/webp"
            hidden
            onChange={onFileChange}
          />
          {preview ? (
            <div className="flex flex-wrap gap-2">
              <button
                type="button"
                onClick={onSave}
                disabled={busy}
                className="border border-accent bg-accent text-on-accent px-3 py-1.5 text-xs font-mono uppercase tracking-[0.15em] hover:bg-accent-strong hover:border-accent disabled:opacity-40"
              >
                {setAvatar.isPending ? 'mentés…' : 'mentés'}
              </button>
              <button
                type="button"
                onClick={() => { setPreview(null); setErr(null) }}
                disabled={busy}
                className="border border-border-subtle px-3 py-1.5 text-xs font-mono uppercase tracking-[0.15em] text-fg-default hover:border-accent hover:text-accent disabled:opacity-40"
              >
                mégse
              </button>
            </div>
          ) : (
            <div className="flex flex-wrap gap-2">
              <button
                type="button"
                onClick={() => inputRef.current?.click()}
                disabled={busy}
                className="border border-border-subtle px-3 py-1.5 text-xs font-mono uppercase tracking-[0.15em] text-fg-default hover:border-accent hover:text-accent disabled:opacity-40"
              >
                kép feltöltése
              </button>
              {team.avatarVersion && (
                <button
                  type="button"
                  onClick={onDelete}
                  disabled={busy}
                  className="border border-border-subtle px-3 py-1.5 text-xs font-mono uppercase tracking-[0.15em] text-danger hover:border-danger disabled:opacity-40"
                >
                  {deleteAvatar.isPending ? 'törlés…' : 'törlés'}
                </button>
              )}
            </div>
          )}
        </div>
      </div>
      {err && <p className="text-xs font-mono text-danger">{err}</p>}
    </div>
  )
}

const aiAddSchema = z.object({
  displayName: z
    .string()
    .trim()
    .min(1, 'Adj meg egy nevet.')
    .max(80, 'Maximum 80 karakter.'),
  mode: z.enum(['Conservative', 'Balanced', 'Bold']),
})
type AiAddValues = z.infer<typeof aiAddSchema>

function AddAiPanel({ teamId }: { teamId: string }) {
  const add = useAddAiMember()
  const {
    register,
    handleSubmit,
    setError,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<AiAddValues>({
    resolver: zodResolver(aiAddSchema),
    defaultValues: { displayName: '', mode: 'Balanced' },
  })

  const onValid = async (values: AiAddValues) => {
    try {
      await add.mutateAsync({
        teamId,
        displayName: values.displayName.trim(),
        mode: values.mode,
      })
      reset({ displayName: '', mode: 'Balanced' })
    } catch (e) {
      setError('root', { message: errorMessage(e) })
    }
  }

  return (
    <section className="border border-border-subtle bg-elevated p-5 space-y-3">
      <h3 className="text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle">
        AI tag hozzáadása
      </h3>
      <p className="font-mono text-xs text-fg-subtle">
        Egy csapatonként legfeljebb egy AI tag. Az AI a megadott stílussal tippel.
      </p>
      <form onSubmit={handleSubmit(onValid)} className="space-y-3">
        <div className="space-y-1">
          <label htmlFor="aiName" className="block text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle">
            AI neve
          </label>
          <input
            id="aiName"
            type="text"
            maxLength={80}
            placeholder="pl. RoboKovács"
            {...register('displayName')}
            className="w-full border border-border-strong px-3 py-2 font-mono"
          />
          {errors.displayName && (
            <p className="text-xs font-mono text-danger">{errors.displayName.message}</p>
          )}
        </div>
        <div className="space-y-1">
          <label htmlFor="aiMode" className="block text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle">
            Stílus
          </label>
          <select
            id="aiMode"
            {...register('mode')}
            className="w-full border border-border-strong px-3 py-2 font-mono"
          >
            {(['Conservative', 'Balanced', 'Bold'] as const).map((m) => (
              <option key={m} value={m}>
                {AI_MODE_LABEL[m]}
              </option>
            ))}
          </select>
        </div>
        {errors.root && (
          <p className="text-xs font-mono text-danger">{errors.root.message}</p>
        )}
        <button
          type="submit"
          disabled={isSubmitting || add.isPending}
          className="w-full border border-accent bg-accent text-on-accent py-2 text-xs font-mono uppercase tracking-[0.2em] hover:bg-accent-strong hover:border-accent disabled:opacity-40 disabled:cursor-not-allowed"
        >
          {add.isPending || isSubmitting ? 'küldés…' : 'AI hozzáadása'}
        </button>
      </form>
    </section>
  )
}

function AiModePanel({ team, editable }: { team: TeamView; editable: boolean }) {
  const patch = usePatchTeam()
  const [mode, setMode] = useState<AiMode>(team.aiMode ?? 'Balanced')
  const [err, setErr] = useState<string | null>(null)

  useEffect(() => {
    setMode(team.aiMode ?? 'Balanced')
  }, [team.aiMode])

  const onSave = async () => {
    setErr(null)
    try {
      await patch.mutateAsync({ teamId: team.id, aiMode: mode })
    } catch (e) {
      setErr(errorMessage(e))
    }
  }

  const aiMember = team.members.find((m) => m.isAi)
  if (!aiMember) return null

  return (
    <section className="border border-border-subtle bg-elevated p-5 space-y-3">
      <h3 className="text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle">
        AI stílus ({aiMember.displayName})
      </h3>
      <div className="flex gap-2 flex-wrap">
        {(['Conservative', 'Balanced', 'Bold'] as const).map((m) => (
          <button
            key={m}
            type="button"
            disabled={!editable}
            onClick={() => setMode(m)}
            className={`border px-3 py-1 text-xs font-mono uppercase tracking-[0.15em] disabled:opacity-50 disabled:cursor-not-allowed ${
              mode === m
                ? 'border-accent bg-accent text-on-accent'
                : 'border-border-subtle text-fg-muted hover:border-accent'
            }`}
          >
            {AI_MODE_LABEL[m]}
          </button>
        ))}
      </div>
      {err && <p className="text-xs font-mono text-danger">{err}</p>}
      {editable && (
        <button
          type="button"
          onClick={onSave}
          disabled={mode === team.aiMode || patch.isPending}
          className="border border-accent bg-accent text-on-accent px-4 py-2 text-xs font-mono uppercase tracking-[0.15em] hover:bg-accent-strong hover:border-accent disabled:opacity-40 disabled:cursor-not-allowed"
        >
          stílus mentése
        </button>
      )}
    </section>
  )
}

function InvitePanel({ teamId }: { teamId: string }) {
  const createInvite = useCreateInvite()
  const [link, setLink] = useState<string | null>(null)
  const [expires, setExpires] = useState<string | null>(null)
  const [err, setErr] = useState<string | null>(null)
  const [copied, setCopied] = useState(false)

  const onGenerate = async () => {
    setErr(null)
    setCopied(false)
    try {
      const invite = await createInvite.mutateAsync(teamId)
      const url = `${window.location.origin}/team/join/${invite.token}`
      setLink(url)
      setExpires(invite.expiresAt)
    } catch (e) {
      setErr(errorMessage(e))
    }
  }

  const onCopy = async () => {
    if (!link) return
    try {
      await navigator.clipboard.writeText(link)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch {
      // Older browsers / blocked permission — leave the link visible for manual copy.
    }
  }

  return (
    <section className="border border-border-subtle bg-elevated p-5 space-y-3">
      <h3 className="text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle">Meghívás</h3>
      <p className="font-mono text-xs text-fg-subtle">
        Egy meghívó-link 7 napig érvényes és egyetlen felhasználó tudja felhasználni.
      </p>
      <button
        type="button"
        onClick={onGenerate}
        disabled={createInvite.isPending}
        className="border border-border-strong bg-elevated text-fg-default px-4 py-2 text-xs font-mono uppercase tracking-[0.15em] hover:bg-accent hover:text-on-accent hover:border-accent disabled:opacity-40 disabled:cursor-not-allowed"
      >
        {createInvite.isPending ? 'generálás…' : 'új meghívó-link'}
      </button>
      {err && <p className="text-xs font-mono text-danger">{err}</p>}
      {link && (
        <div className="space-y-2">
          <input
            type="text"
            readOnly
            value={link}
            onFocus={(e) => e.currentTarget.select()}
            className="w-full border border-border-strong px-3 py-2 font-mono text-xs bg-sunken"
          />
          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={onCopy}
              className="border border-accent bg-accent text-on-accent px-3 py-1 text-xs font-mono uppercase tracking-[0.15em] hover:bg-accent-strong hover:border-accent"
            >
              {copied ? 'másolva ✓' : 'másolás'}
            </button>
            {expires && (
              <span className="text-xs font-mono text-fg-subtle">
                lejár: {formatBudapest(expires)}
              </span>
            )}
          </div>
        </div>
      )}
    </section>
  )
}

function LeavePanel({ teamId, humanCount }: { teamId: string; humanCount: number }) {
  const leave = useLeaveTeam()
  const [confirming, setConfirming] = useState(false)
  const [err, setErr] = useState<string | null>(null)
  const lastHuman = humanCount <= 1

  const onLeave = async () => {
    setErr(null)
    try {
      await leave.mutateAsync(teamId)
      setConfirming(false)
    } catch (e) {
      setErr(errorMessage(e))
    }
  }

  return (
    <section className="border border-danger bg-danger/10 p-5 space-y-3">
      <h3 className="text-xs font-mono uppercase tracking-[0.15em] text-danger">
        Csapat elhagyása
      </h3>
      <p className="font-mono text-xs text-danger">
        {lastHuman
          ? 'Te vagy az egyetlen ember a csapatban — kilépéskor a csapat (és az AI tag) is törlődik.'
          : 'A többi tag a csapatban marad. A megüresedő helyre új tagot lehet meghívni.'}
      </p>
      {!confirming && (
        <button
          type="button"
          onClick={() => setConfirming(true)}
          className="border border-danger bg-elevated text-danger px-4 py-2 text-xs font-mono uppercase tracking-[0.15em] hover:bg-danger hover:text-on-accent"
        >
          kilépek a csapatból
        </button>
      )}
      {confirming && (
        <div className="flex gap-2">
          <button
            type="button"
            onClick={onLeave}
            disabled={leave.isPending}
            className="border border-danger bg-danger text-on-accent px-4 py-2 text-xs font-mono uppercase tracking-[0.15em] hover:opacity-90 disabled:opacity-40"
          >
            {leave.isPending ? 'küldés…' : 'igen, kilépek'}
          </button>
          <button
            type="button"
            onClick={() => setConfirming(false)}
            className="border border-border-strong bg-elevated text-fg-default px-4 py-2 text-xs font-mono uppercase tracking-[0.15em] hover:bg-accent hover:text-on-accent hover:border-accent"
          >
            mégse
          </button>
        </div>
      )}
      {err && <p className="text-xs font-mono text-danger">{err}</p>}
    </section>
  )
}
