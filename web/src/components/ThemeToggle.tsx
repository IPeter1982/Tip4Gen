import { Moon, Sun } from 'lucide-react'
import { useTheme } from '../theme/ThemeProvider'

export function ThemeToggle() {
  const { theme, toggle } = useTheme()
  const isDark = theme === 'dark'
  return (
    <button
      type="button"
      onClick={toggle}
      aria-label="Téma váltása"
      title={isDark ? 'Világos téma' : 'Sötét téma'}
      className="inline-flex h-9 w-9 items-center justify-center rounded-full border border-border-subtle bg-elevated text-fg-muted transition hover:text-accent hover:border-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring-focus"
    >
      {isDark ? <Sun size={18} /> : <Moon size={18} />}
    </button>
  )
}
