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

function isNetworkBlockedError(e: any) {
  const msg = String(e?.message || "")
  return /failed to fetch/i.test(msg) || /networkerror/i.test(msg)
}

export function useAuth() {
  const config = useRuntimeConfig()

  const apiBase = computed(() => {
    const raw = (config.public.apiBase as unknown as string) || ""
    return raw.replace(/\/+$/, "")
  })

  function apiUrl(path: string) {
    const p = path.startsWith("/") ? path : `/${path}`
    return apiBase.value ? `${apiBase.value}${p}` : p
  }

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
    try {
      const res = await $fetch<AuthResponse>(apiUrl("/api/auth/login"), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: { email, password },
      })

      setToken(res.token)
      setUserFromAuthResponse(res)
      return res
    } catch (e: any) {
      if (isNetworkBlockedError(e)) {
        // This is almost always CORS or API down
        throw new Error(
          "Cannot reach API (likely blocked by CORS or API is down). Check API CORS to allow your Azure Static Web App domain."
        )
      }
      throw e
    }
  }

  async function register(email: string, password: string, displayName?: string) {
    try {
      const res = await $fetch<AuthResponse>(apiUrl("/api/auth/register"), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: { email, password, displayName },
      })

      setToken(res.token)
      setUserFromAuthResponse(res)
      return res
    } catch (e: any) {
      if (isNetworkBlockedError(e)) {
        throw new Error(
          "Cannot reach API (likely blocked by CORS or API is down). Check API CORS to allow your Azure Static Web App domain."
        )
      }
      throw e
    }
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