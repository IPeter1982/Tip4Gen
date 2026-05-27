const BUDAPEST = 'Europe/Budapest'

const dateTimeFormatter = new Intl.DateTimeFormat('hu-HU', {
  timeZone: BUDAPEST,
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
})

const timeFormatter = new Intl.DateTimeFormat('hu-HU', {
  timeZone: BUDAPEST,
  hour: '2-digit',
  minute: '2-digit',
})

const dateFormatter = new Intl.DateTimeFormat('hu-HU', {
  timeZone: BUDAPEST,
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
})

export function formatBudapest(iso: string): string {
  return dateTimeFormatter.format(new Date(iso))
}

export function formatBudapestTime(iso: string): string {
  return timeFormatter.format(new Date(iso))
}

export function formatBudapestDate(iso: string): string {
  return dateFormatter.format(new Date(iso))
}

export function formatCountdown(targetIso: string, now: Date = new Date()): string {
  const diffMs = new Date(targetIso).getTime() - now.getTime()
  if (diffMs <= 0) return 'lejárt'
  const totalSeconds = Math.floor(diffMs / 1000)
  const days = Math.floor(totalSeconds / 86400)
  const hours = Math.floor((totalSeconds % 86400) / 3600)
  const minutes = Math.floor((totalSeconds % 3600) / 60)
  const seconds = totalSeconds % 60
  if (days > 0) return `${days} nap ${hours} ó`
  if (hours > 0) return `${hours} ó ${String(minutes).padStart(2, '0')} p`
  return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`
}

export const STAGE_LABEL_HU: Record<string, string> = {
  Group: 'Csoport',
  R32: 'Nyolcaddöntő (R32)',
  R16: 'Nyolcaddöntő',
  QF: 'Negyeddöntő',
  SF: 'Elődöntő',
  Bronze: 'Bronzmérkőzés',
  Final: 'Döntő',
}

export const STATUS_LABEL_HU: Record<string, string> = {
  Scheduled: 'tervezett',
  Live: 'él',
  Finished: 'lejátszott',
  Postponed: 'halasztott',
  Cancelled: 'törölt',
  Abandoned: 'félbeszakadt',
  Awarded: 'odaítélt',
}
