<script setup lang="ts">
import type { AnimalKind, ItemKind } from '../types/game'

export interface ShopItemCardProps {
  itemKind: ItemKind | AnimalKind
  title: string
  description: string
  price: number
  ownedCount?: number
  isAnimal?: boolean
  alreadyOwned?: boolean
}

export interface ShopItemEvent {
  kind: ItemKind | AnimalKind
}

const props = withDefaults(defineProps<ShopItemCardProps>(), {
  ownedCount: 0,
  isAnimal: false,
  alreadyOwned: false,
})

const emit = defineEmits<{
  buy: [payload: ShopItemEvent]
}>()

const handleBuy = () => {
  emit('buy', { kind: props.itemKind })
}
</script>

<template>
  <div class="shopCard">
    <div class="left">
      <div class="title">{{ props.title }}</div>
      <div class="muted small">{{ props.description }}</div>
      <div class="muted small">Price: {{ props.price }}</div>
      <div class="muted small" v-if="!props.isAnimal && props.ownedCount !== undefined">
        Owned: {{ props.ownedCount }}
      </div>
      <div class="muted small" v-if="props.isAnimal && props.alreadyOwned">
        Owned (limit 1)
      </div>
    </div>

    <button class="btn" :disabled="props.alreadyOwned" @click="handleBuy" type="button">
      {{ props.alreadyOwned ? 'Owned' : 'Buy' }}
    </button>
  </div>
</template>

<style scoped>
.shopCard {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: center;
  padding: 12px;
  margin-top: 10px;
  border-radius: 16px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(255, 255, 255, 0.05);
}

.left { min-width: 0; }

.title { font-weight: 900; }

.muted { color: rgba(255, 255, 255, 0.70); }
.small { font-size: 12px; }

.btn {
  border-radius: 14px;
  padding: 10px 14px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(255, 255, 255, 0.06);
  color: rgba(255, 255, 255, 0.92);
  cursor: pointer;
  text-decoration: none;
  white-space: nowrap;
  max-width: 100%;
}

.btn:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

/* Mobile tweaks */
@media (max-width: 520px) {
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
