import type { Animal, AnimalKind, Item, ItemKind, Player, Stats } from '../types/game'

export function titleCase(s: string): string {
  return s.charAt(0).toUpperCase() + s.slice(1)
}

export function newId(): string {
  return Math.random().toString(16).slice(2) + Date.now().toString(16)
}

export function baseStats(kind: AnimalKind): Stats {
  if (kind === 'dog') return { attack: 5, defense: 4, affection: 3, level: 1, hpMax: 30 }
  if (kind === 'cat') return { attack: 6, defense: 3, affection: 4, level: 1, hpMax: 28 }
  if (kind === 'hamster') return { attack: 4, defense: 5, affection: 2, level: 1, hpMax: 32 }

  // bought animals
  if (kind === 'fox') return { attack: 8, defense: 5, affection: 2, level: 1, hpMax: 34 }
  if (kind === 'owl') return { attack: 7, defense: 6, affection: 2, level: 1, hpMax: 36 }
  return { attack: 6, defense: 8, affection: 1, level: 1, hpMax: 40 } // boar
}

export function makeAnimal(
  owner: Player,
  kind: AnimalKind,
  customName?: string,
  idFactory: () => string = newId
): Animal {
  const stats = baseStats(kind)
  return {
    id: idFactory(), // local id; if saved to DB, pages can replace with `pa-<dbId>` if desired
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
 * Affection milestones (FIXED)
 * ------------------------------------------- */
export function applyAffectionMilestones(animal: Animal, beforeAff: number, afterAff: number) {
  const beforeMilestones = Math.floor(beforeAff / 5)
  const afterMilestones = Math.floor(afterAff / 5)
  const gained = afterMilestones - beforeMilestones
  if (gained <= 0) return

  animal.stats.attack += gained
  animal.stats.defense += gained
  animal.stats.hpMax += gained * 2
  animal.hpCurrent = Math.min(animal.stats.hpMax, animal.hpCurrent + gained * 2)
}

export function applyGrowthItemEffect(animal: Animal, itemKind: ItemKind) {
  const item = getShopItems().find(i => i.kind === itemKind)
  if (!item) return

  if (item.effect.attack) animal.stats.attack += item.effect.attack
  if (item.effect.defense) animal.stats.defense += item.effect.defense

  if (item.effect.affection) {
    const before = animal.stats.affection
    const after = Math.min(50, before + item.effect.affection)
    animal.stats.affection = after
    applyAffectionMilestones(animal, before, after)
  }
}
