import { useAuth0 } from '@auth0/auth0-react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useRef, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router'
import { z } from 'zod'
import { ApiError } from '../api/errors'
import {
  useDeleteAvatar,
  useMe,
  useMyTeam,
  usePreferences,
  useRenameMe,
  useSetAvatar,
  useSetPreferences,
} from '../api/hooks'
import { Avatar } from '../components/Avatar'
import { formatBudapest } from '../lib/format'
import { resizeToDataUrl } from '../lib/imageResize'

function errorMessage(e: unknown): string {
  if (e instanceof ApiError) return e.message
  if (e instanceof Error) return e.message
  return String(e)
}

export function Me() {
  const { logout } = useAuth0()
  const me = useMe()
  const myTeam = useMyTeam()

  return (
    <div className="max-w-xl mx-auto px-6 py-10 space-y-6">
      <header>
        <p className="text-xs font-mono uppercase tracking-[0.2em] text-orange-600">Profil</p>
        <h1 className="text-4xl font-black uppercase tracking-tight mt-2 break-words">
          {me.data?.displayName ?? '…'}
        </h1>
      </header>

      {me.isLoading && <p className="font-mono text-stone-500">betöltés…</p>}
      {me.error && (
        <p className="border-2 border-red-700 bg-red-50 p-4 font-mono text-sm text-red-800">
          ⚠ {me.error instanceof Error ? me.error.message : String(me.error)}
        </p>
      )}

      {me.data && (
        <section className="border-2 border-stone-900 bg-white p-5 space-y-4">
          <AvatarSection
            userId={me.data.id}
            displayName={me.data.displayName}
            avatarVersion={me.data.avatarVersion}
          />
          <DisplayNameRow displayName={me.data.displayName} />
          <Row label="Csatlakozott" value={formatBudapest(me.data.createdAt)} />
          {me.data.isAdmin && (
            <Row
              label="Szerepkör"
              value={
                <span className="bg-orange-100 text-orange-800 px-2 py-0.5 text-xs uppercase tracking-[0.15em]">
                  Admin
                </span>
              }
            />
          )}
          <Row
            label="Csapat"
            value={
              myTeam.isLoading
                ? 'betöltés…'
                : myTeam.data
                  ? `${myTeam.data.name} (${myTeam.data.members.length}/4 · ${myTeam.data.status})`
                  : 'nincs'
            }
          />
        </section>
      )}

      <PreferencesPanel />

      <section className="border-2 border-stone-300 bg-white p-5 space-y-3">
        <p className="text-xs font-mono uppercase tracking-[0.15em] text-stone-500">Gyors hivatkozások</p>
        <div className="flex flex-wrap gap-2">
          <QuickLink to="/matches" label="Mérkőzések" />
          <QuickLink to="/long-tips" label="Hosszú tipp" />
          <QuickLink to="/team" label="Csapat" />
          <QuickLink to="/leaderboard" label="Ranglista" />
        </div>
      </section>

      <button
        type="button"
        onClick={() => logout({ logoutParams: { returnTo: window.location.origin } })}
        className="w-full border-2 border-stone-900 bg-white text-stone-900 py-3 text-sm font-mono uppercase tracking-[0.2em] hover:bg-stone-900 hover:text-white"
      >
        Kilépés
      </button>
    </div>
  )
}

function PreferencesPanel() {
  const prefs = usePreferences()
  const setPrefs = useSetPreferences()
  const enabled = prefs.data?.emailRemindersEnabled ?? true
  const hasEmail = prefs.data?.hasEmail ?? false
  const disabled = prefs.isLoading || setPrefs.isPending || !hasEmail

  return (
    <section className="border-2 border-stone-300 bg-white p-5 space-y-3">
      <p className="text-xs font-mono uppercase tracking-[0.15em] text-stone-500">Értesítések</p>

      {prefs.error && (
        <p className="border-2 border-red-700 bg-red-50 p-3 font-mono text-xs text-red-800">
          ⚠ {prefs.error instanceof Error ? prefs.error.message : String(prefs.error)}
        </p>
      )}

      <label className="flex items-start gap-3 cursor-pointer">
        <input
          type="checkbox"
          checked={enabled}
          disabled={disabled}
          onChange={(e) => setPrefs.mutate(e.target.checked)}
          className="size-5 mt-0.5 border-2 border-stone-900 cursor-pointer disabled:cursor-not-allowed"
        />
        <span className="flex-1 space-y-1">
          <span className="block text-sm font-mono">Tipp-emlékeztetők emailben</span>
          <span className="block font-mono text-xs text-stone-500">
            ~24 órával és ~2 órával a meccs előtt szólunk, ha még nem tippeltél.
          </span>
        </span>
      </label>

      {!hasEmail && (
        <p className="text-xs font-mono text-stone-500">
          Az Auth0 fiókod nem tartalmaz email címet, ezért nem tudunk emlékeztetőt küldeni.
        </p>
      )}
      {setPrefs.error && (
        <p className="text-xs font-mono text-red-700">
          Nem sikerült menteni: {setPrefs.error instanceof Error ? setPrefs.error.message : String(setPrefs.error)}
        </p>
      )}
    </section>
  )
}

function AvatarSection({
  userId,
  displayName,
  avatarVersion,
}: {
  userId: string
  displayName: string
  avatarVersion: string | null
}) {
  const [preview, setPreview] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const inputRef = useRef<HTMLInputElement | null>(null)
  const setAvatar = useSetAvatar()
  const deleteAvatar = useDeleteAvatar()

  const pickFile = () => inputRef.current?.click()

  const onFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    e.target.value = ''
    if (!file) return
    setError(null)
    try {
      const dataUrl = await resizeToDataUrl(file)
      setPreview(dataUrl)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Nem sikerült a kép feldolgozása.')
    }
  }

  const onSave = async () => {
    if (!preview) return
    setError(null)
    try {
      await setAvatar.mutateAsync(preview)
      setPreview(null)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const onDelete = async () => {
    setError(null)
    try {
      await deleteAvatar.mutateAsync()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const busy = setAvatar.isPending || deleteAvatar.isPending
  const hasAvatar = !!avatarVersion

  return (
    <div className="flex items-center gap-4 border-b border-stone-200 pb-4">
      {preview ? (
        <img
          src={preview}
          alt=""
          style={{ width: 96, height: 96 }}
          className="rounded-full border-2 border-orange-600 object-cover bg-white shrink-0"
        />
      ) : (
        <Avatar userId={userId} displayName={displayName} version={avatarVersion} size={96} />
      )}
      <div className="flex-1 min-w-0 space-y-2">
        <p className="text-xs font-mono uppercase tracking-[0.15em] text-stone-500">Profilkép</p>
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
              className="border-2 border-stone-900 bg-stone-900 text-white px-3 py-1.5 text-xs font-mono uppercase tracking-[0.15em] hover:bg-orange-600 hover:border-orange-600 disabled:opacity-40"
            >
              {setAvatar.isPending ? 'mentés…' : 'Mentés'}
            </button>
            <button
              type="button"
              onClick={() => { setPreview(null); setError(null) }}
              disabled={busy}
              className="border-2 border-stone-300 px-3 py-1.5 text-xs font-mono uppercase tracking-[0.15em] text-stone-700 hover:border-stone-900 hover:text-stone-900 disabled:opacity-40"
            >
              Mégse
            </button>
          </div>
        ) : (
          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              onClick={pickFile}
              disabled={busy}
              className="border-2 border-stone-300 px-3 py-1.5 text-xs font-mono uppercase tracking-[0.15em] text-stone-700 hover:border-stone-900 hover:text-stone-900 disabled:opacity-40"
            >
              Kép feltöltése
            </button>
            {hasAvatar && (
              <button
                type="button"
                onClick={onDelete}
                disabled={busy}
                className="border-2 border-stone-300 px-3 py-1.5 text-xs font-mono uppercase tracking-[0.15em] text-red-700 hover:border-red-700 disabled:opacity-40"
              >
                {deleteAvatar.isPending ? 'törlés…' : 'Törlés'}
              </button>
            )}
          </div>
        )}
        {error && <p className="text-xs font-mono text-red-700">{error}</p>}
      </div>
    </div>
  )
}

const renameSchema = z.object({
  displayName: z
    .string()
    .trim()
    .min(1, 'A megjelenített név nem lehet üres.')
    .max(120, 'Maximum 120 karakter.'),
})
type RenameValues = z.infer<typeof renameSchema>

function DisplayNameRow({ displayName }: { displayName: string }) {
  const [editing, setEditing] = useState(false)
  const rename = useRenameMe()
  const {
    register,
    handleSubmit,
    setError,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<RenameValues>({
    resolver: zodResolver(renameSchema),
    defaultValues: { displayName },
  })

  const onValid = async (values: RenameValues) => {
    try {
      await rename.mutateAsync(values.displayName.trim())
      setEditing(false)
    } catch (e) {
      setError('displayName', { message: errorMessage(e) })
    }
  }

  const cancel = () => {
    reset({ displayName })
    setEditing(false)
  }

  if (!editing) {
    return (
      <Row
        label="Megjelenített név"
        value={
          <span className="flex items-baseline gap-3 justify-end">
            <span className="break-words min-w-0">{displayName}</span>
            <button
              type="button"
              onClick={() => {
                reset({ displayName })
                setEditing(true)
              }}
              className="border-2 border-stone-300 px-2 py-0.5 text-[10px] font-mono uppercase tracking-[0.15em] text-stone-700 hover:border-stone-900 hover:text-stone-900 shrink-0"
            >
              Átnevez
            </button>
          </span>
        }
      />
    )
  }

  return (
    <div className="border-b border-stone-200 pb-3 last:border-b-0 last:pb-0 space-y-2">
      <label
        htmlFor="displayName"
        className="block text-xs font-mono uppercase tracking-[0.15em] text-stone-500"
      >
        Megjelenített név
      </label>
      <form onSubmit={handleSubmit(onValid)} className="space-y-2">
        <input
          id="displayName"
          type="text"
          maxLength={120}
          autoFocus
          {...register('displayName')}
          className="w-full border-2 border-stone-900 px-3 py-2 font-mono text-sm"
        />
        {errors.displayName && (
          <p className="text-xs font-mono text-red-700">{errors.displayName.message}</p>
        )}
        <div className="flex justify-end gap-2">
          <button
            type="button"
            onClick={cancel}
            disabled={isSubmitting || rename.isPending}
            className="border-2 border-stone-300 px-3 py-1.5 text-xs font-mono uppercase tracking-[0.15em] text-stone-700 hover:border-stone-900 hover:text-stone-900 disabled:opacity-40"
          >
            Mégse
          </button>
          <button
            type="submit"
            disabled={isSubmitting || rename.isPending}
            className="border-2 border-stone-900 bg-stone-900 text-white px-3 py-1.5 text-xs font-mono uppercase tracking-[0.15em] hover:bg-orange-600 hover:border-orange-600 disabled:opacity-40"
          >
            {rename.isPending || isSubmitting ? 'mentés…' : 'Mentés'}
          </button>
        </div>
      </form>
    </div>
  )
}

function Row({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex items-baseline justify-between gap-3 border-b border-stone-200 pb-2 last:border-b-0 last:pb-0">
      <span className="text-xs font-mono uppercase tracking-[0.15em] text-stone-500 shrink-0">
        {label}
      </span>
      <span className="font-mono text-sm text-stone-900 text-right break-words min-w-0">
        {value}
      </span>
    </div>
  )
}

function QuickLink({ to, label }: { to: string; label: string }) {
  return (
    <Link
      to={to}
      className="border-2 border-stone-300 px-3 py-1.5 text-xs font-mono uppercase tracking-[0.15em] text-stone-700 hover:border-stone-900 hover:text-stone-900"
    >
      {label}
    </Link>
  )
}
