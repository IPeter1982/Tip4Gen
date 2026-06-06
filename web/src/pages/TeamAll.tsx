import { useState } from 'react'
import { ArrowLeft, Users } from 'lucide-react'
import { Link, useNavigate } from 'react-router'
import { useAllTeams, useJoinTeamDirect, useMyTeam } from '../api/hooks'
import type { TeamView } from '../api/types'
import { Avatar } from '../components/Avatar'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { TeamAvatar } from '../components/TeamAvatar'
import { errorMessage, STATUS_LABEL } from '../lib/teamReasons'

const MAX_MEMBERS = 3

export function TeamAll() {
  const teams = useAllTeams()
  const myTeam = useMyTeam()
  const join = useJoinTeamDirect()
  const navigate = useNavigate()

  const [confirmTarget, setConfirmTarget] = useState<TeamView | null>(null)
  const [err, setErr] = useState<string | null>(null)

  const hasOwnTeam = !!myTeam.data

  const onConfirm = () => {
    if (!confirmTarget) return
    const teamId = confirmTarget.id
    setErr(null)
    join.mutate(teamId, {
      onSuccess: () => {
        setConfirmTarget(null)
        navigate('/team')
      },
      onError: (e) => {
        setConfirmTarget(null)
        setErr(errorMessage(e))
      },
    })
  }

  return (
    <div className="max-w-3xl mx-auto px-6 py-10 space-y-6">
      <header className="space-y-3">
        <Link
          to="/team"
          className="inline-flex items-center gap-1.5 text-xs font-mono uppercase tracking-[0.15em] text-fg-muted hover:text-accent"
        >
          <ArrowLeft size={14} />
          Vissza a saját csapathoz
        </Link>
        <div>
          <p className="inline-flex items-center gap-1.5 text-xs font-mono uppercase tracking-[0.2em] text-accent">
            <Users size={14} />
            Csapatok
          </p>
          <h1 className="text-4xl font-black uppercase tracking-tight mt-2">
            Összes csapat
          </h1>
          {teams.data && (
            <p className="font-mono text-sm text-fg-muted mt-1">
              Csapatok száma: <span className="text-fg-default">{teams.data.length}</span>
            </p>
          )}
        </div>
      </header>

      {err && (
        <p className="border border-danger bg-danger/10 p-4 font-mono text-sm text-danger">
          ⚠ {err}
        </p>
      )}

      {teams.isLoading && <p className="font-mono text-fg-subtle">betöltés…</p>}
      {teams.error && (
        <p className="border border-danger bg-danger/10 p-4 font-mono text-sm text-danger">
          ⚠ {errorMessage(teams.error)}
        </p>
      )}

      {!teams.isLoading && !teams.error && teams.data && teams.data.length === 0 && (
        <p className="border border-border-subtle bg-elevated p-6 font-mono text-sm text-fg-muted">
          Még nincs egy csapat sem.
        </p>
      )}

      {!teams.isLoading && !teams.error && teams.data && teams.data.length > 0 && (
        <ul className="space-y-4">
          {teams.data.map((team) => (
            <li key={team.id}>
              <TeamCard
                team={team}
                hasOwnTeam={hasOwnTeam}
                isMine={myTeam.data?.id === team.id}
                pending={join.isPending}
                onJoin={() => setConfirmTarget(team)}
              />
            </li>
          ))}
        </ul>
      )}

      <ConfirmDialog
        open={confirmTarget !== null}
        title="Csatlakozás a csapathoz"
        body={
          confirmTarget
            ? `Biztosan belépsz a(z) „${confirmTarget.name}" csapatba?`
            : ''
        }
        confirmLabel="Igen, belépek"
        cancelLabel="Mégsem"
        onConfirm={onConfirm}
        onCancel={() => setConfirmTarget(null)}
      />
    </div>
  )
}

type CardProps = {
  team: TeamView
  hasOwnTeam: boolean
  isMine: boolean
  pending: boolean
  onJoin: () => void
}

function TeamCard({ team, hasOwnTeam, isMine, pending, onJoin }: CardProps) {
  const memberCount = team.members.length
  const full = memberCount >= MAX_MEMBERS
  const forming = team.status === 'Forming'

  const buttonState: { label: string; enabled: boolean } = (() => {
    if (isMine) return { label: 'A te csapatod', enabled: false }
    if (hasOwnTeam) return { label: 'Már van csapatod', enabled: false }
    if (!forming) return { label: STATUS_LABEL[team.status], enabled: false }
    if (full) return { label: 'Megtelt', enabled: false }
    return { label: 'Belépés', enabled: true }
  })()

  return (
    <article
      className={`border bg-elevated p-5 space-y-4 ${
        isMine ? 'border-accent' : 'border-border-strong'
      }`}
    >
      <div className="flex items-start gap-4">
        <TeamAvatar
          teamId={team.id}
          teamName={team.name}
          version={team.avatarVersion}
          size={56}
        />
        <div className="flex-1 min-w-0">
          <h2 className="text-xl font-black uppercase tracking-tight truncate">
            {team.name}
          </h2>
          <p className="mt-1 text-xs font-mono uppercase tracking-[0.15em] text-fg-subtle">
            <span className="text-fg-default">{STATUS_LABEL[team.status]}</span>
            <span className="mx-2 text-fg-subtle">·</span>
            <span className="text-fg-default">{memberCount}/{MAX_MEMBERS}</span>
            <span className="ml-1 text-fg-subtle">tag</span>
          </p>
        </div>
      </div>

      {team.members.length > 0 && (
        <ul className="flex flex-wrap gap-x-4 gap-y-2">
          {team.members.map((m) => (
            <li key={m.id} className="flex items-center gap-2 min-w-0">
              <Avatar
                userId={m.userId}
                displayName={m.displayName}
                version={m.avatarVersion}
                isAi={m.isAi}
                size={28}
              />
              <span className="font-mono text-sm truncate">
                {m.displayName}
                {m.isAi && (
                  <span className="ml-1 text-xs uppercase tracking-[0.15em] text-fg-subtle">
                    AI
                  </span>
                )}
              </span>
            </li>
          ))}
        </ul>
      )}

      <div className="flex justify-end">
        <button
          type="button"
          disabled={!buttonState.enabled || pending}
          onClick={onJoin}
          className="border border-accent bg-accent text-on-accent px-5 py-2 text-xs font-mono uppercase tracking-[0.2em] hover:bg-accent-strong hover:border-accent disabled:bg-elevated disabled:text-fg-subtle disabled:border-border-strong disabled:cursor-not-allowed"
        >
          {pending ? 'küldés…' : buttonState.label}
        </button>
      </div>
    </article>
  )
}
