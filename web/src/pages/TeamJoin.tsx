import { useEffect, useRef, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError } from '../api/errors'
import { useJoinTeam } from '../api/hooks'

function reasonMessage(reason: string): string {
  switch (reason) {
    case 'InviteExpiredOrUsed':
      return 'A meghívó lejárt vagy már felhasznált.'
    case 'AlreadyInTeam':
    case 'UserAlreadyInTeam':
      return 'Már tagja vagy egy másik csapatnak.'
    case 'TeamFull':
      return 'A csapat már elérte a 4 fős maximumot.'
    case 'TournamentStarted':
      return 'A torna már elkezdődött; új csapathoz nem lehet csatlakozni.'
    case 'TeamLocked':
      return 'A csapat lezárult; csatlakozás már nem lehetséges.'
    default:
      return 'A csatlakozás nem sikerült.'
  }
}

export function TeamJoin() {
  const { token } = useParams<{ token: string }>()
  const navigate = useNavigate()
  const join = useJoinTeam()
  const [status, setStatus] = useState<'pending' | 'success' | 'error' | 'notfound'>('pending')
  const [message, setMessage] = useState<string | null>(null)
  const attempted = useRef(false)

  useEffect(() => {
    if (!token || attempted.current) return
    attempted.current = true
    join.mutateAsync(token)
      .then(() => {
        setStatus('success')
        setTimeout(() => navigate('/team'), 1200)
      })
      .catch((e: unknown) => {
        if (e instanceof ApiError && e.status === 404) {
          setStatus('notfound')
          setMessage('A meghívó nem található.')
          return
        }
        if (e instanceof ApiError && e.reason) {
          setStatus('error')
          setMessage(reasonMessage(e.reason))
          return
        }
        setStatus('error')
        setMessage(e instanceof Error ? e.message : String(e))
      })
    // We intentionally fire-and-forget once per token; join is a one-shot redemption.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token])

  return (
    <div className="max-w-xl mx-auto px-6 py-10 space-y-6">
      <header>
        <p className="text-xs font-mono uppercase tracking-[0.2em] text-orange-600">
          Csapatcsatlakozás
        </p>
        <h1 className="text-4xl font-black uppercase tracking-tight mt-2">Meghívó</h1>
      </header>

      {status === 'pending' && (
        <p className="border-2 border-stone-900 bg-white p-4 font-mono text-sm text-stone-700">
          Meghívó beváltása…
        </p>
      )}

      {status === 'success' && (
        <p className="border-2 border-green-700 bg-green-50 p-4 font-mono text-sm text-green-800">
          ✓ Sikeres csatlakozás! Átirányítás a csapat oldalra…
        </p>
      )}

      {(status === 'error' || status === 'notfound') && (
        <>
          <p className="border-2 border-red-700 bg-red-50 p-4 font-mono text-sm text-red-800">
            ⚠ {message}
          </p>
          <div className="flex gap-3">
            <Link
              to="/team"
              className="border-2 border-stone-900 bg-stone-900 text-white px-4 py-2 text-xs font-mono uppercase tracking-[0.15em] hover:bg-orange-600 hover:border-orange-600"
            >
              Vissza a csapathoz
            </Link>
            <Link
              to="/"
              className="border-2 border-stone-900 bg-white text-stone-900 px-4 py-2 text-xs font-mono uppercase tracking-[0.15em] hover:bg-stone-900 hover:text-white"
            >
              Főoldal
            </Link>
          </div>
        </>
      )}
    </div>
  )
}
