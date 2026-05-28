import { useEffect, useRef } from 'react'

type Props = {
  open: boolean
  title: string
  body: string
  confirmLabel?: string
  cancelLabel?: string
  destructive?: boolean
  onConfirm: () => void
  onCancel: () => void
}

/// Brutalist confirm modal. Escape cancels, click-outside cancels, focus moves
/// to the confirm button on open. Destructive=true paints the confirm red so the
/// admin gets a visual gut-check before a cancel/postpone goes through.
export function ConfirmDialog({
  open,
  title,
  body,
  confirmLabel = 'Megerősítés',
  cancelLabel = 'Mégse',
  destructive = false,
  onConfirm,
  onCancel,
}: Props) {
  const confirmRef = useRef<HTMLButtonElement>(null)

  useEffect(() => {
    if (!open) return
    confirmRef.current?.focus()

    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onCancel()
    }
    document.addEventListener('keydown', onKey)
    return () => document.removeEventListener('keydown', onKey)
  }, [open, onCancel])

  if (!open) return null

  return (
    <div
      className="fixed inset-0 z-50 grid place-items-center bg-black/50 p-6"
      onClick={(e) => {
        if (e.target === e.currentTarget) onCancel()
      }}
      role="dialog"
      aria-modal="true"
      aria-labelledby="confirm-title"
    >
      <div className="max-w-md w-full border-2 border-stone-900 bg-white p-6 space-y-4">
        <h2 id="confirm-title" className="text-xl font-black uppercase tracking-tight">
          {title}
        </h2>
        <p className="font-mono text-sm text-stone-700">{body}</p>
        <div className="flex items-center justify-end gap-3 pt-2">
          <button
            type="button"
            onClick={onCancel}
            className="border-2 border-stone-900 bg-white px-4 py-2 text-xs font-mono uppercase tracking-[0.15em] hover:bg-stone-100"
          >
            {cancelLabel}
          </button>
          <button
            ref={confirmRef}
            type="button"
            onClick={onConfirm}
            className={`border-2 px-4 py-2 text-xs font-mono uppercase tracking-[0.15em] ${
              destructive
                ? 'border-red-700 bg-red-700 text-white hover:bg-red-800 hover:border-red-800'
                : 'border-stone-900 bg-stone-900 text-white hover:bg-orange-600 hover:border-orange-600'
            }`}
          >
            {confirmLabel}
          </button>
        </div>
      </div>
    </div>
  )
}
