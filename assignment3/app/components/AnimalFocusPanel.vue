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
  feed: [animalId: string, kind: ItemKind | undefined]
}>()

const { petAnimal, feedAnimal } = usePlayerState()

// Pet mechanics
const PET_COOLDOWN_MS = 2500
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

const petReadyAt = ref(0)
const canPet = computed(() => nowMs.value >= petReadyAt.value)
const petCooldownLeftMs = computed(() => Math.max(0, petReadyAt.value - nowMs.value))

function formatCooldown(ms: number) {
  const s = Math.ceil(ms / 1000)
  return `${s}s`
}

// VFX
type Heart = { id: string; x: number; y: number; driftX: number }
const hearts = ref<Heart[]>([])

function rand(min: number, max: number) {
  return min + Math.random() * (max - min)
}

function spawnHeartsForAnimal() {
  const SPRITE_SIZE = 64
  // Assuming the animal is rendered somewhere, we spawn from center
  const baseX = 32 // Center of sprite
  const baseY = 58 // Above sprite

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

function spawnFloatText(kind: 'atk' | 'def', amount: number) {
  const baseX = 32
  const baseY = 48

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

function crossedMultipleOf5(before: number, after: number) {
  return Math.floor(after / 5) > Math.floor(before / 5)
}

function applyMilestoneBoostIfNeeded(beforeAff: number) {
  const afterAff = props.animal.stats.affection

  if (!crossedMultipleOf5(beforeAff, afterAff)) return

  const roll = Math.random() < 0.5 ? 'atk' : 'def'
  if (roll === 'atk') {
    props.animal.stats.attack += 2
    spawnFloatText('atk', 2)
  } else {
    props.animal.stats.defense += 2
    spawnFloatText('def', 2)
  }
}

function doPet() {
  if (!canPet.value || !props.player) return

  const beforeAff = props.animal.stats.affection

  // Start cooldown FIRST (anti spam)
  petReadyAt.value = Date.now() + PET_COOLDOWN_MS
  nowMs.value = Date.now()
  startNowTicker()

  petAnimal(props.animal.id)
  spawnHeartsForAnimal()
  applyMilestoneBoostIfNeeded(beforeAff)

  emit('pet', props.animal.id)
}

const feedChoice = ref<ItemKind | 'basic'>('basic')

function doFeed() {
  const choice = feedChoice.value
  feedAnimal(props.animal.id, choice === 'basic' ? undefined : choice)
  emit('feed', props.animal.id, choice === 'basic' ? undefined : (choice as ItemKind))
}

onBeforeUnmount(() => {
  if (nowRaf !== null) cancelAnimationFrame(nowRaf)
})
</script>

<template>
  <div class="panel">
    <div class="panelTop">
      <div class="panelTitle">
        <span class="tag">{{ animal.kind.toUpperCase() }}</span>
        <span class="name">{{ animal.name }}</span>
      </div>
      <button class="xBtn" @click="$emit('close')" type="button" aria-label="Close">✕</button>
    </div>

    <div class="muted small">{{ animal.ownerName }}'s {{ animal.kind }}</div>

    <!-- VFX layers (hearts and float text) -->
    <div class="vfxContainer">
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
    </div>

    <div class="stats">
      <div class="stat"><b>HP</b> {{ animal.hpCurrent }} / {{ animal.stats.hpMax }}</div>
      <div class="stat"><b>ATK</b> {{ animal.stats.attack }}</div>
      <div class="stat"><b>DEF</b> {{ animal.stats.defense }}</div>
      <div class="stat"><b>Affection</b> {{ animal.stats.affection }} / 50</div>
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
  </div>
</template>

<style scoped>
.panel {
  border-radius: 18px;
  border: 1px solid rgba(255, 255, 255, 0.14);
  background: rgba(15, 18, 30, 0.86);
  box-shadow: 0 18px 60px rgba(0, 0, 0, 0.55);
  padding: 14px;
  backdrop-filter: blur(8px);
  position: relative;
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

/* VFX */
.vfxContainer {
  position: absolute;
  inset: 0;
  pointer-events: none;
  overflow: hidden;
  border-radius: 18px;
}

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

/* Stats */
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
</style>
