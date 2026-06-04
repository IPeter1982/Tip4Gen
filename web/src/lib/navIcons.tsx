import {
  House,
  BookOpen,
  Goal,
  Trophy,
  Users,
  BarChart3,
  User,
  ShieldCheck,
  type LucideIcon,
} from 'lucide-react'

export type NavItem = {
  path: string
  end?: boolean
  label: string
  Icon: LucideIcon
}

export const NAV_ITEMS: NavItem[] = [
  { path: '/', end: true, label: 'Főoldal', Icon: House },
  { path: '/szabalyzat', label: 'Szabályzat', Icon: BookOpen },
  { path: '/matches', label: 'Mérkőzések', Icon: Goal },
  { path: '/long-tips', label: 'Végső győztes', Icon: Trophy },
  { path: '/team', label: 'Csapat', Icon: Users },
  { path: '/leaderboard', label: 'Ranglista', Icon: BarChart3 },
  { path: '/me', label: 'Profil', Icon: User },
  { path: '/admin', label: 'Admin', Icon: ShieldCheck },
]

export const NAV_ICONS: Record<string, LucideIcon> = Object.fromEntries(
  NAV_ITEMS.map((i) => [i.path, i.Icon]),
)
