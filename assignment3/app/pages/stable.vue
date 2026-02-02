<script setup lang="ts">
import type { ItemKind } from '../types/game'

const { player, petAnimal, feedAnimal } = usePlayerState()

// Selected animal id
const selectedId = ref<string | null>(null)

// Selected animal object (or null)
const selected = computed(() => {
  return player.value?.animals.find(a => a.id === selectedId.value) ?? null
})

// If no player exists yet, send them to onboarding (/)
watchEffect(() => {
  if (import.meta.client && !player.value) {
    navigateTo('/')
  }
})

// Default select the first animal when animals load
watchEffect(() => {
  const first = player.value?.animals?.[0]
  if (!selectedId.value && first) {
    selectedId.value = first.id
  }
})

// Feed dropdown selection
const feedChoice = ref<ItemKind | 'basic'>('basic')

function doFeed() {
  if (!selected.value) return
  const choice = feedChoice.value
  feedAnimal(selected.value.id, choice === 'basic' ? undefined : choice)
}

/** -------------------------
 *  VISUAL STABLE (wandering)
 *  ------------------------- */

type SpriteState = {
  id: string
  x: number
  y: number
  vx: number
  vy: number
  bobDelay: number
}

const penRef = ref<HTMLElement | null>(null)

// Tune these if you want bigger/smaller animals
const SPRITE_SIZE = 64
const PADDING = 6

const sprites = ref<SpriteState[]>([])

function rand(min: number, max: number) {
  return min + Math.random() * (max - min)
}

function spriteUrl(kind: string) {
  // Put sprites at: /public/sprites/dog.png etc
  return `/sprites/${kind}.png`
}

function ensureSpritesForAnimals() {
  const animals = player.value?.animals ?? []
  const map = new Map(sprites.value.map(s => [s.id, s]))

  // Add missing
  for (const a of animals) {
    if (!map.has(a.id)) {
      map.set(a.id, {
        id: a.id,
        x: rand(10, 200),
        y: rand(10, 120),
        vx: rand(-0.45, 0.45),
        vy: rand(-0.35, 0.35),
        bobDelay: rand(0, 1.2),
      })
    }
  }

  // Remove sprites for animals that no longer exist
  const ids = new Set(animals.map(a => a.id))
  sprites.value = Array.from(map.values()).filter(s => ids.has(s.id))
}

// Keep sprites in sync when animals change (buy new animal, etc)
watchEffect(() => {
  if (!import.meta.client) return
  if (!player.value) return
  ensureSpritesForAnimals()
})

// Movement loop
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

    // Soft random drift
    s.vx += rand(-0.02, 0.02)
    s.vy += rand(-0.02, 0.02)

    // Clamp speed
    const maxSpeed = 0.9
    s.vx = Math.max(-maxSpeed, Math.min(maxSpeed, s.vx))
    s.vy = Math.max(-maxSpeed, Math.min(maxSpeed, s.vy))

    // Bounce walls
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

function onPenAnimalClick(animalId: string) {
  selectedId.value = animalId
}

// Helpful for a small highlight ring on selected sprite
function isSelectedSprite(id: string) {
  return id === selectedId.value
}

const animalsById = computed(() => {
  const m = new Map<string, (typeof player.value extends null ? never : any)>()
  for (const a of player.value?.animals ?? []) m.set(a.id, a)
  return m
})
</script>

<template>
  <section class="card">
    <h1>Stable</h1>
    <p class="muted">
      Click an animal to focus it. Pet increases affection. Feed reduces hunger and can apply items.
    </p>

    <!-- VISUAL PEN -->
    <div class="penWrap">
      <div class="pen" ref="penRef">
        <!-- "ground" strip -->
        <div class="ground" />

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

      <div class="penHint muted">
        Sprites load from <code>/public/sprites/&lt;kind&gt;.png</code>. Example: <code>dog.png</code>
        <br />
        Background loads from <code>/public/grass.jpg</code>.
      </div>
    </div>

    <div class="row">
      <!-- Left: animal list -->
      <div class="panel">
        <h3>Your Animals</h3>

        <div v-if="(player?.animals?.length ?? 0) === 0" class="muted">
          You don't have any animals yet. Go to the home page to pick a starter.
        </div>

        <button
          v-for="a in player?.animals ?? []"
          :key="a.id"
          class="animalBtn"
          :class="{ active: a.id === selectedId }"
          @click="selectedId = a.id"
          type="button"
        >
          <div class="topLine">
            <span class="tag">{{ a.kind.toUpperCase() }}</span>
            <span class="name">{{ a.name }}</span>
          </div>
          <div class="small muted">
            HP {{ a.hpCurrent }}/{{ a.stats.hpMax }} • ATK {{ a.stats.attack }} • DEF {{ a.stats.defense }}
          </div>
        </button>

        <div class="navRow">
          <NuxtLink class="btn" to="/gauntlet">Go to Gauntlet</NuxtLink>
          <NuxtLink class="btn" to="/shop">Go to Shop</NuxtLink>
        </div>
      </div>

      <!-- Right: selected animal panel -->
      <div class="panel" v-if="selected">
        <h3>Focused Animal</h3>
        <div class="muted">
          {{ selected.ownerName }}’s {{ selected.kind }} • <b>{{ selected.name }}</b>
        </div>

        <div class="stats">
          <div class="stat"><b>HP</b> {{ selected.hpCurrent }} / {{ selected.stats.hpMax }}</div>
          <div class="stat"><b>Attack</b> {{ selected.stats.attack }}</div>
          <div class="stat"><b>Defense</b> {{ selected.stats.defense }}</div>
          <div class="stat"><b>Affection</b> {{ selected.stats.affection }}</div>
          <div class="stat"><b>Hunger</b> {{ selected.stats.hunger }} (lower is better)</div>
        </div>

        <div class="actions">
          <button class="btn primary" @click="petAnimal(selected.id)" type="button">Pet</button>

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

        <div class="hint muted">Tip: Win fights in the Gauntlet to earn gold, then buy items/animals in the Shop.</div>
      </div>

      <div class="panel" v-else>
        <h3>Focused Animal</h3>
        <p class="muted">No animal selected.</p>
      </div>
    </div>
  </section>
</template>

<style scoped>
.card {
  border: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: 18px;
  background: rgba(255, 255, 255, 0.06);
  box-shadow: 0 12px 40px rgba(0, 0, 0, 0.35);
  padding: 18px;
}

.muted {
  color: rgba(255, 255, 255, 0.7);
}

/* ---------- VISUAL PEN ---------- */
.penWrap {
  margin-top: 14px;
  margin-bottom: 12px;
}

.pen {
  position: relative;
  height: 240px;
  border-radius: 18px;
  border: 1px solid rgba(255, 255, 255, 0.12);

  /* ✅ Grass background image (place file at /public/grass.jpg) */
  background:
    radial-gradient(600px 260px at 30% 10%, rgba(124, 92, 255, 0.18), transparent 60%),
    radial-gradient(600px 260px at 80% 30%, rgba(53, 214, 197, 0.14), transparent 60%),
    url('/grass.jpg');

  background-size: auto, auto, cover;
  background-position: center, center, center;
  background-repeat: no-repeat, no-repeat, no-repeat;

  overflow: hidden;
}

/* Optional: subtle dark overlay so sprites pop more */
.pen::before {
  content: '';
  position: absolute;
  inset: 0;
  background: rgba(0, 0, 0, 0.14);
  pointer-events: none;
}

.ground {
  position: absolute;
  left: 0;
  right: 0;
  bottom: 0;
  height: 72px;
  background: linear-gradient(to top, rgba(0, 0, 0, 0.28), transparent);
  border-top: 1px solid rgba(255, 255, 255, 0.06);
  pointer-events: none;
}

.spriteBtn {
  position: absolute;
  width: 64px;
  height: 64px;
  padding: 0;
  border: none;
  background: transparent;
  cursor: pointer;

  /* ensure sprites are above background/overlays */
  z-index: 2;
}

.spriteBtn.selected::after {
  content: '';
  position: absolute;
  inset: -6px;
  border-radius: 18px;
  border: 2px solid rgba(53, 214, 197, 0.8);
  box-shadow: 0 0 0 6px rgba(53, 214, 197, 0.12);
}

.spriteFlip {
  width: 64px;
  height: 64px;
  display: grid;
  place-items: center;
}

.spriteFlip.facingLeft {
  transform: scaleX(-1);
}

/* actual image bobbing */
.spriteImg {
  width: 64px;
  height: 64px;
  object-fit: contain;
  user-select: none;
  -webkit-user-drag: none;
  animation: bob 1.8s ease-in-out infinite;
}

@keyframes bob {
  0%,
  100% {
    transform: translateY(0px);
  }
  50% {
    transform: translateY(-6px);
  }
}

.penHint {
  margin-top: 8px;
  font-size: 12px;
}

code {
  padding: 2px 6px;
  border-radius: 8px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(0, 0, 0, 0.18);
}

/* ---------- EXISTING LAYOUT ---------- */
.row {
  display: flex;
  gap: 14px;
  flex-wrap: wrap;
  margin-top: 14px;
}

.panel {
  flex: 1 1 320px;
  min-width: 280px;
  padding: 14px;
  border-radius: 16px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(255, 255, 255, 0.04);
}

.animalBtn {
  width: 100%;
  text-align: left;
  border-radius: 14px;
  padding: 10px 12px;
  margin-top: 10px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(255, 255, 255, 0.05);
  color: rgba(255, 255, 255, 0.92);
  cursor: pointer;
}

.animalBtn.active {
  border: none;
  color: #0b1020;
  font-weight: 900;
  background: linear-gradient(90deg, #7c5cff, #35d6c5);
}

.topLine {
  display: flex;
  gap: 8px;
  align-items: baseline;
  flex-wrap: wrap;
}

.tag {
  font-size: 11px;
  opacity: 0.85;
}

.name {
  font-weight: 900;
}

.small {
  font-size: 12px;
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
  background: rgba(255, 255, 255, 0.04);
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
  background: rgba(255, 255, 255, 0.06);
  color: rgba(255, 255, 255, 0.92);
  cursor: pointer;
  text-decoration: none;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

.btn.primary {
  border: none;
  background: linear-gradient(90deg, #7c5cff, #35d6c5);
  color: #0b1020;
  font-weight: 900;
}

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
  background: rgba(0, 0, 0, 0.18);
  color: rgba(255, 255, 255, 0.92);
}

.navRow {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
  margin-top: 14px;
}

.hint {
  margin-top: 14px;
  font-size: 12px;
}
</style>
