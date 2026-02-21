<template>
  <main class="wrap">
    <h1>Create account</h1>

    <form class="card" @submit.prevent="onSubmit">
      <label>Email</label>
      <input v-model="email" type="email" required />

      <label>Display name (optional)</label>
      <input v-model="displayName" type="text" />

      <label>Password</label>
      <input v-model="password" type="password" required />

      <button :disabled="loading" type="submit">
        {{ loading ? "Creating..." : "Create account" }}
      </button>

      <p v-if="err" class="error">{{ err }}</p>
      <p class="muted">
        Already have an account? <NuxtLink to="/login">Log in</NuxtLink>
      </p>
    </form>
  </main>
</template>

<script setup lang="ts">
import { ref } from "vue"
import { useRouter } from "vue-router"
import { useAuth } from "../../composables/useAuth"

const router = useRouter()
const { register } = useAuth()

const email = ref("")
const password = ref("")
const displayName = ref("")

const loading = ref(false)
const err = ref<string | null>(null)

async function onSubmit() {
  err.value = null
  loading.value = true
  try {
    await register(email.value, password.value, displayName.value || undefined)
    await router.push("/public")
  } catch (e: any) {
    err.value = e?.data?.message || e?.message || "Registration failed."
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