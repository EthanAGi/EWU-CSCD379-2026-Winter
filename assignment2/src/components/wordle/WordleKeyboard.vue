<template>
  <div class="kb">
    <div class="kb-row">
      <v-btn
        v-for="k in row1"
        :key="k"
        size="small"
        class="kb-key"
        :class="keyClass(k)"
        @click="emit('press', k)"
      >
        {{ k }}
      </v-btn>
    </div>

    <div class="kb-row">
      <v-btn
        v-for="k in row2"
        :key="k"
        size="small"
        class="kb-key"
        :class="keyClass(k)"
        @click="emit('press', k)"
      >
        {{ k }}
      </v-btn>
    </div>

    <div class="kb-row">
      <v-btn size="small" class="kb-key wide" @click="emit('press', 'ENTER')">Enter</v-btn>

      <v-btn
        v-for="k in row3"
        :key="k"
        size="small"
        class="kb-key"
        :class="keyClass(k)"
        @click="emit('press', k)"
      >
        {{ k }}
      </v-btn>

      <v-btn size="small" class="kb-key wide" @click="emit('press', 'BACK')">⌫</v-btn>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { LetterState } from "../../game/score";

const props = defineProps<{
  keyStates: Record<string, LetterState>;
}>();

const emit = defineEmits<{
  (e: "press", key: string): void;
}>();

const row1 = ["Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P"];
const row2 = ["A", "S", "D", "F", "G", "H", "J", "K", "L"];
const row3 = ["Z", "X", "C", "V", "B", "N", "M"];

function keyClass(k: string) {
  const st = props.keyStates[k];
  return st ? st : "";
}
</script>

<style scoped>
.kb {
  display: grid;
  gap: 10px;
  max-width: 520px;
}
.kb-row {
  display: flex;
  gap: 6px;
  justify-content: center;
  flex-wrap: nowrap;
}
.kb-key {
  min-width: 36px;
}
.wide {
  min-width: 70px;
}

/* NYT-ish colors */
.correct {
  background: #6aaa64 !important;
  color: white !important;
}
.present {
  background: #c9b458 !important;
  color: white !important;
}
.absent {
  background: #787c7e !important;
  color: white !important;
}
</style>
