<template>
  <div class="board">
    <div v-for="(row, r) in rows" :key="r" class="row" :class="{ shake: shakeRow === r }">
      <div v-for="(tile, c) in row" :key="c" class="tile" :class="tile.state">
        {{ tile.letter }}
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
type TileState = "empty" | "absent" | "present" | "correct";
type Tile = { letter: string; state: TileState };

defineProps<{
  rows: Tile[][];
  shakeRow: number | null;
}>();
</script>

<style scoped>
/*
  Responsive Wordle board:
  - Uses CSS variables so tile size + gap can shrink on tiny screens
  - Uses clamp() to scale smoothly from very small phones up to desktop
*/

.board {
  /* tile size and gaps adapt with viewport */
  --tile: clamp(40px, 12vw, 56px);
  --gap: clamp(4px, 1.8vw, 8px);
  --row-gap: clamp(6px, 2.2vw, 10px);

  display: grid;
  gap: var(--row-gap);

  /* Fit exactly 5 tiles + 4 gaps, never overflow */
  width: min(100%, calc((var(--tile) * 5) + (var(--gap) * 4)));
  margin-inline: auto;
  box-sizing: border-box;
}

.row {
  display: grid;
  grid-template-columns: repeat(5, var(--tile));
  gap: var(--gap);
  justify-content: center;
}

.tile {
  width: var(--tile);
  height: var(--tile);
  border: 2px solid #3a3a3a;
  display: grid;
  place-items: center;

  /* scale text with tile size */
  font-size: clamp(16px, calc(var(--tile) * 0.4), 22px);
  font-weight: 700;
  text-transform: uppercase;
  user-select: none;
}

/* states */
.empty {
  background: transparent;
}

.correct {
  background: #6aaa64;
  border-color: #6aaa64;
  color: white;
}

.present {
  background: #c9b458;
  border-color: #c9b458;
  color: white;
}

.absent {
  background: #787c7e;
  border-color: #787c7e;
  color: white;
}

/* shake animation */
.shake {
  animation: shake 0.35s;
}

@keyframes shake {
  0% { transform: translateX(0); }
  20% { transform: translateX(-8px); }
  40% { transform: translateX(8px); }
  60% { transform: translateX(-6px); }
  80% { transform: translateX(6px); }
  100% { transform: translateX(0); }
}

/* Extra-tight tuning for ultra-small screens (e.g., 320px wide) */
@media (max-width: 360px) {
  .board {
    --tile: clamp(36px, 11.5vw, 46px);
    --gap: clamp(3px, 1.2vw, 6px);
    --row-gap: clamp(5px, 1.8vw, 8px);
  }

  .tile {
    border-width: 2px;
    font-size: clamp(14px, calc(var(--tile) * 0.38), 18px);
  }
}
</style>
