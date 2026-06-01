import { useMe } from '../api/hooks'

type Props = {
  userId: string | null | undefined
  displayName: string
  version: string | null | undefined
  isAi?: boolean
  size?: number
  className?: string
}

export function Avatar({ userId, displayName, version, isAi, size = 32, className = '' }: Props) {
  const me = useMe()
  const aiVersion = me.data?.aiAvatarVersion ?? null

  const trimmed = displayName.trim()
  const initial = (trimmed.length > 0 ? trimmed[0] : '?').toUpperCase()
  const hue = hashHue(userId ?? trimmed)
  const dim = { width: size, height: size }

  // AI branch first — AI members never carry a userId, and the admin-uploaded
  // default replaces the per-name letter circle for the whole roster.
  if (isAi && aiVersion) {
    return (
      <img
        src={`/api/ai-avatar?v=${aiVersion}`}
        alt=""
        style={dim}
        className={`rounded-full border-2 border-stone-900 object-cover bg-white shrink-0 ${className}`}
      />
    )
  }

  if (userId && version) {
    return (
      <img
        src={`/api/users/${userId}/avatar?v=${version}`}
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

export function hashHue(seed: string): number {
  let h = 0
  for (let i = 0; i < seed.length; i++) h = (h * 31 + seed.charCodeAt(i)) >>> 0
  return h % 360
}
