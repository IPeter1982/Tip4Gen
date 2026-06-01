import {
  Listbox,
  ListboxButton,
  ListboxOption,
  ListboxOptions,
} from '@headlessui/react'
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
          className="w-full border-2 border-stone-900 px-3 py-2 font-mono text-sm bg-white text-left flex items-center gap-2 data-[disabled]:bg-stone-100 data-[disabled]:cursor-not-allowed focus:outline-none focus:bg-orange-50"
        >
          {selected ? (
            <>
              <TeamFlag code={selected.code} size="sm" />
              <span className="flex-1 truncate">{selected.name}</span>
            </>
          ) : (
            <span className="flex-1 text-stone-500">{placeholder}</span>
          )}
          <span aria-hidden className="text-stone-500">▾</span>
        </ListboxButton>
        <ListboxOptions
          anchor="bottom start"
          className="w-[var(--button-width)] border-2 border-stone-900 bg-white font-mono text-sm max-h-72 overflow-y-auto focus:outline-none z-50"
        >
          <ListboxOption
            value=""
            className="px-3 py-2 cursor-pointer text-stone-500 data-[focus]:bg-stone-100"
          >
            {placeholder}
          </ListboxOption>
          {teams.map((t) => (
            <ListboxOption
              key={t.id}
              value={t.id}
              className="px-3 py-2 cursor-pointer flex items-center gap-2 data-[focus]:bg-orange-50 data-[selected]:font-bold"
            >
              <TeamFlag code={t.code} size="sm" />
              <span className="flex-1 truncate">{t.name}</span>
              {t.code && (
                <span className="text-xs text-stone-500 uppercase tracking-[0.1em]">{t.code}</span>
              )}
            </ListboxOption>
          ))}
        </ListboxOptions>
      </div>
    </Listbox>
  )
}
