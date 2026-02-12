import type { Animal, AnimalKind, ItemKind, Player } from '../types/game'
import {
  applyAffectionMilestones,
  applyGrowthItemEffect,
  getAnimalPrices,
  getShopItems,
  makeAnimal,
  newId,
  titleCase,
} from '../game/playerLogic'

const STORAGE_KEY = 'stable_run_v1'

/* -------------------------------------------
 * localStorage helpers
 * ------------------------------------------- */
function load(): Player | null {
  if (!import.meta.client) return null
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    return JSON.parse(raw) as Player
  } catch {
    return null
  }
}

function save(p: Player) {
  if (!import.meta.client) return
  localStorage.setItem(STORAGE_KEY, JSON.stringify(p))
}

function clearSaved() {
  if (!import.meta.client) return
  try {
    localStorage.removeItem(STORAGE_KEY)
  } catch {
    // ignore
  }
}

/* -------------------------------------------
 * DB DTOs + helpers
 * ------------------------------------------- */
type PlayerAnimalDto = {
  id: number
  ownerPlayerId: string
  ownerName: string
  name: string
  kind: AnimalKind
  attack: number
  defense: number
  affection: number
  level: number
  hpMax: number
  hpCurrent: number
  createdAt: string
  templateId: number
}

function dtoToAnimal(a: PlayerAnimalDto): Animal {
  return {
    id: `pa-${a.id}`,
    ownerPlayerId: a.ownerPlayerId,
    ownerName: a.ownerName,
    name: a.name,
    kind: a.kind,
    stats: {
      attack: a.attack,
      defense: a.defense,
      affection: a.affection,
      level: a.level,
      hpMax: a.hpMax,
    },
    hpCurrent: a.hpCurrent,
  }
}

function animalIdToDbId(id: string): number | null {
  if (!id.startsWith('pa-')) return null
  const n = Number(id.slice(3))
  return Number.isFinite(n) ? n : null
}

function getApiBaseOrThrow(): string {
  const config = useRuntimeConfig()
  const apiBase = (config.public.apiBase ?? '').toString().replace(/\/+$/, '')
  if (!apiBase) throw new Error('Missing runtimeConfig.public.apiBase (check NUXT_PUBLIC_API_BASE / _PROD)')
  return apiBase
}

export function usePlayerState() {
  const player = useState<Player | null>('player', () => null)

  onMounted(() => {
    if (import.meta.client && player.value === null) {
      player.value = load()
    }
  })

  const isReady = computed(() => !!player.value)

  function createPlayer(name: string) {
    const p: Player = {
      id: newId(),
      name: name.trim(),
      gold: 300,
      animals: [],
      inventory: {
        treat: 0,
        armorSnack: 0,
        proteinBite: 0,
        bandage: 0,
        medkit: 0,
        attackPill: 0,
        defensePill: 0,
      },
    }
    player.value = p
    save(p)
  }

  /**
   * ✅ Hard reset of the local player + persisted storage.
   * Use this when you want to force the user back through starter flow.
   */
  function resetPlayer() {
    player.value = null
    clearSaved()
  }

  function chooseStarter(kind: AnimalKind) {
    if (!player.value) return
    if (player.value.animals.length > 0) return
    player.value.animals.push(makeAnimal(player.value, kind))
    save(player.value)
  }

  function addGold(amount: number) {
    if (!player.value) return
    player.value.gold += amount
    save(player.value)
  }

  function spendGold(amount: number): boolean {
    if (!player.value) return false
    if (player.value.gold < amount) return false
    player.value.gold -= amount
    save(player.value)
    return true
  }

  function buyItem(kind: ItemKind, qty = 1): boolean {
    const item = getShopItems().find(i => i.kind === kind)
    if (!item || !player.value) return false
    const total = item.price * qty
    if (!spendGold(total)) return false
    player.value.inventory[kind] = (player.value.inventory[kind] ?? 0) + qty
    save(player.value)
    return true
  }

  function buyAnimal(kind: AnimalKind): boolean {
    if (!player.value) return false
    const alreadyOwnsKind = player.value.animals.some(a => a.kind === kind)
    if (alreadyOwnsKind) return false

    const price = getAnimalPrices()[kind]
    if (!spendGold(price)) return false

    player.value.animals.push(makeAnimal(player.value, kind))
    save(player.value)
    return true
  }

  function petAnimal(animalId: string) {
    if (!player.value) return
    const a = player.value.animals.find(x => x.id === animalId)
    if (!a) return

    const before = a.stats.affection
    const after = Math.min(50, before + 1)

    a.stats.affection = after
    applyAffectionMilestones(a, before, after)

    save(player.value)
  }

  function feedAnimal(animalId: string, itemKind?: ItemKind) {
    if (!player.value) return
    const a = player.value.animals.find(x => x.id === animalId)
    if (!a) return
    if (!itemKind) return

    const inv = player.value.inventory[itemKind] ?? 0
    if (inv <= 0) return
    player.value.inventory[itemKind] = inv - 1

    applyGrowthItemEffect(a, itemKind)

    save(player.value)
  }

  function healAnimalToFull(animalId: string) {
    if (!player.value) return
    const a = player.value.animals.find(x => x.id === animalId)
    if (!a) return
    a.hpCurrent = a.stats.hpMax
    save(player.value)
  }

  /* -------------------------------------------
   * ✅ DB functions
   * ------------------------------------------- */

  async function dbLoadPlayerAnimals(playerId: string): Promise<Animal[]> {
    const apiBase = getApiBaseOrThrow()
    const url = `${apiBase}/api/animals/player/${encodeURIComponent(playerId)}`
    const rows = await $fetch<PlayerAnimalDto[]>(url)
    return Array.isArray(rows) ? rows.map(dtoToAnimal) : []
  }

  async function dbClaimAnimal(kind: AnimalKind, customName?: string): Promise<Animal | null> {
    if (!player.value) return null
    const apiBase = getApiBaseOrThrow()

    const created = await $fetch<PlayerAnimalDto>(`${apiBase}/api/animals/claim`, {
      method: 'POST',
      body: {
        ownerPlayerId: player.value.id,
        ownerName: player.value.name,
        kind,
        name: customName ?? `${player.value.name}'s ${titleCase(kind)}`,
      },
    })

    if (!created || typeof created.id !== 'number') return null
    return dtoToAnimal(created)
  }

  async function dbUpdatePlayerAnimal(animal: Animal): Promise<boolean> {
    const dbId = animalIdToDbId(animal.id)
    if (!dbId) return false
    const apiBase = getApiBaseOrThrow()

    await $fetch(`${apiBase}/api/animals/playerAnimal/${dbId}`, {
      method: 'PUT',
      body: {
        name: animal.name,
        kind: animal.kind,
        attack: animal.stats.attack,
        defense: animal.stats.defense,
        affection: animal.stats.affection,
        level: animal.stats.level,
        hpMax: animal.stats.hpMax,
        hpCurrent: animal.hpCurrent,
      },
    })

    return true
  }

  return {
    player,
    isReady,

    // local state actions
    createPlayer,
    resetPlayer,
    chooseStarter,
    addGold,
    buyItem,
    buyAnimal,
    petAnimal,
    feedAnimal,
    healAnimalToFull,

    // DB helpers
    dbLoadPlayerAnimals,
    dbClaimAnimal,
    dbUpdatePlayerAnimal,
  }
}
