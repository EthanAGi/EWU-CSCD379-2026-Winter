<script setup lang="ts">
import type { ItemKind } from '../types/game'

const { player, petAnimal, feedAnimal } = usePlayerState()

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

/** ---------- Pet mechanic + VFX ---------- */
const PET_COOLDOWN_MS = 2500

// Reactive time ticker for cooldown
const nowMs = ref(Date.now())
let nowRaf: number | null = null

function startNowTicker() {
  if (nowRaf !== null) cancelAnimationFrame(nowRaf)
  const loop = () => {
    nowMs.value = Date.now()
    if (nowMs.value < petReadyAt.value) nowRaf = requestAnimationFrame(loop)
    else nowRaf = null
  }
  loop()
}

onBeforeUnmount(() => {
  if (nowRaf !== null) cancelAnimationFrame(nowRaf)
})

const petReadyAt = ref(0)
const canPet = computed(() => nowMs.value >= petReadyAt.value)
const petCooldownLeftMs = computed(() => Math.max(0, petReadyAt.value - nowMs.value))

function formatCooldown(ms: number) {
  const s = Math.ceil(ms / 1000)
  return `${s}s`
}

type Heart = { id: string; x: number; y: number; driftX: number }
const hearts = ref<Heart[]>([])

function spawnHeartsForAnimal(animalId: string) {
  const s = sprites.value.find(sp => sp.id === animalId)
  if (!s) return
  const baseX = s.x + SPRITE_SIZE / 2
  const baseY = s.y - 6

  const count = 8
  const batch: Heart[] = []
  for (let i = 0; i < count; i++) {
    batch.push({
      id: Math.random().toString(16).slice(2),
      x: baseX + rand(-18, 18),
      y: baseY + rand(-12, 12),
      driftX: rand(-10, 10),
    })
  }

  hearts.value.push(...batch)

  window.setTimeout(() => {
    const ids = new Set(batch.map(h => h.id))
    hearts.value = hearts.value.filter(h => !ids.has(h.id))
  }, 900)
}

type FloatText = {
  id: string
  x: number
  y: number
  driftX: number
  text: string
  kind: 'atk' | 'def'
}
const floatTexts = ref<FloatText[]>([])

function spawnFloatText(animalId: string, kind: 'atk' | 'def', amount: number) {
  const s = sprites.value.find(sp => sp.id === animalId)
  if (!s) return

  const baseX = s.x + SPRITE_SIZE / 2
  const baseY = s.y - 16

  const item: FloatText = {
    id: Math.random().toString(16).slice(2),
    x: baseX + rand(-16, 16),
    y: baseY + rand(-10, 8),
    driftX: rand(-10, 10),
    text: `${kind === 'atk' ? 'ATK' : 'DEF'} +${amount}`,
    kind,
  }

  floatTexts.value.push(item)
  window.setTimeout(() => {
    floatTexts.value = floatTexts.value.filter(t => t.id !== item.id)
  }, 950)
}

/** Milestone boosts (every +5 affection) */
function crossedMultipleOf5(before: number, after: number) {
  return Math.floor(after / 5) > Math.floor(before / 5)
}

function applyMilestoneBoostIfNeeded(animalId: string, beforeAff: number) {
  const a = player.value?.animals.find(x => x.id === animalId)
  if (!a) return
  const afterAff = a.stats.affection

  if (!crossedMultipleOf5(beforeAff, afterAff)) return

  const roll = Math.random() < 0.5 ? 'atk' : 'def'
  if (roll === 'atk') {
    a.stats.attack += 2
    spawnFloatText(animalId, 'atk', 2)
  } else {
    a.stats.defense += 2
    spawnFloatText(animalId, 'def', 2)
  }
}

function doPet() {
  if (!selected.value) return
  if (!canPet.value) return
  if (!player.value) return

  const animalId = selected.value.id

  // Start cooldown FIRST (anti spam)
  petReadyAt.value = Date.now() + PET_COOLDOWN_MS
  nowMs.value = Date.now()
  startNowTicker()

  const beforeAff = selected.value.stats.affection

  petAnimal(animalId)
  spawnHeartsForAnimal(animalId)
  applyMilestoneBoostIfNeeded(animalId, beforeAff)
}

/** ---------- Feeding ---------- */
const feedChoice = ref<ItemKind | 'basic'>('basic')

function doFeed() {
  if (!selected.value) return
  const choice = feedChoice.value
  feedAnimal(selected.value.id, choice === 'basic' ? undefined : choice)
}
</script>

<template>
  <section class="page">
    <div class="stage">
      <div class="pen" ref="penRef">
        <!-- Camera world -->
        <div class="world" ref="worldRef" :style="{ transform: worldTransform }">
          <div class="ground" />

          <!-- Floating hearts -->
          <div class="heartsLayer" aria-hidden="true">
            <div
              v-for="h in hearts"
              :key="h.id"
              class="heart"
              :style="{ left: h.x + 'px', top: h.y + 'px', '--drift': h.driftX + 'px' }"
            >
              ♥
            </div>
          </div>

          <!-- Floating stat text -->
          <div class="floatTextLayer" aria-hidden="true">
            <div
              v-for="t in floatTexts"
              :key="t.id"
              class="floatText"
              :class="{ atk: t.kind === 'atk', def: t.kind === 'def' }"
              :style="{ left: t.x + 'px', top: t.y + 'px', '--drift': t.driftX + 'px' }"
            >
              {{ t.text }}
            </div>
          </div>

          <!-- Sprites -->
          <button
            v-for="s in sprites"
            :key="s.id"
            class="spriteBtn"
            :class="{ selected: isSelectedSprite(s.id) }"
            :style="{ left: s.x + 'px', top: s.y + 'px' }"
            @click="onPenAnimalClick(s.id)"
            type="button"
          >
            <div class="spriteFlip" :class="{ facingLeft: s.vx < 0 }">
              <img
                class="spriteImg"
                :style="{ animationDelay: s.bobDelay + 's' }"
                :src="spriteUrl(animalsById.get(s.id)?.kind ?? 'dog')"
                :alt="animalsById.get(s.id)?.name ?? 'animal'"
                draggable="false"
              />
            </div>
          </button>
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

        <!-- Stats Panel -->
        <div v-if="zoomed && selected" class="panel">
          <div class="panelTop">
            <div class="panelTitle">
              <span class="tag">{{ selected.kind.toUpperCase() }}</span>
              <span class="name">{{ selected.name }}</span>
            </div>
            <button class="xBtn" @click="resetCamera" type="button" aria-label="Close">✕</button>
          </div>

          <div class="muted small">{{ selected.ownerName }}’s {{ selected.kind }}</div>

          <div class="stats">
            <div class="stat"><b>HP</b> {{ selected.hpCurrent }} / {{ selected.stats.hpMax }}</div>
            <div class="stat"><b>ATK</b> {{ selected.stats.attack }}</div>
            <div class="stat"><b>DEF</b> {{ selected.stats.defense }}</div>
            <div class="stat"><b>Affection</b> {{ selected.stats.affection }} / 50</div>
          </div>

          <div class="actions">
            <button class="btn primary" @click="doPet" type="button" :disabled="!canPet">
              <span v-if="canPet">Pet</span>
              <span v-else>Pet ({{ formatCooldown(petCooldownLeftMs) }})</span>
            </button>

            <div class="feedRow">
              <select v-model="feedChoice" class="select">
                <option value="basic">Basic Feed (free)</option>
                <option value="treat">Treat (inv: {{ player?.inventory?.treat ?? 0 }})</option>
                <option value="armorSnack">Armor Snack (inv: {{ player?.inventory?.armorSnack ?? 0 }})</option>
                <option value="proteinBite">Protein Bite (inv: {{ player?.inventory?.proteinBite ?? 0 }})</option>
              </select>

              <button class="btn" @click="doFeed" type="button">Feed</button>
            </div>
          </div>

          <div class="hint muted">
            ✅ Centered + aligned with site layout. Camera clamped to the pen border.
          </div>
        </div>

        <!-- Bottom hint -->
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
