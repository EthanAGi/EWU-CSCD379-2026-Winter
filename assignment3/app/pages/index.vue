<script setup lang="ts">
import type { AnimalKind } from '../types/game'

const { player, createPlayer, chooseStarter, addGold } = usePlayerState()

const config = useRuntimeConfig()
const API_BASE = (config.public.apiBase as string | undefined)?.replace(/\/+$/, '') || '' // e.g. http://localhost:5072

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

async function pickStarter(kind: AnimalKind) {
  // 1) local save: adds starter animal to local state
  chooseStarter(kind)

  // 2) give starter bonus gold
  addGold(300)

  // 3) persist to API/DB as a PLAYER ANIMAL (PlayersAnimal)
  const p = player.value
  if (!p) return

  try {
    // Call the .NET API directly (avoids Nuxt router 404 / proxy confusion)
    await $fetch(`${API_BASE}/api/animals/claim`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: {
        ownerPlayerId: p.id,
        ownerName: p.name,
        kind,
        // "Player's animal" naming format
        name: `${p.name}'s ${titleCase(kind)}`,
      },
    })
  } catch (e) {
    // Don’t block the player from starting if the DB call fails
    console.error('Failed to save starter to DB:', e)
  }
}
</script>

<template>
  <section class="card">
    <h1 v-if="stage === 'name'">Welcome</h1>
    <h1 v-else-if="stage === 'starter'">Choose Your Starter</h1>

    <p class="muted" v-if="stage === 'name'">
      Enter your name to begin. (Saved locally for now.)
    </p>

    <div v-if="stage === 'name'" class="row">
      <input class="input" v-model="name" placeholder="Player name..." @keydown.enter="submitName" />
      <button class="btn primary" @click="submitName">Continue</button>
    </div>

    <div v-else-if="stage === 'starter'" class="row">
      <button class="btn" @click="pickStarter('dog')">Dog</button>
      <button class="btn" @click="pickStarter('cat')">Cat</button>
      <button class="btn" @click="pickStarter('hamster')">Hamster</button>
    </div>
  </section>
</template>

<style scoped>
.card{ border:1px solid rgba(255,255,255,.12); border-radius:18px; background:rgba(255,255,255,.06); box-shadow:0 12px 40px rgba(0,0,0,.35); padding:18px; margin-top:20px;}
.muted{ color:rgba(255,255,255,.70); }
.row{ display:flex; gap:12px; flex-wrap:wrap; align-items:center; margin-top:14px;}
.input{ flex:1 1 240px; padding:10px 12px; border-radius:14px; border:1px solid rgba(255,255,255,.12); background:rgba(0,0,0,.18); color:rgba(255,255,255,.92); }
.btn{ border-radius:14px; padding:10px 14px; border:1px solid rgba(255,255,255,.12); background:rgba(255,255,255,.06); color:rgba(255,255,255,.92); cursor:pointer; }
.btn.primary{ border:none; background:linear-gradient(90deg,#7c5cff,#35d6c5); color:#0b1020; font-weight:900; }
</style>
