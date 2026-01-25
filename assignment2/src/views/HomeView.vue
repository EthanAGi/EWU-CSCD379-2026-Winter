<template>
  <v-container>
    <h1>Welcome to the Gamer Zone</h1>

    <v-btn color="primary" class="mt-4" to="/game">
      Go to Game
    </v-btn>

    <!-- 🔒 Secret button (hidden most of the time) -->
    <transition name="fade">
      <v-btn
        v-if="showSecret"
        color="secondary"
        class="mt-4 ml-4 secret-btn"
        to="/secret"
      >
        Secret
      </v-btn>
    </transition>
  </v-container>
</template>

<script setup lang="ts">
import { onMounted, onBeforeUnmount, ref } from "vue";

/**
 * Timing constants
 */
const APPEAR_INTERVAL_MS = 15 * 1000; // shows every 15 seconds
const VISIBLE_DURATION_MS = 8 * 1000; // stays visible for 4 seconds

const showSecret = ref(false);

let intervalId: number | null = null;
let hideTimeoutId: number | null = null;
let firstTimeoutId: number | null = null;

function showSecretTemporarily() {
  showSecret.value = true;

  // clear any previous hide timeout
  if (hideTimeoutId !== null) {
    clearTimeout(hideTimeoutId);
    hideTimeoutId = null;
  }

  hideTimeoutId = window.setTimeout(() => {
    showSecret.value = false;
    hideTimeoutId = null;
  }, VISIBLE_DURATION_MS);
}

onMounted(() => {
  // show once after interval
  firstTimeoutId = window.setTimeout(() => {
    showSecretTemporarily();

    // then repeat
    intervalId = window.setInterval(() => {
      showSecretTemporarily();
    }, APPEAR_INTERVAL_MS);
  }, APPEAR_INTERVAL_MS);
});

onBeforeUnmount(() => {
  if (intervalId !== null) clearInterval(intervalId);
  if (hideTimeoutId !== null) clearTimeout(hideTimeoutId);
  if (firstTimeoutId !== null) clearTimeout(firstTimeoutId);
});
</script>

<style scoped>
/* ✨ Fade animation (existing) */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.8s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

/* =========================
   Secret button effects
   ========================= */

/* Apply multiple animations at once */
.secret-btn {
  position: relative;

  /* pulse = subtle scale, whisper = glow */
  animation:
    secret-pulse 1.15s ease-in-out infinite,
    secret-whisper 1.4s ease-in-out infinite,
    secret-shake 3.2s ease-in-out infinite;
}

/* Pulse: small in/out */
@keyframes secret-pulse {
  0%   { transform: scale(2); }
  50%  { transform: scale(2.05); }
  100% { transform: scale(2); }
}

/* Whisper: soft glow that "breathes" */
@keyframes secret-whisper {
  0% {
    filter: drop-shadow(0 0 0px rgba(255, 255, 255, 0));
    text-shadow: 0 0 0px rgba(255, 255, 255, 0);
  }
  50% {
    filter: drop-shadow(0 0 10px rgba(255, 255, 255, 0.35));
    text-shadow: 0 0 12px rgba(255, 255, 255, 0.35);
  }
  100% {
    filter: drop-shadow(0 0 0px rgba(255, 255, 255, 0));
    text-shadow: 0 0 0px rgba(255, 255, 255, 0);
  }
}

/* Shake: only shakes briefly, then rests (so it's not annoying) */
@keyframes secret-shake {
  0%, 84%, 100% { transform: translateX(0) scale(1); }

  /* quick jitter burst */
  86% { transform: translateX(-2px) rotate(-1deg) scale(1.03); }
  88% { transform: translateX(2px) rotate(1deg) scale(1.03); }
  90% { transform: translateX(-2px) rotate(-1deg) scale(1.03); }
  92% { transform: translateX(2px) rotate(1deg) scale(1.03); }
  94% { transform: translateX(0) rotate(0deg) scale(1.03); }
}
</style>
