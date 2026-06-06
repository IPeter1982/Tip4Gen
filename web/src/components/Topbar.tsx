import { useAuth0 } from '@auth0/auth0-react'
import { Link, NavLink } from 'react-router'
import { CloseButton, Popover, PopoverButton, PopoverPanel } from '@headlessui/react'
import { Goal, Menu, X } from 'lucide-react'
import { useMe } from '../api/hooks'
import { isAuthConfigured } from '../auth/authConfig'
import { NAV_ITEMS } from '../lib/navIcons'
import { Avatar } from './Avatar'
import { ThemeToggle } from './ThemeToggle'

const NAV_LINK_BASE =
  'inline-flex items-center gap-1.5 transition hover:text-accent aria-[current=page]:text-accent aria-[current=page]:underline aria-[current=page]:decoration-2 aria-[current=page]:underline-offset-[6px]'

const MOBILE_LINK_BASE =
  'flex items-center gap-2 rounded-md px-3 py-2 text-sm font-mono uppercase tracking-[0.15em] text-fg-muted transition hover:bg-sunken hover:text-accent aria-[current=page]:bg-accent-soft aria-[current=page]:text-accent-strong'

export function Topbar() {
  const { isAuthenticated, isLoading, user, loginWithRedirect, logout, error } = useAuth0()
  const me = useMe()

  const visibleItems = NAV_ITEMS
    .filter((i) => !i.requiresAuth || isAuthenticated)
    .filter((i) => !i.path.startsWith('/admin') || me.data?.isAdmin)

  return (
    <header className="sticky top-0 z-30 border-b border-border-subtle bg-elevated/80 backdrop-blur">
      <div className="max-w-5xl mx-auto px-4 sm:px-6 py-3 flex items-center gap-x-4 gap-y-2 flex-wrap">
        <Link to="/" className="flex items-center gap-2 text-lg sm:text-xl font-bold tracking-tight text-fg-default">
          <Goal size={22} className="text-accent" />
          <span>Tip4Gen</span>
        </Link>
        <Popover className="md:hidden">
          <PopoverButton
            aria-label="Menü"
            className="inline-flex h-9 w-9 items-center justify-center rounded-full border border-border-subtle bg-elevated text-fg-muted transition hover:text-accent hover:border-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring-focus data-[open]:text-accent data-[open]:border-accent"
          >
            {({ open }) => (open ? <X size={18} /> : <Menu size={18} />)}
          </PopoverButton>
          <PopoverPanel className="absolute left-0 right-0 top-full mt-1 z-40 rounded-lg border border-border-subtle bg-elevated shadow-xl p-2 flex flex-col gap-1 mx-4 sm:mx-6">
            {visibleItems.map(({ path, end, label, Icon }) => (
              <CloseButton
                key={path}
                as={NavLink}
                to={path}
                end={end}
                className={path.startsWith('/admin') ? `${MOBILE_LINK_BASE} text-accent` : MOBILE_LINK_BASE}
              >
                <Icon size={16} />
                {label}
              </CloseButton>
            ))}
          </PopoverPanel>
        </Popover>
        <nav className="hidden md:flex items-center gap-x-4 text-xs font-mono uppercase tracking-[0.15em] text-fg-muted">
          {visibleItems.map(({ path, end, label, Icon }) => (
            <NavLink
              key={path}
              to={path}
              end={end}
              className={path.startsWith('/admin') ? `${NAV_LINK_BASE} text-accent` : NAV_LINK_BASE}
            >
              <Icon size={14} />
              {label}
            </NavLink>
          ))}
        </nav>
        <div className="ml-auto flex items-center gap-2 sm:gap-3 flex-wrap">
          {!isAuthConfigured && (
            <span className="text-xs font-mono text-warning">Auth0 nincs beállítva</span>
          )}
          {error && !/invalid state/i.test(error.message) && (
            <span className="text-xs font-mono text-danger truncate max-w-[400px]" title={error.message}>
              auth0 hiba: {error.message}
            </span>
          )}
          {isAuthConfigured && isLoading && (
            <span className="text-xs font-mono text-fg-subtle">betöltés…</span>
          )}
          {isAuthConfigured && !isLoading && !isAuthenticated && (
            <button
              type="button"
              onClick={() => loginWithRedirect()}
              className="rounded-lg bg-accent px-3 py-1.5 text-xs font-mono uppercase tracking-[0.15em] text-on-accent transition hover:bg-accent-strong focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring-focus"
            >
              Belépés
            </button>
          )}
          {isAuthConfigured && isAuthenticated && (
            <>
              {me.data ? (
                <span className="hidden sm:flex items-center gap-2 text-xs font-mono text-fg-muted max-w-[220px]">
                  <Avatar
                    userId={me.data.id}
                    displayName={me.data.displayName}
                    version={me.data.avatarVersion}
                    size={24}
                  />
                  <span className="truncate">{me.data.displayName}</span>
                </span>
              ) : (
                <span className="hidden sm:inline text-xs font-mono text-fg-muted truncate max-w-[180px]">
                  {user?.name ?? user?.email ?? user?.sub}
                </span>
              )}
              <ThemeToggle />
              <button
                type="button"
                onClick={() => logout({ logoutParams: { returnTo: window.location.origin } })}
                className="rounded-lg border border-border-strong px-3 py-1.5 text-xs font-mono uppercase tracking-[0.15em] text-fg-muted transition hover:border-accent hover:text-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring-focus"
              >
                Kilépés
              </button>
            </>
          )}
          {(!isAuthConfigured || !isAuthenticated) && <ThemeToggle />}
        </div>
      </div>
    </header>
  )
}
