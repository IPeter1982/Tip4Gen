import { useState } from 'react'
import { fifaToIso2 } from '../lib/teamFlag'

type Size = 'sm' | 'md' | 'lg'

const SIZE: Record<Size, { w: number; h: number; cls: string }> = {
  sm: { w: 20, h: 15, cls: 'w-5 h-[15px]' },
  md: { w: 28, h: 21, cls: 'w-7 h-[21px]' },
  lg: { w: 40, h: 30, cls: 'w-10 h-[30px]' },
}

interface TeamFlagProps {
  code: string | null | undefined
  size?: Size
  className?: string
}

export function TeamFlag({ code, size = 'sm', className }: TeamFlagProps) {
  const iso = fifaToIso2(code)
  const [failed, setFailed] = useState(false)
  if (!iso || failed) return null
  const s = SIZE[size]
  return (
    <img
      src={`https://flagcdn.com/${iso}.svg`}
      alt=""
      aria-hidden
      width={s.w}
      height={s.h}
      loading="lazy"
      onError={() => setFailed(true)}
      className={`inline-block shrink-0 rounded-sm ring-1 ring-border-subtle dark:ring-white/15 object-cover ${s.cls} ${className ?? ''}`}
    />
  )
}
