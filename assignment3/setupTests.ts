// Setup file for vitest
// Add any global test configuration here
import { vi } from 'vitest'

// Mock localStorage if needed
global.localStorage = {
  getItem: vi.fn(),
  setItem: vi.fn(),
  removeItem: vi.fn(),
  clear: vi.fn(),
  key: vi.fn(),
  length: 0,
} as any
