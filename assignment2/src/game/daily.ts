// src/game/daily.ts
const WORD_LEN = 5;

// NYT Wordle lists (answers + accepted guesses)
// Source repo describes these as taken from NYT Wordle JS.
const WORDLES_URL = "https://raw.githubusercontent.com/stuartpb/wordles/main/wordles.json";
const NONWORDLES_URL = "https://raw.githubusercontent.com/stuartpb/wordles/main/nonwordles.json";

type CachePayload<T> = { savedAt: number; data: T };
const ONE_DAY_MS = 24 * 60 * 60 * 1000;

const LS_WORDLES = "wordle_wordles_cache_v2";
const LS_NONWORDLES = "wordle_nonwordles_cache_v2";

// “Attempted NYT today” lock
const LS_NYT_ATTEMPT_PREFIX = "wordle_nyt_attempt_"; // + <localDayId>

// ✅ cache for definition lookups so we don’t spam dictionaryapi.dev
const LS_DEF_TEXT_CACHE = "wordle_def_text_cache_v1"; // { WORD: "def" }
const LS_DEF_OK_CACHE = "wordle_def_ok_cache_v1"; // { WORD: true/false }

export type GameMode = "random" | "nyt";

// ---------- small helpers ----------
function normalizeLower(w: string): string {
  return w.trim().toLowerCase();
}

function normalizeUpper(w: string): string {
  return w.trim().toUpperCase();
}

function isFiveLettersUpper(w: string): boolean {
  return w.length === WORD_LEN && /^[A-Z]{5}$/.test(w);
}

// ✅ FIX: stable "local day id" (based on local midnight), avoids stuck locks
function localDayId(d = new Date()): number {
  const midnight = new Date(d.getFullYear(), d.getMonth(), d.getDate());
  return Math.floor(midnight.getTime() / ONE_DAY_MS);
}

function nytAttemptKey(d = new Date()): string {
  // Example: wordle_nyt_attempt_19876
  return LS_NYT_ATTEMPT_PREFIX + String(localDayId(d));
}

// Optional: keep localStorage from filling up with tons of old attempt keys
function cleanupOldAttemptKeys(keepDays = 14): void {
  try {
    const cutoff = localDayId(new Date()) - keepDays;
    const toDelete: string[] = [];
    for (let i = 0; i < localStorage.length; i++) {
      const k = localStorage.key(i);
      if (!k) continue;
      if (!k.startsWith(LS_NYT_ATTEMPT_PREFIX)) continue;

      const suffix = k.slice(LS_NYT_ATTEMPT_PREFIX.length);
      const id = Number(suffix);
      if (Number.isFinite(id) && id < cutoff) toDelete.push(k);
    }
    toDelete.forEach((k) => localStorage.removeItem(k));
  } catch {
    // ignore
  }
}

function loadCache<T>(key: string): T | null {
  try {
    const raw = localStorage.getItem(key);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as CachePayload<T>;
    if (!parsed?.savedAt) return null;
    if (Date.now() - parsed.savedAt > ONE_DAY_MS) return null;
    return parsed.data;
  } catch {
    return null;
  }
}

function saveCache<T>(key: string, data: T): void {
  try {
    const payload: CachePayload<T> = { savedAt: Date.now(), data };
    localStorage.setItem(key, JSON.stringify(payload));
  } catch {
    // ignore storage failures
  }
}

// ---------- fetch + cache NYT lists ----------
async function fetchJson<T>(url: string): Promise<T> {
  const res = await fetch(url);
  if (!res.ok) throw new Error(`Fetch failed ${res.status} for ${url}`);
  return (await res.json()) as T;
}

async function getWordlesList(): Promise<string[]> {
  const cached = loadCache<string[]>(LS_WORDLES);
  if (cached && cached.length) return cached;

  const list = await fetchJson<string[]>(WORDLES_URL);
  saveCache(LS_WORDLES, list);
  return list;
}

async function getNonWordlesList(): Promise<string[]> {
  const cached = loadCache<string[]>(LS_NONWORDLES);
  if (cached && cached.length) return cached;

  const list = await fetchJson<string[]>(NONWORDLES_URL);
  saveCache(LS_NONWORDLES, list);
  return list;
}

// ✅ Build sets for O(1) lookup (cached in-memory per page load)
let wordlesSet: Set<string> | null = null;
let nonWordlesSet: Set<string> | null = null;

async function ensureSetsLoaded(): Promise<void> {
  if (wordlesSet && nonWordlesSet) return;

  const [wordles, nonwordles] = await Promise.all([getWordlesList(), getNonWordlesList()]);

  // store as lowercase for comparisons
  wordlesSet = new Set(wordles.map((w) => normalizeLower(w)));
  nonWordlesSet = new Set(nonwordles.map((w) => normalizeLower(w)));
}

// ✅ NEW: export the answer list for the hint generator
// This lets src/game/hint.ts pull candidates without duplicating your fetch/cache logic.
export async function getNytAnswerList(): Promise<string[]> {
  const list = await getWordlesList();
  return list
    .map((w) => normalizeUpper(String(w ?? "")))
    .filter((w) => /^[A-Z]{5}$/.test(w));
}

// ---------- NYT “word of the day” ----------
function daysSinceEpoch(localDate: Date): number {
  // Wordle #0 date is commonly treated as 2021-06-19 for the published answer list indexing.
  // We use local midnight-to-midnight in the user's locale.
  const epoch = new Date(2021, 5, 19); // 5 = June (0-based months)
  const start = new Date(epoch.getFullYear(), epoch.getMonth(), epoch.getDate());
  const today = new Date(localDate.getFullYear(), localDate.getMonth(), localDate.getDate());
  const diff = today.getTime() - start.getTime();
  return Math.floor(diff / ONE_DAY_MS);
}

/**
 * ✅ Debug helper so you can PROVE what it’s picking and why.
 * Call this from game.vue and log it.
 */
export async function getNytDebugInfo(date = new Date()): Promise<{
  localDate: string;
  epochStartLocal: string;
  daysSinceEpoch: number;
  listLength: number;
  indexUsed: number;
  word: string;
}> {
  const wordles = await getWordlesList();
  const idx = daysSinceEpoch(date);
  const safeLen = wordles.length || 1;
  const indexUsed = ((idx % safeLen) + safeLen) % safeLen;
  const w = normalizeUpper(wordles[indexUsed] ?? "ARISE");

  const localDate = new Date(date.getFullYear(), date.getMonth(), date.getDate()).toString();
  const epoch = new Date(2021, 5, 19);
  const epochStartLocal = new Date(epoch.getFullYear(), epoch.getMonth(), epoch.getDate()).toString();

  return {
    localDate,
    epochStartLocal,
    daysSinceEpoch: idx,
    listLength: wordles.length,
    indexUsed,
    word: w,
  };
}

export async function getNytDailyWord(date = new Date()): Promise<string> {
  const wordles = await getWordlesList();
  if (!wordles.length) return "ARISE";

  const idx = daysSinceEpoch(date);
  const w = wordles[((idx % wordles.length) + wordles.length) % wordles.length];
  return normalizeUpper(w ?? "ARISE");
}

// ---------- “attempted today” lock ----------
export function hasAttemptedNytToday(): boolean {
  cleanupOldAttemptKeys(); // safe no-op if storage blocked

  const key = nytAttemptKey(new Date());
  try {
    return localStorage.getItem(key) === "1";
  } catch {
    return false;
  }
}

export function markAttemptedNytToday(): void {
  cleanupOldAttemptKeys(); // safe no-op if storage blocked

  const key = nytAttemptKey(new Date());
  try {
    localStorage.setItem(key, "1");
  } catch {
    // ignore
  }
}

// ✅ reset helpers (unlock NYT)
export function resetNytLockToday(): void {
  cleanupOldAttemptKeys(); // optional
  const key = nytAttemptKey(new Date());
  try {
    localStorage.removeItem(key);
  } catch {
    // ignore
  }
}

// Optional: wipe ALL NYT lock keys (debug nuke)
export function resetNytLockAll(): void {
  try {
    const toDelete: string[] = [];
    for (let i = 0; i < localStorage.length; i++) {
      const k = localStorage.key(i);
      if (k && k.startsWith(LS_NYT_ATTEMPT_PREFIX)) toDelete.push(k);
    }
    toDelete.forEach((k) => localStorage.removeItem(k));
  } catch {
    // ignore
  }
}

// ---------- ✅ Guess validation (NO dictionary API!) ----------
export async function isAllowedGuess(word: string): Promise<boolean> {
  const w = normalizeLower(word);

  if (w.length !== WORD_LEN) return false;
  if (!/^[a-z]+$/.test(w)) return false;

  await ensureSetsLoaded();

  if (!wordlesSet || !nonWordlesSet) return false;
  return wordlesSet.has(w) || nonWordlesSet.has(w);
}

// ---------- ✅ Definition fetching + caching ----------
type DefTextCache = Record<string, string>;
type DefOkCache = Record<string, boolean>;

function loadDefTextCache(): DefTextCache {
  try {
    const raw = localStorage.getItem(LS_DEF_TEXT_CACHE);
    return raw ? (JSON.parse(raw) as DefTextCache) : {};
  } catch {
    return {};
  }
}

function saveDefTextCache(cache: DefTextCache): void {
  try {
    localStorage.setItem(LS_DEF_TEXT_CACHE, JSON.stringify(cache));
  } catch {
    // ignore
  }
}

function loadDefOkCache(): DefOkCache {
  try {
    const raw = localStorage.getItem(LS_DEF_OK_CACHE);
    return raw ? (JSON.parse(raw) as DefOkCache) : {};
  } catch {
    return {};
  }
}

function saveDefOkCache(cache: DefOkCache): void {
  try {
    localStorage.setItem(LS_DEF_OK_CACHE, JSON.stringify(cache));
  } catch {
    // ignore
  }
}

async function fetchDefinitionFromDictionaryApi(wordUpper: string): Promise<string | null> {
  const w = normalizeLower(wordUpper);

  try {
    const res = await fetch(`https://api.dictionaryapi.dev/api/v2/entries/en/${w}`);
    if (!res.ok) return null;

    const data = await res.json();
    const def = data?.[0]?.meanings?.[0]?.definitions?.[0]?.definition ?? null;

    return typeof def === "string" && def.trim().length > 0 ? def.trim() : null;
  } catch {
    return null;
  }
}

async function hasDefinition(wordUpper: string): Promise<boolean> {
  const W = normalizeUpper(wordUpper);
  if (!isFiveLettersUpper(W)) return false;

  const okCache = loadDefOkCache();
  if (typeof okCache[W] === "boolean") return okCache[W];

  const def = await fetchDefinitionFromDictionaryApi(W);
  const ok = def !== null;

  okCache[W] = ok;
  saveDefOkCache(okCache);

  if (ok && def) {
    const textCache = loadDefTextCache();
    textCache[W] = def;
    saveDefTextCache(textCache);
  }

  return ok;
}

export async function getDefinition(word: string): Promise<string | null> {
  const W = normalizeUpper(word);
  if (!isFiveLettersUpper(W)) return null;

  const textCache = loadDefTextCache();
  if (typeof textCache[W] === "string" && textCache[W].trim().length > 0) {
    return textCache[W];
  }

  const def = await fetchDefinitionFromDictionaryApi(W);
  if (!def) {
    const okCache = loadDefOkCache();
    okCache[W] = false;
    saveDefOkCache(okCache);
    return null;
  }

  textCache[W] = def;
  saveDefTextCache(textCache);

  const okCache = loadDefOkCache();
  okCache[W] = true;
  saveDefOkCache(okCache);

  return def;
}

// ---------- ✅ Random playable word (NYT answer list + definable only) ----------
async function getRandomWordFromNytAnswers(): Promise<string> {
  const wordles = await getWordlesList();
  if (!wordles.length) return "ARISE";

  for (let tries = 0; tries < 150; tries++) {
    const w = wordles[Math.floor(Math.random() * wordles.length)];
    const W = normalizeUpper(w ?? "");
    if (!isFiveLettersUpper(W)) continue;

    if (await hasDefinition(W)) return W;
  }

  return "ARISE";
}

export async function getPlayableWord(mode: GameMode): Promise<string> {
  if (mode === "nyt") return getNytDailyWord();
  return getRandomWordFromNytAnswers();
}
