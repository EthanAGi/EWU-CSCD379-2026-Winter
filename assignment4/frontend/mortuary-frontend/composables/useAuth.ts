// composables/useAuth.ts
import { computed } from "vue"
import { useState, useRuntimeConfig } from "#imports"
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
  // ✅ Nuxt runtime config (typed more reliably via #imports)
  const config = useRuntimeConfig()

  // ✅ Force apiBase to be treated as a string (fixes TS: '{}' has no endsWith/slice)
  const apiBase = computed(() => {
    const raw = (config.public.apiBase as unknown as string) || ""
    return raw.replace(/\/+$/, "") // trim trailing slashes
  })

  /**
   * Build an API URL that works in BOTH environments:
   * - DEV: apiBase == ""  -> "/api/..."
   * - PROD: apiBase == "https://<api-app-service>" -> "https://<api-app-service>/api/..."
   */
  function apiUrl(path: string) {
    const p = path.startsWith("/") ? path : `/${path}`
    return apiBase.value ? `${apiBase.value}${p}` : p
  }

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
    const res = await $fetch<AuthResponse>(apiUrl("/api/auth/login"), {
      method: "POST",
      body: { email, password },
    })

    setToken(res.token)
    setUserFromAuthResponse(res)
    return res
  }

  async function register(email: string, password: string, displayName?: string) {
    const res = await $fetch<AuthResponse>(apiUrl("/api/auth/register"), {
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
      const me = await $fetch<MeResponse>(apiUrl("/api/auth/me"), {
        headers: { Authorization: `Bearer ${token.value}` },
      })

      user.value = me
      roles.value = me.roles ?? roles.value ?? []
      return me
    } catch {
      logout()
      return null
    }
  }

  function logout() {
    setToken(null)
    roles.value = []
    user.value = null
  }

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