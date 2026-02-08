<script setup lang="ts">
export interface SpriteState {
  id: string
  x: number
  y: number
  vx: number
  vy: number
  bobDelay: number
}

export interface AnimalData {
  id: string
  name: string
  kind: string
}

export interface AnimalSpriteProps {
  sprite: SpriteState
  animal: AnimalData
  isSelected: boolean
}

const props = withDefaults(defineProps<AnimalSpriteProps>(), {
  isSelected: false,
})

const emit = defineEmits<{
  click: [animalId: string]
}>()

const spriteUrl = (kind: string) => `/sprites/${kind}.png`

const handleClick = () => {
  emit('click', props.sprite.id)
}
</script>

<template>
  <button
    class="spriteBtn"
    :class="{ selected: props.isSelected }"
    :style="{ left: props.sprite.x + 'px', top: props.sprite.y + 'px' }"
    @click="handleClick"
    type="button"
    :aria-label="`Select ${props.animal.name}`"
  >
    <div class="spriteFlip" :class="{ facingLeft: props.sprite.vx < 0 }">
      <img
        class="spriteImg"
        :style="{ animationDelay: props.sprite.bobDelay + 's' }"
        :src="spriteUrl(props.animal.kind)"
        :alt="props.animal.name"
        draggable="false"
      />
    </div>
  </button>
</template>

<style scoped>
.spriteBtn {
  position: absolute;
  width: 64px;
  height: 64px;
  padding: 0;
  border: none;
  background: transparent;
  cursor: pointer;
}

/* keep sprite above ground etc; stable can layer world/panel independently */
.spriteBtn { z-index: 2; }

.spriteBtn.selected::after {
  content: '';
  position: absolute;
  inset: -6px;
  border-radius: 18px;
  border: 2px solid rgba(53, 214, 197, 0.85);
  box-shadow: 0 0 0 6px rgba(53, 214, 197, 0.14);
}

.spriteFlip {
  width: 64px;
  height: 64px;
  display: grid;
  place-items: center;
}

.spriteFlip.facingLeft { transform: scaleX(-1); }

.spriteImg {
  width: 64px;
  height: 64px;
  object-fit: contain;
  user-select: none;
  -webkit-user-drag: none;
  animation: bob 1.8s ease-in-out infinite;
}

@keyframes bob {
  0%, 100% { transform: translateY(0px); }
  50% { transform: translateY(-6px); }
}
</style>
