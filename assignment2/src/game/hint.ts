// src/game/hint.ts
import { getNytAnswerList } from "./daily";

type TileState = "empty" | "absent" | "present" | "correct";

export type HintInput = {
  guesses: string[];
  states: TileState[][];
  exclude?: Set<string>;
};

// Strong openers (safe + common)
const STARTERS = [
  "SLATE", "CRANE", "ADIEU", "REACT", "STARE", "SOARE",
  "AUDIO", "OCEAN", "RAISE", "AISLE", "IRATE", "ROATE", "TEARS",
  "EARNS", "NEARS", "SALET", "SCARE", "CORES", "LOUIE", "ALONE",
  "ANISE", "OPERA", "URAEI", "AEONS", "ARENO", "OLEAS", "AEROS",
  "OSIER", "ORATE", "IDEAS", "SEORA"
];


// ✅ Persisted per-session starter rotation index
const SS_STARTER_INDEX = "ewu_wordle_starter_index_v1";

function normalizeUpper(w: string): string {
  return w.trim().toUpperCase();
}

function isFiveLettersUpper(w: string): boolean {
  return /^[A-Z]{5}$/.test(w);
}

/**
 * With `noUncheckedIndexedAccess`, `arr[i]` becomes `T | undefined`.
 * So we NEVER do `bannedAt[i].add(...)` directly.
 */
function makeBannedAt(): Set<string>[] {
  return Array.from({ length: 5 }, () => new Set<string>());
}

// Shared immutable empty set for out-of-range safety
const EMPTY_SET: Set<string> = new Set<string>();

function getBannedSet(bannedAt: Set<string>[], i: number): Set<string> {
  // Always return a set (never undefined)
  // If i is out of range, return a harmless empty set.
  return bannedAt[i] ?? EMPTY_SET;
}

function obeysBasicConstraints(word: string, guesses: string[], states: TileState[][]): boolean {
  const W = normalizeUpper(word);
  if (!isFiveLettersUpper(W)) return false;

  const greens: Array<string | null> = Array(5).fill(null);
  const bannedAt = makeBannedAt(); // length 5
  const required = new Set<string>();
  const excluded = new Set<string>();

  for (let g = 0; g < guesses.length; g++) {
    const guess = normalizeUpper(guesses[g] ?? "");
    const st = states[g];

    if (!isFiveLettersUpper(guess)) continue;
    if (!Array.isArray(st) || st.length !== 5) continue;

    for (let i = 0; i < 5; i++) {
      const ch = guess[i]!;
      const s = st[i]!;

      if (s === "correct") {
        greens[i] = ch;
        required.add(ch);
        continue;
      }

      if (s === "present") {
        required.add(ch);
        getBannedSet(bannedAt, i).add(ch);
        continue;
      }

      if (s === "absent") {
        excluded.add(ch);
        continue;
      }

      // "empty" => ignore
    }
  }

  // If a letter is known required, it cannot be globally excluded.
  for (const r of required) excluded.delete(r);

  // Apply positional + excluded checks
  for (let i = 0; i < 5; i++) {
    const ch = W[i]!;
    const green = greens[i];

    if (green && green !== ch) return false;
    if (getBannedSet(bannedAt, i).has(ch)) return false;
    if (excluded.has(ch)) return false;
  }

  // Apply "must contain" checks
  for (const r of required) {
    if (!W.includes(r)) return false;
  }

  return true;
}

// Prefer trying new letters, plus prefer 5 unique letters early
function scoreCandidate(word: string, guesses: string[]): number {
  const used = new Set<string>(normalizeUpper(guesses.join("")).split(""));
  const letters = new Set<string>(normalizeUpper(word).split(""));
  let newLetters = 0;
  for (const ch of letters) if (!used.has(ch)) newLetters++;
  return newLetters * 10 + letters.size;
}

// ✅ Starter cycling helpers
function loadStarterIndex(): number {
  try {
    const raw = sessionStorage.getItem(SS_STARTER_INDEX);
    const n = raw == null ? 0 : Number(raw);
    return Number.isFinite(n) ? n : 0;
  } catch {
    return 0;
  }
}

function saveStarterIndex(n: number): void {
  try {
    sessionStorage.setItem(SS_STARTER_INDEX, String(n));
  } catch {
    // ignore
  }
}

/**
 * Returns the next starter in rotation, skipping excluded ones when possible.
 * Always advances the stored index so repeated clicks cycle.
 */
function getNextStarter(exclude: Set<string>): string | null {
  if (STARTERS.length === 0) return null;

  let idx = loadStarterIndex();
  idx = ((idx % STARTERS.length) + STARTERS.length) % STARTERS.length;

  // Try up to STARTERS.length candidates, starting from idx
  for (let tries = 0; tries < STARTERS.length; tries++) {
    const pick = STARTERS[(idx + tries) % STARTERS.length]!;
    if (!exclude.has(pick)) {
      // Advance index to the *next* position after the one we chose
      saveStarterIndex((idx + tries + 1) % STARTERS.length);
      return pick;
    }
  }

  // All starters excluded; still advance by 1 so user sees cycling behavior
  saveStarterIndex((idx + 1) % STARTERS.length);
  return null;
}

/**
 * Main generator:
 * - If no guesses: return a starter (cycles each time)
 * - Else: return a candidate from NYT answers that fits learned constraints
 */
export async function getSuggestedGuess(input: HintInput): Promise<string | null> {
  const exclude = input.exclude ?? new Set<string>();

  // 0 guesses => cycle starter list
  if (input.guesses.length === 0) {
    const starter = getNextStarter(exclude);
    if (starter) return starter;
    // fallback continues below if all starters were excluded
  }

  const answers = await getNytAnswerList(); // expected uppercase 5-letter strings
  const filteredAnswers = answers.filter((w) => isFiveLettersUpper(w));

  const candidates = filteredAnswers
    .filter((w) => !exclude.has(w))
    .filter((w) => obeysBasicConstraints(w, input.guesses, input.states));

  if (candidates.length === 0) {
    const fallback = filteredAnswers.filter((w) => !exclude.has(w));
    if (fallback.length === 0) return null;

    fallback.sort((a, b) => scoreCandidate(b, input.guesses) - scoreCandidate(a, input.guesses));
    return fallback[0] ?? null;
  }

  candidates.sort((a, b) => scoreCandidate(b, input.guesses) - scoreCandidate(a, input.guesses));
  return candidates[0] ?? null;
}
