// plugins/api-auth.ts
import { defineNuxtPlugin, useRuntimeConfig } from 'nuxt/app'
import { $fetch, type FetchContext } from 'ofetch'
import { useAuth } from '../composables/useAuth'

export default defineNuxtPlugin(() => {
  const config = useRuntimeConfig()
  const apiBase = (typeof config.public.apiBase === 'string' ? config.public.apiBase : '').replace(/\/+$/, '') // trim trailing /

  const { token, loadFromStorage } = useAuth()

  // Guard: only touch localStorage on client
  if (import.meta.client) {
    loadFromStorage()
  }

  // ✅ Non-authed API client (uses apiBase in prod)
  const api = $fetch.create({
    baseURL: apiBase || undefined, // if empty, it will behave like normal relative fetch
  })

  // ✅ Authed API client (baseURL + Authorization)
  const authedApi = $fetch.create({
    baseURL: apiBase || undefined,
    onRequest(ctx: FetchContext) {
      if (!token.value) return

      const headers = new Headers(ctx.options.headers as HeadersInit | undefined)
      headers.set('Authorization', `Bearer ${token.value}`)
      ctx.options.headers = headers
    },
  })

  return {
    provide: {
      api,        // use as: const { $api } = useNuxtApp()
      authedApi,  // use as: const { $authedApi } = useNuxtApp()
    },
  }
})