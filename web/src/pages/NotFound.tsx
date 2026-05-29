import { Link, useLocation } from 'react-router'

export function NotFound() {
  const location = useLocation()
  return (
    <div className="max-w-xl mx-auto px-6 py-16 space-y-6">
      <header className="space-y-2">
        <p className="text-xs font-mono uppercase tracking-[0.2em] text-orange-600">404</p>
        <h1 className="text-5xl font-black uppercase tracking-tight">Nincs ilyen oldal</h1>
      </header>

      <section className="border-2 border-stone-900 bg-white p-5 space-y-3">
        <p className="font-mono text-sm text-stone-700">
          Az elérni kívánt útvonal nem található:
        </p>
        <pre className="font-mono text-xs text-stone-500 break-all whitespace-pre-wrap">
          {location.pathname}
        </pre>
      </section>

      <div className="flex flex-wrap gap-2">
        <Link
          to="/"
          className="border-2 border-stone-900 bg-stone-900 text-white px-4 py-2 text-xs font-mono uppercase tracking-[0.15em] hover:bg-orange-600 hover:border-orange-600"
        >
          Főoldal
        </Link>
        <Link
          to="/matches"
          className="border-2 border-stone-900 bg-white text-stone-900 px-4 py-2 text-xs font-mono uppercase tracking-[0.15em] hover:bg-stone-900 hover:text-white"
        >
          Mérkőzések
        </Link>
      </div>
    </div>
  )
}
