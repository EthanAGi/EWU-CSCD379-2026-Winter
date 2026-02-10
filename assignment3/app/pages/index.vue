<script setup lang="ts">
import type { AnimalKind } from '../types/game'
import { onBeforeRouteLeave } from 'vue-router'

const { player, createPlayer, chooseStarter, addGold } = usePlayerState()

const name = ref('')

const stage = computed(() => {
  if (!player.value) return 'name'
  if (player.value.animals.length === 0) return 'starter'
  return 'done'
})

watchEffect(() => {
  if (stage.value === 'done') navigateTo('/stable')
})

function submitName() {
  const n = name.value.trim()
  if (n.length < 2) return
  createPlayer(n)
}

function titleCase(s: string) {
  if (!s) return s
  return s.charAt(0).toUpperCase() + s.slice(1)
}

/** Optional UI feedback */
const savingStarter = ref<AnimalKind | null>(null)
const starterMsg = ref<string | null>(null)
const isSaving = computed(() => savingStarter.value !== null)

/**
 * ----------------------------
 * Leave behavior (NO modal during starter save)
 * ----------------------------
 */
onBeforeRouteLeave((to, from, next) => {
  if (!isSaving.value) return next()
  if (to.fullPath === from.fullPath) return next()
  next(false)
})

/** Browser refresh/close warning while saving (best-effort) */
function handleBeforeUnload(e: BeforeUnloadEvent) {
  if (!isSaving.value) return
  e.preventDefault()
  e.returnValue = ''
}

onMounted(() => {
  if (!import.meta.client) return
  window.addEventListener('beforeunload', handleBeforeUnload)
})

onBeforeUnmount(() => {
  if (!import.meta.client) return
  window.removeEventListener('beforeunload', handleBeforeUnload)
})

async function pickStarter(kind: AnimalKind) {
  starterMsg.value = null

  const p = player.value
  if (!p) return

  const prevGold = p.gold
  const prevAnimals = [...p.animals]

  chooseStarter(kind)
  addGold(300)

  savingStarter.value = kind
  try {
    await $fetch(`/api/animals/claim`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: {
        ownerPlayerId: p.id,
        ownerName: p.name,
        kind,
        name: `${p.name}'s ${titleCase(kind)}`,
      },
    })

    starterMsg.value = 'Starter saved to DB ✅'
  } catch (e: any) {
    p.gold = prevGold
    p.animals = prevAnimals

    starterMsg.value = `DB save failed — starter rollback: ${e?.message ?? 'unknown error'}`
    console.error('Failed to save starter to DB:', e)
  } finally {
    savingStarter.value = null
  }
}

/** Disable nav buttons while saving */
const navDisabled = computed(() => isSaving.value)
</script>

<template>
  <section class="wrap">
    <nav class="nav">
      <div class="navLeft">
        <NuxtLink
          class="navBtn"
          :class="{ disabled: navDisabled }"
          to="/reviews"
          :tabindex="navDisabled ? -1 : 0"
        >
          Reviews
        </NuxtLink>

        <template v-if="player">
          <NuxtLink
            class="navBtn iconOnly"
            :class="{ disabled: navDisabled }"
            to="/stable"
            :tabindex="navDisabled ? -1 : 0"
            aria-label="Stable"
            title="Stable"
          >
            <svg
              class="navIcon"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
              stroke-linecap="round"
              stroke-linejoin="round"
              aria-hidden="true"
            >
              <path d="M3 10.5L12 3l9 7.5" />
              <path d="M5 10v10h14V10" />
              <path d="M10 20v-6h4v6" />
            </svg>
          </NuxtLink>

          <NuxtLink
            class="navBtn iconOnly"
            :class="{ disabled: navDisabled }"
            to="/shop"
            :tabindex="navDisabled ? -1 : 0"
            aria-label="Shop"
            title="Shop"
          >
            <svg
              class="navIcon"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
              stroke-linecap="round"
              stroke-linejoin="round"
              aria-hidden="true"
            >
              <path d="M6 7h12l-1 13H7L6 7z" />
              <path d="M9 7a3 3 0 016 0" />
            </svg>
          </NuxtLink>
        </template>
      </div>
    </nav>

    <section class="card">
      <h1 v-if="stage === 'name'">Welcome</h1>
      <h1 v-else-if="stage === 'starter'">Choose Your Starter</h1>

      <p class="muted" v-if="stage === 'name'">
        Enter your name to begin. (Saved locally for now.)
      </p>

      <div v-if="stage === 'name'" class="row">
        <input
          class="input"
          v-model="name"
          placeholder="Player name..."
          @keydown.enter="submitName"
        />
        <button class="btn primary" @click="submitName">Continue</button>
      </div>

      <div v-else-if="stage === 'starter'" class="row">
        <button class="btn" :disabled="savingStarter !== null" @click="pickStarter('dog')">
          {{ savingStarter === 'dog' ? 'Saving…' : 'Dog' }}
        </button>
        <button class="btn" :disabled="savingStarter !== null" @click="pickStarter('cat')">
          {{ savingStarter === 'cat' ? 'Saving…' : 'Cat' }}
        </button>
        <button class="btn" :disabled="savingStarter !== null" @click="pickStarter('hamster')">
          {{ savingStarter === 'hamster' ? 'Saving…' : 'Hamster' }}
        </button>
      </div>

      <p v-if="starterMsg" class="muted" style="margin-top: 12px;">
        {{ starterMsg }}
      </p>
    </section>
  </section>
</template>

<style scoped>
.wrap {
  width: min(900px, calc(100% - 32px));
  margin: 18px auto 0;
}

.nav {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 14px;
}

.navLeft {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
  align-items: center;
}

.navBtn {
  border-radius: 14px;
  padding: 10px 14px;
  border: none;
  background: rgba(0, 0, 0, 0.22);
  color: rgba(255, 255, 255, 0.92);
  text-decoration: none;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  backdrop-filter: blur(6px);
  box-shadow: 0 8px 22px rgba(0, 0, 0, 0.22);
}

.navBtn.iconOnly {
  padding: 10px;
  width: 42px;
  height: 42px;
}

.navIcon {
  width: 18px;
  height: 18px;
}

.navBtn.disabled {
  opacity: 0.45;
  pointer-events: none;
}

.card {
  border: none;
  border-radius: 18px;
  background: rgba(255, 255, 255, 0.06);
  box-shadow: 0 12px 40px rgba(0, 0, 0, 0.35);
  padding: 18px;
}

.muted {
  color: rgba(255, 255, 255, 0.7);
}

.row {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
  align-items: center;
  margin-top: 14px;
}

.input {
  flex: 1 1 240px;
  padding: 10px 12px;
  border-radius: 14px;
  border: none;
  background: rgba(0, 0, 0, 0.18);
  color: rgba(255, 255, 255, 0.92);
  outline: none;
}

.btn {
  border-radius: 14px;
  padding: 10px 14px;
  border: none;
  background: rgba(255, 255, 255, 0.06);
  color: rgba(255, 255, 255, 0.92);
  cursor: pointer;
  box-shadow: 0 10px 26px rgba(0, 0, 0, 0.22);
}

.btn:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.btn.primary {
  background: linear-gradient(90deg, #7c5cff, #35d6c5);
  color: #0b1020;
  font-weight: 900;
}
</style>
