import {
  Listbox,
  ListboxButton,
  ListboxOption,
  ListboxOptions,
} from '@headlessui/react'
import { ChevronDown } from 'lucide-react'
import { TeamFlag } from './TeamFlag'

interface Team {
  id: string
  name: string
  code: string | null
}

interface TeamSelectProps {
  teams: Team[]
  value: string | null
  onChange: (id: string | null) => void
  placeholder?: string
  disabled?: boolean
  id?: string
  className?: string
}

export function TeamSelect({
  teams,
  value,
  onChange,
  placeholder = '— válassz csapatot —',
  disabled,
  id,
  className,
}: TeamSelectProps) {
  const selected = teams.find((t) => t.id === value) ?? null

  return (
    <Listbox value={value ?? ''} onChange={(v) => onChange(v === '' ? null : v)} disabled={disabled}>
      <div className={`relative ${className ?? ''}`}>
        <ListboxButton
          id={id}
          className="w-full rounded-lg border border-border-strong bg-sunken px-3 py-2 font-mono text-sm text-fg-default text-left flex items-center gap-2 data-[disabled]:opacity-60 data-[disabled]:cursor-not-allowed focus:outline-none focus-visible:ring-2 focus-visible:ring-ring-focus"
        >
          {selected ? (
            <>
              <TeamFlag code={selected.code} size="sm" />
              <span className="flex-1 truncate">{selected.name}</span>
            </>
          ) : (
            <span className="flex-1 text-fg-subtle">{placeholder}</span>
          )}
          <ChevronDown size={16} aria-hidden className="text-fg-subtle" />
        </ListboxButton>
        <ListboxOptions
          anchor="bottom start"
          className="w-[var(--button-width)] rounded-lg border border-border-strong bg-elevated font-mono text-sm text-fg-default max-h-72 overflow-y-auto focus:outline-none z-50 shadow-xl"
        >
          <ListboxOption
            value=""
            className="px-3 py-2 cursor-pointer text-fg-subtle data-[focus]:bg-sunken"
          >
            {placeholder}
          </ListboxOption>
          {teams.map((t) => (
            <ListboxOption
              key={t.id}
              value={t.id}
              className="px-3 py-2 cursor-pointer flex items-center gap-2 data-[focus]:bg-accent-soft data-[focus]:text-accent-strong data-[selected]:font-bold"
            >
              <TeamFlag code={t.code} size="sm" />
              <span className="flex-1 truncate">{t.name}</span>
              {t.code && (
                <span className="text-xs text-fg-subtle uppercase tracking-[0.1em]">{t.code}</span>
              )}
            </ListboxOption>
          ))}
        </ListboxOptions>
      </div>
    </Listbox>
  )
}
