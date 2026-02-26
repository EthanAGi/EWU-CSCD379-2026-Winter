<template>
  <main class="wrap">
    <h1>Log in</h1>

    <form class="card" @submit.prevent="onSubmit">
      <label for="email">Email</label>
      <input id="email" v-model.trim="email" type="email" autocomplete="email" required />

      <label for="pw">Password</label>
      <input id="pw" v-model="password" type="password" autocomplete="current-password" required />

      <button :disabled="loading" type="submit">
        {{ loading ? "Logging in..." : "Log in" }}
      </button>

      <p v-if="err" class="error">{{ err }}</p>

      <p class="muted">
        Need an account?
        <NuxtLink to="/register">Create account</NuxtLink>
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
    await login(email.value, password.value)

    const me = await fetchMe()
    if (!me) {
      err.value =
        "Login succeeded, but token validation failed on /api/auth/me (401). This is almost always a JWT Issuer/Audience mismatch on the API."
      return
    }

    await router.replace("/public")
  } catch (e: any) {
    const status = e?.status || e?.response?.status
    err.value = e?.data?.message || e?.message || (status === 401 ? "Invalid email/password." : "Login failed.")
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.wrap {
  max-width: 900px;
  margin: 0 auto;
  padding: 16px;
}
.card {
  display: grid;
  gap: 10px;
  max-width: 420px;
  padding: 14px;
  border: 1px solid #ddd;
  border-radius: 10px;
}
label {
  font-weight: 600;
}
input {
  padding: 10px;
  border: 1px solid #ccc;
  border-radius: 8px;
}
button {
  padding: 10px;
  border-radius: 8px;
  border: 1px solid #333;
  cursor: pointer;
}
button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
.error {
  color: #b00020;
}
.muted {
  opacity: 0.7;
}
</style>