import { TeamFlag } from './TeamFlag'

interface TeamLabelProps {
  team: { name: string; code: string | null }
  size?: 'sm' | 'md' | 'lg'
  className?: string
}

export function TeamLabel({ team, size = 'sm', className }: TeamLabelProps) {
  return (
    <span className={`inline-flex items-center gap-2 ${className ?? ''}`}>
      <TeamFlag code={team.code} size={size} />
      <span>{team.name}</span>
    </span>
  )
}
