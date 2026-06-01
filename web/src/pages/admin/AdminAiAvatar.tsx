import { useRef, useState } from 'react'
import { Link } from 'react-router'
import { ApiError } from '../../api/errors'
import { useDeleteAiAvatar, useMe, useSetAiAvatar } from '../../api/hooks'
import { Avatar } from '../../components/Avatar'
import { resizeToDataUrl } from '../../lib/imageResize'

function errorMessage(e: unknown): string {
  if (e instanceof ApiError) return e.message
  if (e instanceof Error) return e.message
  return String(e)
}

export function AdminAiAvatar() {
  const me = useMe()
  const setAvatar = useSetAiAvatar()
  const deleteAvatar = useDeleteAiAvatar()
  const inputRef = useRef<HTMLInputElement | null>(null)
  const [preview, setPreview] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  const aiVersion = me.data?.aiAvatarVersion ?? null
  const busy = setAvatar.isPending || deleteAvatar.isPending

  const pickFile = () => inputRef.current?.click()

  const onFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    e.target.value = ''
    if (!file) return
    setError(null)
    setSuccess(null)
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
    setSuccess(null)
    try {
      await setAvatar.mutateAsync({ dataUrl: preview })
      setPreview(null)
      setSuccess('Az AI profilkép frissítve.')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const onDelete = async () => {
    setError(null)
    setSuccess(null)
    try {
      await deleteAvatar.mutateAsync(null)
      setSuccess('Az AI profilkép törölve. Most a kezdőbetűs alapérték jelenik meg.')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="max-w-2xl mx-auto px-6 py-10 space-y-6">
      <header className="space-y-2">
        <p className="text-xs font-mono uppercase tracking-[0.2em] text-orange-600">Admin</p>
        <h1 className="text-3xl font-black uppercase tracking-tight">AI tippelő profilkép</h1>
        <p className="font-mono text-sm text-stone-600">
          Egy közös kép minden AI csapattaghoz. Az adminisztrátor által feltöltött kép automatikusan
          megjelenik minden olyan helyen, ahol AI tippelő van — csapat oldal, ranglista.
        </p>
        <Link to="/admin" className="inline-block text-xs font-mono uppercase tracking-[0.15em] text-stone-500 hover:text-stone-900">
          ← Admin főoldal
        </Link>
      </header>

      <section className="border-2 border-stone-900 bg-white p-5 space-y-4">
        <div className="flex items-center gap-5">
          {preview ? (
            <img
              src={preview}
              alt=""
              style={{ width: 128, height: 128 }}
              className="rounded-full border-2 border-orange-600 object-cover bg-white shrink-0"
            />
          ) : (
            <Avatar userId={null} displayName="AI" version={null} isAi size={128} />
          )}
          <div className="flex-1 min-w-0 space-y-2">
            <p className="text-xs font-mono uppercase tracking-[0.15em] text-stone-500">
              Jelenlegi állapot
            </p>
            <p className="font-mono text-sm">
              {aiVersion
                ? 'Egyedi kép van feltöltve.'
                : 'Még nincs feltöltve egyedi kép — a kezdőbetűs alapérték jelenik meg.'}
            </p>
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
                {aiVersion && (
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
          </div>
        </div>
        {error && (
          <p className="border-2 border-red-700 bg-red-50 p-3 font-mono text-xs text-red-800">
            ⚠ {error}
          </p>
        )}
        {success && (
          <p className="border-2 border-emerald-700 bg-emerald-50 p-3 font-mono text-xs text-emerald-800">
            {success}
          </p>
        )}
      </section>
    </div>
  )
}
