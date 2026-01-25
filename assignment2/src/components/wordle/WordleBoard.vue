<template>
  <div class="board">
    <div v-for="(row, r) in rows" :key="r" class="row" :class="{ shake: shakeRow === r }">
      <div
        v-for="(tile, c) in row"
        :key="c"
        class="tile"
        :class="tile.state"
      >
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
.board {
  display: grid;
  gap: 10px;
  max-width: 360px;
}

.row {
  display: grid;
  grid-template-columns: repeat(5, 56px);
  gap: 8px;
  justify-content: start;
}

.tile {
  width: 56px;
  height: 56px;
  border: 2px solid #3a3a3a;
  display: grid;
  place-items: center;
  font-size: 22px;
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
</style>
