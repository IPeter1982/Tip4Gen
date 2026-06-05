import {
  Combobox,
  ComboboxButton,
  ComboboxInput,
  ComboboxOption,
  ComboboxOptions,
} from '@headlessui/react'
import { ChevronDown } from 'lucide-react'
import { useMemo, useState } from 'react'
import type { Player } from '../api/types'
import { TeamFlag } from './TeamFlag'

interface PlayerSelectProps {
  players: Player[]
  value: string | null
  onChange: (id: string | null) => void
  placeholder?: string
  disabled?: boolean
  id?: string
  className?: string
}

const MIN_QUERY_LENGTH = 3

export function PlayerSelect({
  players,
  value,
  onChange,
  placeholder = '— válassz játékost —',
  disabled,
  id,
  className,
}: PlayerSelectProps) {
  const [query, setQuery] = useState('')

  const trimmedQuery = query.trim().toLowerCase()
  const queryTooShort = trimmedQuery.length < MIN_QUERY_LENGTH

  const filtered = useMemo(() => {
    if (queryTooShort) return []
    return players.filter(
      (p) =>
        p.name.toLowerCase().includes(trimmedQuery) ||
        p.teamName.toLowerCase().includes(trimmedQuery) ||
        (p.teamCode?.toLowerCase().includes(trimmedQuery) ?? false),
    )
  }, [players, trimmedQuery, queryTooShort])

  return (
    <Combobox
      value={value}
      onChange={(v: string | null) => onChange(v && v.length > 0 ? v : null)}
      disabled={disabled}
    >
      <div className={`relative ${className ?? ''}`}>
        <div className="flex items-center gap-2 rounded-lg border border-border-strong bg-sunken px-3 py-2 focus-within:ring-2 focus-within:ring-ring-focus data-[disabled]:opacity-60">
          <ComboboxInput
            id={id}
            className="flex-1 bg-transparent font-mono text-sm text-fg-default outline-none placeholder:text-fg-subtle"
            displayValue={(v: string | null) => {
              const p = players.find((x) => x.id === v)
              return p ? `${p.name} (${p.teamCode ?? p.teamName})` : ''
            }}
            onChange={(e) => setQuery(e.target.value)}
            placeholder={placeholder}
            autoComplete="off"
          />
          <ComboboxButton className="text-fg-subtle">
            <ChevronDown size={16} aria-hidden />
          </ComboboxButton>
        </div>
        <ComboboxOptions
          anchor="bottom start"
          className="w-[var(--input-width)] rounded-lg border border-border-strong bg-elevated font-mono text-sm text-fg-default max-h-72 overflow-y-auto focus:outline-none z-50 shadow-xl"
        >
          {queryTooShort && (
            <div className="px-3 py-2 text-fg-subtle">
              Gépelj legalább {MIN_QUERY_LENGTH} karaktert (név vagy ország).
            </div>
          )}
          {!queryTooShort && filtered.length === 0 && (
            <div className="px-3 py-2 text-fg-subtle">Nincs találat.</div>
          )}
          {filtered.map((p) => (
            <ComboboxOption
              key={p.id}
              value={p.id}
              className="px-3 py-2 cursor-pointer flex items-center gap-2 data-[focus]:bg-accent-soft data-[focus]:text-accent-strong data-[selected]:font-bold"
            >
              <TeamFlag code={p.teamCode} size="sm" />
              <span className="flex-1 truncate">{p.name}</span>
              <span className="text-xs text-fg-subtle uppercase tracking-[0.1em]">
                {p.teamCode ?? p.teamName.slice(0, 3)}
              </span>
            </ComboboxOption>
          ))}
        </ComboboxOptions>
      </div>
    </Combobox>
  )
}
