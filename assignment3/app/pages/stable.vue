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
</script>

<template>
  <section class="card">
    <h1>Stable</h1>
    <p class="muted">
      Click an animal to focus it. Pet increases affection. Feed reduces hunger and can apply items.
    </p>

    <div class="row">
      <!-- Left: animal list -->
      <div class="panel">
        <h3>Your Animals</h3>

        <div v-if="(player?.animals?.length ?? 0) === 0" class="muted">
          You don't have any animals yet. Go to the home page to pick a starter.
        </div>

        <button
          v-for="a in (player?.animals ?? [])"
          :key="a.id"
          class="animalBtn"
          :class="{ active: a.id === selectedId }"
          @click="selectedId = a.id"
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
          <button class="btn primary" @click="petAnimal(selected.id)">Pet</button>

          <div class="feedRow">
            <select v-model="feedChoice" class="select">
              <option value="basic">Basic Feed (free)</option>
              <option value="treat">Treat (inv: {{ player?.inventory?.treat ?? 0 }})</option>
              <option value="armorSnack">Armor Snack (inv: {{ player?.inventory?.armorSnack ?? 0 }})</option>
              <option value="proteinBite">Protein Bite (inv: {{ player?.inventory?.proteinBite ?? 0 }})</option>
            </select>

            <button class="btn" @click="doFeed">Feed</button>
          </div>
        </div>

        <div class="hint muted">
          Tip: Win fights in the Gauntlet to earn gold, then buy items/animals in the Shop.
        </div>
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
