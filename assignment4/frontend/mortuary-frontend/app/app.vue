<template>
  <div>
    <nav class="navbar">
      <div class="nav-left">
        <NuxtLink to="/" class="brand">MortuaryAssist</NuxtLink>
      </div>

      <div class="nav-links">
        <NuxtLink to="/" class="nav-item">Home</NuxtLink>
        <NuxtLink to="/public" class="nav-item">Cases</NuxtLink>

        <!-- If NOT logged in -->
        <NuxtLink
          v-if="!isLoggedIn"
          to="/login"
          class="nav-item"
        >
          Login
        </NuxtLink>

        <!-- If logged in -->
        <button
          v-else
          class="nav-item logout-btn"
          @click="handleLogout"
        >
          Log Out
        </button>
      </div>
    </nav>

    <NuxtPage />
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from "vue"
import { useRouter } from "vue-router"

const router = useRouter()
const user = ref(null)

// computed login state
const isLoggedIn = computed(() => !!user.value)

async function handleLogout() {
  user.value = null
  router.push("/")
}

async function logout() {
  user.value = null
}
</script>

<style scoped>
.navbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px;
  background-color: #111827;
  color: white;
}

.brand {
  font-weight: bold;
  font-size: 18px;
  text-decoration: none;
  color: white;
}

.nav-links {
  display: flex;
  gap: 16px;
  align-items: center;
}

.nav-item {
  text-decoration: none;
  color: white;
  padding: 6px 10px;
  border-radius: 6px;
  background: none;
  border: none;
  cursor: pointer;
}

.nav-item:hover {
  background-color: #1f2937;
}

.logout-btn {
  font: inherit;
}
</style>