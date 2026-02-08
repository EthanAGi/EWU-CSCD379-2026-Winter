<script setup lang="ts">
import type { AnimalKind } from '../types/game'

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

async function pickStarter(kind: AnimalKind) {
  starterMsg.value = null

  // Need player before doing anything
  const p = player.value
  if (!p) return

  // Snapshot state for rollback if DB fails
  const prevGold = p.gold
  const prevAnimals = [...p.animals]

  // 1) local save: adds starter animal to local state
  chooseStarter(kind)

  // 2) give starter bonus gold
  addGold(300)

  // 3) persist to DB via Nuxt server route (same-origin)
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
    // ✅ Roll back local state if DB save fails
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
</style>
