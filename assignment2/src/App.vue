<script setup lang="ts">
import { RouterView } from "vue-router";
import { onMounted, onBeforeUnmount, ref } from "vue";

/**
 * Timing constants (same behavior as HomeView)
 */
const APPEAR_INTERVAL_MS = 15 * 1000;
const VISIBLE_DURATION_MS = 4 * 1000;

const showSecret = ref(false);

let intervalId: number | null = null;
let hideTimeoutId: number | null = null;
let firstTimeoutId: number | null = null;

function showSecretTemporarily() {
  showSecret.value = true;

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
  firstTimeoutId = window.setTimeout(() => {
    showSecretTemporarily();

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

<template>
  <v-app>
    <v-main>
      <v-container class="py-6">
        <!-- 🔹 NAV BAR -->
        <div class="d-flex ga-2 mb-6 align-center">
          <v-btn to="/" variant="outlined">Home</v-btn>
          <v-btn to="/game" color="primary">Game</v-btn>

          <!-- 🔒 Secret nav button -->
          <transition name="fade">
            <v-btn
              v-if="showSecret"
              to="/secret"
              color="secondary"
              class="secret-btn"
              variant="outlined"
            >
              Secret
            </v-btn>
          </transition>
        </div>

        <RouterView />
      </v-container>
    </v-main>
  </v-app>
</template>

<style scoped>
/* ✨ Fade animation */
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

.secret-btn {
  position: relative;

  animation:
    secret-pulse 1.15s ease-in-out infinite,
    secret-whisper 1.4s ease-in-out infinite,
    secret-shake 3.2s ease-in-out infinite;
}

/* Pulse */
@keyframes secret-pulse {
  0%   { transform: scale(1); }
  50%  { transform: scale(1.05); }
  100% { transform: scale(1); }
}

/* Whisper glow */
@keyframes secret-whisper {
  0% {
    filter: drop-shadow(0 0 0 rgba(255, 255, 255, 0));
    text-shadow: 0 0 0 rgba(255, 255, 255, 0);
  }
  50% {
    filter: drop-shadow(0 0 10px rgba(255, 255, 255, 0.35));
    text-shadow: 0 0 12px rgba(255, 255, 255, 0.35);
  }
  100% {
    filter: drop-shadow(0 0 0 rgba(255, 255, 255, 0));
    text-shadow: 0 0 0 rgba(255, 255, 255, 0);
  }
}

/* Shake (brief, not constant) */
@keyframes secret-shake {
  0%, 84%, 100% { transform: translateX(0) scale(1); }

  86% { transform: translateX(-2px) rotate(-1deg) scale(1.03); }
  88% { transform: translateX(2px) rotate(1deg) scale(1.03); }
  90% { transform: translateX(-2px) rotate(-1deg) scale(1.03); }
  92% { transform: translateX(2px) rotate(1deg) scale(1.03); }
  94% { transform: translateX(0) rotate(0deg) scale(1.03); }
}
</style>
