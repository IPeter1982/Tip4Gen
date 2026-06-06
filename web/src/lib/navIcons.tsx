import {
  House,
  BookOpen,
  Goal,
  Trophy,
  Users,
  UserRound,
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
  requiresAuth?: boolean
}

export const NAV_ITEMS: NavItem[] = [
  { path: '/', end: true, label: 'Főoldal', Icon: House },
  { path: '/szabalyzat', label: 'Szabályzat', Icon: BookOpen },
  { path: '/matches', label: 'Mérkőzések', Icon: Goal, requiresAuth: true },
  { path: '/long-tips', label: 'Végső győztes', Icon: Trophy, requiresAuth: true },
  { path: '/team', label: 'Csapat', Icon: Users, requiresAuth: true },
  { path: '/leaderboard', label: 'Ranglista', Icon: BarChart3, requiresAuth: true },
  { path: '/me', label: 'Profil', Icon: User, requiresAuth: true },
  { path: '/admin', label: 'Admin', Icon: ShieldCheck, requiresAuth: true },
  { path: '/admin/teams', label: 'Csapatok', Icon: Users, requiresAuth: true },
  { path: '/admin/players', label: 'Játékosok', Icon: UserRound, requiresAuth: true },
]

export const NAV_ICONS: Record<string, LucideIcon> = Object.fromEntries(
  NAV_ITEMS.map((i) => [i.path, i.Icon]),
)
