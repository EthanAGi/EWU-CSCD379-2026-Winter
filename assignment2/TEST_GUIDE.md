# Test Suite Documentation

## Overview

This project includes comprehensive unit tests for the EWU Wordle game using **Vitest** and **Vue Test Utils**. The tests cover core game logic and component functionality.

## Test Structure

```
assignment2/
├── src/
│   ├── game/
│   │   └── __tests__/
│   │       └── score.spec.ts           # Tests for scoring logic
│   ├── views/
│   │   └── __tests__/
│   │       └── game.spec.ts            # Tests for game logic and state
│   └── components/
│       └── __tests__/
│           └── HelloWorld.spec.ts      # Example component test
```

## Test Files

### 1. `src/game/__tests__/score.spec.ts`

Tests the core Wordle scoring algorithm (`scoreGuess`) and keyboard state management (`mergeKeyState`).

**scoreGuess Tests:**
- ✅ Returns all correct when guess matches solution
- ✅ Returns all absent when no letters match
- ✅ Correctly identifies present letters (letters in solution but wrong position)
- ✅ Handles duplicate letters correctly
- ✅ Correctly handles letter frequency
- ✅ Returns array of length 5
- ✅ Handles solution with repeated letters
- ✅ Is case insensitive

**mergeKeyState Tests:**
- ✅ Merges key state with new guess results
- ✅ Updates existing keys with better ranks (absent → present → correct)
- ✅ Preserves existing state when new state is worse
- ✅ Handles multiple occurrences of same letter
- ✅ Is case insensitive
- ✅ Returns new object without mutating input
- ✅ Correctly ranks absent < present < correct

### 2. `src/views/__tests__/game.spec.ts`

Tests game state management, persistence, and game mechanics.

**Session Stats Tests:**
- ✅ Initialize with default stats
- ✅ Calculate average attempts correctly
- ✅ Display "—" when no wins

**Game Constants Tests:**
- ✅ Have correct game constants (MAX_ATTEMPTS = 6, WORD_LEN = 5)

**NYT Lock Logic Tests:**
- ✅ Calculate correct day ID for same date
- ✅ Calculate different day IDs for different dates
- ✅ Mark and check NYT attempt correctly

**Session Persistence Tests:**
- ✅ Save and restore random game state
- ✅ Validate random state payload

**Stats Persistence Tests:**
- ✅ Save stats to session storage
- ✅ Load stats with defaults for missing values

**Game Status Logic Tests:**
- ✅ Track game status correctly (playing, won, lost)
- ✅ Record win with attempts
- ✅ Record loss

**Guess Management Tests:**
- ✅ Add guess to list
- ✅ Manage current guess input

## Running Tests

### Run all tests once
```bash
npm run test:unit -- --run
```

### Run tests in watch mode (for development)
```bash
npm run test:unit
```

### Run specific test file
```bash
npm run test:unit -- src/game/__tests__/score.spec.ts --run
```

### Run tests with coverage
```bash
npm run test:unit -- --coverage
```

## Test Results

All 32 tests passing:
- ✅ 15 tests in `score.spec.ts`
- ✅ 16 tests in `game.spec.ts`
- ✅ 1 test in `HelloWorld.spec.ts`

## Testing Framework

**Vitest** - A blazing fast unit test framework powered by Vite
- Fast execution
- Instant feedback in watch mode
- Native ESM support
- Great Vue 3 integration

**Vue Test Utils** - Official testing library for Vue components
- Component mounting and testing
- Event simulation
- Property testing

## Key Concepts Tested

### 1. Scoring Algorithm
The tests verify that the Wordle scoring algorithm correctly:
- Identifies exact letter matches (correct)
- Identifies letters present in wrong positions (present)
- Marks letters not in solution (absent)
- Handles duplicate letters with correct frequency
- Works case-insensitively

### 2. Game State Management
Tests verify:
- Stats persistence (wins, losses, average attempts)
- Session storage for random mode
- NYT daily lock mechanism
- Game status transitions
- Guess tracking and validation

### 3. Data Persistence
Tests validate:
- Session storage operations
- Local storage for NYT attempts
- State recovery after page reload
- Data validation before restoration

## Coverage Areas

✅ **Functional Logic:**
- Word scoring system
- Key state merging
- Stats calculation

✅ **State Management:**
- Game status tracking
- Session persistence
- Storage operations

✅ **Game Rules:**
- Daily NYT mode locking
- Random mode persistence
- Win/loss recording

## Future Test Enhancements

Potential areas for additional tests:
- Component rendering tests for Wordle components
- Integration tests for API calls
- E2E tests for complete user workflows
- Performance benchmarks for scoring algorithm

## Troubleshooting

### Tests failing due to timezone issues
The day ID calculation uses millisecond timestamps. If tests fail due to timezone differences, use UTC timestamps directly:
```typescript
const timestamp = 1705276800000 // 2024-01-15 00:00:00 UTC
```

### Mock modules not working
Ensure mocks are placed at the top of test files before imports:
```typescript
vi.mock('path/to/module', () => ({
  // mock implementation
}))
```

## References

- [Vitest Documentation](https://vitest.dev/)
- [Vue Test Utils](https://test-utils.vuejs.org/)
- [Testing Library Best Practices](https://testing-library.com/docs/queries/about)
