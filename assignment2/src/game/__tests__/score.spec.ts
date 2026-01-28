import { describe, it, expect } from 'vitest'
import { scoreGuess, mergeKeyState } from '../score'

describe('scoreGuess', () => {
  it('returns all correct when guess matches solution', () => {
    const result = scoreGuess('ARISE', 'ARISE')
    expect(result).toEqual(['correct', 'correct', 'correct', 'correct', 'correct'])
  })

  it('returns all absent when no letters match', () => {
    const result = scoreGuess('QWERT', 'AYUIO')
    expect(result).toEqual(['absent', 'absent', 'absent', 'absent', 'absent'])
  })

  it('correctly identifies present letters', () => {
    // Solution: STALE (S-T-A-L-E)
    // Guess: SLATE (S-L-A-T-E)
    // S matches position 0 - correct
    // L is in solution at position 3 but guess has it at position 1 - present
    // A matches position 2 - correct
    // T is in solution at position 1 but guess has it at position 3 - present
    // E matches position 4 - correct
    const result = scoreGuess('SLATE', 'STALE')
    expect(result[0]).toBe('correct') // S
    expect(result[1]).toBe('present') // L - in solution but wrong position
    expect(result[2]).toBe('correct') // A
    expect(result[3]).toBe('present') // T - in solution but wrong position
    expect(result[4]).toBe('correct') // E
  })

  it('handles duplicate letters correctly', () => {
    // Solution: ROBOT (R-O-B-O-T)
    // Guess: FLOOR (F-L-O-O-R)
    // F(0) - not in solution - absent
    // L(1) - not in solution - absent
    // O(2) - in solution at positions 1 and 3, counts as 2 - present (first O)
    // O(3) - in solution, but both Os already accounted for - correct (happens to match position 3)
    // R(4) - in solution at position 0 - present
    const result = scoreGuess('FLOOR', 'ROBOT')
    expect(result[0]).toBe('absent') // F
    expect(result[1]).toBe('absent') // L
    expect(result[2]).toBe('present') // O in solution but wrong position
    expect(result[3]).toBe('correct') // O matches position 3
    expect(result[4]).toBe('present') // R in solution but wrong position
  })

  it('correctly handles letter frequency', () => {
    // Solution: ABBEY (A-B-B-E-Y)
    // Guess: KAYAK (K-A-Y-A-K)
    const result = scoreGuess('KAYAK', 'ABBEY')
    expect(result[0]).toBe('absent') // K
    expect(result[1]).toBe('present') // A
    expect(result[2]).toBe('present') // Y
    expect(result[3]).toBe('absent') // A - second A, already used
    expect(result[4]).toBe('absent') // K
  })

  it('returns array of length 5', () => {
    const result = scoreGuess('HELLO', 'WORLD')
    expect(result).toHaveLength(5)
  })

  it('handles solution with repeated letters', () => {
    // Solution: SPEED (S-P-E-E-D)
    // Guess: ERASE (E-R-A-S-E)
    const result = scoreGuess('ERASE', 'SPEED')
    expect(result[0]).toBe('present') // E in solution but wrong position
    expect(result[1]).toBe('absent') // R
    expect(result[2]).toBe('absent') // A
    expect(result[3]).toBe('present') // S in solution but wrong position
    expect(result[4]).toBe('present') // E in solution but wrong position
  })

  it('is case insensitive', () => {
    const result1 = scoreGuess('arise', 'ARISE')
    const result2 = scoreGuess('ARISE', 'arise')
    const result3 = scoreGuess('ArIsE', 'aRiSe')
    expect(result1).toEqual(result2)
    expect(result2).toEqual(result3)
  })
})

describe('mergeKeyState', () => {
  it('merges key state with new guess results', () => {
    const current = {}
    const result = mergeKeyState(current, 'ARISE', ['correct', 'present', 'absent', 'correct', 'present'])
    expect(result['A']).toBe('correct')
    expect(result['R']).toBe('present')
    expect(result['I']).toBe('absent')
    expect(result['S']).toBe('correct')
    expect(result['E']).toBe('present')
  })

  it('updates existing keys with better ranks', () => {
    const current = { A: 'absent' as const, R: 'present' as const }
    const result = mergeKeyState(current, 'RAISE', ['correct', 'absent', 'present', 'correct', 'present'])
    expect(result['A']).toBe('absent')
    expect(result['R']).toBe('correct')
  })

  it('preserves existing state when new state is worse', () => {
    const current = { A: 'correct' as const }
    const result = mergeKeyState(current, 'BEACH', ['absent', 'correct', 'absent', 'absent', 'absent'])
    expect(result['A']).toBe('correct')
  })

  it('handles multiple occurrences of same letter', () => {
    const current = {}
    const result = mergeKeyState(current, 'ERASE', ['present', 'absent', 'absent', 'absent', 'correct'])
    expect(result['E']).toBe('correct')
  })

  it('is case insensitive', () => {
    const current1 = {}
    const current2 = {}
    const result1 = mergeKeyState(current1, 'arise', ['correct', 'present', 'absent', 'correct', 'present'])
    const result2 = mergeKeyState(current2, 'ARISE', ['correct', 'present', 'absent', 'correct', 'present'])
    expect(result1).toEqual(result2)
  })

  it('returns new object without mutating input', () => {
    const original = { A: 'absent' as const }
    const originalCopy = { ...original }
    mergeKeyState(original, 'ARISE', ['correct', 'present', 'absent', 'correct', 'present'])
    expect(original).toEqual(originalCopy)
  })

  it('correctly ranks absent < present < correct', () => {
    // First merge: A gets 'present' from BEACH where A is at position 0
    // BEACH: B-E-A-C-H, states: [present, correct, absent, absent, absent]
    // So B='present', E='correct', A='absent', C='absent', H='absent'
    // Wait, let me test the order. The guess is BEACH and result shows:
    // Position 0: B (present), Position 1: E (correct), Position 2: A (absent), Position 3: C (absent), Position 4: H (absent)
    // So when merging with current { A: 'absent' }, A gets 'absent' from position 2 of BEACH
    // Let me use a different test case
    const current = { A: 'absent' as const }
    // AROSE: A-R-O-S-E with states [present, correct, absent, correct, present]
    // So A='present' at position 0
    let result = mergeKeyState(current, 'AROSE', ['present', 'correct', 'absent', 'correct', 'present'])
    expect(result['A']).toBe('present') // upgraded from absent
    expect(result['R']).toBe('correct')

    // Now CANVAS: C-A-N-V-A-S with states [correct, absent, absent, absent, absent, absent]
    // Wait, that's 6 letters. Let me use a 5-letter word
    // MAXIM with A at position 1: M-A-X-I-M, states [absent, correct, absent, absent, absent]
    // So A='correct' at position 1
    result = mergeKeyState(result, 'MAXIM', ['absent', 'correct', 'absent', 'absent', 'absent'])
    expect(result['A']).toBe('correct') // upgraded from present
  })
})
