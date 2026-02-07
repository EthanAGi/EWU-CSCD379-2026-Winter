<script setup lang="ts">
import AnimalFocusPanel from '~/components/AnimalFocusPanel.vue'

const { player } = usePlayerState()

/**
 * NOTE on "one-of-each type":
 * - Real enforcement should happen in usePlayerState.buyAnimal/chooseStarter.
 * - This page assumes player.animals is already unique by kind,
 *   but we still defensively de-dupe for display so it never breaks.
 */
const allAnimals = computed(() => player.value?.animals ?? [])
const animals = computed(() => {
  const seen = new Set<string>()
  const out: typeof allAnimals.value = []
  for (const a of allAnimals.value) {
    if (seen.has(a.kind)) continue
    seen.add(a.kind)
    out.push(a)
  }
  return out
})

// Redirect if no player
watchEffect(() => {
  if (import.meta.client && !player.value) navigateTo('/')
})

/** ---------- Selection + “camera zoom” ---------- */
const selectedId = ref<string | null>(null)
const selected = computed(() => animals.value.find(a => a.id === selectedId.value) ?? null)

// Is the “camera zoomed in”?
const zoomed = ref(false)

// We transform this “world” layer (camera)
const worldRef = ref<HTMLElement | null>(null)
const penRef = ref<HTMLElement | null>(null)

// Sprite physics state
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

function spriteUrl(kind: string) {
  return `/sprites/${kind}.png`
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

// Keep sprites synced
watchEffect(() => {
  if (!import.meta.client) return
  if (!player.value) return
  ensureSpritesForAnimals()
})

// Auto-select first animal (so zoom works instantly)
watchEffect(() => {
  const first = animals.value[0]
  if (!selectedId.value && first) selectedId.value = first.id
})

watchEffect(() => {
  // If the selected animal disappears, clear selection and zoom
  if (!selectedId.value) return
  const ok = animals.value.some(a => a.id === selectedId.value)
  if (!ok) {
    selectedId.value = animals.value[0]?.id ?? null
    zoomed.value = false
  }
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

    // Soft drift
    s.vx += rand(-0.02, 0.02)
    s.vy += rand(-0.02, 0.02)

    // Clamp
    const maxSpeed = 0.9
    s.vx = Math.max(-maxSpeed, Math.min(maxSpeed, s.vx))
    s.vy = Math.max(-maxSpeed, Math.min(maxSpeed, s.vy))

    // Bounce
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
  const m = new Map<string, any>()
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
  const pen = penRef.value
  if (!pen) return
  zoomed.value = true
}

/**
 * ---------- CAMERA CLAMPING ----------
 * We clamp the translate so the world never exposes "outside" the pen background.
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

  // sprite center in world coords
  const targetX = s.x + SPRITE_SIZE / 2
  const targetY = s.y + SPRITE_SIZE / 2

  // viewport center
  const viewCX = rect.width / 2
  const viewCY = rect.height / 2

  // ideal translate
  let tx = viewCX - targetX * scale
  let ty = viewCY - targetY * scale

  // clamp translate so we never move beyond edges
  const minTx = rect.width - rect.width * scale // negative
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
        <!-- Camera world -->
        <div class="world" ref="worldRef" :style="{ transform: worldTransform }">
          <div class="ground" />

          <!-- Sprites -->
          <AnimalSprite
            v-for="s in sprites"
            :key="s.id"
            :sprite="s"
            :animal="animalsById.get(s.id)!"
            :isSelected="isSelectedSprite(s.id)"
            @click="onPenAnimalClick"
          />
        </div>

        <!-- Top HUD -->
        <div class="hud">
          <div class="hudLeft">
            <div class="title">Stable</div>
            <div class="subtitle muted">Click an animal to zoom and manage it.</div>
          </div>

          <div class="hudRight">
            <NuxtLink class="hudBtn" to="/shop">Shop</NuxtLink>
            <NuxtLink class="hudBtn" to="/gauntlet">Gauntlet</NuxtLink>
            <button v-if="zoomed" class="hudBtn" @click="resetCamera" type="button">Reset View</button>
          </div>
        </div>

        <AnimalFocusPanel
          v-if="zoomed && selected"
          :animal="selected"
          :player="player"
          @close="resetCamera"
          @pet="() => {}"
          @feed="() => {}"
        />








        <div v-if="!zoomed" class="bottomHint muted">
          No scroll. Background stays inside the centered pen.
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
/*
  KEY FIX for your screenshot:
  - DO NOT use 100vw here (it ignores your app container and causes horizontal misalignment)
  - Instead, keep width:100% and center a max-width "stage" that matches the rest of your site.
*/

.page {
  width: 100%;
}

/* Centers the stable area to match your app’s centered layout */
.stage {
  width: min(1200px, calc(100% - 32px));
  margin: 0 auto;
}

/* The pen is the bounded "background box" */
.pen {
  position: relative;
  width: 100%;
  /* Fill most of the visible viewport WITHOUT causing scroll.
     Adjust the 140px if your app header is taller/shorter. */
  height: calc(100dvh - 140px);
  min-height: 520px;
  max-height: 780px;

  overflow: hidden; /* ✅ everything stays inside border */
  border-radius: 18px;
  border: 1px solid rgba(255, 255, 255, 0.10);
  box-shadow: 0 18px 60px rgba(0, 0, 0, 0.45);

  background:
    radial-gradient(900px 420px at 30% 10%, rgba(124, 92, 255, 0.18), transparent 60%),
    radial-gradient(900px 420px at 80% 30%, rgba(53, 214, 197, 0.14), transparent 60%),
    url('/grass.jpg');
  background-size: auto, auto, cover;
  background-position: center, center, center; /* ✅ centered background */
  background-repeat: no-repeat, no-repeat, no-repeat;
}

/* subtle dark overlay */
.pen::before {
  content: '';
  position: absolute;
  inset: 0;
  background: rgba(0, 0, 0, 0.18);
  pointer-events: none;
  z-index: 0;
}

/* Camera world layer */
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

/* VFX layers */
.heartsLayer,
.floatTextLayer {
  position: absolute;
  inset: 0;
  pointer-events: none;
}

.heart {
  position: absolute;
  font-size: 18px;
  line-height: 1;
  color: rgba(255, 120, 170, 0.95);
  text-shadow: 0 6px 18px rgba(0, 0, 0, 0.35);
  animation: floatHeart 900ms ease-out forwards;
  user-select: none;
}

@keyframes floatHeart {
  0% { transform: translate3d(0px, 0px, 0px) scale(0.9); opacity: 0; }
  12% { opacity: 1; }
  100% { transform: translate3d(var(--drift), -44px, 0px) scale(1.2); opacity: 0; }
}

.floatText {
  position: absolute;
  font-size: 14px;
  font-weight: 1000;
  letter-spacing: 0.2px;
  padding: 4px 8px;
  border-radius: 999px;
  border: 1px solid rgba(255,255,255,0.12);
  background: rgba(0,0,0,0.18);
  backdrop-filter: blur(6px);
  text-shadow: 0 10px 26px rgba(0,0,0,0.40);
  animation: floatText 950ms ease-out forwards;
  user-select: none;
  transform: translate3d(0,0,0);
}

.floatText.atk { color: rgba(255, 70, 90, 0.98); }
.floatText.def { color: rgba(255, 214, 80, 0.98); }

@keyframes floatText {
  0% { transform: translate3d(0px, 6px, 0px) scale(0.95); opacity: 0; }
  15% { opacity: 1; }
  100% { transform: translate3d(var(--drift), -46px, 0px) scale(1.05); opacity: 0; }
}

/* Sprites */
.spriteBtn {
  position: absolute;
  width: 64px;
  height: 64px;
  padding: 0;
  border: none;
  background: transparent;
  cursor: pointer;
}

.spriteBtn.selected::after {
  content: '';
  position: absolute;
  inset: -6px;
  border-radius: 18px;
  border: 2px solid rgba(53, 214, 197, 0.85);
  box-shadow: 0 0 0 6px rgba(53, 214, 197, 0.14);
}

.spriteFlip {
  width: 64px;
  height: 64px;
  display: grid;
  place-items: center;
}

.spriteFlip.facingLeft { transform: scaleX(-1); }

.spriteImg {
  width: 64px;
  height: 64px;
  object-fit: contain;
  user-select: none;
  -webkit-user-drag: none;
  animation: bob 1.8s ease-in-out infinite;
}

@keyframes bob {
  0%, 100% { transform: translateY(0px); }
  50% { transform: translateY(-6px); }
}

/* HUD */
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

/* Panel */
.panel {
  position: absolute;
  right: 14px;
  bottom: 14px;
  width: min(420px, calc(100% - 28px)); /* ✅ relative to pen, not viewport */
  border-radius: 18px;
  border: 1px solid rgba(255, 255, 255, 0.14);
  background: rgba(15, 18, 30, 0.86);
  box-shadow: 0 18px 60px rgba(0, 0, 0, 0.55);
  padding: 14px;
  z-index: 4;
  backdrop-filter: blur(8px);
}

.panelTop {
  display: flex;
  justify-content: space-between;
  gap: 10px;
  align-items: center;
}

.panelTitle {
  display: flex;
  gap: 10px;
  align-items: baseline;
  flex-wrap: wrap;
}

.tag {
  font-size: 11px;
  opacity: 0.85;
  border: 1px solid rgba(255,255,255,0.14);
  padding: 4px 8px;
  border-radius: 999px;
}

.name {
  font-weight: 1000;
  font-size: 18px;
  color: rgba(255,255,255,0.96);
}

.xBtn {
  border: 1px solid rgba(255,255,255,0.14);
  background: rgba(255,255,255,0.06);
  color: rgba(255,255,255,0.92);
  border-radius: 12px;
  width: 38px;
  height: 38px;
  cursor: pointer;
}

.muted { color: rgba(255, 255, 255, 0.72); }
.small { font-size: 12px; }

.stats {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
  margin-top: 10px;
}

.stat {
  padding: 6px 10px;
  border-radius: 999px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(255, 255, 255, 0.05);
  font-size: 12px;
}

.actions {
  margin-top: 12px;
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
  align-items: center;
}

.btn {
  border-radius: 14px;
  padding: 10px 14px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(255, 255, 255, 0.08);
  color: rgba(255, 255, 255, 0.92);
  cursor: pointer;
}

.btn.primary {
  border: none;
  background: linear-gradient(90deg, #7c5cff, #35d6c5);
  color: #0b1020;
  font-weight: 900;
}

.btn:disabled { opacity: 0.55; cursor: not-allowed; }

.feedRow {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
  align-items: center;
}

.select {
  padding: 10px 12px;
  border-radius: 14px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(0, 0, 0, 0.22);
  color: rgba(255, 255, 255, 0.92);
}

.hint { margin-top: 12px; font-size: 12px; }

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
</style>
