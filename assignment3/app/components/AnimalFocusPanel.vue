<script setup lang="ts">
import type { Animal, ItemKind } from '../types/game'

export interface AnimalFocusPanelProps {
  animal: Animal
  player: any
}

const props = withDefaults(defineProps<AnimalFocusPanelProps>(), {})

const emit = defineEmits<{
  close: []
  pet: [animalId: string]
  feed: [animalId: string, kind: ItemKind]
}>()

const { petAnimal, feedAnimal } = usePlayerState()

/* ------------------------------
 * Pet cooldown logic (3 seconds)
 * ------------------------------ */
const PET_COOLDOWN_MS = 3000
const nowMs = ref(Date.now())
let nowRaf: number | null = null

const petReadyAt = ref(0)
const canPet = computed(() => nowMs.value >= petReadyAt.value)
const petCooldownLeftMs = computed(() => Math.max(0, petReadyAt.value - nowMs.value))

function startNowTicker() {
  if (nowRaf !== null) cancelAnimationFrame(nowRaf)
  const loop = () => {
    nowMs.value = Date.now()
    if (nowMs.value < petReadyAt.value) nowRaf = requestAnimationFrame(loop)
    else nowRaf = null
  }
  loop()
}

function formatCooldown(ms: number) {
  return `${Math.ceil(ms / 1000)}s`
}

/* ------------------------------
 * VFX helpers
 * ------------------------------ */
type Heart = { id: string; x: number; y: number; driftX: number }
const hearts = ref<Heart[]>([])

type FloatText = {
  id: string
  x: number
  y: number
  driftX: number
  text: string
  kind: 'atk' | 'def'
}
const floatTexts = ref<FloatText[]>([])

function rand(min: number, max: number) {
  return min + Math.random() * (max - min)
}

function spawnHearts() {
  const batch: Heart[] = Array.from({ length: 8 }).map(() => ({
    id: Math.random().toString(16).slice(2),
    x: 32 + rand(-18, 18),
    y: 58 + rand(-12, 12),
    driftX: rand(-10, 10),
  }))

  hearts.value.push(...batch)
  setTimeout(() => {
    const ids = new Set(batch.map(h => h.id))
    hearts.value = hearts.value.filter(h => !ids.has(h.id))
  }, 900)
}

function spawnFloat(kind: 'atk' | 'def', amount: number) {
  const item: FloatText = {
    id: Math.random().toString(16).slice(2),
    x: 32 + rand(-16, 16),
    y: 48 + rand(-10, 8),
    driftX: rand(-10, 10),
    text: `${kind.toUpperCase()} +${amount}`,
    kind,
  }

  floatTexts.value.push(item)
  setTimeout(() => {
    floatTexts.value = floatTexts.value.filter(t => t.id !== item.id)
  }, 950)
}

/* ------------------------------
 * Affection milestone logic
 * - when crossing 5,10,15,... give boosts
 * ------------------------------ */
function applyAffectionMilestones(before: number, after: number) {
  const beforeMilestones = Math.floor(before / 5)
  const afterMilestones = Math.floor(after / 5)
  const gained = afterMilestones - beforeMilestones
  if (gained <= 0) return

  props.animal.stats.attack += gained
  props.animal.stats.defense += gained
  props.animal.stats.hpMax += gained * 2
  props.animal.hpCurrent = Math.min(
    props.animal.stats.hpMax,
    props.animal.hpCurrent + gained * 2
  )

  spawnFloat('atk', gained)
  spawnFloat('def', gained)
}

/* ------------------------------
 * Growth items dropdown
 * ------------------------------ */
type GrowthKind = 'treat' | 'armorSnack' | 'proteinBite'
const growthOptions = [
  { label: 'None', value: '' },
  { label: 'Treat (+2 AFF)', value: 'treat' },
  { label: 'Armor Snack (+DEF)', value: 'armorSnack' },
  { label: 'Protein Bite (+ATK)', value: 'proteinBite' },
] as const

const selectedGrowth = ref<string>('')

function invCount(kind: GrowthKind) {
  return Number(props.player?.inventory?.[kind] ?? 0)
}

const canUseSelected = computed(() => {
  const k = selectedGrowth.value as GrowthKind | ''
  if (!k) return false
  return invCount(k) > 0
})

/* ------------------------------
 * FOLLOW PANEL (camera/overlay)
 * Panel follows selected animal, clamps to pen edges.
 * ------------------------------ */
const panelRef = ref<HTMLElement | null>(null)
const xPx = ref(0)
const yPx = ref(0)

let followRaf: number | null = null

function clamp(n: number, min: number, max: number) {
  return Math.max(min, Math.min(max, n))
}

function findPenEl(): HTMLElement | null {
  // Stable page should have <div class="pen"> as container
  const root = panelRef.value?.parentElement
  return (root?.closest?.('.pen') as HTMLElement | null) ?? null
}

function findSelectedSpriteEl(): HTMLElement | null {
  // Selected sprite has .spriteBtn.selected on the button
  const pen = findPenEl()
  if (!pen) return null
  return pen.querySelector('.spriteBtn.selected') as HTMLElement | null
}

function setPanelToFollow() {
  const pen = findPenEl()
  const spriteEl = findSelectedSpriteEl()
  const panelEl = panelRef.value
  if (!pen || !spriteEl || !panelEl) return

  const penRect = pen.getBoundingClientRect()
  const spRect = spriteEl.getBoundingClientRect()

  // anchor: to the right of sprite, vertically aligned to its top
  const desiredLeft = spRect.right - penRect.left + 12
  const desiredTop = spRect.top - penRect.top - 6

  const panelW = panelEl.offsetWidth || 360
  const panelH = panelEl.offsetHeight || 220

  // clamp to pen bounds (freeze at edge)
  const minLeft = 12
  const minTop = 12
  const maxLeft = Math.max(minLeft, penRect.width - panelW - 12)
  const maxTop = Math.max(minTop, penRect.height - panelH - 12)

  xPx.value = clamp(desiredLeft, minLeft, maxLeft)
  yPx.value = clamp(desiredTop, minTop, maxTop)
}

function followLoop() {
  setPanelToFollow()
  followRaf = requestAnimationFrame(followLoop)
}

function startFollow() {
  if (followRaf !== null) cancelAnimationFrame(followRaf)
  followRaf = requestAnimationFrame(followLoop)
}

function stopFollow() {
  if (followRaf !== null) cancelAnimationFrame(followRaf)
  followRaf = null
}

/* ---- window resize listener (NO VueUse) ---- */
function onResize() {
  setPanelToFollow()
}
let resizeBound = false

onMounted(() => {
  startFollow()

  if (import.meta.client && !resizeBound) {
    window.addEventListener('resize', onResize, { passive: true })
    resizeBound = true
  }

  // initial position after render
  nextTick(() => setPanelToFollow())
})

watch(
  () => props.animal?.id,
  () => {
    nextTick(() => setPanelToFollow())
  }
)

watch(
  () => panelRef.value,
  () => nextTick(() => setPanelToFollow())
)

/* ------------------------------
 * Actions
 * IMPORTANT:
 * - do NOT manually change affection here (state should update in composable)
 * - compute before/after then apply milestone boosts locally
 * ------------------------------ */
function doPet() {
  if (!props.player) return
  if (!canPet.value) return

  const beforeAff = props.animal.stats.affection

  petReadyAt.value = Date.now() + PET_COOLDOWN_MS
  nowMs.value = Date.now()
  startNowTicker()

  // should modify animal in state
  petAnimal(props.animal.id)

  const afterAff = props.animal.stats.affection
  applyAffectionMilestones(beforeAff, afterAff)

  spawnHearts()
  emit('pet', props.animal.id)
}

function doUseGrowth() {
  if (!props.player) return
  const kind = selectedGrowth.value as GrowthKind | ''
  if (!kind) return
  if (invCount(kind) <= 0) return

  const beforeAff = props.animal.stats.affection

  feedAnimal(props.animal.id, kind as unknown as ItemKind)

  const afterAff = props.animal.stats.affection
  applyAffectionMilestones(beforeAff, afterAff)

  spawnHearts()
  emit('feed', props.animal.id, kind as unknown as ItemKind)
}

onBeforeUnmount(() => {
  if (nowRaf !== null) cancelAnimationFrame(nowRaf)
  stopFollow()

  if (import.meta.client && resizeBound) {
    window.removeEventListener('resize', onResize)
    resizeBound = false
  }
})
</script>

<template>
  <div ref="panelRef" class="panel" :style="{ left: xPx + 'px', top: yPx + 'px' }">
    <div class="panelTop">
      <div class="panelTitle">
        <span class="tag">{{ animal.kind.toUpperCase() }}</span>
        <span class="name">{{ animal.name }}</span>
      </div>
      <button class="xBtn" @click="$emit('close')" type="button" aria-label="Close">✕</button>
    </div>

    <div class="stats">
      <div class="stat"><b>HP</b> {{ animal.hpCurrent }} / {{ animal.stats.hpMax }}</div>
      <div class="stat"><b>ATK</b> {{ animal.stats.attack }}</div>
      <div class="stat"><b>DEF</b> {{ animal.stats.defense }}</div>
      <div class="stat"><b>AFF</b> {{ animal.stats.affection }}</div>
    </div>

    <div class="actions">
      <button class="btn primary" @click="doPet" :disabled="!canPet" type="button">
        <span v-if="canPet">Pet (+1 AFF)</span>
        <span v-else>Pet ({{ formatCooldown(petCooldownLeftMs) }})</span>
      </button>

      <div class="feedRow">
        <select v-model="selectedGrowth" class="select" aria-label="Select growth item">
          <option v-for="opt in growthOptions" :key="opt.value" :value="opt.value">
            {{ opt.label }}
          </option>
        </select>

        <button class="btn" type="button" @click="doUseGrowth" :disabled="!canUseSelected">
          Use
        </button>
      </div>

      <div class="counts muted small">
        <span>Treat: {{ invCount('treat') }}</span>
        <span>Armor: {{ invCount('armorSnack') }}</span>
        <span>Protein: {{ invCount('proteinBite') }}</span>
      </div>
    </div>

    <div class="vfxContainer" aria-hidden="true">
      <div
        v-for="h in hearts"
        :key="h.id"
        class="heart"
        :style="{ left: h.x + 'px', top: h.y + 'px', '--drift': h.driftX + 'px' }"
      >
        ♥
      </div>

      <div
        v-for="t in floatTexts"
        :key="t.id"
        class="floatText"
        :class="t.kind"
        :style="{ left: t.x + 'px', top: t.y + 'px', '--drift': t.driftX + 'px' }"
      >
        {{ t.text }}
      </div>
    </div>
  </div>
</template>

<style scoped>
.panel {
  position: absolute;
  z-index: 10;

  width: min(420px, calc(100% - 28px));
  border-radius: 18px;
  border: 1px solid rgba(255, 255, 255, 0.14);
  background: rgba(15, 18, 30, 0.86);
  box-shadow: 0 18px 60px rgba(0, 0, 0, 0.55);
  padding: 14px;
  backdrop-filter: blur(8px);
  transform: translate3d(0, 0, 0);
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
  border: 1px solid rgba(255, 255, 255, 0.14);
  padding: 4px 8px;
  border-radius: 999px;
}

.name {
  font-weight: 1000;
  font-size: 18px;
  color: rgba(255, 255, 255, 0.96);
}

.xBtn {
  border: 1px solid rgba(255, 255, 255, 0.14);
  background: rgba(255, 255, 255, 0.06);
  color: rgba(255, 255, 255, 0.92);
  border-radius: 12px;
  width: 38px;
  height: 38px;
  cursor: pointer;
}

.muted { color: rgba(255, 255, 255, 0.72); }
.small { font-size: 12px; }

.vfxContainer {
  position: absolute;
  inset: 0;
  pointer-events: none;
  overflow: hidden;
  border-radius: 18px;
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
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(0, 0, 0, 0.18);
  backdrop-filter: blur(6px);
  text-shadow: 0 10px 26px rgba(0, 0, 0, 0.40);
  animation: floatText 950ms ease-out forwards;
  user-select: none;
  transform: translate3d(0, 0, 0);
}

.floatText.atk { color: rgba(255, 70, 90, 0.98); }
.floatText.def { color: rgba(255, 214, 80, 0.98); }

@keyframes floatText {
  0% { transform: translate3d(0px, 6px, 0px) scale(0.95); opacity: 0; }
  15% { opacity: 1; }
  100% { transform: translate3d(var(--drift), -46px, 0px) scale(1.05); opacity: 0; }
}

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
  flex-direction: column;
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
  flex: 1;
  min-width: 180px;
  padding: 10px 12px;
  border-radius: 14px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(0, 0, 0, 0.22);
  color: rgba(255, 255, 255, 0.92);
}

.counts {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
}
</style>
