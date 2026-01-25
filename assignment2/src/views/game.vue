<template>
  <v-container>
    <div class="d-flex align-center justify-space-between flex-wrap ga-2 mb-3">
      <h1 class="text-h4">EWU Wordle</h1>

      <div class="d-flex ga-2">
        <v-btn
          size="small"
          :disabled="nytLocked"
          :variant="mode === 'nyt' ? 'elevated' : 'outlined'"
          @click="setMode('nyt')"
        >
          NYT Daily
        </v-btn>

        <v-btn
          size="small"
          :variant="mode === 'random' ? 'elevated' : 'outlined'"
          @click="setMode('random')"
        >
          Random
        </v-btn>
      </div>
    </div>

    <div class="d-flex flex-wrap ga-3 mb-3">
      <div class="text-body-2"><b>Wins:</b> {{ stats.wins }}</div>
      <div class="text-body-2"><b>Losses:</b> {{ stats.losses }}</div>
      <div class="text-body-2"><b>Avg attempts (wins):</b> {{ avgAttemptsDisplay }}</div>
    </div>

    <div v-if="nytLocked" class="text-caption mb-3">
      NYT Daily is locked for today (you already attempted it). Random mode only.
    </div>

    <div v-if="loading" class="mb-4">
      <v-progress-linear indeterminate />
      <div class="mt-2">Loading word...</div>
    </div>

    <!-- 👁️ Timed creepy message -->
    <Transition name="fade">
      <div v-if="watchMsgVisible" class="watch-msg" role="status" aria-live="polite">
        You feel like you are being watched
      </div>
    </Transition>

    <div class="play-area">
      <div class="board-wrap">
        <WordleBoard :rows="rows" :shakeRow="shakeRow" />
      </div>

      <div class="guess-wrap">
        <v-btn
          variant="outlined"
          :disabled="loading || status !== 'playing'"
          @click="generateGuess"
        >
          Guess
        </v-btn>
      </div>

      <div class="keyboard-wrap">
        <WordleKeyboard :keyStates="keyStates" @press="onKey" />
      </div>
    </div>

    <v-snackbar v-model="snack.show" :timeout="1800">
      {{ snack.text }}
    </v-snackbar>

    <v-dialog v-model="endDialogOpen" max-width="520">
      <v-card>
        <v-card-title>{{ status === "won" ? "You win!" : "You lose!" }}</v-card-title>

        <v-card-text>
          <div class="mb-2">Word was: <b>{{ solution }}</b></div>

          <div v-if="status === 'won'" class="mb-2">Nice! 🎉</div>
          <div v-else class="mb-2">Try again tomorrow (or hit Restart).</div>

          <div v-if="status === 'won' && definition" class="mt-4">
            <div class="text-subtitle-2 mb-1">Definition</div>
            <div>{{ definition }}</div>
          </div>

          <div v-else-if="status === 'won' && defLoading" class="mt-4">
            <v-progress-linear indeterminate />
            <div class="mt-2">Fetching definition...</div>
          </div>

          <div v-else-if="status === 'won' && !defLoading && definition === null" class="mt-4">
            <div class="text-subtitle-2 mb-1">Definition</div>
            <div>(No definition found.)</div>
          </div>

          <div class="mt-4 text-body-2">
            <b>Session stats:</b>
            Wins {{ stats.wins }}, Losses {{ stats.losses }}, Avg attempts (wins)
            {{ avgAttemptsDisplay }}
          </div>
        </v-card-text>

        <v-card-actions>
          <v-btn variant="text" to="/">Home</v-btn>
          <v-spacer />
          <v-btn color="primary" @click="restart">Restart</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-container>
</template>

<script setup lang="ts">
import { computed, onMounted, onBeforeUnmount, ref, watch } from "vue";
import WordleBoard from "../components/wordle/WordleBoard.vue";
import WordleKeyboard from "../components/wordle/WordleKeyboard.vue";
import { scoreGuess, mergeKeyState, type LetterState } from "../game/score";

import {
  getPlayableWord,
  isAllowedGuess,
  getDefinition,
  type GameMode,
  getNytDebugInfo,
} from "../game/daily";

import { getSuggestedGuess } from "../game/hint";

type TileState = "empty" | LetterState;
type Tile = { letter: string; state: TileState };

const MAX_ATTEMPTS = 6;
const WORD_LEN = 5;
const RANDOM_CLEAR_AFTER_GUESSES = 5;

const loading = ref<boolean>(true);
const solution = ref<string>("ARISE");

const guesses = ref<string[]>([]);
const currentGuess = ref<string>("");
const status = ref<"playing" | "won" | "lost">("playing");

const keyStates = ref<Record<string, LetterState>>({});
const shakeRow = ref<number | null>(null);

const snack = ref<{ show: boolean; text: string }>({ show: false, text: "" });

const endDialogOpen = ref<boolean>(false);

const definition = ref<string | null>(null);
const defLoading = ref<boolean>(false);

const guessStates = ref<LetterState[][]>([]);
const nytLocked = ref<boolean>(false);

const mode = ref<GameMode>("nyt");

watch(
  status,
  (s) => {
    endDialogOpen.value = s !== "playing";
  },
  { immediate: true },
);

// -----------------------------
// 👁️ Timed fade-in/out message
// - show after 3 minutes on page
// - stay visible ~8 seconds
// -----------------------------
const watchMsgVisible = ref<boolean>(false);
let watchShowTimer: number | null = null;
let watchHideTimer: number | null = null;
let watchTriggered = false;

function scheduleWatchMessage(): void {
  // only schedule once per mount
  if (watchTriggered) return;
  watchTriggered = true;

  // show after 3 minutes (180,000ms)
  watchShowTimer = window.setTimeout(() => {
    watchMsgVisible.value = true;

    // hide after ~8 seconds
    watchHideTimer = window.setTimeout(() => {
      watchMsgVisible.value = false;
    }, 4000);
  }, 10000);
}

function clearWatchMessageTimers(): void {
  if (watchShowTimer !== null) {
    window.clearTimeout(watchShowTimer);
    watchShowTimer = null;
  }
  if (watchHideTimer !== null) {
    window.clearTimeout(watchHideTimer);
    watchHideTimer = null;
  }
}

// -----------------------------
// Session stats
// -----------------------------
type SessionStats = {
  wins: number;
  losses: number;
  winAttemptsTotal: number;
};

const SS_STATS = "ewu_wordle_stats_v1";

const stats = ref<SessionStats>({
  wins: 0,
  losses: 0,
  winAttemptsTotal: 0,
});

const gameCounted = ref<boolean>(false);

function loadStats(): void {
  try {
    const raw = sessionStorage.getItem(SS_STATS);
    if (!raw) return;
    const parsed = JSON.parse(raw) as Partial<SessionStats>;
    stats.value = {
      wins: Number(parsed.wins ?? 0) || 0,
      losses: Number(parsed.losses ?? 0) || 0,
      winAttemptsTotal: Number(parsed.winAttemptsTotal ?? 0) || 0,
    };
  } catch {}
}

function saveStats(): void {
  try {
    sessionStorage.setItem(SS_STATS, JSON.stringify(stats.value));
  } catch {}
}

function recordWin(attemptsUsed: number): void {
  stats.value.wins += 1;
  stats.value.winAttemptsTotal += attemptsUsed;
  saveStats();
}

function recordLoss(): void {
  stats.value.losses += 1;
  saveStats();
}

const avgAttemptsDisplay = computed(() => {
  if (stats.value.wins <= 0) return "—";
  const avg = stats.value.winAttemptsTotal / stats.value.wins;
  return avg.toFixed(2);
});

// -----------------------------
// NYT lock logic
// -----------------------------
const ONE_DAY_MS = 24 * 60 * 60 * 1000;
const LS_NYT_ATTEMPT_PREFIX = "wordle_nyt_attempt_";

function localDayId(d = new Date()): number {
  const midnight = new Date(d.getFullYear(), d.getMonth(), d.getDate());
  return Math.floor(midnight.getTime() / ONE_DAY_MS);
}

function nytAttemptKeyForToday(): string {
  return LS_NYT_ATTEMPT_PREFIX + String(localDayId(new Date()));
}

function computeNytLockedToday(): boolean {
  const key = nytAttemptKeyForToday();
  try {
    return localStorage.getItem(key) === "1";
  } catch {
    return false;
  }
}

function markNytAttemptedToday(): void {
  const key = nytAttemptKeyForToday();
  try {
    localStorage.setItem(key, "1");
  } catch {}
}

function resetNytAttemptedToday(): void {
  const key = nytAttemptKeyForToday();
  try {
    localStorage.removeItem(key);
  } catch {}
}

function refreshNytLockFromStorage(): void {
  nytLocked.value = computeNytLockedToday();
}

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
  } catch {}
}

// -----------------------------
// Session persistence (random)
// -----------------------------
const SS_MODE = "ewu_wordle_mode";
const SS_RANDOM_WORD = "ewu_wordle_random_word";
const SS_RANDOM_STATE = "ewu_wordle_random_state_v1";

type RandomStatePayload = {
  solution: string;
  guesses: string[];
  currentGuess: string;
  status: "playing" | "won" | "lost";
  keyStates: Record<string, LetterState>;
  guessStates: LetterState[][];
};

function saveRandomState(): void {
  if (mode.value !== "random") return;

  const payload: RandomStatePayload = {
    solution: solution.value,
    guesses: [...guesses.value],
    currentGuess: currentGuess.value,
    status: status.value,
    keyStates: { ...keyStates.value },
    guessStates: guessStates.value.map((row) => [...row]),
  };

  try {
    sessionStorage.setItem(SS_RANDOM_STATE, JSON.stringify(payload));
  } catch {}
}

function clearRandomState(): void {
  try {
    sessionStorage.removeItem(SS_RANDOM_STATE);
  } catch {}
}

function restoreRandomState(): boolean {
  try {
    const raw = sessionStorage.getItem(SS_RANDOM_STATE);
    if (!raw) return false;
    const parsed = JSON.parse(raw) as Partial<RandomStatePayload>;

    if (!parsed.solution || typeof parsed.solution !== "string") return false;
    if (!Array.isArray(parsed.guesses)) return false;

    solution.value = parsed.solution.toUpperCase();
    guesses.value = parsed.guesses
      .map((g) => String(g).toUpperCase())
      .filter((g) => g.length === WORD_LEN);

    currentGuess.value =
      typeof parsed.currentGuess === "string" ? parsed.currentGuess.toUpperCase() : "";

    status.value =
      parsed.status === "won" || parsed.status === "lost" ? parsed.status : "playing";

    keyStates.value =
      (parsed.keyStates && typeof parsed.keyStates === "object"
        ? parsed.keyStates
        : {}) as Record<string, LetterState>;

    if (Array.isArray(parsed.guessStates)) {
      guessStates.value = parsed.guessStates
        .map((row) => (Array.isArray(row) ? row.slice(0, WORD_LEN) : []))
        .filter((row) => row.length === WORD_LEN) as LetterState[][];
    } else {
      guessStates.value = [];
    }

    return true;
  } catch {
    return false;
  }
}

function clearSavedRandomWord(): void {
  try {
    sessionStorage.removeItem(SS_RANDOM_WORD);
  } catch {}
}

function setInProgress(v: boolean) {
  try {
    sessionStorage.setItem("ewu_wordle_in_progress", v ? "1" : "0");
  } catch {}
}

function isInProgress(): boolean {
  return status.value === "playing" && guesses.value.length > 0;
}

function dbg(...args: any[]) {
  console.log("[WORDLE DEBUG]", ...args);
}

function showMessage(text: string) {
  snack.value.text = text;
  snack.value.show = true;
}

async function loadGameWord(opts?: { forceNewRandom?: boolean; reason?: string }) {
  const reason = opts?.reason ?? "unknown";
  const forceNewRandom = opts?.forceNewRandom ?? false;

  loading.value = true;
  definition.value = null;
  defLoading.value = false;

  try {
    dbg("loadGameWord()", { mode: mode.value, reason, forceNewRandom });

    if (mode.value === "random") {
      if (guesses.value.length > 0 || isInProgress()) {
        dbg("random: keeping solution =", solution.value);
        return;
      }

      if (!forceNewRandom) {
        const saved = sessionStorage.getItem(SS_RANDOM_WORD);
        if (saved && saved.length === WORD_LEN) {
          solution.value = saved.toUpperCase();
          dbg("random: restored solution from sessionStorage =", solution.value);
          saveRandomState();
          return;
        }
      }

      const w = await getPlayableWord("random");
      solution.value = w.toUpperCase();

      try {
        sessionStorage.setItem(SS_RANDOM_WORD, solution.value);
      } catch {}

      dbg("random: new solution =", solution.value);
      saveRandomState();
      return;
    }

    const w = await getPlayableWord("nyt");
    solution.value = w.toUpperCase();
    dbg("nyt: solution =", solution.value);

    const info = await getNytDebugInfo();
    console.log("[NYT DEBUG INFO]", info);
  } catch (err) {
    console.error("[WORDLE DEBUG] loadGameWord failed:", err);
    solution.value = "ARISE";
    showMessage("Could not load word. Using fallback word.");
  } finally {
    loading.value = false;
  }
}

function resetBoardState() {
  guesses.value = [];
  guessStates.value = [];
  currentGuess.value = "";
  status.value = "playing";
  keyStates.value = {};
  shakeRow.value = null;
  definition.value = null;
  defLoading.value = false;
  setInProgress(false);

  endDialogOpen.value = false;

  gameCounted.value = false;

  if (mode.value === "random") clearRandomState();
}

async function setModeSafe(next: GameMode) {
  cleanupOldAttemptKeys();
  refreshNytLockFromStorage();

  if (next === "nyt" && nytLocked.value) return;

  mode.value = next;

  try {
    sessionStorage.setItem(SS_MODE, mode.value);
  } catch {}

  resetBoardState();
  await loadGameWord({ reason: "mode switch", forceNewRandom: false });
}

function setMode(next: GameMode) {
  void setModeSafe(next);
}

function buildRows(): Tile[][] {
  const rows: Tile[][] = [];

  for (let r = 0; r < MAX_ATTEMPTS; r++) {
    const rowGuess =
      guesses.value[r] ?? (r === guesses.value.length ? currentGuess.value : "");
    const letters = rowGuess.toUpperCase().padEnd(WORD_LEN).slice(0, WORD_LEN);

    let states: TileState[] = Array(WORD_LEN).fill("empty");

    if (guesses.value[r]) {
      const saved = guessStates.value[r];
      if (saved && saved.length === WORD_LEN) {
        states = saved as TileState[];
      } else {
        const scored = scoreGuess(guesses.value[r]!, solution.value) as LetterState[];
        states = scored as TileState[];
      }
    }

    rows.push(
      Array.from({ length: WORD_LEN }, (_, i) => ({
        letter: letters.charAt(i),
        state: states[i]!,
      })),
    );
  }

  return rows;
}

const rows = computed<Tile[][]>(() => buildRows());

function triggerShake() {
  shakeRow.value = guesses.value.length;
  window.setTimeout(() => (shakeRow.value = null), 400);
}

async function generateGuess(): Promise<void> {
  if (loading.value) return;
  if (status.value !== "playing") return;

  try {
    const suggestion = await getSuggestedGuess({
      guesses: guesses.value,
      states: guessStates.value,
      exclude: new Set(guesses.value.map((g) => g.toUpperCase())),
    });

    if (!suggestion) {
      showMessage("No suggestion found.");
      return;
    }

    currentGuess.value = suggestion;
    if (mode.value === "random") saveRandomState();

    showMessage(`Suggestion: ${suggestion}`);
  } catch (e) {
    console.error("generateGuess failed", e);
    showMessage("Could not generate a guess.");
  }
}

async function endGame(as: "won" | "lost"): Promise<void> {
  status.value = as;
  setInProgress(false);

  if (!gameCounted.value) {
    gameCounted.value = true;
    if (as === "won") recordWin(guesses.value.length);
    else recordLoss();
  }

  if (as === "won") {
    defLoading.value = true;
    definition.value = null;

    try {
      definition.value = await getDefinition(solution.value);
    } catch {
      definition.value = null;
    } finally {
      defLoading.value = false;
    }
  }

  if (mode.value === "nyt") {
    mode.value = "random";
    try {
      sessionStorage.setItem(SS_MODE, "random");
    } catch {}
    dbg("nyt: ended -> auto-switched mode to random");
  }

  if (mode.value === "random") saveRandomState();
}

async function submitGuess(): Promise<void> {
  if (loading.value) return;
  if (status.value !== "playing") return;

  const g = currentGuess.value.toUpperCase();

  if (g.length !== WORD_LEN) {
    showMessage("Need 5 letters.");
    triggerShake();
    return;
  }

  const alreadyGuessed = guesses.value.some((x) => x.toUpperCase() === g);
  if (alreadyGuessed) {
    showMessage("Already guessed.");
    triggerShake();
    return;
  }

  const ok = await isAllowedGuess(g);
  if (!ok) {
    showMessage("Not in word list.");
    triggerShake();
    return;
  }

  if (mode.value === "nyt" && guesses.value.length === 0) {
    markNytAttemptedToday();
    nytLocked.value = true;
  }

  const statesForMerge = scoreGuess(g, solution.value) as LetterState[];
  keyStates.value = mergeKeyState(keyStates.value, g, statesForMerge);

  guesses.value.push(g);
  guessStates.value.push(statesForMerge);
  setInProgress(true);

  if (mode.value === "random") {
    saveRandomState();

    if (guesses.value.length >= RANDOM_CLEAR_AFTER_GUESSES) {
      clearSavedRandomWord();
      dbg("random: reached 5 guesses -> cleared saved random word for next game");
    }
  }

  currentGuess.value = "";

  if (g === solution.value) {
    await endGame("won");
    return;
  }

  if (guesses.value.length >= MAX_ATTEMPTS) {
    await endGame("lost");
  }

  if (mode.value === "random") saveRandomState();
}

function onKey(k: string) {
  if (loading.value) return;
  if (status.value !== "playing") return;

  if (k === "ENTER") {
    void submitGuess();
    return;
  }

  if (k === "BACK") {
    currentGuess.value = currentGuess.value.slice(0, -1);
    if (mode.value === "random") saveRandomState();
    return;
  }

  if (/^[A-Z]$/.test(k)) {
    if (currentGuess.value.length < WORD_LEN) {
      currentGuess.value += k;
      if (mode.value === "random") saveRandomState();
    }
  }
}

function onKeyDown(e: KeyboardEvent) {
  if (e.ctrlKey && e.shiftKey && e.key.toLowerCase() === "u") {
    e.preventDefault();
    resetNytAttemptedToday();
    refreshNytLockFromStorage();
    showMessage("NYT Daily unlocked for today.");
    dbg("dev: reset nyt lock -> nytLocked =", nytLocked.value);
    return;
  }

  if (e.key === "Enter") return onKey("ENTER");
  if (e.key === "Backspace") return onKey("BACK");

  const ch = e.key.toUpperCase();
  if (/^[A-Z]$/.test(ch)) onKey(ch);
}

async function restart(): Promise<void> {
  resetBoardState();

  if (mode.value === "random") {
    clearSavedRandomWord();
    clearRandomState();
    dbg("random: restart -> cleared saved random word + random state");
  }

  await loadGameWord({ reason: "restart", forceNewRandom: mode.value === "random" });
}

function onVisibilityChange() {
  if (document.visibilityState === "visible") {
    cleanupOldAttemptKeys();
    refreshNytLockFromStorage();
    dbg("visibilitychange -> refreshed nytLocked =", nytLocked.value);
  }
}

onMounted(() => {
  // schedule the creepy message
  scheduleWatchMessage();

  loadStats();

  cleanupOldAttemptKeys();
  refreshNytLockFromStorage();

  mode.value = nytLocked.value ? "random" : "nyt";

  try {
    sessionStorage.setItem(SS_MODE, mode.value);
  } catch {}

  dbg("mounted", { nytLocked: nytLocked.value, mode: mode.value });

  window.addEventListener("keydown", onKeyDown);
  document.addEventListener("visibilitychange", onVisibilityChange);

  if (mode.value === "random") {
    const restored = restoreRandomState();
    if (restored && (guesses.value.length > 0 || status.value !== "playing")) {
      dbg("random: restored state", {
        solution: solution.value,
        guesses: guesses.value.length,
        status: status.value,
      });
      loading.value = false;
      return;
    }
  }

  void loadGameWord({ reason: "mounted", forceNewRandom: false });
});

onBeforeUnmount(() => {
  window.removeEventListener("keydown", onKeyDown);
  document.removeEventListener("visibilitychange", onVisibilityChange);

  clearWatchMessageTimers();
});
</script>

<style scoped>
/* Fade transition for the creepy message */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 650ms ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

/* Positioned banner */
.watch-msg {
  position: fixed;
  left: 50%;
  top: 16px;
  transform: translateX(-50%);
  z-index: 3000;

  padding: 10px 14px;
  border-radius: 10px;

  background: rgba(0, 0, 0, 0.78);
  color: #ffffff;
  font-weight: 600;
  letter-spacing: 0.2px;

  /* keep it readable on small screens */
  max-width: min(92vw, 520px);
  text-align: center;
  pointer-events: none;
}

.play-area {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  width: 100%;
  margin-inline: auto;

  padding-bottom: max(24px, env(safe-area-inset-bottom));
  gap: 16px;
}

.board-wrap {
  width: min(92vw, 420px);
  display: flex;
  justify-content: center;
}

.guess-wrap {
  width: min(92vw, 520px);
  display: flex;
  justify-content: center;
}

.keyboard-wrap {
  width: min(96vw, 520px);
  display: flex;
  justify-content: center;

  margin-bottom: 12px;
  padding-bottom: max(12px, env(safe-area-inset-bottom));
}

@media (max-width: 420px) {
  .play-area {
    gap: 12px;
    padding-bottom: max(32px, env(safe-area-inset-bottom));
  }
  .board-wrap {
    width: 94vw;
  }
  .keyboard-wrap {
    width: 98vw;
  }
}
</style>
