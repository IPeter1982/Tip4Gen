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
        className={`rounded-full border-2 border-stone-900 object-cover bg-white shrink-0 ${className}`}
      />
    )
  }
  return (
    <span
      aria-hidden
      style={{ ...dim, backgroundColor: `hsl(${hue}, 60%, 45%)`, fontSize: Math.round(size * 0.42) }}
      className={`inline-flex items-center justify-center rounded-full border-2 border-stone-900 text-white font-bold leading-none shrink-0 ${className}`}
    >
      {initial}
    </span>
  )
}
