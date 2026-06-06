import { useState } from 'react'
import { Link } from 'react-router'
import { ChevronDown, ChevronRight, Pencil, ShieldCheck, Trash2, UserMinus } from 'lucide-react'
import {
  useAdminTeams,
  useDeleteAdminTeam,
  useRemoveAdminTeamMember,
  useRenameAdminTeam,
} from '../../api/hooks'
import type { TeamAdminMemberView, TeamAdminView, TeamStatus } from '../../api/types'
import { ApiError } from '../../api/errors'
import { ConfirmDialog } from '../../components/ConfirmDialog'
import { formatBudapest } from '../../lib/format'

const STATUS_LABEL: Record<TeamStatus, string> = {
  Forming: 'Alakuló',
  Locked: 'Lezárva',
  Disqualified: 'Kizárva',
}

const STATUS_CLASS: Record<TeamStatus, string> = {
  Forming: 'text-warning',
  Locked: 'text-accent',
  Disqualified: 'text-danger',
}

function errorMessage(err: unknown): string {
  if (err instanceof ApiError) return err.message
  if (err instanceof Error) return err.message
  return String(err)
}

export function AdminTeams() {
  const teams = useAdminTeams()
  const [expanded, setExpanded] = useState<Set<string>>(new Set())
  const [renameFor, setRenameFor] = useState<TeamAdminView | null>(null)
  const [deleteFor, setDeleteFor] = useState<TeamAdminView | null>(null)
  const [removeMember, setRemoveMember] = useState<
    { team: TeamAdminView; member: TeamAdminMemberView } | null
  >(null)

  const toggle = (id: string) =>
    setExpanded((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })

  return (
    <div className="max-w-5xl mx-auto px-6 py-10 space-y-6">
      <Link
        to="/admin"
        className="text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle hover:text-accent"
      >
        ← vissza
      </Link>

      <header>
        <p className="inline-flex items-center gap-1.5 text-xs font-mono uppercase tracking-[0.2em] text-accent">
          <ShieldCheck size={14} />
          Admin
        </p>
        <h1 className="text-4xl font-black uppercase tracking-tight mt-2">Csapatok</h1>
        <p className="font-mono text-sm text-fg-subtle mt-2">
          Minden csapat (Alakuló, Lezárva, Kizárva). Átnevezés és törlés bármilyen állapotban —
          tag eltávolítás után, ha a csapat 3 fő alá esik, visszakerül Alakuló állapotba.
        </p>
      </header>

      {teams.isLoading && <p className="font-mono text-fg-subtle">betöltés…</p>}
      {teams.error && (
        <p className="border-2 border-danger bg-danger/10 p-4 font-mono text-sm text-danger">
          ⚠ {errorMessage(teams.error)}
        </p>
      )}

      {teams.data && (
        <section className="space-y-3">
          {teams.data.length === 0 && (
            <p className="border-2 border-border-subtle p-4 font-mono text-sm text-fg-subtle">
              nincs csapat
            </p>
          )}

          {teams.data.map((team) => {
            const isOpen = expanded.has(team.id)
            return (
              <article key={team.id} className="border-2 border-border-strong bg-elevated">
                <header className="flex items-center gap-3 px-4 py-3">
                  <button
                    type="button"
                    onClick={() => toggle(team.id)}
                    className="inline-flex items-center gap-2 text-left flex-1 min-w-0"
                    aria-expanded={isOpen}
                  >
                    {isOpen ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
                    <span className="font-mono font-bold truncate">{team.name}</span>
                    <span
                      className={`text-xs font-mono uppercase tracking-[0.15em] ${STATUS_CLASS[team.status]}`}
                    >
                      {STATUS_LABEL[team.status]}
                    </span>
                    <span className="text-xs font-mono text-fg-subtle">
                      {team.memberCount}/3 fő
                      {team.aiMemberCount > 0 && ` · ${team.aiMemberCount} AI`}
                    </span>
                  </button>
                  <span className="hidden sm:inline text-xs font-mono text-fg-subtle">
                    {formatBudapest(team.createdAt)}
                  </span>
                  <button
                    type="button"
                    onClick={() => setRenameFor(team)}
                    className="inline-flex items-center gap-1.5 border-2 border-border-strong px-3 py-1 text-xs font-mono uppercase tracking-[0.15em] hover:bg-sunken"
                    title="Átnevezés"
                  >
                    <Pencil size={12} />
                    Átnevezés
                  </button>
                  <button
                    type="button"
                    onClick={() => setDeleteFor(team)}
                    className="inline-flex items-center gap-1.5 border-2 border-danger text-danger px-3 py-1 text-xs font-mono uppercase tracking-[0.15em] hover:bg-danger hover:text-white"
                    title="Csapat törlése"
                  >
                    <Trash2 size={12} />
                    Törlés
                  </button>
                </header>

                {isOpen && (
                  <div className="border-t-2 border-border-subtle bg-sunken px-4 py-3 space-y-2">
                    {team.members.length === 0 && (
                      <p className="font-mono text-xs text-fg-subtle">nincs tag</p>
                    )}
                    {team.members.map((m) => (
                      <div
                        key={m.id}
                        className="flex items-center gap-3 border-2 border-border-subtle bg-elevated px-3 py-2"
                      >
                        <span className="font-mono text-sm flex-1 truncate">
                          {m.displayName}
                        </span>
                        <span className="text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle">
                          {m.isAi ? 'AI' : 'Ember'}
                        </span>
                        <span className="hidden sm:inline text-xs font-mono text-fg-subtle">
                          csatlakozott: {formatBudapest(m.joinedAt)}
                        </span>
                        <button
                          type="button"
                          onClick={() => setRemoveMember({ team, member: m })}
                          className="inline-flex items-center gap-1.5 border-2 border-border-strong text-danger px-2 py-1 text-xs font-mono uppercase tracking-[0.15em] hover:bg-danger hover:text-white hover:border-danger"
                          title="Tag eltávolítása"
                        >
                          <UserMinus size={12} />
                          Eltávolít
                        </button>
                      </div>
                    ))}
                  </div>
                )}
              </article>
            )
          })}
        </section>
      )}

      {renameFor && (
        <RenameModal team={renameFor} onClose={() => setRenameFor(null)} />
      )}

      {deleteFor && (
        <DeleteTeamConfirm team={deleteFor} onClose={() => setDeleteFor(null)} />
      )}

      {removeMember && (
        <RemoveMemberConfirm
          team={removeMember.team}
          member={removeMember.member}
          onClose={() => setRemoveMember(null)}
        />
      )}
    </div>
  )
}

function RenameModal({ team, onClose }: { team: TeamAdminView; onClose: () => void }) {
  const [name, setName] = useState(team.name)
  const [reason, setReason] = useState('')
  const mut = useRenameAdminTeam()

  const submit = async () => {
    const trimmed = name.trim()
    if (!trimmed) return
    try {
      await mut.mutateAsync({ teamId: team.id, name: trimmed, reason: reason.trim() || null })
      onClose()
    } catch {
      // error surfaced below
    }
  }

  return (
    <div
      className="fixed inset-0 z-50 grid place-items-center bg-black/60 backdrop-blur-sm p-6"
      onClick={(e) => {
        if (e.target === e.currentTarget) onClose()
      }}
      role="dialog"
      aria-modal="true"
    >
      <div className="max-w-md w-full rounded-2xl border border-border-strong bg-elevated p-6 space-y-4 shadow-2xl">
        <h2 className="text-xl font-bold tracking-tight">Csapat átnevezése</h2>
        <p className="text-xs font-mono text-fg-subtle">jelenleg: {team.name}</p>

        <div className="space-y-2">
          <label className="block text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle">
            új név
          </label>
          <input
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            maxLength={80}
            autoFocus
            className="w-full border-2 border-border-strong px-3 py-2 font-mono text-sm bg-elevated"
          />
        </div>

        <div className="space-y-2">
          <label className="block text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle">
            indoklás (opcionális)
          </label>
          <input
            type="text"
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            className="w-full border-2 border-border-strong px-3 py-2 font-mono text-sm bg-elevated"
          />
        </div>

        {mut.error && (
          <p className="border-2 border-danger bg-danger/10 p-3 font-mono text-xs text-danger">
            ⚠ {errorMessage(mut.error)}
          </p>
        )}

        <div className="flex items-center justify-end gap-3 pt-2">
          <button
            type="button"
            onClick={onClose}
            disabled={mut.isPending}
            className="rounded-lg border border-border-strong bg-elevated px-4 py-2 text-xs font-mono uppercase tracking-[0.15em] text-fg-muted hover:border-accent hover:text-accent disabled:opacity-40"
          >
            Mégse
          </button>
          <button
            type="button"
            onClick={submit}
            disabled={mut.isPending || !name.trim() || name.trim() === team.name}
            className="rounded-lg bg-accent text-on-accent px-4 py-2 text-xs font-mono uppercase tracking-[0.15em] hover:bg-accent-strong disabled:opacity-40"
          >
            {mut.isPending ? 'mentés…' : 'Mentés'}
          </button>
        </div>
      </div>
    </div>
  )
}

function DeleteTeamConfirm({ team, onClose }: { team: TeamAdminView; onClose: () => void }) {
  const mut = useDeleteAdminTeam()

  const confirm = async () => {
    try {
      await mut.mutateAsync({ teamId: team.id, reason: null })
      onClose()
    } catch {
      // error visible in dialog
    }
  }

  return (
    <>
      <ConfirmDialog
        open={!mut.error}
        title={`Csapat törlése: ${team.name}`}
        body={`Biztosan törlöd a(z) „${team.name}" csapatot (${team.memberCount} fő)? A csapattagok pontjai az egyéni rangsorban megmaradnak, de a csapat eltűnik a csapat rangsorból.`}
        confirmLabel={mut.isPending ? 'törlés…' : 'Törlés'}
        destructive
        onConfirm={confirm}
        onCancel={onClose}
      />
      {mut.error && (
        <div className="fixed inset-0 z-50 grid place-items-center bg-black/60 backdrop-blur-sm p-6">
          <div className="max-w-md w-full rounded-2xl border border-danger bg-elevated p-6 space-y-4 shadow-2xl">
            <h2 className="text-xl font-bold text-danger">Hiba</h2>
            <p className="font-mono text-sm text-danger">⚠ {errorMessage(mut.error)}</p>
            <div className="flex items-center justify-end">
              <button
                type="button"
                onClick={onClose}
                className="rounded-lg border border-border-strong bg-elevated px-4 py-2 text-xs font-mono uppercase tracking-[0.15em] hover:border-accent hover:text-accent"
              >
                Bezár
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  )
}

function RemoveMemberConfirm({
  team,
  member,
  onClose,
}: {
  team: TeamAdminView
  member: TeamAdminMemberView
  onClose: () => void
}) {
  const mut = useRemoveAdminTeamMember()

  const isLastHuman =
    !member.isAi && team.humanMemberCount === 1
  const willRevert =
    !isLastHuman && team.status === 'Locked' && team.memberCount - 1 < 3

  const body = isLastHuman
    ? `${member.displayName} az utolsó emberi tag — eltávolítása a teljes csapatot törli (és az AI tagot is, ha van). A csapattagok egyéni pontjai megmaradnak.`
    : willRevert
      ? `${member.displayName} eltávolítása. A csapat 3 fő alá esik, így visszakerül Alakuló állapotba és a csapat rangsorban 0 pont lesz, amíg újra fel nem töltik.`
      : `${member.displayName} eltávolítása a(z) „${team.name}" csapatból.`

  const confirm = async () => {
    try {
      await mut.mutateAsync({ teamId: team.id, memberId: member.id, reason: null })
      onClose()
    } catch {
      // shown below
    }
  }

  return (
    <>
      <ConfirmDialog
        open={!mut.error}
        title={`Tag eltávolítása: ${member.displayName}`}
        body={body}
        confirmLabel={mut.isPending ? 'eltávolítás…' : 'Eltávolítás'}
        destructive
        onConfirm={confirm}
        onCancel={onClose}
      />
      {mut.error && (
        <div className="fixed inset-0 z-50 grid place-items-center bg-black/60 backdrop-blur-sm p-6">
          <div className="max-w-md w-full rounded-2xl border border-danger bg-elevated p-6 space-y-4 shadow-2xl">
            <h2 className="text-xl font-bold text-danger">Hiba</h2>
            <p className="font-mono text-sm text-danger">⚠ {errorMessage(mut.error)}</p>
            <div className="flex items-center justify-end">
              <button
                type="button"
                onClick={onClose}
                className="rounded-lg border border-border-strong bg-elevated px-4 py-2 text-xs font-mono uppercase tracking-[0.15em] hover:border-accent hover:text-accent"
              >
                Bezár
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  )
}
