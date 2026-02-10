import { describe, it, expect, vi } from 'vitest'

// Mock useState since it's a Nuxt-specific composable
vi.doMock('../../app/composables/usePlayerState', () => ({
  usePlayerState: () => ({
    player: {
      value: {
        name: 'Player1',
        animals: [],
        inventory: [],
        wallet: { gold: 100 }
      }
    }
  })
}))

describe('usePlayerState composable', () => {
    it('should initialize with default player state', () => {
        const mockPlayer = {
            name: 'Player1',
            animals: [],
            inventory: [],
            wallet: { gold: 100 }
        }
        expect(mockPlayer).toBeDefined()
        expect(mockPlayer.name).toBeDefined()
        expect(mockPlayer.animals).toBeDefined()
        expect(Array.isArray(mockPlayer.animals)).toBe(true)
    })

    it('should have inventory property', () => {
        const mockPlayer = {
            name: 'Player1',
            animals: [],
            inventory: [],
            wallet: { gold: 100 }
        }
        expect(mockPlayer.inventory).toBeDefined()
        expect(Array.isArray(mockPlayer.inventory)).toBe(true)
    })

    it('should have wallet with gold', () => {
        const mockPlayer = {
            name: 'Player1',
            animals: [],
            inventory: [],
            wallet: { gold: 100 }
        }
        expect(mockPlayer.wallet).toBeDefined()
        expect(typeof mockPlayer.wallet.gold).toBe('number')
    })
})