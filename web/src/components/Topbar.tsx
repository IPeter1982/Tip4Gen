import { useAuth0 } from '@auth0/auth0-react'
import { Link } from 'react-router'
import { useMe } from '../api/hooks'
import { isAuthConfigured } from '../auth/authConfig'

export function Topbar() {
  const { isAuthenticated, isLoading, user, loginWithRedirect, logout, error } = useAuth0()
  const me = useMe()

  return (
    <header className="border-b-2 border-stone-900 bg-white">
      <div className="max-w-5xl mx-auto px-6 py-4 flex items-center gap-6">
        <Link to="/" className="font-black uppercase tracking-tight text-2xl">
          Tip4Gen
        </Link>
        <nav className="flex items-center gap-4 text-xs font-mono uppercase tracking-[0.15em]">
          <Link to="/" className="text-stone-600 hover:text-stone-900">Főoldal</Link>
          <Link to="/matches" className="text-stone-600 hover:text-stone-900">Mérkőzések</Link>
          <Link to="/long-tips" className="text-stone-600 hover:text-stone-900">Hosszú tipp</Link>
          <Link to="/team" className="text-stone-600 hover:text-stone-900">Csapat</Link>
          <Link to="/leaderboard" className="text-stone-600 hover:text-stone-900">Ranglista</Link>
          <Link to="/me" className="text-stone-600 hover:text-stone-900">Profil</Link>
          {me.data?.isAdmin && (
            <Link to="/admin" className="text-orange-600 hover:text-orange-800">Admin</Link>
          )}
        </nav>
        <div className="ml-auto flex items-center gap-3">
          {!isAuthConfigured && (
            <span className="text-xs font-mono text-orange-600">Auth0 nincs beállítva</span>
          )}
          {error && (
            <span className="text-xs font-mono text-red-700 truncate max-w-[400px]" title={error.message}>
              auth0 hiba: {error.message}
            </span>
          )}
          {isAuthConfigured && isLoading && (
            <span className="text-xs font-mono text-stone-500">betöltés…</span>
          )}
          {isAuthConfigured && !isLoading && !isAuthenticated && (
            <button
              type="button"
              onClick={() => loginWithRedirect()}
              className="border-2 border-stone-900 px-3 py-1 text-xs font-mono uppercase tracking-[0.15em] hover:bg-stone-900 hover:text-white"
            >
              Belépés
            </button>
          )}
          {isAuthConfigured && isAuthenticated && (
            <>
              <span className="text-xs font-mono text-stone-600 truncate max-w-[180px]">
                {user?.name ?? user?.email ?? user?.sub}
              </span>
              <button
                type="button"
                onClick={() => logout({ logoutParams: { returnTo: window.location.origin } })}
                className="border-2 border-stone-900 px-3 py-1 text-xs font-mono uppercase tracking-[0.15em] hover:bg-stone-900 hover:text-white"
              >
                Kilépés
              </button>
            </>
          )}
        </div>
      </div>
    </header>
  )
}
