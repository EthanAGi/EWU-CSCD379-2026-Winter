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
      <v-btn size="small" class="kb-key wide" @click="emit('press', 'ENTER')">
        Enter
      </v-btn>

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
/* ✅ Take full available width and allow scaling down */
.kb {
  width: 100%;
  max-width: 520px;
  margin-inline: auto;
  display: grid;
  gap: 10px;

  /* avoids padding/gaps causing overflow */
  box-sizing: border-box;
  padding-inline: 6px;
}

/* ✅ Keep rows single-line but let keys shrink */
.kb-row {
  display: flex;
  gap: 6px;
  justify-content: center;
  flex-wrap: nowrap;
  width: 100%;
}

/*
  ✅ The key to responsiveness:
  - flex: 1 1 0 => all keys share available space evenly
  - min-width: 0 => allows shrinking smaller than content width
*/
.kb-key {
  flex: 1 1 0;
  min-width: 0;

  /* make height and font responsive */
  height: clamp(38px, 6.5vh, 48px);
  font-size: clamp(0.72rem, 2.2vw, 0.95rem);

  /* reduce default Vuetify button padding so they fit */
  padding-inline: 0 !important;

  /* prevent the label from forcing width */
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* wide keys get more share, but still shrink if needed */
.wide {
  flex: 1.6 1 0;
}

/* Slightly tighter on very small screens */
@media (max-width: 360px) {
  .kb {
    gap: 8px;
    padding-inline: 4px;
  }
  .kb-row {
    gap: 4px;
  }
  .kb-key {
    height: clamp(34px, 6.2vh, 44px);
    font-size: clamp(0.68rem, 2.6vw, 0.9rem);
  }
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
