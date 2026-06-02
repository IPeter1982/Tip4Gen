import { hashHue } from './Avatar'

type Props = {
  teamId: string
  teamName: string
  version: string | null | undefined
  size?: number
  className?: string
}

export function TeamAvatar({ teamId, teamName, version, size = 32, className = '' }: Props) {
  const trimmed = teamName.trim()
  const initial = (trimmed.length > 0 ? trimmed[0] : '?').toUpperCase()
  const hue = hashHue(teamId || trimmed)
  const dim = { width: size, height: size }

  if (version) {
    return (
      <img
        src={`/api/teams/${teamId}/avatar?v=${version}`}
        alt=""
        style={dim}
        className={`rounded-full ring-1 ring-border-strong object-cover bg-elevated shrink-0 ${className}`}
      />
    )
  }
  return (
    <span
      aria-hidden
      style={{ ...dim, backgroundColor: `hsl(${hue}, 65%, 50%)`, fontSize: Math.round(size * 0.42) }}
      className={`inline-flex items-center justify-center rounded-full ring-1 ring-white/10 text-white font-bold leading-none shrink-0 ${className}`}
    >
      {initial}
    </span>
  )
}
