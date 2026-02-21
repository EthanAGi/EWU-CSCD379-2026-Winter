<template>
  <div>
    <nav class="navbar">
      <div class="nav-left">
        <NuxtLink to="/" class="brand">MortuaryAssist</NuxtLink>
      </div>

      <div class="nav-links">
        <NuxtLink to="/" class="nav-item">Home</NuxtLink>

        <!-- Public case board (everyone) -->
        <NuxtLink to="/public" class="nav-item">Public Board</NuxtLink>

        <!-- Cases page (Admin OR Mortician) -->
        <NuxtLink v-if="canSeeCases" to="/cases" class="nav-item">Cases</NuxtLink>

        <!-- Only show Account if logged in AND (Admin OR Mortician) -->
        <NuxtLink v-if="canSeeAccount" to="/account" class="nav-item">Account</NuxtLink>

        <!-- If NOT logged in -->
        <NuxtLink v-if="!isLoggedIn" to="/login" class="nav-item">
          Login
        </NuxtLink>

        <!-- If logged in -->
        <button v-else class="nav-item logout-btn" type="button" @click="handleLogout">
          Log Out
        </button>
      </div>
    </nav>

    <NuxtPage />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from "vue"
import { useRouter } from "vue-router"
import { useAuth } from "../composables/useAuth"

const router = useRouter()
const { user, roles, logout, initAuth } = useAuth()

onMounted(() => {
  initAuth()
})

const isLoggedIn = computed(() => !!user.value)
const isAdmin = computed(() => roles.value?.includes("Admin"))
const isMortician = computed(() => roles.value?.includes("Mortician"))

const canSeeAccount = computed(() => isLoggedIn.value && (isAdmin.value || isMortician.value))
const canSeeCases = computed(() => isLoggedIn.value && (isAdmin.value || isMortician.value))

function handleLogout() {
  logout()
  router.push("/")
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