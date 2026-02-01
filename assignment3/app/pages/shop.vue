<script setup lang="ts">
import { getShopItems, getAnimalPrices, usePlayerState } from '../composables/usePlayerState'
import type { AnimalKind, ItemKind } from '../types/game'

const { player, buyItem, buyAnimal } = usePlayerState()
const items = getShopItems()
const animalPrices = getAnimalPrices()

const buyMsg = ref<string | null>(null)
function flash(msg: string) {
  buyMsg.value = msg
  setTimeout(() => (buyMsg.value = null), 1800)
}

function onBuyItem(kind: ItemKind) {
  const ok = buyItem(kind, 1)
  flash(ok ? 'Purchased!' : 'Not enough gold.')
}

function onBuyAnimal(kind: AnimalKind) {
  const ok = buyAnimal(kind)
  flash(ok ? 'New animal joined your stable!' : 'Not enough gold.')
}
</script>

<template>
  <section class="card">
    <h1>Shop</h1>
    <p class="muted">Buy items to feed your animals, or save up for rare animals.</p>

    <div class="pill" v-if="player">💰 Gold: {{ player.gold }}</div>
    <div v-if="buyMsg" class="pill" style="margin-top:10px;">{{ buyMsg }}</div>

    <div class="row">
      <div class="panel">
        <h3>Items</h3>
        <div v-for="i in items" :key="i.id" class="shopCard">
          <div>
            <div class="title">{{ i.name }}</div>
            <div class="muted small">{{ i.description }}</div>
            <div class="muted small">Price: {{ i.price }}</div>
          </div>
          <button class="btn" @click="onBuyItem(i.kind)">Buy</button>
        </div>
      </div>

      <div class="panel">
        <h3>Animals</h3>

        <div class="shopCard">
          <div>
            <div class="title">Fox</div>
            <div class="muted small">Price: {{ animalPrices.fox }}</div>
          </div>
          <button class="btn" @click="onBuyAnimal('fox')">Buy</button>
        </div>

        <div class="shopCard">
          <div>
            <div class="title">Owl</div>
            <div class="muted small">Price: {{ animalPrices.owl }}</div>
          </div>
          <button class="btn" @click="onBuyAnimal('owl')">Buy</button>
        </div>

        <div class="shopCard">
          <div>
            <div class="title">Boar</div>
            <div class="muted small">Price: {{ animalPrices.boar }}</div>
          </div>
          <button class="btn" @click="onBuyAnimal('boar')">Buy</button>
        </div>
      </div>
    </div>

    <div style="margin-top:14px;">
      <NuxtLink class="btn" to="/stable">Back to Stable</NuxtLink>
    </div>
  </section>
</template>

<style scoped>
.card {
  border: 1px solid rgba(255,255,255,0.12);
  border-radius: 18px;
  background: rgba(255,255,255,0.06);
  box-shadow: 0 12px 40px rgba(0,0,0,0.35);
  padding: 18px;
}
.muted { color: rgba(255,255,255,0.70); }
.row { display: flex; gap: 14px; flex-wrap: wrap; margin-top: 14px; }
.panel {
  flex: 1 1 320px;
  min-width: 280px;
  padding: 14px;
  border-radius: 16px;
  border: 1px solid rgba(255,255,255,0.12);
  background: rgba(255,255,255,0.04);
}
.shopCard {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: center;
  padding: 12px;
  margin-top: 10px;
  border-radius: 16px;
  border: 1px solid rgba(255,255,255,0.12);
  background: rgba(255,255,255,0.05);
}
.title { font-weight: 900; }
.small { font-size: 12px; }
.btn {
  border-radius: 14px;
  padding: 10px 14px;
  border: 1px solid rgba(255,255,255,0.12);
  background: rgba(255,255,255,0.06);
  color: rgba(255,255,255,0.92);
  cursor: pointer;
  text-decoration: none;
}
.pill {
  display: inline-block;
  border: 1px solid rgba(255,255,255,0.12);
  background: rgba(255,255,255,0.05);
  padding: 6px 10px;
  border-radius: 999px;
  font-size: 12px;
  color: rgba(255,255,255,0.80);
}
</style>
