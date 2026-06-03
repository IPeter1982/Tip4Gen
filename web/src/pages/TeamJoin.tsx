import { useEffect, useRef, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { AlertTriangle, ArrowLeft, CheckCircle2, Loader2 } from 'lucide-react'
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
      return 'A csapat már elérte a 3 fős maximumot.'
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
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token])

  return (
    <div className="max-w-xl mx-auto px-6 py-10 space-y-6">
      <header>
        <p className="text-xs font-mono uppercase tracking-[0.2em] text-accent">
          Csapatcsatlakozás
        </p>
        <h1 className="text-4xl font-bold tracking-tight mt-2 text-fg-default">Meghívó</h1>
      </header>

      {status === 'pending' && (
        <p className="inline-flex items-center gap-2 rounded-xl border border-border-subtle bg-elevated p-4 font-mono text-sm text-fg-muted">
          <Loader2 size={16} className="animate-spin text-accent" />
          Meghívó beváltása…
        </p>
      )}

      {status === 'success' && (
        <p className="inline-flex items-center gap-2 rounded-xl border border-success/40 bg-success/10 p-4 font-mono text-sm text-success">
          <CheckCircle2 size={16} />
          Sikeres csatlakozás! Átirányítás a csapat oldalra…
        </p>
      )}

      {(status === 'error' || status === 'notfound') && (
        <>
          <p className="inline-flex items-center gap-2 rounded-xl border border-danger/40 bg-danger/10 p-4 font-mono text-sm text-danger">
            <AlertTriangle size={16} />
            {message}
          </p>
          <div className="flex gap-3">
            <Link
              to="/team"
              className="inline-flex items-center gap-1.5 rounded-lg bg-accent px-4 py-2 text-xs font-mono uppercase tracking-[0.15em] text-on-accent transition hover:bg-accent-strong"
            >
              <ArrowLeft size={14} />
              Vissza a csapathoz
            </Link>
            <Link
              to="/"
              className="inline-flex items-center gap-1.5 rounded-lg border border-border-strong px-4 py-2 text-xs font-mono uppercase tracking-[0.15em] text-fg-muted transition hover:text-accent hover:border-accent"
            >
              Főoldal
            </Link>
          </div>
        </>
      )}
    </div>
  )
}
