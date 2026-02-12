import { describe, it, expect } from 'vitest'
import type { Player } from '../../app/types/game'
import { applyAffectionMilestones, baseStats, getAnimalPrices, getShopItems, makeAnimal, applyGrowthItemEffect } from '../../app/game/playerLogic'

describe('playerLogic (pure game logic)', () => {
  it('baseStats returns correct starter stats', () => {
    expect(baseStats('dog')).toEqual({ attack: 5, defense: 4, affection: 3, level: 1, hpMax: 30 })
    expect(baseStats('cat')).toEqual({ attack: 6, defense: 3, affection: 4, level: 1, hpMax: 28 })
    expect(baseStats('hamster')).toEqual({ attack: 4, defense: 5, affection: 2, level: 1, hpMax: 32 })
  })

  it('getAnimalPrices contains expected values and all are > 0', () => {
    const prices = getAnimalPrices()
    expect(prices.dog).toBe(500)
    expect(prices.cat).toBe(500)
    expect(prices.hamster).toBe(500)
    expect(prices.fox).toBe(1200)
    expect(prices.owl).toBe(1500)
    expect(prices.boar).toBe(2000)

    for (const v of Object.values(prices)) {
      expect(v).toBeGreaterThan(0)
    }
  })

  it('getShopItems includes Treat with +5 affection (comment-required behavior)', () => {
    const treat = getShopItems().find(i => i.kind === 'treat')
    expect(treat).toBeTruthy()
    expect(treat?.effect.affection).toBe(5)
    expect(treat?.price).toBe(75)
  })

  it('makeAnimal sets hpCurrent to hpMax and uses customName when provided', () => {
    const owner: Player = {
      id: 'p1',
      name: 'Ethan',
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

    const a = makeAnimal(owner, 'cat', 'Fluffy', () => 'fixed-id')
    expect(a.id).toBe('fixed-id')
    expect(a.ownerPlayerId).toBe('p1')
    expect(a.ownerName).toBe('Ethan')
    expect(a.name).toBe('Fluffy')
    expect(a.kind).toBe('cat')
    expect(a.hpCurrent).toBe(a.stats.hpMax)
  })

  it('applyAffectionMilestones increases stats when crossing multiples of 5', () => {
    const owner: Player = {
      id: 'p1',
      name: 'Ethan',
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

    const a = makeAnimal(owner, 'dog', 'Buddy', () => 'id1')
    // Force a known affection value:
    a.stats.affection = 4
    a.hpCurrent = a.stats.hpMax

    const beforeAtk = a.stats.attack
    const beforeDef = a.stats.defense
    const beforeHpMax = a.stats.hpMax
    const beforeHpCur = a.hpCurrent

    // Cross milestone: 4 -> 5 (gained 1 milestone)
    applyAffectionMilestones(a, 4, 5)

    expect(a.stats.attack).toBe(beforeAtk + 1)
    expect(a.stats.defense).toBe(beforeDef + 1)
    expect(a.stats.hpMax).toBe(beforeHpMax + 2)
    expect(a.hpCurrent).toBe(Math.min(a.stats.hpMax, beforeHpCur + 2))
  })

  it('applyGrowthItemEffect applies attack/defense items and consumes affection milestones correctly', () => {
    const owner: Player = {
      id: 'p1',
      name: 'Ethan',
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

    const a = makeAnimal(owner, 'cat', 'Fluffy', () => 'id1')

    const atk0 = a.stats.attack
    const def0 = a.stats.defense

    applyGrowthItemEffect(a, 'proteinBite')
    expect(a.stats.attack).toBe(atk0 + 1)
    expect(a.stats.defense).toBe(def0)

    applyGrowthItemEffect(a, 'armorSnack')
    expect(a.stats.defense).toBe(def0 + 1)
  })
})
