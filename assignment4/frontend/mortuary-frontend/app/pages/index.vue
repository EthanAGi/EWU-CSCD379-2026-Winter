<template>
  <main class="wrap">
    <p class="muted">Redirecting to Public Case Board…</p>
  </main>
</template>

<script setup lang="ts">
import { onMounted } from "vue"
import { navigateTo } from "#imports"

onMounted(() => {
  navigateTo("/public", { replace: true })
})
</script>

<style scoped>
.wrap {
  max-width: 900px;
  margin: 0 auto;
  padding: 16px;
}

.muted {
  opacity: 0.7;
}
</style>