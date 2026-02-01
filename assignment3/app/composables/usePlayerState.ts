import type { Animal, AnimalKind, Item, ItemKind, Player, Stats } from '../types/game'

const STORAGE_KEY = 'stable_run_v1'

function newId(): string {
  return Math.random().toString(16).slice(2) + Date.now().toString(16)
}

function titleCase(s: string): string {
  return s.charAt(0).toUpperCase() + s.slice(1)
}

function baseStats(kind: AnimalKind): Stats {
  if (kind === 'dog') return { attack: 5, defense: 4, affection: 3, hunger: 3, level: 1, hpMax: 30 }
  if (kind === 'cat') return { attack: 6, defense: 3, affection: 4, hunger: 3, level: 1, hpMax: 28 }
  if (kind === 'hamster') return { attack: 4, defense: 5, affection: 2, hunger: 3, level: 1, hpMax: 32 }

  // bought animals
  if (kind === 'fox') return { attack: 8, defense: 5, affection: 2, hunger: 3, level: 1, hpMax: 34 }
  if (kind === 'owl') return { attack: 7, defense: 6, affection: 2, hunger: 3, level: 1, hpMax: 36 }
  return { attack: 6, defense: 8, affection: 1, hunger: 3, level: 1, hpMax: 40 } // boar
}

function makeAnimal(owner: Player, kind: AnimalKind, customName?: string): Animal {
  const stats = baseStats(kind)
  return {
    id: newId(),
    ownerPlayerId: owner.id,
    ownerName: owner.name,
    name: customName ?? titleCase(kind),
    kind,
    stats,
    hpCurrent: stats.hpMax,
  }
}

export function getShopItems(): Item[] {
  return [
    {
      id: 'treat',
      kind: 'treat',
      name: 'Treat',
      description: 'Small affection boost. Reduces hunger a bit.',
      price: 75,
      effect: { affection: 2, hunger: -1 },
    },
    {
      id: 'armorSnack',
      kind: 'armorSnack',
      name: 'Armor Snack',
      description: 'Boost defense slightly.',
      price: 150,
      effect: { defense: 1, hunger: -1 },
    },
    {
      id: 'proteinBite',
      kind: 'proteinBite',
      name: 'Protein Bite',
      description: 'Boost attack slightly.',
      price: 150,
      effect: { attack: 1, hunger: -1 },
    },
  ]
}

export function getAnimalPrices(): Record<AnimalKind, number> {
  return {
    dog: 0,
    cat: 0,
    hamster: 0,
    fox: 1200,
    owl: 1500,
    boar: 2000,
  }
}

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
      inventory: { treat: 0, armorSnack: 0, proteinBite: 0 },
    }
    player.value = p
    save(p)
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
    a.stats.affection = Math.min(30, a.stats.affection + 1)
    save(player.value)
  }

  function feedAnimal(animalId: string, itemKind?: ItemKind) {
    if (!player.value) return
    const a = player.value.animals.find(x => x.id === animalId)
    if (!a) return

    // basic feed always reduces hunger
    a.stats.hunger = Math.max(0, a.stats.hunger - 1)

    if (itemKind) {
      const inv = player.value.inventory[itemKind] ?? 0
      if (inv <= 0) return
      player.value.inventory[itemKind] = inv - 1

      const item = getShopItems().find(i => i.kind === itemKind)
      if (item?.effect.attack) a.stats.attack += item.effect.attack
      if (item?.effect.defense) a.stats.defense += item.effect.defense
      if (item?.effect.affection) a.stats.affection += item.effect.affection
      if (item?.effect.hunger) a.stats.hunger = Math.max(0, a.stats.hunger + item.effect.hunger)
    }

    save(player.value)
  }

  // ✅ NEW: Heal one animal back to full HP (use after gauntlet)
  function healAnimalToFull(animalId: string) {
    if (!player.value) return
    const a = player.value.animals.find(x => x.id === animalId)
    if (!a) return
    a.hpCurrent = a.stats.hpMax
    save(player.value)
  }

  return {
    player,
    isReady,
    createPlayer,
    chooseStarter,
    addGold,
    buyItem,
    buyAnimal,
    petAnimal,
    feedAnimal,
    healAnimalToFull, // ✅ export it
  }
}
