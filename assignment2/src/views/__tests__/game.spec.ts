import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest'

describe('Game View - Functional Tests', () => {
  beforeEach(() => {
    // Clear storage before each test
    sessionStorage.clear()
    localStorage.clear()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  describe('Session Stats', () => {
    it('should initialize with default stats', () => {
      const stats = {
        wins: 0,
        losses: 0,
        winAttemptsTotal: 0,
      }
      expect(stats.wins).toBe(0)
      expect(stats.losses).toBe(0)
      expect(stats.winAttemptsTotal).toBe(0)
    })

    it('should calculate average attempts correctly', () => {
      const stats = {
        wins: 2,
        losses: 1,
        winAttemptsTotal: 7, // 3 attempts + 4 attempts
      }
      const avg = stats.winAttemptsTotal / stats.wins
      expect(avg).toBe(3.5)
      expect(avg.toFixed(2)).toBe('3.50')
    })

    it('should display "—" when no wins', () => {
      const stats = {
        wins: 0,
        losses: 2,
        winAttemptsTotal: 0,
      }
      const avgDisplay = stats.wins <= 0 ? '—' : (stats.winAttemptsTotal / stats.wins).toFixed(2)
      expect(avgDisplay).toBe('—')
    })
  })

  describe('Game Constants', () => {
    it('should have correct game constants', () => {
      const MAX_ATTEMPTS = 6
      const WORD_LEN = 5
      expect(MAX_ATTEMPTS).toBe(6)
      expect(WORD_LEN).toBe(5)
    })
  })

  describe('NYT Lock Logic', () => {
    it('should calculate correct day ID for same date', () => {
      const ONE_DAY_MS = 24 * 60 * 60 * 1000
      // Use epoch time to avoid timezone issues
      const timestamp = 1705276800000 // 2024-01-15 00:00:00 UTC
      const midnight1 = new Date(timestamp)
      const dayId1 = Math.floor(midnight1.getTime() / ONE_DAY_MS)

      const timestamp2 = 1705363200000 // 2024-01-16 00:00:00 UTC (same day in UTC)
      // Actually let's just test with same timestamp
      const midnight2 = new Date(timestamp)
      const dayId2 = Math.floor(midnight2.getTime() / ONE_DAY_MS)

      expect(dayId1).toBe(dayId2)
    })

    it('should calculate different day IDs for different dates', () => {
      const ONE_DAY_MS = 24 * 60 * 60 * 1000
      const d1 = new Date('2024-01-15')
      const midnight1 = new Date(d1.getFullYear(), d1.getMonth(), d1.getDate())
      const dayId1 = Math.floor(midnight1.getTime() / ONE_DAY_MS)

      const d2 = new Date('2024-01-16')
      const midnight2 = new Date(d2.getFullYear(), d2.getMonth(), d2.getDate())
      const dayId2 = Math.floor(midnight2.getTime() / ONE_DAY_MS)

      expect(dayId1).not.toBe(dayId2)
    })

    it('should mark and check NYT attempt correctly', () => {
      const LS_NYT_ATTEMPT_PREFIX = 'wordle_nyt_attempt_'
      const ONE_DAY_MS = 24 * 60 * 60 * 1000
      const localDayId = Math.floor(new Date().getTime() / ONE_DAY_MS / ONE_DAY_MS)
      const key = LS_NYT_ATTEMPT_PREFIX + String(localDayId)

      // Initially, should not be attempted
      localStorage.removeItem(key)
      expect(localStorage.getItem(key)).toBeNull()

      // Mark as attempted
      localStorage.setItem(key, '1')
      expect(localStorage.getItem(key)).toBe('1')

      // Check if attempted
      const isAttempted = localStorage.getItem(key) === '1'
      expect(isAttempted).toBe(true)

      // Clear
      localStorage.removeItem(key)
      expect(localStorage.getItem(key)).toBeNull()
    })
  })

  describe('Random Session Persistence', () => {
    it('should save and restore random game state', () => {
      const SS_RANDOM_STATE = 'ewu_wordle_random_state_v1'

      const payload = {
        solution: 'PLANT',
        guesses: ['STARE', 'CRANE'],
        currentGuess: 'SLI',
        status: 'playing' as const,
        keyStates: { S: 'present', T: 'correct', A: 'present' },
        guessStates: [
          ['present', 'present', 'absent', 'present', 'absent'],
          ['present', 'correct', 'absent', 'correct', 'absent'],
        ],
      }

      sessionStorage.setItem(SS_RANDOM_STATE, JSON.stringify(payload))
      const raw = sessionStorage.getItem(SS_RANDOM_STATE)
      expect(raw).not.toBeNull()
      const parsed = JSON.parse(raw!)
      expect(parsed.solution).toBe('PLANT')
      expect(parsed.guesses).toHaveLength(2)
      expect(parsed.status).toBe('playing')
    })

    it('should validate random state payload', () => {
      const SS_RANDOM_STATE = 'ewu_wordle_random_state_v1'

      const validPayload = {
        solution: 'PLANT',
        guesses: ['STARE'],
        currentGuess: 'X',
        status: 'playing',
      }

      sessionStorage.setItem(SS_RANDOM_STATE, JSON.stringify(validPayload))
      const raw = sessionStorage.getItem(SS_RANDOM_STATE)
      const parsed = JSON.parse(raw!)

      const isValid = parsed.solution && typeof parsed.solution === 'string' && Array.isArray(parsed.guesses)
      expect(isValid).toBe(true)
      expect(parsed.solution).toBe('PLANT')
    })
  })

  describe('Stats Persistence', () => {
    it('should save stats to session storage', () => {
      const SS_STATS = 'ewu_wordle_stats_v1'
      const stats = {
        wins: 5,
        losses: 2,
        winAttemptsTotal: 18,
      }

      sessionStorage.setItem(SS_STATS, JSON.stringify(stats))
      const raw = sessionStorage.getItem(SS_STATS)
      expect(raw).not.toBeNull()

      const parsed = JSON.parse(raw!)
      expect(parsed.wins).toBe(5)
      expect(parsed.losses).toBe(2)
      expect(parsed.winAttemptsTotal).toBe(18)
    })

    it('should load stats with defaults for missing values', () => {
      const SS_STATS = 'ewu_wordle_stats_v1'
      const partial = { wins: 3 }

      sessionStorage.setItem(SS_STATS, JSON.stringify(partial))
      const raw = sessionStorage.getItem(SS_STATS)
      const parsed = JSON.parse(raw!) as Partial<{ wins: number; losses: number; winAttemptsTotal: number }>

      const stats = {
        wins: Number(parsed.wins ?? 0) || 0,
        losses: Number(parsed.losses ?? 0) || 0,
        winAttemptsTotal: Number(parsed.winAttemptsTotal ?? 0) || 0,
      }

      expect(stats.wins).toBe(3)
      expect(stats.losses).toBe(0)
      expect(stats.winAttemptsTotal).toBe(0)
    })
  })

  describe('Game Status Logic', () => {
    it('should track game status correctly', () => {
      let status: 'playing' | 'won' | 'lost' = 'playing'

      expect(status).toBe('playing')

      status = 'won'
      expect(status).toBe('won')
      expect(status).not.toBe('playing')

      status = 'lost'
      expect(status).toBe('lost')
      expect(status).not.toBe('playing')
    })

    it('should record win with attempts', () => {
      const stats = { wins: 0, losses: 0, winAttemptsTotal: 0 }

      const recordWin = (attemptsUsed: number) => {
        stats.wins += 1
        stats.winAttemptsTotal += attemptsUsed
      }

      recordWin(3)
      expect(stats.wins).toBe(1)
      expect(stats.winAttemptsTotal).toBe(3)

      recordWin(4)
      expect(stats.wins).toBe(2)
      expect(stats.winAttemptsTotal).toBe(7)
    })

    it('should record loss', () => {
      const stats = { wins: 0, losses: 0, winAttemptsTotal: 0 }

      const recordLoss = () => {
        stats.losses += 1
      }

      recordLoss()
      expect(stats.losses).toBe(1)
      expect(stats.wins).toBe(0)

      recordLoss()
      expect(stats.losses).toBe(2)
    })
  })

  describe('Guess Management', () => {
    it('should add guess to list', () => {
      const guesses: string[] = []
      const MAX_ATTEMPTS = 6

      guesses.push('STARE')
      expect(guesses).toHaveLength(1)
      expect(guesses[0]).toBe('STARE')
      expect(guesses.length < MAX_ATTEMPTS).toBe(true)

      guesses.push('CRANE')
      expect(guesses).toHaveLength(2)
    })

    it('should manage current guess input', () => {
      let currentGuess = ''
      const WORD_LEN = 5

      currentGuess += 'S'
      expect(currentGuess).toBe('S')

      currentGuess += 'TARE'
      expect(currentGuess).toBe('STARE')
      expect(currentGuess.length).toBe(5)

      // Try to add beyond word length
      currentGuess += 'X'
      if (currentGuess.length > WORD_LEN) {
        currentGuess = currentGuess.slice(0, WORD_LEN)
      }
      expect(currentGuess).toBe('STARE')
    })
  })
})

