// composables/useBgm.ts
type BgmZone = 'stable' | 'gauntlet' | null

type BgmState = {
  enabled: boolean
  initialized: boolean
  currentZone: BgmZone

  // Which audio element is currently "active" (we crossfade between 2)
  aIndex: 0 | 1
  switching: boolean

  // Remember which specific track was chosen for each zone
  chosenTrackByZone: Record<'stable' | 'gauntlet', string | null>
}

function clamp(n: number, min: number, max: number) {
  return Math.max(min, Math.min(max, n))
}

/**
 * ✅ Playlists:
 * Add/remove mp3 files here when you drop songs into /public/audio/<zone>/
 *
 * Paths are from /public, so they start with /audio/...
 *
 * YOUR CURRENT FILES (from your screenshot):
 * /public/audio/gauntlet/
 *   - God.mp3
 *   - Homeward.mp3
 *   - Metaphor.mp3
 *   - Royal.mp3
 *
 * If you also have stable tracks, add them in the stable list below.
 */
const PLAYLISTS: Record<'stable' | 'gauntlet', string[]> = {
  stable: [
    '/audio/stable/Rain.mp3',
    '/audio/stable/Secret.mp3',
    '/audio/stable/Whistle.mp3',
  ],
  gauntlet: [
    '/audio/gauntlet/God.mp3',
    '/audio/gauntlet/Homeward.mp3',
    '/audio/gauntlet/Metaphor.mp3',
    '/audio/gauntlet/Royal.mp3',
  ],
}

function pickRandom(list: string[], avoid?: string | null): string | null {
  if (!list || list.length === 0) return null
  if (list.length === 1) return list[0]!

  // Try to pick something different than the currently playing track for that zone
  for (let tries = 0; tries < 6; tries++) {
    const candidate = list[Math.floor(Math.random() * list.length)]!
    if (!avoid || candidate !== avoid) return candidate
  }
  // Fallback
  return list[Math.floor(Math.random() * list.length)]!
}

export function useBgm() {
  const state = useState<BgmState>('bgm_state_v2', () => ({
    enabled: true,
    initialized: false,
    currentZone: null,
    aIndex: 0,
    switching: false,
    chosenTrackByZone: {
      stable: null,
      gauntlet: null,
    },
  }))

  const audios = useState<(HTMLAudioElement | null)[]>('bgm_audios_v1', () => [null, null])

  function ensureAudioElements() {
    if (!import.meta.client) return
    if (audios.value[0] && audios.value[1]) return

    const a0 = new Audio()
    const a1 = new Audio()

    for (const a of [a0, a1]) {
      a.loop = true
      a.preload = 'auto'
      a.volume = 0
    }

    audios.value = [a0, a1]
  }

  /**
   * Browser audio requires a user gesture before playback.
   * We “unlock” on first click/keypress.
   */
  function initOnce() {
    if (!import.meta.client) return
    if (state.value.initialized) return

    ensureAudioElements()

    const unlock = async () => {
      state.value.initialized = true
      window.removeEventListener('pointerdown', unlock)
      window.removeEventListener('keydown', unlock)

      // If we already chose a zone earlier, start it after unlock
      if (state.value.enabled && state.value.currentZone) {
        await playZone(state.value.currentZone)
      }
    }

    window.addEventListener('pointerdown', unlock, { once: true })
    window.addEventListener('keydown', unlock, { once: true })
  }

  function setEnabled(on: boolean) {
    state.value.enabled = on
    if (!import.meta.client) return

    ensureAudioElements()
    const a0 = audios.value[0]
    const a1 = audios.value[1]
    if (!a0 || !a1) return

    if (!on) {
      fadeTo(a0, 0, 250, true)
      fadeTo(a1, 0, 250, true)
      return
    }

    // Turning on: resume current zone (if unlocked)
    if (state.value.currentZone && state.value.initialized) {
      playZone(state.value.currentZone)
    }
  }

  function getZoneForPath(path: string): BgmZone {
    if (path.startsWith('/stable')) return 'stable'
    if (path.startsWith('/gauntlet')) return 'gauntlet'

    // reviews + shop + other pages: keep current track (no forced change)
    return null
  }

  /**
   * Called from layout watch(route.path)
   * - Only changes when entering Stable or Gauntlet
   * - Does nothing for reviews/shop so music continues
   */
  async function setZoneFromRoute(path: string) {
    initOnce()

    const next = getZoneForPath(path)

    // null means "don’t force a change"
    if (next === null) return

    // If re-entering same zone, do nothing (prevents restart)
    if (state.value.currentZone === next) return

    state.value.currentZone = next

    if (!state.value.enabled) return
    if (!import.meta.client) return
    if (!state.value.initialized) return

    await playZone(next)
  }

  function fadeTo(a: HTMLAudioElement, targetVol: number, ms: number, pauseAtEnd = false) {
    targetVol = clamp(targetVol, 0, 1)

    const startVol = a.volume
    const start = performance.now()
    const dur = Math.max(1, ms)

    const tick = () => {
      const t = (performance.now() - start) / dur
      const k = clamp(t, 0, 1)
      a.volume = startVol + (targetVol - startVol) * k

      if (k < 1) {
        requestAnimationFrame(tick)
      } else {
        a.volume = targetVol
        if (pauseAtEnd && targetVol === 0) {
          try {
            a.pause()
            a.currentTime = 0
          } catch {
            // ignore
          }
        }
      }
    }

    requestAnimationFrame(tick)
  }

  function pickTrackForZone(zone: Exclude<BgmZone, null>): string | null {
    const list = PLAYLISTS[zone]
    const prev = state.value.chosenTrackByZone[zone]
    const chosen = prev ?? pickRandom(list, null)

    // If no previous, store newly chosen
    if (!prev && chosen) state.value.chosenTrackByZone[zone] = chosen
    return chosen
  }

  /**
   * Crossfade:
   * - start new zone track on the inactive audio element
   * - fade old one down, new one up
   */
  async function playZone(zone: Exclude<BgmZone, null>) {
    if (!import.meta.client) return
    if (!state.value.enabled) return

    ensureAudioElements()
    const a0 = audios.value[0]
    const a1 = audios.value[1]
    if (!a0 || !a1) return
    if (state.value.switching) return

    const track = pickTrackForZone(zone)
    if (!track) return

    state.value.switching = true

    const fromIndex = state.value.aIndex
    const toIndex: 0 | 1 = fromIndex === 0 ? 1 : 0
    const from = audios.value[fromIndex]!
    const to = audios.value[toIndex]!

    // ✅ Only swap src if needed (avoids hiccups)
    if (to.src !== new URL(track, window.location.href).href) {
      to.src = track
      to.currentTime = 0
    }

    to.loop = true
    to.volume = 0

    try {
      await to.play()
    } catch {
      state.value.switching = false
      return
    }

    const FADE_MS = 700
    fadeTo(from, 0, FADE_MS, true)
    fadeTo(to, 1, FADE_MS, false)

    state.value.aIndex = toIndex

    window.setTimeout(() => {
      state.value.switching = false
    }, FADE_MS + 50)
  }

  /**
   * OPTIONAL helper:
   * Call this if you ever want to re-randomize the current zone’s track
   * (e.g., “Next Track” button).
   */
  async function nextTrackInCurrentZone() {
    if (!state.value.currentZone) return
    const zone = state.value.currentZone
    const list = PLAYLISTS[zone]
    const prev = state.value.chosenTrackByZone[zone]
    const next = pickRandom(list, prev)
    state.value.chosenTrackByZone[zone] = next
    if (state.value.enabled && state.value.initialized && next) {
      await playZone(zone)
    }
  }

  return {
    enabled: computed(() => state.value.enabled),
    currentZone: computed(() => state.value.currentZone),
    switching: computed(() => state.value.switching),

    initOnce,
    setEnabled,
    setZoneFromRoute,

    // optional
    nextTrackInCurrentZone,
  }
}
