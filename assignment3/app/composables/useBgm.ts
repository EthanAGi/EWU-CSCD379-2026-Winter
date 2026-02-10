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

  // ✅ Master volume (0..1) controlled by slider
  volume: number

  // ✅ Per-audio crossfade mix (0..1); final audio.volume = mix[i] * volume
  mix: [number, number]

  // ✅ Per-zone soundtrack cycling state
  orderByZone: Record<'stable' | 'gauntlet', string[]>
  indexByZone: Record<'stable' | 'gauntlet', number>
}

function clamp(n: number, min: number, max: number) {
  return Math.max(min, Math.min(max, n))
}

/**
 * ✅ Playlists:
 * Paths are from /public, so they start with /audio/...
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

function shuffle<T>(arr: readonly T[]): T[] {
  const a = [...arr]
  for (let i = a.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1))
    const tmp = a[i]!     // indices are guaranteed in-range here
    a[i] = a[j]!
    a[j] = tmp
  }
  return a
}


function sameHref(a: HTMLAudioElement, track: string) {
  // audio.src becomes absolute; compare against absolute URL
  const abs = new URL(track, window.location.href).href
  return a.src === abs
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

    volume: 0.6,
    mix: [0, 0],

    // cycling state
    orderByZone: { stable: [], gauntlet: [] },
    indexByZone: { stable: 0, gauntlet: 0 },
  }))

  const audios = useState<(HTMLAudioElement | null)[]>('bgm_audios_v1', () => [null, null])
  const listenersAttached = useState<boolean>('bgm_listeners_v1', () => false)

  function applyVolumes() {
    if (!import.meta.client) return
    const a0 = audios.value[0]
    const a1 = audios.value[1]
    if (!a0 || !a1) return

    const master = clamp(state.value.volume, 0, 1)
    a0.volume = clamp(state.value.mix[0], 0, 1) * master
    a1.volume = clamp(state.value.mix[1], 0, 1) * master
  }

  function buildCycle(zone: Exclude<BgmZone, null>, preferFirst?: string | null) {
    const list = PLAYLISTS[zone] ?? []
    if (list.length === 0) {
      state.value.orderByZone[zone] = []
      state.value.indexByZone[zone] = 0
      state.value.chosenTrackByZone[zone] = null
      return
    }

    // New cycle = shuffled playlist (so you still "cycle all tracks" but not always same order)
    let order = shuffle(list)

    // Try to avoid immediate repeat across cycles:
    // if preferFirst is the first track after shuffle, rotate once.
    if (preferFirst && order.length > 1 && order[0] === preferFirst) {
      order = [...order.slice(1), order[0]]
    }

    // If caller wants a particular track to be current, rotate so it becomes first.
    if (preferFirst) {
      const idx = order.indexOf(preferFirst)
      if (idx >= 0) {
        order = [...order.slice(idx), ...order.slice(0, idx)]
      }
    }

    state.value.orderByZone[zone] = order
    state.value.indexByZone[zone] = 0
    state.value.chosenTrackByZone[zone] = order[0] ?? null
  }

  function getCurrentTrack(zone: Exclude<BgmZone, null>): string | null {
    const order = state.value.orderByZone[zone]
    if (!order || order.length === 0) {
      // If we had a remembered track, start cycle with it; otherwise random cycle
      buildCycle(zone, state.value.chosenTrackByZone[zone])
    }
    const order2 = state.value.orderByZone[zone]
    if (!order2 || order2.length === 0) return null
    const i = clamp(state.value.indexByZone[zone], 0, order2.length - 1)
    state.value.indexByZone[zone] = i
    const t = order2[i] ?? null
    state.value.chosenTrackByZone[zone] = t
    return t
  }

  function advanceToNextTrack(zone: Exclude<BgmZone, null>): string | null {
    const order = state.value.orderByZone[zone]
    if (!order || order.length === 0) {
      buildCycle(zone, null)
    }

    const order2 = state.value.orderByZone[zone]
    if (!order2 || order2.length === 0) return null

    const prev = state.value.chosenTrackByZone[zone] ?? null
    let nextIndex = state.value.indexByZone[zone] + 1

    // End of cycle -> start a NEW cycle (loop the soundtrack)
    if (nextIndex >= order2.length) {
      buildCycle(zone, prev) // new shuffled cycle, avoid immediate repeat
      const t = state.value.orderByZone[zone][0] ?? null
      state.value.indexByZone[zone] = 0
      state.value.chosenTrackByZone[zone] = t
      return t
    }

    state.value.indexByZone[zone] = nextIndex
    const t = order2[nextIndex] ?? null
    state.value.chosenTrackByZone[zone] = t
    return t
  }

  function ensureAudioElements() {
    if (!import.meta.client) return
    if (audios.value[0] && audios.value[1]) return

    const a0 = new Audio()
    const a1 = new Audio()

    for (const a of [a0, a1]) {
      a.loop = false // ✅ IMPORTANT: we handle advancing ourselves
      a.preload = 'auto'
      a.volume = 0
    }

    audios.value = [a0, a1]

    // Attach ended listeners once
    if (!listenersAttached.value) {
      listenersAttached.value = true

      const onEnded = async (endedIndex: 0 | 1) => {
        if (!import.meta.client) return
        if (!state.value.enabled) return
        if (state.value.switching) return
        const zone = state.value.currentZone
        if (!zone) return

        // Only advance if the ended audio is the currently active one.
        if (state.value.aIndex !== endedIndex) return

        const a = audios.value[endedIndex]
        if (!a) return

        const next = advanceToNextTrack(zone)
        if (!next) return

        // Swap src to next track and play
        if (!sameHref(a, next)) {
          a.src = next
          a.currentTime = 0
        }

        // Keep this element as "active" (mix 1), other stays 0
        state.value.mix[endedIndex] = 1
        state.value.mix[endedIndex === 0 ? 1 : 0] = 0
        applyVolumes()

        try {
          await a.play()
        } catch {
          // If browser blocks it, user gesture will unlock again via initOnce()
        }
      }

      a0.addEventListener('ended', () => void onEnded(0))
      a1.addEventListener('ended', () => void onEnded(1))
    }

    applyVolumes()
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

  function fadeMixTo(index: 0 | 1, targetMix: number, ms: number, pauseAtEnd = false) {
    targetMix = clamp(targetMix, 0, 1)
    const a = audios.value[index]
    if (!a) return

    const startMix = state.value.mix[index]
    const start = performance.now()
    const dur = Math.max(1, ms)

    const tick = () => {
      const t = (performance.now() - start) / dur
      const k = clamp(t, 0, 1)

      const mixNow = startMix + (targetMix - startMix) * k
      state.value.mix[index] = mixNow
      applyVolumes()

      if (k < 1) {
        requestAnimationFrame(tick)
      } else {
        state.value.mix[index] = targetMix
        applyVolumes()

        if (pauseAtEnd && targetMix === 0) {
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

  function setEnabled(on: boolean) {
    state.value.enabled = on
    if (!import.meta.client) return

    ensureAudioElements()
    const a0 = audios.value[0]
    const a1 = audios.value[1]
    if (!a0 || !a1) return

    if (!on) {
      fadeMixTo(0, 0, 250, true)
      fadeMixTo(1, 0, 250, true)
      return
    }

    // Turning on: resume current zone (if unlocked)
    if (state.value.currentZone && state.value.initialized) {
      playZone(state.value.currentZone)
    }
  }

  // ✅ Called by navbar slider (0..1)
  function setVolume(v: number) {
    state.value.volume = clamp(v, 0, 1)
    applyVolumes()
  }

  function getZoneForPath(path: string): BgmZone {
    if (path.startsWith('/stable')) return 'stable'
    if (path.startsWith('/gauntlet')) return 'gauntlet'
    return null
  }

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

  /**
   * Crossfade into the zone's current track.
   * Tracks will continue automatically when each ends (see ended listeners).
   */
  async function playZone(zone: Exclude<BgmZone, null>) {
    if (!import.meta.client) return
    if (!state.value.enabled) return

    ensureAudioElements()
    const a0 = audios.value[0]
    const a1 = audios.value[1]
    if (!a0 || !a1) return
    if (state.value.switching) return

    // Pick "current" track for this zone from the cycle
    const track = getCurrentTrack(zone)
    if (!track) return

    state.value.switching = true

    const fromIndex = state.value.aIndex
    const toIndex: 0 | 1 = fromIndex === 0 ? 1 : 0
    const from = audios.value[fromIndex]!
    const to = audios.value[toIndex]!

    if (!sameHref(to, track)) {
      to.src = track
      to.currentTime = 0
    }

    to.loop = false

    // Start "to" silent at mix 0
    state.value.mix[toIndex] = 0
    applyVolumes()

    try {
      await to.play()
    } catch {
      state.value.switching = false
      return
    }

    const FADE_MS = 700
    fadeMixTo(fromIndex, 0, FADE_MS, true)
    fadeMixTo(toIndex, 1, FADE_MS, false)

    state.value.aIndex = toIndex

    window.setTimeout(() => {
      state.value.switching = false
    }, FADE_MS + 50)
  }

  /**
   * Optional helper: skip to next track in current zone immediately.
   */
  async function nextTrackInCurrentZone() {
    if (!state.value.currentZone) return
    const zone = state.value.currentZone
    const next = advanceToNextTrack(zone)
    if (!next) return
    if (!state.value.enabled || !state.value.initialized || !import.meta.client) return

    ensureAudioElements()
    const idx = state.value.aIndex
    const a = audios.value[idx]
    if (!a) return

    if (!sameHref(a, next)) {
      a.src = next
      a.currentTime = 0
    }

    state.value.mix[idx] = 1
    state.value.mix[idx === 0 ? 1 : 0] = 0
    applyVolumes()

    try {
      await a.play()
    } catch {
      // ignore
    }
  }

  return {
    enabled: computed(() => state.value.enabled),
    currentZone: computed(() => state.value.currentZone),
    switching: computed(() => state.value.switching),

    volume: computed(() => state.value.volume),
    setVolume,

    initOnce,
    setEnabled,
    setZoneFromRoute,

    nextTrackInCurrentZone,
  }
}
