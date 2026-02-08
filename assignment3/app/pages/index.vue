<script setup lang="ts">
import type { AnimalKind } from '../types/game'
import { onBeforeRouteLeave, type RouteLocationNormalized } from 'vue-router'

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

/**
 * ----------------------------
 * Leave warning (during starter save)
 * ----------------------------
 */
const leaveModalOpen = ref(false)
const pendingTo = ref<RouteLocationNormalized | null>(null)
const isSaving = computed(() => savingStarter.value !== null)

onBeforeRouteLeave((to, from, next) => {
  // If not saving, allow navigation
  if (!isSaving.value) return next()

  // If route isn't actually changing, allow
  if (to.fullPath === from.fullPath) return next()

  // Block and show modal
  leaveModalOpen.value = true
  pendingTo.value = to
  next(false)
})

function stayOnPage() {
  leaveModalOpen.value = false
  pendingTo.value = null
}

async function leaveAnyway() {
  const to = pendingTo.value
  leaveModalOpen.value = false
  pendingTo.value = null

  // We ARE choosing to leave while saving; let it proceed.
  // Use navigateTo so Nuxt handles it correctly.
  if (to) {
    await navigateTo(to.fullPath)
  }
}

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
</script>

<template>
  <section class="wrap">
    <!-- ✅ Leave warning modal (only when saving) -->
    <div v-if="leaveModalOpen" class="overlay" role="dialog" aria-modal="true">
      <div class="overlayCard">
        <div class="overlayTop">
          <div class="overlayBadge warn">LEAVING NOW?</div>
        </div>

        <div class="overlayText">
          <div class="overlayHeadline">Starter save in progress</div>
          <div class="muted">
            Your starter is currently being saved. If you leave now, the save may fail and you could lose progress.
          </div>
        </div>

        <div class="overlayActions">
          <button class="btn primary" type="button" @click="stayOnPage">Stay</button>
          <button class="btn" type="button" @click="leaveAnyway">Leave Anyway</button>
        </div>
      </div>
    </div>

    <!-- ✅ Simple nav (NO Home link ever) -->
    <nav class="nav">
      <NuxtLink class="navBtn" to="/reviews">Reviews</NuxtLink>

      <template v-if="player">
        <NuxtLink class="navBtn" to="/stable">Stable</NuxtLink>
        <NuxtLink class="navBtn" to="/shop">Shop</NuxtLink>
      </template>
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

      <p v-if="starterMsg" class="muted" style="margin-top:12px;">
        {{ starterMsg }}
      </p>
    </section>
  </section>
</template>

<style scoped>
.wrap { width: min(900px, calc(100% - 32px)); margin: 0 auto; margin-top: 18px; }

.nav{
  display:flex;
  gap:10px;
  flex-wrap:wrap;
  align-items:center;
  margin-bottom:14px;
}

.navBtn{
  border-radius:14px;
  padding:10px 14px;
  border:1px solid rgba(255,255,255,.12);
  background:rgba(0,0,0,.22);
  color:rgba(255,255,255,.92);
  text-decoration:none;
  display:inline-flex;
  align-items:center;
  justify-content:center;
  backdrop-filter: blur(6px);
}

.card{
  border:1px solid rgba(255,255,255,.12);
  border-radius:18px;
  background:rgba(255,255,255,.06);
  box-shadow:0 12px 40px rgba(0,0,0,.35);
  padding:18px;
}

.muted{ color:rgba(255,255,255,.70); }
.row{ display:flex; gap:12px; flex-wrap:wrap; align-items:center; margin-top:14px; }
.input{
  flex:1 1 240px;
  padding:10px 12px;
  border-radius:14px;
  border:1px solid rgba(255,255,255,.12);
  background:rgba(0,0,0,.18);
  color:rgba(255,255,255,.92);
}

.btn{
  border-radius:14px;
  padding:10px 14px;
  border:1px solid rgba(255,255,255,.12);
  background:rgba(255,255,255,.06);
  color:rgba(255,255,255,.92);
  cursor:pointer;
}
.btn:disabled { opacity: 0.55; cursor: not-allowed; }
.btn.primary{
  border:none;
  background:linear-gradient(90deg,#7c5cff,#35d6c5);
  color:#0b1020;
  font-weight:900;
}

/* Modal overlay */
.overlay{
  position: fixed;
  inset: 0;
  background: rgba(0,0,0,0.55);
  backdrop-filter: blur(6px);
  display: grid;
  place-items: center;
  z-index: 999;
  padding: 12px;
  overflow: auto;
  overscroll-behavior: contain;
}

.overlayCard{
  width: min(520px, 94vw);
  max-width: 94vw;
  max-height: 92vh;
  overflow: auto;
  border-radius: 20px;
  border: 1px solid rgba(255,255,255,.14);
  background:
    radial-gradient(600px 260px at 30% 10%, rgba(124,92,255,.18), transparent 55%),
    radial-gradient(600px 260px at 80% 35%, rgba(53, 214, 197, .14), transparent 55%),
    rgba(15, 18, 30, 0.92);
  box-shadow: 0 24px 80px rgba(0,0,0,.55);
  padding: 16px;
  box-sizing: border-box;
}

.overlayTop{ display:flex; justify-content:flex-end; }

.overlayBadge{
  padding: 8px 12px;
  border-radius: 999px;
  font-weight: 900;
  letter-spacing: 1px;
  border: 1px solid rgba(255,255,255,.14);
  background: rgba(255,255,255,.06);
  max-width: 100%;
  overflow-wrap: anywhere;
}
.overlayBadge.warn{ background: rgba(255, 200, 70, .16); }

.overlayText{ margin-top: 8px; text-align:center; }
.overlayHeadline{
  font-size: 22px;
  font-weight: 1000;
  margin-bottom: 6px;
  overflow-wrap: anywhere;
}

.overlayActions{
  margin-top: 14px;
  display:flex;
  gap: 10px;
  justify-content:center;
  flex-wrap:wrap;
}
</style>
