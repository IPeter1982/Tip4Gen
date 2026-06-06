import { ApiError } from '../api/errors'
import type { TeamView } from '../api/types'

export const STATUS_LABEL: Record<TeamView['status'], string> = {
  Forming: 'Alakuló',
  Locked: 'Lezárva',
  Disqualified: 'Kizárva',
}

export function reasonMessage(reason: string): string {
  switch (reason) {
    case 'NameBlank':
      return 'A csapatnévnek nem szabad üresnek lennie.'
    case 'NameTooLong':
      return 'A csapatnév maximum 80 karakter lehet.'
    case 'TournamentStarted':
      return 'A torna már elkezdődött; a csapat-beállítások véglegesek.'
    case 'TeamLocked':
      return 'A csapat lezárult; ekkor már nem módosítható.'
    case 'TeamFull':
      return 'A csapat már elérte a 3 fős maximumot.'
    case 'AiSlotTaken':
      return 'A csapatban már van AI tag.'
    case 'UserAlreadyInTeam':
    case 'AlreadyInTeam':
      return 'Már tagja vagy egy másik csapatnak.'
    case 'TeamNotFound':
      return 'A csapat időközben megszűnt.'
    case 'AvatarMissing':
      return 'Nincs kép kiválasztva.'
    case 'AvatarUnsupportedFormat':
      return 'Csak JPEG, PNG vagy WebP képek tölthetők fel.'
    case 'AvatarTooLarge':
      return 'A kép maximum 50 KB lehet.'
    case 'AvatarInvalidDataUrl':
      return 'Érvénytelen kép-adat.'
    default:
      return 'A művelet nem hajtható végre.'
  }
}

export function errorMessage(e: unknown): string {
  if (e instanceof ApiError && e.reason) return reasonMessage(e.reason)
  if (e instanceof ApiError) return e.message
  if (e instanceof Error) return e.message
  return String(e)
}
