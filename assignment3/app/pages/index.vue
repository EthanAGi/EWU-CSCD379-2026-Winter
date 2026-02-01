<script setup lang="ts">
import type { AnimalKind } from '../types/game'

const { player, createPlayer, chooseStarter } = usePlayerState()

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

function pickStarter(kind: AnimalKind) {
  chooseStarter(kind)
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
