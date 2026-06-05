import { useState } from 'react'
import { Link } from 'react-router'
import { useAdminAuditLog } from '../../api/hooks'
import type { AdminAuditAction, AdminAuditRow } from '../../api/types'
import { formatBudapest } from '../../lib/format'

const ACTION_LABEL: Record<AdminAuditAction, string> = {
  MatchSetResult: 'Eredmény beírása',
  MatchCancel: 'Mérkőzés lemondása',
  MatchPostpone: 'Halasztás',
  MatchRescore: 'Újraértékelés',
  LongTipOutcomesSet: 'Végső győztes eredmény',
  AiAvatarSet: 'AI profilkép beállítása',
  AiAvatarDeleted: 'AI profilkép törlése',
  AiTipperManualRun: 'AI tippelő manuális futtatás',
  PlayersImported: 'Játékosok importálása',
}

const PAGE_SIZE = 50

export function AdminAudit() {
  const [matchFilter, setMatchFilter] = useState('')
  const [skip, setSkip] = useState(0)
  const audit = useAdminAuditLog(matchFilter.trim() || undefined, PAGE_SIZE, skip)

  return (
    <div className="max-w-4xl mx-auto px-6 py-10 space-y-6">
      <Link
        to="/admin"
        className="text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle hover:text-accent"
      >
        ← vissza
      </Link>

      <header>
        <p className="text-xs font-mono uppercase tracking-[0.2em] text-accent">Admin</p>
        <h1 className="text-4xl font-black uppercase tracking-tight mt-2">Audit log</h1>
      </header>

      <div className="flex items-center gap-3">
        <input
          type="text"
          placeholder="Mérkőzés id szűrő (uuid)"
          value={matchFilter}
          onChange={(e) => {
            setMatchFilter(e.target.value)
            setSkip(0)
          }}
          className="flex-1 border-2 border-border-strong px-3 py-2 font-mono text-sm"
        />
        {matchFilter && (
          <button
            type="button"
            onClick={() => {
              setMatchFilter('')
              setSkip(0)
            }}
            className="border-2 border-border-strong bg-elevated px-3 py-2 text-xs font-mono uppercase tracking-[0.15em] hover:bg-sunken"
          >
            Töröl
          </button>
        )}
      </div>

      {audit.isLoading && <p className="font-mono text-fg-subtle">betöltés…</p>}
      {audit.error && (
        <p className="border-2 border-danger bg-danger/10 p-4 font-mono text-sm text-danger">
          ⚠ {audit.error instanceof Error ? audit.error.message : String(audit.error)}
        </p>
      )}

      {audit.data && (
        <>
          <p className="text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle">
            {audit.data.total} bejegyzés · {skip + 1}–{Math.min(skip + audit.data.rows.length, audit.data.total)}
          </p>
          <ul className="space-y-3">
            {audit.data.rows.map((row) => (
              <AuditRow key={row.id} row={row} />
            ))}
            {audit.data.rows.length === 0 && (
              <li className="border-2 border-border-subtle p-4 font-mono text-sm text-fg-subtle">
                nincs ilyen bejegyzés
              </li>
            )}
          </ul>

          <div className="flex items-center gap-3">
            <button
              type="button"
              disabled={skip === 0}
              onClick={() => setSkip(Math.max(0, skip - PAGE_SIZE))}
              className="border-2 border-border-strong bg-elevated px-3 py-2 text-xs font-mono uppercase tracking-[0.15em] disabled:opacity-40"
            >
              ← előző
            </button>
            <button
              type="button"
              disabled={skip + PAGE_SIZE >= audit.data.total}
              onClick={() => setSkip(skip + PAGE_SIZE)}
              className="border-2 border-border-strong bg-elevated px-3 py-2 text-xs font-mono uppercase tracking-[0.15em] disabled:opacity-40"
            >
              következő →
            </button>
          </div>
        </>
      )}
    </div>
  )
}

function AuditRow({ row }: { row: AdminAuditRow }) {
  const [open, setOpen] = useState(false)

  return (
    <li className="border-2 border-border-strong bg-elevated">
      <button
        type="button"
        onClick={() => setOpen(!open)}
        className="w-full text-left px-4 py-3 flex items-baseline gap-3 hover:bg-sunken"
      >
        <span className="text-xs font-mono uppercase tracking-[0.15em] text-accent">
          {ACTION_LABEL[row.action] ?? row.action}
        </span>
        <span className="font-mono text-sm text-fg-default flex-1 truncate">
          {row.adminDisplayName}
          {row.entityId && <span className="text-fg-subtle"> · {row.entityType} {row.entityId.slice(0, 8)}…</span>}
        </span>
        <span className="text-xs font-mono text-fg-subtle">{formatBudapest(row.occurredAt)}</span>
      </button>
      {open && (
        <div className="border-t-2 border-border-subtle px-4 py-3 space-y-2 bg-sunken">
          {row.reason && (
            <div>
              <p className="text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle">Megjegyzés</p>
              <p className="font-mono text-sm">{row.reason}</p>
            </div>
          )}
          {row.beforeJson && (
            <div>
              <p className="text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle">Előtte</p>
              <pre className="font-mono text-xs whitespace-pre-wrap text-fg-default bg-elevated border-2 border-border-subtle p-2">
                {formatJson(row.beforeJson)}
              </pre>
            </div>
          )}
          {row.afterJson && (
            <div>
              <p className="text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle">Utána</p>
              <pre className="font-mono text-xs whitespace-pre-wrap text-fg-default bg-elevated border-2 border-border-subtle p-2">
                {formatJson(row.afterJson)}
              </pre>
            </div>
          )}
        </div>
      )}
    </li>
  )
}

function formatJson(raw: string): string {
  try {
    return JSON.stringify(JSON.parse(raw), null, 2)
  } catch {
    return raw
  }
}
