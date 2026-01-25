// src/game/word_api.ts
const WORD_LEN = 5;

// localStorage keys
const LS_TODAY_KEY = "ewu_wordle_today_key_v2";
const LS_TODAY_WORD = "ewu_wordle_today_word_v2";
const LS_PLAYED_KEY = "ewu_wordle_played_key_v2";
const LS_POOL = "ewu_wordle_pool_v2";

// NEW: store a big list of common 5-letter words from Datamuse
const LS_WORDLIST = "ewu_wordle_wordlist_v2";
const LS_WORDLIST_STAMP = "ewu_wordle_wordlist_stamp_v2";

// ✅ NEW: keep the current RANDOM solution stable during a play session
// If you clear this key, the next random word will be different.
const SS_RANDOM_WORD = "ewu_wordle_random_word_session_v2";

// How many words to fetch & cache locally
const WORDLIST_MAX = 1000;

function todayKey(): string {
  const d = new Date();
  const yyyy = d.getFullYear();
  const mm = String(d.getMonth() + 1).padStart(2, "0");
  const dd = String(d.getDate()).padStart(2, "0");
  return `${yyyy}-${mm}-${dd}`;
}

function normalize(w: string): string {
  return w.trim().toUpperCase();
}

function isFiveLetters(w: string): boolean {
  return w.length === WORD_LEN && /^[A-Z]{5}$/.test(w);
}

// ✅ FIX: can return null if the array is empty
function pickRandom<T>(arr: readonly T[]): T | null {
  if (arr.length === 0) return null;
  return arr[Math.floor(Math.random() * arr.length)] ?? null;
}

function hashStringToInt(s: string): number {
  // simple deterministic hash
  let h = 0;
  for (let i = 0; i < s.length; i++) {
    h = (h * 31 + s.charCodeAt(i)) >>> 0;
  }
  return h;
}

/**
 * 1) WORD VALIDATION (Datamuse)
 * Datamuse returns 200 + [] if not found (no more dictionaryapi.dev 404 spam)
 */
const VALID_CACHE_KEY = "ewu_wordle_valid_cache_v2";

function loadValidCache(): Record<string, true> {
  try {
    return JSON.parse(localStorage.getItem(VALID_CACHE_KEY) ?? "{}") as Record<string, true>;
  } catch {
    return {};
  }
}

function saveValidCache(cache: Record<string, true>) {
  try {
    localStorage.setItem(VALID_CACHE_KEY, JSON.stringify(cache));
  } catch {
    // ignore
  }
}

export async function isAllowedGuess(word: string): Promise<boolean> {
  const w = normalize(word);
  if (!isFiveLetters(w)) return false;

  const cache = loadValidCache();
  if (cache[w]) return true;

  try {
    const res = await fetch(`https://api.datamuse.com/words?sp=${w.toLowerCase()}&max=1`);
    if (!res.ok) return false;

    const data = (await res.json()) as Array<{ word?: string }>;
    const ok = data.length > 0 && (data[0]?.word ?? "").toLowerCase() === w.toLowerCase();

    if (ok) {
      cache[w] = true;
      saveValidCache(cache);
    }

    return ok;
  } catch {
    return false;
  }
}

/**
 * 2) FETCH A BIG 5-LETTER WORDLIST (Datamuse) ONCE, THEN REUSE IT
 *
 * Endpoint: sp=????? returns words with 5 letters.
 * We cache the list in localStorage so we don't hit the API constantly.
 */
function loadWordList(): string[] {
  try {
    const raw = JSON.parse(localStorage.getItem(LS_WORDLIST) ?? "[]") as unknown;
    if (!Array.isArray(raw)) return [];
    return raw
      .filter((x) => typeof x === "string")
      .map((x) => normalize(x))
      .filter(isFiveLetters);
  } catch {
    return [];
  }
}

function saveWordList(words: string[]): void {
  try {
    localStorage.setItem(LS_WORDLIST, JSON.stringify(words));
    localStorage.setItem(LS_WORDLIST_STAMP, String(Date.now()));
  } catch {
    // ignore
  }
}

function wordListIsFresh(maxAgeDays = 14): boolean {
  // refresh every ~2 weeks
  const stampStr = localStorage.getItem(LS_WORDLIST_STAMP);
  if (!stampStr) return false;
  const stamp = Number(stampStr);
  if (!Number.isFinite(stamp)) return false;

  const ageMs = Date.now() - stamp;
  const maxAgeMs = maxAgeDays * 24 * 60 * 60 * 1000;
  return ageMs <= maxAgeMs;
}

async function fetchDatamuseWordList(): Promise<string[]> {
  const res = await fetch(`https://api.datamuse.com/words?sp=?????&max=${WORDLIST_MAX}`);
  if (!res.ok) throw new Error("datamuse wordlist fetch failed");

  const data = (await res.json()) as Array<{ word?: string }>;
  const words = data
    .map((x) => (x.word ?? "").toString())
    .map((w) => normalize(w))
    .filter(isFiveLetters);

  // de-dupe
  return Array.from(new Set(words));
}

async function ensureWordListLoaded(): Promise<string[]> {
  const existing = loadWordList();
  if (existing.length >= 200 && wordListIsFresh()) {
    return existing;
  }

  const fresh = await fetchDatamuseWordList();
  const finalList = fresh.length >= 200 ? fresh : existing;

  if (finalList.length > 0) saveWordList(finalList);

  return finalList.length > 0 ? finalList : ["ARISE"];
}

/**
 * 3) RANDOM SOLUTION WORD (from cached list)
 */
async function getRandomWordFromList(exclude?: string): Promise<string> {
  const list = await ensureWordListLoaded();
  if (list.length === 0) return "ARISE";

  const ex = exclude ? normalize(exclude) : null;

  for (let tries = 0; tries < 40; tries++) {
    const w = pickRandom(list);
    if (!w) continue;
    if (ex && w === ex) continue;
    return w;
  }

  return "ARISE";
}

/**
 * ✅ NEW: Random word that stays the same during a play session.
 * This prevents “random mode changing the word” due to reloads/rerenders.
 *
 * - Call this once when starting Random mode.
 * - Call clearRandomWord() when the Random game ends (win/loss) or when the user hits Restart.
 */
export async function getRandomWord(exclude?: string): Promise<string> {
  // 1) try restore from sessionStorage
  try {
    const stored = sessionStorage.getItem(SS_RANDOM_WORD);
    if (stored && isFiveLetters(normalize(stored))) {
      const w = normalize(stored);
      const ex = exclude ? normalize(exclude) : null;
      if (!ex || w !== ex) return w;
      // if it equals exclude, fall through and pick a new one
    }
  } catch {
    // ignore
  }

  // 2) pick a new one and store it
  const picked = await getRandomWordFromList(exclude);

  try {
    sessionStorage.setItem(SS_RANDOM_WORD, picked);
  } catch {
    // ignore
  }

  return picked;
}

/**
 * ✅ NEW: Call this when Random game ends or user restarts.
 * It guarantees the next random game uses a new word.
 */
export function clearRandomWord(): void {
  try {
    sessionStorage.removeItem(SS_RANDOM_WORD);
  } catch {
    // ignore
  }
}

/**
 * 4) POOL CACHING (same idea you already had)
 */
function loadPool(): string[] {
  try {
    const arr = JSON.parse(localStorage.getItem(LS_POOL) ?? "[]") as unknown;
    if (!Array.isArray(arr)) return [];
    return arr
      .filter((x) => typeof x === "string")
      .map((x) => normalize(x))
      .filter(isFiveLetters);
  } catch {
    return [];
  }
}

function savePool(words: string[]) {
  try {
    localStorage.setItem(LS_POOL, JSON.stringify(words));
  } catch {
    // ignore
  }
}

async function refillPoolIfNeeded(minSize = 10): Promise<void> {
  const pool = loadPool();
  if (pool.length >= minSize) return;

  const needed = minSize - pool.length;
  const newWords: string[] = [];

  for (let i = 0; i < needed; i++) {
    newWords.push(await getRandomWordFromList());
  }

  savePool([...pool, ...newWords]);
}

function takeFromPool(exclude?: string): string | null {
  const pool = loadPool();
  if (pool.length === 0) return null;

  const ex = exclude ? normalize(exclude) : null;

  const idx = pool.findIndex((w) => (ex ? w !== ex : true));
  if (idx === -1) return null;

  const picked = pool[idx];
  if (!picked) return null;

  pool.splice(idx, 1);
  savePool(pool);

  return picked;
}

/**
 * 5) TODAY WORD (deterministic per-day)
 * Stable for the day and uses the cached word list.
 */
export async function getTodayWord(): Promise<string> {
  const key = todayKey();
  const storedKey = localStorage.getItem(LS_TODAY_KEY);
  const storedWord = localStorage.getItem(LS_TODAY_WORD);

  if (storedKey === key && storedWord && isFiveLetters(normalize(storedWord))) {
    return normalize(storedWord);
  }

  const list = await ensureWordListLoaded();
  const safeList = list.length > 0 ? list : ["ARISE"];

  const idx = hashStringToInt(key) % safeList.length;
  const picked = normalize(safeList[idx] ?? "ARISE");

  localStorage.setItem(LS_TODAY_KEY, key);
  localStorage.setItem(LS_TODAY_WORD, picked);

  return picked;
}

/**
 * 6) MAIN RULE (your original behavior):
 * - If not played today → return today word
 * - If played today → return a different random word
 *
 * NOTE: This is your “daily once then random” behavior, NOT the mode-switch UI.
 */
export async function getPlayableWord(): Promise<string> {
  const key = todayKey();
  const playedKey = localStorage.getItem(LS_PLAYED_KEY);

  const todayWord = await getTodayWord();

  if (playedKey !== key) {
    return todayWord;
  }

  await refillPoolIfNeeded();
  return takeFromPool(todayWord) ?? (await getRandomWordFromList(todayWord));
}

/** Call when the game ends (win or loss) for the DAILY-once behavior. */
export function markPlayedToday(): void {
  localStorage.setItem(LS_PLAYED_KEY, todayKey());
}
