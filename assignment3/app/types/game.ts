export type AnimalKind = 'dog' | 'cat' | 'hamster' | 'fox' | 'owl' | 'boar'
export type ItemKind = 'treat' | 'armorSnack' | 'proteinBite'

export type Stats = {
  attack: number
  defense: number
  affection: number
  hunger: number
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

export type Item = {
  id: string
  kind: ItemKind
  name: string
  description: string
  price: number
  effect: { attack?: number; defense?: number; affection?: number; hunger?: number }
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
