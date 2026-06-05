import { useState } from 'react'
import { Link } from 'react-router'
import { Users } from 'lucide-react'
import { ApiError } from '../../api/errors'
import { useImportPlayers, useLastPlayersImport, usePlayers } from '../../api/hooks'
import type { PlayersImportResponse } from '../../api/types'
import { ConfirmDialog } from '../../components/ConfirmDialog'
import { formatBudapest } from '../../lib/format'

function errorMessage(e: unknown): string {
  if (e instanceof ApiError) return e.message
  if (e instanceof Error) return e.message
  return String(e)
}

type LastSummary = {
  added?: number
  skipped?: number
  unmatchedTeams?: number
  totalAfter?: number
  parsedTotal?: number
}

function parseLastSummary(afterJson: string | null): LastSummary | null {
  if (!afterJson) return null
  try {
    return JSON.parse(afterJson) as LastSummary
  } catch {
    return null
  }
}

export function AdminPlayers() {
  const players = usePlayers()
  const lastImport = useLastPlayersImport()
  const importPlayers = useImportPlayers()

  const [confirmOpen, setConfirmOpen] = useState(false)
  const [result, setResult] = useState<PlayersImportResponse | null>(null)
  const [error, setError] = useState<string | null>(null)

  const onConfirm = async () => {
    setConfirmOpen(false)
    setError(null)
    setResult(null)
    try {
      const r = await importPlayers.mutateAsync()
      setResult(r)
    } catch (e) {
      setError(errorMessage(e))
    }
  }

  const lastSummary = parseLastSummary(lastImport.data?.afterJson ?? null)
  const currentCount = players.data?.length ?? 0

  return (
    <div className="max-w-2xl mx-auto px-6 py-10 space-y-6">
      <Link
        to="/admin"
        className="text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle hover:text-accent"
      >
        ← vissza
      </Link>

      <header className="space-y-2">
        <p className="inline-flex items-center gap-1.5 text-xs font-mono uppercase tracking-[0.2em] text-accent">
          <Users size={14} />
          Admin
        </p>
        <h1 className="text-3xl font-black uppercase tracking-tight">Játékosok importálása</h1>
        <p className="font-mono text-sm text-fg-muted">
          Forrás:{' '}
          <a
            href="https://en.wikipedia.org/wiki/2026_FIFA_World_Cup_squads"
            target="_blank"
            rel="noopener noreferrer"
            className="underline hover:text-accent"
          >
            Wikipedia – 2026 FIFA World Cup squads
          </a>
          . Az importálás idempotens: a meglévő játékosokat nem módosítja, csak az új sorokat
          szúrja be. Az ország a játékos nemzeti csapata szerint dől el.
        </p>
      </header>

      <section className="border-2 border-border-strong bg-elevated p-5 space-y-3">
        <p className="text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle">
          Jelenlegi állapot
        </p>
        <p className="font-mono text-sm">
          Adatbázisban: <span className="text-fg-default font-bold">{currentCount}</span> játékos.
        </p>
        {lastImport.data ? (
          <p className="font-mono text-xs text-fg-muted">
            Utolsó importálás: {formatBudapest(lastImport.data.occurredAt)}
            {lastSummary && (
              <>
                {' '}· hozzáadva: {lastSummary.added ?? 0} · kihagyva: {lastSummary.skipped ?? 0}
                {(lastSummary.unmatchedTeams ?? 0) > 0 && (
                  <> · ismeretlen csapat: {lastSummary.unmatchedTeams}</>
                )}
              </>
            )}
          </p>
        ) : (
          <p className="font-mono text-xs text-fg-subtle">Még nem futott importálás.</p>
        )}
      </section>

      <div>
        <button
          type="button"
          onClick={() => setConfirmOpen(true)}
          disabled={importPlayers.isPending}
          className="border-2 border-accent bg-accent text-on-accent px-4 py-2 text-xs font-mono uppercase tracking-[0.15em] hover:bg-accent-strong hover:border-accent disabled:opacity-40"
        >
          {importPlayers.isPending ? 'futtatás…' : 'Importálás futtatása'}
        </button>
      </div>

      {result && (
        <section className="border-2 border-success bg-success/10 p-4 font-mono text-xs text-success space-y-1">
          <p className="uppercase tracking-[0.15em]">Kész</p>
          <p>
            Hozzáadva: <span className="font-bold">{result.added}</span> · Kihagyva:{' '}
            {result.skipped} · Ismeretlen csapat: {result.unmatchedTeams} · Összesen most:{' '}
            <span className="font-bold">{result.totalAfter}</span> · Idő: {result.durationMs} ms
          </p>
        </section>
      )}

      {error && (
        <section className="border-2 border-danger bg-danger/10 p-4 font-mono text-xs text-danger">
          ⚠ {error}
        </section>
      )}

      <ConfirmDialog
        open={confirmOpen}
        title="Importálás futtatása"
        body="A Wikipediáról letöltjük a 2026-os vb-keret-listát és új játékos-sorokat szúrunk be. A folyamat ~10–30 másodperc lehet. Biztosan futtatod?"
        confirmLabel="Igen, futtasd"
        onConfirm={onConfirm}
        onCancel={() => setConfirmOpen(false)}
      />
    </div>
  )
}
