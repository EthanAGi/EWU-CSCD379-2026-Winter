<script setup lang="ts">
import { getShopItems, getAnimalPrices, usePlayerState } from '../composables/usePlayerState'
import ShopItemCard from '~/components/ShopItemCard.vue'
import type { AnimalKind, ItemKind } from '../types/game'

const { player, buyItem, buyAnimal } = usePlayerState()

/**
 * ✅ FIX: don't store these as a single mixed list in UI.
 * getShopItems() returns BOTH growth + battle.
 * We filter by category so battle items never appear in growth again.
 */
const allItems = computed(() => getShopItems())
const growthItems = computed(() => allItems.value.filter(i => i.category === 'growth'))
const battleItems = computed(() => allItems.value.filter(i => i.category === 'battle'))

/**
 * ✅ Show ALL animals (including starters) with “buy once” rule.
 * getAnimalPrices() already includes dog/cat/hamster with price 0.
 */
const animalPrices = computed(() => getAnimalPrices())
const animalKinds = computed(() => Object.keys(animalPrices.value) as AnimalKind[])

/** -------------------------
 *  Type limiting (one of each animal kind)
 *  ------------------------- */
const ownedKinds = computed(() => {
  const set = new Set<AnimalKind>()
  for (const a of player.value?.animals ?? []) set.add(a.kind)
  return set
})

function alreadyOwnsKind(kind: AnimalKind) {
  return ownedKinds.value.has(kind)
}

/** -------------------------
 *  Flash messages
 *  ------------------------- */
const buyMsg = ref<string | null>(null)
let msgTimeout: number | null = null

function flash(msg: string) {
  buyMsg.value = msg
  if (msgTimeout !== null) window.clearTimeout(msgTimeout)
  msgTimeout = window.setTimeout(() => (buyMsg.value = null), 1800)
}

onBeforeUnmount(() => {
  if (msgTimeout !== null) window.clearTimeout(msgTimeout)
})

/** -------------------------
 *  Buy handlers
 *  ------------------------- */
function onBuyItem(kind: ItemKind) {
  // ✅ FIX: prevent "random" failures when player hasn't loaded yet
  if (!player.value) {
    flash('Loading save… try again in a second.')
    return
  }

  const ok = buyItem(kind, 1)
  flash(ok ? 'Purchased!' : 'Not enough gold.')
}

function onBuyAnimal(kind: AnimalKind) {
  // ✅ rule: only once per kind
  if (alreadyOwnsKind(kind)) {
    flash(`You already own a ${kind.toUpperCase()}. Only one of each type is allowed.`)
    return
  }

  // ✅ FIX: prevent "random" failures when player hasn't loaded yet
  if (!player.value) {
    flash('Loading save… try again in a second.')
    return
  }

  const ok = buyAnimal(kind)
  flash(ok ? 'New animal joined your stable!' : 'Not enough gold.')
}
</script>

<template>
  <section class="card">
    <h1>Shop</h1>
    <p class="muted">Buy items to feed your animals, or save up for rare animals.</p>

    <div class="pill" v-if="player">💰 Gold: {{ player.gold }}</div>
    <div class="pill" v-else>Loading player…</div>

    <div v-if="buyMsg" class="pill" style="margin-top:10px;">{{ buyMsg }}</div>

    <div class="row">
      <!-- Growth Items -->
      <div class="panel">
        <h3>Growth Items</h3>

        <div v-for="i in growthItems" :key="i.id">
          <ShopItemCard
            :itemKind="i.kind"
            :title="i.name"
            :description="i.description"
            :price="i.price"
            :ownedCount="player?.inventory?.[i.kind] ?? 0"
            @buy="(e) => onBuyItem(e.kind as ItemKind)"
          />
        </div>

        <div v-if="growthItems.length === 0" class="muted small note">
          No growth items available.
        </div>
      </div>

      <!-- Battle Items -->
      <div class="panel">
        <h3>Battle Items</h3>

        <div v-for="b in battleItems" :key="b.id">
          <ShopItemCard
            :itemKind="b.kind"
            :title="b.name"
            :description="b.description"
            :price="b.price"
            :ownedCount="player?.inventory?.[b.kind] ?? 0"
            @buy="(e) => onBuyItem(e.kind as ItemKind)"
          />
        </div>

        <div class="muted small note">
          Pills wear off at the end of the battle (we’ll wire this into Gauntlet next).
        </div>
      </div>

      <!-- Animals -->
      <div class="panel">
        <h3>Animals</h3>

        <div v-for="kind in animalKinds" :key="kind">
          <ShopItemCard
            :itemKind="kind"
            :title="kind.toUpperCase()"
            :description="`A ${kind} companion`"
            :price="animalPrices[kind]"
            :isAnimal="true"
            :alreadyOwned="alreadyOwnsKind(kind)"
            @buy="(e) => onBuyAnimal(e.kind as AnimalKind)"
          />
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
.small { font-size: 12px; }

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
  border: 1px solid rgba(255,255,255,0.12);
  background: rgba(255,255,255,0.04);
}

.btn {
  border-radius: 14px;
  padding: 10px 14px;
  border: 1px solid rgba(255,255,255,0.12);
  background: rgba(255,255,255,0.06);
  color: rgba(255,255,255,0.92);
  cursor: pointer;
  text-decoration: none;
  white-space: nowrap;
  max-width: 100%;
}

.btn:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.pill {
  display: inline-block;
  border: 1px solid rgba(255,255,255,0.12);
  background: rgba(255,255,255,0.05);
  padding: 6px 10px;
  border-radius: 999px;
  font-size: 12px;
  color: rgba(255,255,255,0.80);
  max-width: 100%;
  overflow-wrap: anywhere;
}

.note {
  margin-top: 10px;
  padding: 10px 12px;
  border-radius: 14px;
  border: 1px solid rgba(255,255,255,0.12);
  background: rgba(255,255,255,0.03);
}

/* Mobile tweaks */
@media (max-width: 520px) {
  .card { padding: 14px; }

  .shopCard {
    flex-direction: column;
    align-items: stretch;
  }

  .btn {
    width: 100%;
    justify-content: center;
  }
}
</style>
