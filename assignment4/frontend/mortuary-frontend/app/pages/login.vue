<template>
  <main class="wrap">
    <h1>Log in</h1>

    <form class="card" @submit.prevent="onSubmit">
      <label>Email</label>
      <input v-model="email" type="email" required />

      <label>Password</label>
      <input v-model="password" type="password" required />

      <button :disabled="loading" type="submit">
        {{ loading ? "Logging in..." : "Log in" }}
      </button>

      <p v-if="err" class="error">{{ err }}</p>
      <p class="muted">
        Need an account? <NuxtLink to="/register">Register</NuxtLink>
      </p>
    </form>
  </main>
</template>

<script setup lang="ts">
import { ref } from "vue"
import { useRouter } from "vue-router"
import { useAuth } from "../../composables/useAuth"

const router = useRouter()
const { login, fetchMe } = useAuth()

const email = ref("")
const password = ref("")
const loading = ref(false)
const err = ref<string | null>(null)

async function onSubmit() {
  err.value = null
  loading.value = true
  try {
    // ✅ login() already uses the new apiUrl(...) logic inside useAuth
    await login(email.value, password.value)

    // ✅ populate roles/user right away (helps gated pages)
    await fetchMe()

    await router.push("/public")
  } catch (e: any) {
    // ofetch errors often put server payload on e.data
    err.value = e?.data?.message || e?.message || "Login failed."
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.wrap { max-width: 900px; margin: 0 auto; padding: 16px; }
.card { display: grid; gap: 8px; max-width: 420px; padding: 12px; border: 1px solid #ddd; border-radius: 8px; }
input { padding: 8px; border: 1px solid #ccc; border-radius: 6px; }
button { padding: 10px; border-radius: 6px; border: 1px solid #333; }
.error { color: #b00020; }
.muted { opacity: 0.7; }
</style>