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
  LongTipOutcomesSet: 'Hosszú tipp eredmény',
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
        className="text-xs font-mono uppercase tracking-[0.15em] text-stone-500 hover:text-stone-900"
      >
        ← vissza
      </Link>

      <header>
        <p className="text-xs font-mono uppercase tracking-[0.2em] text-orange-600">Admin</p>
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
          className="flex-1 border-2 border-stone-900 px-3 py-2 font-mono text-sm"
        />
        {matchFilter && (
          <button
            type="button"
            onClick={() => {
              setMatchFilter('')
              setSkip(0)
            }}
            className="border-2 border-stone-900 bg-white px-3 py-2 text-xs font-mono uppercase tracking-[0.15em] hover:bg-stone-100"
          >
            Töröl
          </button>
        )}
      </div>

      {audit.isLoading && <p className="font-mono text-stone-500">betöltés…</p>}
      {audit.error && (
        <p className="border-2 border-red-700 bg-red-50 p-4 font-mono text-sm text-red-800">
          ⚠ {audit.error instanceof Error ? audit.error.message : String(audit.error)}
        </p>
      )}

      {audit.data && (
        <>
          <p className="text-xs font-mono uppercase tracking-[0.15em] text-stone-500">
            {audit.data.total} bejegyzés · {skip + 1}–{Math.min(skip + audit.data.rows.length, audit.data.total)}
          </p>
          <ul className="space-y-3">
            {audit.data.rows.map((row) => (
              <AuditRow key={row.id} row={row} />
            ))}
            {audit.data.rows.length === 0 && (
              <li className="border-2 border-stone-200 p-4 font-mono text-sm text-stone-500">
                nincs ilyen bejegyzés
              </li>
            )}
          </ul>

          <div className="flex items-center gap-3">
            <button
              type="button"
              disabled={skip === 0}
              onClick={() => setSkip(Math.max(0, skip - PAGE_SIZE))}
              className="border-2 border-stone-900 bg-white px-3 py-2 text-xs font-mono uppercase tracking-[0.15em] disabled:opacity-40"
            >
              ← előző
            </button>
            <button
              type="button"
              disabled={skip + PAGE_SIZE >= audit.data.total}
              onClick={() => setSkip(skip + PAGE_SIZE)}
              className="border-2 border-stone-900 bg-white px-3 py-2 text-xs font-mono uppercase tracking-[0.15em] disabled:opacity-40"
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
    <li className="border-2 border-stone-900 bg-white">
      <button
        type="button"
        onClick={() => setOpen(!open)}
        className="w-full text-left px-4 py-3 flex items-baseline gap-3 hover:bg-stone-50"
      >
        <span className="text-xs font-mono uppercase tracking-[0.15em] text-orange-600">
          {ACTION_LABEL[row.action] ?? row.action}
        </span>
        <span className="font-mono text-sm text-stone-700 flex-1 truncate">
          {row.adminDisplayName}
          {row.entityId && <span className="text-stone-400"> · {row.entityType} {row.entityId.slice(0, 8)}…</span>}
        </span>
        <span className="text-xs font-mono text-stone-500">{formatBudapest(row.occurredAt)}</span>
      </button>
      {open && (
        <div className="border-t-2 border-stone-200 px-4 py-3 space-y-2 bg-stone-50">
          {row.reason && (
            <div>
              <p className="text-xs font-mono uppercase tracking-[0.15em] text-stone-500">Megjegyzés</p>
              <p className="font-mono text-sm">{row.reason}</p>
            </div>
          )}
          {row.beforeJson && (
            <div>
              <p className="text-xs font-mono uppercase tracking-[0.15em] text-stone-500">Előtte</p>
              <pre className="font-mono text-xs whitespace-pre-wrap text-stone-800 bg-white border-2 border-stone-200 p-2">
                {formatJson(row.beforeJson)}
              </pre>
            </div>
          )}
          {row.afterJson && (
            <div>
              <p className="text-xs font-mono uppercase tracking-[0.15em] text-stone-500">Utána</p>
              <pre className="font-mono text-xs whitespace-pre-wrap text-stone-800 bg-white border-2 border-stone-200 p-2">
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
