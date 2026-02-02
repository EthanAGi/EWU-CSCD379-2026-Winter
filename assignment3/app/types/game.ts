export type AnimalKind = 'dog' | 'cat' | 'hamster' | 'fox' | 'owl' | 'boar'

export type ItemKind =
  | 'treat'
  | 'armorSnack'
  | 'proteinBite'
  // Battle items
  | 'bandage'
  | 'medkit'
  | 'attackPill'
  | 'defensePill'

export type ItemCategory = 'growth' | 'battle'

export type Stats = {
  attack: number
  defense: number
  affection: number
  level: number
  hpMax: number
}

export type Animal = {
  id: string
  ownerPlayerId: string
  ownerName: string
  name: string
  kind: AnimalKind
  stats: Stats
  hpCurrent: number
}

export type ItemEffect = {
  // Growth item effects
  attack?: number
  defense?: number
  affection?: number

  // Battle item effects (used in Gauntlet, wears off after battle)
  healPct?: number // 0.30 = heal 30% of hpMax, 0.70 = heal 70%
  attackMultiplier?: number // 2 = double attack
  defenseMultiplier?: number // 2 = double defense
}

export type Item = {
  id: string
  kind: ItemKind
  category: ItemCategory
  name: string
  description: string
  price: number
  effect: ItemEffect
}

export type Player = {
  id: string
  name: string
  gold: number
  animals: Animal[]
  inventory: Record<ItemKind, number>
}

export type BattleMove = 'attack' | 'defend'

export type BattleState = {
  stage: 1 | 2 | 3
  round: number
  playerAnimalId: string
  enemy: Animal
  playerDefending: boolean
  enemyDefending: boolean
  log: string[]
  ended: boolean
  outcome?: 'win' | 'loss'
  rewardGold?: number
}
