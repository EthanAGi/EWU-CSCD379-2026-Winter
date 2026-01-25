export type LetterState = "absent" | "present" | "correct";

export function scoreGuess(guess: string, solution: string): LetterState[] {
  const g = guess.toUpperCase();
  const s = solution.toUpperCase();

  const result: LetterState[] = Array(5).fill("absent");
  const remaining: Record<string, number> = {};

  // First pass: mark correct + count remaining letters in solution
  for (let i = 0; i < 5; i++) {
    const gch = g.charAt(i);
    const sch = s.charAt(i);

    if (gch === sch) {
      result[i] = "correct";
    } else {
      remaining[sch] = (remaining[sch] ?? 0) + 1;
    }
  }

  // Second pass: mark present based on remaining counts
  for (let i = 0; i < 5; i++) {
    if (result[i] === "correct") continue;

    const gch = g.charAt(i);
    const count = remaining[gch] ?? 0;

    if (count > 0) {
      result[i] = "present";
      remaining[gch] = count - 1;
    } else {
      result[i] = "absent";
    }
  }

  return result;
}

export function mergeKeyState(
  current: Record<string, LetterState>,
  guess: string,
  states: LetterState[],
) {
  const rank = (s: LetterState) => (s === "correct" ? 3 : s === "present" ? 2 : 1);

  const next = { ...current };
  const g = guess.toUpperCase();

  for (let i = 0; i < 5; i++) {
    const letter = g.charAt(i);
    const st = states[i]!; // <-- IMPORTANT: non-null assertion

    const prev = next[letter];
    if (!prev || rank(st) > rank(prev)) {
      next[letter] = st;
    }
  }

  return next;
}
