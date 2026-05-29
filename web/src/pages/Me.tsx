import { useAuth0 } from '@auth0/auth0-react'
import { Link } from 'react-router'
import { useMe, useMyTeam } from '../api/hooks'
import { formatBudapest } from '../lib/format'

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
        <section className="border-2 border-stone-900 bg-white p-5 space-y-3">
          <Row label="Megjelenített név" value={me.data.displayName} />
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
