<script setup lang="ts">
import AnimalFocusPanel from '~/components/AnimalFocusPanel.vue'
import type { Animal, AnimalKind } from '../types/game'

const { player } = usePlayerState()

/**
 * ----------------------------
 * DB DTOs (Nitro server routes)
 * ----------------------------
 */
type PlayerAnimalDto = {
  id: number
  ownerPlayerId: string
  ownerName: string
  name: string
  kind: AnimalKind
  attack: number
  defense: number
  affection: number
  level: number
  hpMax: number
  hpCurrent: number
  createdAt: string
  templateId: number
}

/**
 * ----------------------------
 * Stable state sourced from DB
 * ----------------------------
 */
const dbAnimals = ref<Animal[]>([])
const dbLoading = ref(false)
const dbError = ref<string | null>(null)

/**
 * ----------------------------
 * Starter fallback (empty stable)
 * ----------------------------
 */
const needsStarter = ref(false)
const starterBusy = ref(false)
const starterError = ref<string | null>(null)

const stableIsEmpty = computed(() => !dbLoading.value && !dbError.value && dbAnimals.value.length === 0)

/** Redirect if no player */
watchEffect(() => {
  if (import.meta.client && !player.value) navigateTo('/')
})

function dtoToAnimal(a: PlayerAnimalDto): Animal {
  return {
    id: `pa-${a.id}`,
    ownerPlayerId: a.ownerPlayerId,
    ownerName: a.ownerName,
    name: a.name,
    kind: a.kind as any,
    stats: {
      attack: a.attack,
      defense: a.defense,
      affection: a.affection,
      level: a.level,
      hpMax: a.hpMax,
    },
    hpCurrent: a.hpCurrent,
  }
}

/**
 * Load player animals from DB (same-origin Nitro route):
 * GET /api/animals/player/{playerId}
 *
 * ✅ IMPORTANT:
 * On Azure App Service (Nuxt + Nitro), always call SAME-ORIGIN /api/... routes.
 * The server route talks to SQL; the browser never hits localhost and CORS is avoided.
 */
async function loadAnimalsFromDb() {
  if (!player.value) return
  dbLoading.value = true
  dbError.value = null

  try {
    const url = `/api/animals/player/${encodeURIComponent(player.value.id)}`
    const rows = await $fetch<PlayerAnimalDto[]>(url)
    const mapped = Array.isArray(rows) ? rows.map(dtoToAnimal) : []

    dbAnimals.value = mapped

    // Mirror into global state ONLY if we got a valid array back
    if (Array.isArray(rows)) {
      player.value.animals = mapped
    }

    // ✅ If stable is empty after a successful load, show starter prompt
    needsStarter.value = mapped.length === 0
  } catch (e: any) {
    console.error('Failed to load animals from DB:', e)
    dbError.value = e?.message ?? 'Failed to load animals from DB.'
    dbAnimals.value = player.value?.animals ?? []

    // Don’t force starter if DB failed; user can retry load
    needsStarter.value = false
  } finally {
    dbLoading.value = false
  }
}

onMounted(() => {
  loadAnimalsFromDb()
})

/**
 * Give the player a starter via a Nitro route you implement server-side.
 * Suggested: POST /api/animals/starter
 * Body: { playerId: string }
 */
async function chooseStarter() {
  if (!player.value) return
  starterBusy.value = true
  starterError.value = null

  try {
    await $fetch('/api/animals/starter', {
      method: 'POST',
      body: { playerId: player.value.id },
    })

    // Reload from DB so the stable populates
    await loadAnimalsFromDb()
    needsStarter.value = false
  } catch (e: any) {
    console.error('Failed to grant starter:', e)
    starterError.value = e?.message ?? 'Failed to grant starter.'
  } finally {
    starterBusy.value = false
  }
}

/**
 * ----------------------------
 * One-of-each kind (defensive)
 * ----------------------------
 */
const animals = computed(() => {
  const seen = new Set<string>()
  const out: Animal[] = []
  for (const a of dbAnimals.value) {
    if (seen.has(a.kind)) continue
    seen.add(a.kind)
    out.push(a)
  }
  return out
})

/** ---------- Selection + “camera zoom” ---------- */
const selectedId = ref<string | null>(null)
const selected = computed(() => animals.value.find(a => a.id === selectedId.value) ?? null)
const zoomed = ref(false)

const worldRef = ref<HTMLElement | null>(null)
const penRef = ref<HTMLElement | null>(null)

/** Auto-select first animal */
watchEffect(() => {
  const first = animals.value[0]
  if (!selectedId.value && first) selectedId.value = first.id
})

watchEffect(() => {
  if (!selectedId.value) return
  const ok = animals.value.some(a => a.id === selectedId.value)
  if (!ok) {
    selectedId.value = animals.value[0]?.id ?? null
    zoomed.value = false
  }
})

/**
 * ----------------------------
 * Sprite physics
 * ----------------------------
 */
type SpriteState = {
  id: string
  x: number
  y: number
  vx: number
  vy: number
  bobDelay: number
}

const SPRITE_SIZE = 64
const PADDING = 10
const sprites = ref<SpriteState[]>([])

function rand(min: number, max: number) {
  return min + Math.random() * (max - min)
}

function ensureSpritesForAnimals() {
  const list = animals.value
  const map = new Map(sprites.value.map(s => [s.id, s]))

  for (const a of list) {
    if (!map.has(a.id)) {
      map.set(a.id, {
        id: a.id,
        x: rand(30, 260),
        y: rand(30, 180),
        vx: rand(-0.45, 0.45),
        vy: rand(-0.35, 0.35),
        bobDelay: rand(0, 1.2),
      })
    }
  }

  const ids = new Set(list.map(a => a.id))
  sprites.value = Array.from(map.values()).filter(s => ids.has(s.id))
}

watchEffect(() => {
  if (!import.meta.client) return
  if (!player.value) return
  ensureSpritesForAnimals()
})

/** ---------- Movement loop ---------- */
let rafId: number | null = null

function tick() {
  const el = penRef.value
  if (!el) {
    rafId = requestAnimationFrame(tick)
    return
  }

  const rect = el.getBoundingClientRect()
  const maxX = Math.max(0, rect.width - SPRITE_SIZE - PADDING)
  const maxY = Math.max(0, rect.height - SPRITE_SIZE - PADDING)

  for (const s of sprites.value) {
    s.x += s.vx
    s.y += s.vy
    s.vx += rand(-0.02, 0.02)
    s.vy += rand(-0.02, 0.02)

    const maxSpeed = 0.9
    s.vx = Math.max(-maxSpeed, Math.min(maxSpeed, s.vx))
    s.vy = Math.max(-maxSpeed, Math.min(maxSpeed, s.vy))

    if (s.x < PADDING) {
      s.x = PADDING
      s.vx *= -1
    } else if (s.x > maxX) {
      s.x = maxX
      s.vx *= -1
    }

    if (s.y < PADDING) {
      s.y = PADDING
      s.vy *= -1
    } else if (s.y > maxY) {
      s.y = maxY
      s.vy *= -1
    }
  }

  rafId = requestAnimationFrame(tick)
}

onMounted(() => {
  if (!import.meta.client) return
  ensureSpritesForAnimals()
  rafId = requestAnimationFrame(tick)
})

onBeforeUnmount(() => {
  if (rafId !== null) cancelAnimationFrame(rafId)
})

const animalsById = computed(() => {
  const m = new Map<string, Animal>()
  for (const a of animals.value) m.set(a.id, a)
  return m
})

function isSelectedSprite(id: string) {
  return id === selectedId.value
}

function onPenAnimalClick(animalId: string) {
  selectedId.value = animalId
  zoomToSelected()
}

function resetCamera() {
  zoomed.value = false
}

function zoomToSelected() {
  if (!selectedId.value) return
  if (!penRef.value) return
  zoomed.value = true
}

/**
 * ----------------------------
 * CAMERA CLAMPING
 * ----------------------------
 */
const ZOOM_SCALE = 2.0
function clamp(n: number, min: number, max: number) {
  return Math.max(min, Math.min(max, n))
}

const worldTransform = computed(() => {
  const pen = penRef.value
  const selId = selectedId.value
  if (!pen || !selId || !zoomed.value) {
    return 'translate3d(0px,0px,0px) scale(1)'
  }

  const rect = pen.getBoundingClientRect()
  const s = sprites.value.find(x => x.id === selId)
  if (!s) return 'translate3d(0px,0px,0px) scale(1)'

  const scale = ZOOM_SCALE
  const targetX = s.x + SPRITE_SIZE / 2
  const targetY = s.y + SPRITE_SIZE / 2
  const viewCX = rect.width / 2
  const viewCY = rect.height / 2

  let tx = viewCX - targetX * scale
  let ty = viewCY - targetY * scale

  const minTx = rect.width - rect.width * scale
  const maxTx = 0
  const minTy = rect.height - rect.height * scale
  const maxTy = 0

  tx = clamp(tx, minTx, maxTx)
  ty = clamp(ty, minTy, maxTy)

  return `translate3d(${tx}px, ${ty}px, 0px) scale(${scale})`
})
</script>

<template>
  <section class="page">
    <div class="stage">
      <div class="pen" ref="penRef">
        <div class="world" ref="worldRef" :style="{ transform: worldTransform }">
          <div class="ground" />

          <AnimalSprite
            v-for="s in sprites"
            :key="s.id"
            :sprite="s"
            :animal="animalsById.get(s.id)!"
            :isSelected="isSelectedSprite(s.id)"
            @click="onPenAnimalClick"
          />
        </div>

        <div class="hud">
          <div class="hudLeft">
            <div class="title">Stable</div>
            <div class="subtitle muted">Click an animal to zoom and manage it.</div>
            <div v-if="dbLoading" class="subtitle muted">Loading from DB…</div>
            <div v-else-if="dbError" class="subtitle muted">DB load failed: {{ dbError }}</div>
            <div v-else-if="stableIsEmpty" class="subtitle muted">Your stable is empty.</div>
          </div>

          <div class="hudRight">
            <NuxtLink class="hudBtn" to="/shop">Shop</NuxtLink>
            <NuxtLink class="hudBtn" to="/gauntlet">Gauntlet</NuxtLink>
            <button v-if="zoomed" class="hudBtn" @click="resetCamera" type="button">
              Reset View
            </button>
          </div>
        </div>

        <!-- Starter prompt overlay -->
        <div v-if="needsStarter" class="starterOverlay" role="dialog" aria-modal="true">
          <div class="starterCard">
            <div class="starterTitle">Choose a starter</div>
            <div class="starterText muted">
              Your stable is empty. Let’s get you started with a companion.
            </div>

            <div v-if="starterError" class="starterText muted" style="margin-top: 8px;">
              {{ starterError }}
            </div>

            <div class="starterActions">
              <button class="hudBtn" type="button" :disabled="starterBusy" @click="chooseStarter">
                {{ starterBusy ? 'Granting…' : 'Get Starter' }}
              </button>

              <button class="hudBtn" type="button" :disabled="starterBusy" @click="loadAnimalsFromDb">
                Retry Load
              </button>
            </div>
          </div>
        </div>

        <AnimalFocusPanel v-if="zoomed && selected" :animal="selected" :player="player" @close="resetCamera" />

        <div v-if="!zoomed" class="bottomHint muted">
          No scroll. Background stays inside the centered pen.
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
.page { width: 100%; }
.stage { width: min(1200px, calc(100% - 32px)); margin: 0 auto; }

.pen {
  position: relative;
  width: 100%;
  height: calc(100dvh - 140px);
  min-height: 520px;
  max-height: 780px;
  overflow: hidden;
  border-radius: 18px;
  border: 1px solid rgba(255, 255, 255, 0.10);
  box-shadow: 0 18px 60px rgba(0, 0, 0, 0.45);
  background:
    radial-gradient(900px 420px at 30% 10%, rgba(124, 92, 255, 0.18), transparent 60%),
    radial-gradient(900px 420px at 80% 30%, rgba(53, 214, 197, 0.14), transparent 60%),
    url('/grass.jpg');
  background-size: auto, auto, cover;
  background-position: center, center, center;
  background-repeat: no-repeat, no-repeat, no-repeat;
}

.pen::before {
  content: '';
  position: absolute;
  inset: 0;
  background: rgba(0, 0, 0, 0.18);
  pointer-events: none;
  z-index: 0;
}

.world {
  position: absolute;
  inset: 0;
  transform-origin: 0 0;
  transition: transform 420ms cubic-bezier(.2, .9, .2, 1);
  z-index: 1;
}

.ground {
  position: absolute;
  left: 0;
  right: 0;
  bottom: 0;
  height: 90px;
  background: linear-gradient(to top, rgba(0, 0, 0, 0.35), transparent);
  border-top: 1px solid rgba(255, 255, 255, 0.06);
  pointer-events: none;
}

.hud {
  position: absolute;
  top: 14px;
  left: 14px;
  right: 14px;
  display: flex;
  justify-content: space-between;
  gap: 12px;
  z-index: 3;
  pointer-events: none;
}

.hudLeft { pointer-events: none; }

.title {
  font-weight: 1000;
  font-size: 22px;
  color: rgba(255, 255, 255, 0.96);
  letter-spacing: 0.3px;
}
.subtitle { margin-top: 4px; font-size: 12px; }

.hudRight {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
  justify-content: flex-end;
  pointer-events: auto;
}

.hudBtn {
  border-radius: 14px;
  padding: 10px 14px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(0, 0, 0, 0.25);
  color: rgba(255, 255, 255, 0.92);
  cursor: pointer;
  text-decoration: none;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  backdrop-filter: blur(6px);
}

.muted { color: rgba(255, 255, 255, 0.72); }

.bottomHint {
  position: absolute;
  left: 14px;
  bottom: 14px;
  z-index: 3;
  font-size: 12px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(0, 0, 0, 0.22);
  padding: 8px 10px;
  border-radius: 14px;
  backdrop-filter: blur(6px);
}

/* Starter overlay */
.starterOverlay {
  position: absolute;
  inset: 0;
  z-index: 4;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 18px;
  background: rgba(0, 0, 0, 0.55);
  backdrop-filter: blur(10px);
}

.starterCard {
  width: min(520px, 100%);
  border-radius: 18px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(0, 0, 0, 0.35);
  box-shadow: 0 18px 60px rgba(0, 0, 0, 0.55);
  padding: 18px;
}

.starterTitle {
  font-weight: 900;
  font-size: 18px;
  color: rgba(255, 255, 255, 0.96);
}

.starterText {
  margin-top: 8px;
  font-size: 12px;
}

.starterActions {
  margin-top: 14px;
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
}
</style>
