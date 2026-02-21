// composables/useAuth.ts
import { computed } from "vue"
import { useState } from "nuxt/app"
import { $fetch } from "ofetch"

type AuthResponse = {
  token: string
  expiresAtUtc: string
  email: string
  displayName?: string | null
  roles: string[]
}

type MeResponse = {
  email: string
  displayName?: string | null
  roles?: string[]
}

const TOKEN_KEY = "ma_token"

function isClient() {
  return typeof window !== "undefined"
}

export function useAuth() {
  // global reactive auth state (shared across app)
  const token = useState<string | null>("auth_token", () => null)
  const roles = useState<string[]>("auth_roles", () => [])
  const user = useState<MeResponse | null>("auth_user", () => null)

  const isLoggedIn = computed(() => !!token.value)

  function loadFromStorage() {
    if (!isClient()) return
    token.value = localStorage.getItem(TOKEN_KEY)
  }

  function setToken(t: string | null) {
    token.value = t
    if (!isClient()) return

    if (t) localStorage.setItem(TOKEN_KEY, t)
    else localStorage.removeItem(TOKEN_KEY)
  }

  function setUserFromAuthResponse(res: AuthResponse) {
    user.value = {
      email: res.email,
      displayName: res.displayName ?? null,
      roles: res.roles ?? [],
    }
    roles.value = res.roles ?? []
  }

  async function login(email: string, password: string) {
    const res = await $fetch<AuthResponse>("/api/auth/login", {
      method: "POST",
      body: { email, password },
    })

    setToken(res.token)
    setUserFromAuthResponse(res)
    return res
  }

  async function register(email: string, password: string, displayName?: string) {
    const res = await $fetch<AuthResponse>("/api/auth/register", {
      method: "POST",
      body: { email, password, displayName },
    })

    setToken(res.token)
    setUserFromAuthResponse(res)
    return res
  }

  async function fetchMe() {
    if (!token.value) {
      user.value = null
      roles.value = []
      return null
    }

    try {
      const me = await $fetch<MeResponse>("/api/auth/me", {
        headers: { Authorization: `Bearer ${token.value}` },
      })

      user.value = me
      roles.value = me.roles ?? roles.value ?? []
      return me
    } catch {
      // token might be expired/invalid
      logout()
      return null
    }
  }

  function logout() {
    setToken(null)
    roles.value = []
    user.value = null
  }

  /**
   * Optional helper:
   * Call this once at app start to restore auth state.
   * - loads token from localStorage
   * - if token exists, fetches /me to populate user
   */
  async function initAuth() {
    loadFromStorage()
    if (token.value) await fetchMe()
  }

  return {
    token,
    roles,
    user,
    isLoggedIn,
    initAuth,
    loadFromStorage,
    login,
    register,
    fetchMe,
    logout,
  }
}