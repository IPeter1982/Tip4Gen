import { Link, useLocation } from 'react-router'
import { ArrowLeft, Home as HomeIcon } from 'lucide-react'

export function NotFound() {
  const location = useLocation()
  return (
    <div className="max-w-xl mx-auto px-6 py-16 space-y-6">
      <header className="space-y-2">
        <p className="text-xs font-mono uppercase tracking-[0.2em] text-accent">404</p>
        <h1 className="text-5xl font-bold tracking-tight text-fg-default">Nincs ilyen oldal</h1>
      </header>

      <section className="rounded-2xl border border-border-subtle bg-elevated p-5 space-y-3">
        <p className="font-mono text-sm text-fg-muted">
          Az elérni kívánt útvonal nem található:
        </p>
        <pre className="font-mono text-xs text-fg-subtle break-all whitespace-pre-wrap">
          {location.pathname}
        </pre>
      </section>

      <div className="flex flex-wrap gap-2">
        <Link
          to="/"
          className="inline-flex items-center gap-1.5 rounded-lg bg-accent px-4 py-2 text-xs font-mono uppercase tracking-[0.15em] text-on-accent transition hover:bg-accent-strong"
        >
          <HomeIcon size={14} />
          Főoldal
        </Link>
        <Link
          to="/matches"
          className="inline-flex items-center gap-1.5 rounded-lg border border-border-strong px-4 py-2 text-xs font-mono uppercase tracking-[0.15em] text-fg-muted transition hover:text-accent hover:border-accent"
        >
          <ArrowLeft size={14} />
          Mérkőzések
        </Link>
      </div>
    </div>
  )
}
