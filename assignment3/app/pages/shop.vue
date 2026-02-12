<script setup lang="ts">
import { usePlayerState } from '~/composables/usePlayerState'
import { getShopItems, getAnimalPrices } from '~/game/playerLogic'
import ShopItemCard from '~/components/ShopItemCard.vue'
import type { AnimalKind, ItemKind } from '../types/game'

const { player, buyItem, buyAnimal } = usePlayerState()

/**
 * ✅ SWA + separate API App Service:
 * Client calls must hit the .NET API base URL from runtimeConfig.
 */
const config = useRuntimeConfig()

function apiBaseOrThrow(): string {
  const base = (config.public.apiBase ?? '').toString().replace(/\/+$/, '')
  if (!base) throw new Error('Missing runtimeConfig.public.apiBase (set NUXT_PUBLIC_API_BASE in SWA env vars)')
  return base
}

/**
 * ✅ FIX: don't store these as a single mixed list in UI.
 * getShopItems() returns BOTH growth + battle.
 * We filter by category so battle items never appear in growth again.
 */
const allItems = computed(() => getShopItems())
const growthItems = computed(() => allItems.value.filter(i => i.category === 'growth'))
const battleItems = computed(() => allItems.value.filter(i => i.category === 'battle'))

/**
 * ✅ Animal list should come from the DB (AnimalTemplates) so the UI matches the server.
 */
type AnimalTemplateDto = {
  id: number
  kind: AnimalKind
  attack: number
  defense: number
  affection: number
  level: number
  hpMax: number
}

const templates = ref<AnimalTemplateDto[]>([])
const templatesLoading = ref(false)
const templatesError = ref<string | null>(null)

async function loadTemplates() {
  templatesLoading.value = true
  templatesError.value = null
  try {
    const base = apiBaseOrThrow()
    templates.value = await $fetch<AnimalTemplateDto[]>(`${base}/api/animals/templates`)
  } catch (e: any) {
    templatesError.value = e?.message ?? 'Failed to load animals from server.'
    templates.value = []
  } finally {
    templatesLoading.value = false
  }
}

onMounted(loadTemplates)

/**
 * ✅ Prices still come from your existing game rules.
 */
const animalPrices = computed(() => getAnimalPrices())

/**
 * ✅ Show kinds based on what the server says exists.
 * (If server fails, we fall back to local list so the UI still works.)
 */
const animalKinds = computed<AnimalKind[]>(() => {
  if (templates.value.length > 0) return templates.value.map(t => t.kind)
  return Object.keys(animalPrices.value) as AnimalKind[]
})

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

function titleCase(s: string) {
  if (!s) return s
  return s.charAt(0).toUpperCase() + s.slice(1)
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
  if (!player.value) {
    flash('Loading save… try again in a second.')
    return
  }

  const ok = buyItem(kind, 1)
  flash(ok ? 'Purchased!' : 'Not enough gold.')
}

/**
 * ✅ When buying an animal:
 *  1) Local purchase
 *  2) Persist to DB via POST {apiBase}/api/animals/claim
 *  If DB save fails, rollback local purchase.
 */
const buyingAnimal = ref<AnimalKind | null>(null)

async function onBuyAnimal(kind: AnimalKind) {
  if (alreadyOwnsKind(kind)) {
    flash(`You already own a ${kind.toUpperCase()}. Only one of each type is allowed.`)
    return
  }

  if (!player.value) {
    flash('Loading save… try again in a second.')
    return
  }

  if (templatesLoading.value) {
    flash('Loading animals from server… try again in a second.')
    return
  }

  // If server provided templates, ensure this kind exists in DB
  if (templates.value.length > 0 && !templates.value.some(t => t.kind === kind)) {
    flash(`Server doesn’t have template for ${kind}. Check your DB seed/migration.`)
    return
  }

  // Snapshot state for rollback
  const prevGold = player.value.gold
  const prevAnimals = [...player.value.animals]

  // 1) Local purchase
  const ok = buyAnimal(kind)
  if (!ok) {
    flash('Not enough gold.')
    return
  }

  // 2) DB write
  buyingAnimal.value = kind
  try {
    const base = apiBaseOrThrow()
    await $fetch(`${base}/api/animals/claim`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: {
        ownerPlayerId: player.value.id,
        ownerName: player.value.name,
        kind,
        name: `${player.value.name}'s ${titleCase(kind)}`,
      },
    })

    flash('New animal joined your stable! (Saved to DB ✅)')
  } catch (e: any) {
    // rollback
    player.value.gold = prevGold
    player.value.animals = prevAnimals
    flash(`DB save failed — purchase rolled back: ${e?.message ?? 'unknown error'}`)
    console.error('Failed to save bought animal to DB:', e)
  } finally {
    buyingAnimal.value = null
  }
}
</script>

<template>
  <section class="card">
    <h1>Shop</h1>
    <p class="muted">Purchase growth items to enhance your animals. Battle items are useable in combat.</p>

    <div class="pill" v-if="player">💰 Gold: {{ player.gold }}</div>
    <div class="pill" v-else>Loading player…</div>

    <div class="pill" v-if="templatesLoading" style="margin-top:10px;">
      Loading animals from server…
    </div>
    <div class="pill" v-else-if="templatesError" style="margin-top:10px;">
      Server animals load failed (falling back to local list): {{ templatesError }}
    </div>

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
            :price="animalPrices[kind] ?? 0"
            :isAnimal="true"
            :alreadyOwned="alreadyOwnsKind(kind)"
            :disabled="buyingAnimal === kind"
            @buy="(e) => onBuyAnimal(e.kind as AnimalKind)"
          />
        </div>

        <div v-if="buyingAnimal" class="muted small note">
          Saving {{ buyingAnimal.toUpperCase() }} to database…
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

@media (max-width: 520px) {
  .card { padding: 14px; }
  .btn { width: 100%; justify-content: center; }
}
</style>
