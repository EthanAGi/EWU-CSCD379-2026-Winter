import type { Animal, AnimalKind, Item, ItemKind, Player, Stats } from '../types/game'

const STORAGE_KEY = 'stable_run_v1'

function newId(): string {
  return Math.random().toString(16).slice(2) + Date.now().toString(16)
}

function titleCase(s: string): string {
  return s.charAt(0).toUpperCase() + s.slice(1)
}

function baseStats(kind: AnimalKind): Stats {
  if (kind === 'dog') return { attack: 5, defense: 4, affection: 3, level: 1, hpMax: 30 }
  if (kind === 'cat') return { attack: 6, defense: 3, affection: 4, level: 1, hpMax: 28 }
  if (kind === 'hamster') return { attack: 4, defense: 5, affection: 2, level: 1, hpMax: 32 }

  // bought animals
  if (kind === 'fox') return { attack: 8, defense: 5, affection: 2, level: 1, hpMax: 34 }
  if (kind === 'owl') return { attack: 7, defense: 6, affection: 2, level: 1, hpMax: 36 }
  return { attack: 6, defense: 8, affection: 1, level: 1, hpMax: 40 } // boar
}

function makeAnimal(owner: Player, kind: AnimalKind, customName?: string): Animal {
  const stats = baseStats(kind)
  return {
    id: newId(), // local id; if saved to DB, pages can replace with `pa-<dbId>` if desired
    ownerPlayerId: owner.id,
    ownerName: owner.name,
    name: customName ?? titleCase(kind),
    kind,
    stats,
    hpCurrent: stats.hpMax,
  }
}

/* -------------------------------------------
 * Shop data
 * ------------------------------------------- */
export function getShopItems(): Item[] {
  return [
    // -------------------
    // Growth Items
    // -------------------
    {
      id: 'treat',
      kind: 'treat',
      category: 'growth',
      name: 'Treat',
      description: 'Growth item: Big affection boost.',
      price: 75,
      effect: { affection: 5 }, // ✅ treat gives +5 affection
    },
    {
      id: 'armorSnack',
      kind: 'armorSnack',
      category: 'growth',
      name: 'Armor Snack',
      description: 'Growth item: Boost defense slightly.',
      price: 150,
      effect: { defense: 1 },
    },
    {
      id: 'proteinBite',
      kind: 'proteinBite',
      category: 'growth',
      name: 'Protein Bite',
      description: 'Growth item: Boost attack slightly.',
      price: 150,
      effect: { attack: 1 },
    },

    // -------------------
    // Battle Items
    // -------------------
    {
      id: 'bandage',
      kind: 'bandage',
      category: 'battle',
      name: 'Bandage',
      description: 'Battle item: Heals 30% HP (used during battle).',
      price: 180,
      effect: { healPct: 0.3 },
    },
    {
      id: 'medkit',
      kind: 'medkit',
      category: 'battle',
      name: 'Medkit',
      description: 'Battle item: Heals 70% HP (used during battle).',
      price: 420,
      effect: { healPct: 0.7 },
    },
    {
      id: 'attackPill',
      kind: 'attackPill',
      category: 'battle',
      name: 'Attack Boost Pill',
      description: 'Battle item: Doubles attack until the battle ends.',
      price: 300,
      effect: { attackMultiplier: 2 },
    },
    {
      id: 'defensePill',
      kind: 'defensePill',
      category: 'battle',
      name: 'Defense Pill',
      description: 'Battle item: Doubles defense until the battle ends.',
      price: 300,
      effect: { defenseMultiplier: 2 },
    },
  ]
}

export function getAnimalPrices(): Record<AnimalKind, number> {
  return {
    dog: 500,
    cat: 500,
    hamster: 500,
    fox: 1200,
    owl: 1500,
    boar: 2000,
  }
}

/* -------------------------------------------
 * localStorage helpers
 * ------------------------------------------- */
function load(): Player | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    return JSON.parse(raw) as Player
  } catch {
    return null
  }
}

function save(p: Player) {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(p))
}

function clearSaved() {
  try {
    localStorage.removeItem(STORAGE_KEY)
  } catch {
    // ignore
  }
}

/* -------------------------------------------
 * Affection milestones (FIXED)
 * ------------------------------------------- */
function applyAffectionMilestones(animal: Animal, beforeAff: number, afterAff: number) {
  const beforeMilestones = Math.floor(beforeAff / 5)
  const afterMilestones = Math.floor(afterAff / 5)
  const gained = afterMilestones - beforeMilestones
  if (gained <= 0) return

  animal.stats.attack += gained
  animal.stats.defense += gained
  animal.stats.hpMax += gained * 2
  animal.hpCurrent = Math.min(animal.stats.hpMax, animal.hpCurrent + gained * 2)
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
      gold: 0,
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
   * ✅ NEW:
   * Hard reset of the local player + persisted storage.
   * Use this when you want to force the user back through starter flow.
   */
  function resetPlayer() {
    player.value = null
    if (import.meta.client) {
      clearSaved()
    }
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

    const item = getShopItems().find(i => i.kind === itemKind)

    if (item?.effect.attack) a.stats.attack += item.effect.attack
    if (item?.effect.defense) a.stats.defense += item.effect.defense

    if (item?.effect.affection) {
      const before = a.stats.affection
      const after = Math.min(50, before + item.effect.affection)
      a.stats.affection = after
      applyAffectionMilestones(a, before, after)
    }

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
   * ✅ DB functions (UPDATED: same-origin /api)
   * ------------------------------------------- */

  async function dbLoadPlayerAnimals(playerId: string): Promise<Animal[]> {
    const url = `/api/animals/player/${encodeURIComponent(playerId)}`
    const rows = await $fetch<PlayerAnimalDto[]>(url)
    return Array.isArray(rows) ? rows.map(dtoToAnimal) : []
  }

  async function dbClaimAnimal(kind: AnimalKind, customName?: string): Promise<Animal | null> {
    if (!player.value) return null

    const created = await $fetch<PlayerAnimalDto>(`/api/animals/claim`, {
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

  /**
   * IMPORTANT:
   * This assumes you created a Nitro route:
   *   PUT /api/animals/playerAnimal/{id}
   * i.e. file:
   *   server/api/animals/playerAnimal/[id].put.ts
   */
  async function dbUpdatePlayerAnimal(animal: Animal): Promise<boolean> {
    const dbId = animalIdToDbId(animal.id)
    if (!dbId) return false

    await $fetch(`/api/animals/playerAnimal/${dbId}`, {
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
    resetPlayer, // ✅ added
    chooseStarter,
    addGold,
    buyItem,
    buyAnimal,
    petAnimal,
    feedAnimal,
    healAnimalToFull,

    // ✅ DB helpers
    dbLoadPlayerAnimals,
    dbClaimAnimal,
    dbUpdatePlayerAnimal,
  }
}
