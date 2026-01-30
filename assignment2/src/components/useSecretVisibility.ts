// src/composables/useSecretVisibility.ts
import { onMounted, onBeforeUnmount, ref } from "vue";

const APPEAR_INTERVAL_MS = 15 * 1000;
const VISIBLE_DURATION_MS = 4 * 1000;

export function useSecretVisibility() {
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

  return { showSecret };
}
