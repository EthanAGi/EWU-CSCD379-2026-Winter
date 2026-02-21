// plugins/api-auth.ts
import { defineNuxtPlugin } from 'nuxt/app'
import { $fetch, type FetchContext } from 'ofetch'
import { useAuth } from '../composables/useAuth'

export default defineNuxtPlugin(() => {
  const { token, loadFromStorage } = useAuth()

  // Your composable guards localStorage access (client-only)
  loadFromStorage()

  const authedFetch = $fetch.create({
    onRequest(ctx: FetchContext) {
      if (!token.value) return

      // ofetch headers can be: Headers | Record<string,string> | [string,string][]
      // To satisfy TS (and be safe at runtime), always normalize to a Headers object.
      const headers = new Headers(ctx.options.headers as HeadersInit | undefined)
      headers.set('Authorization', `Bearer ${token.value}`)
      ctx.options.headers = headers
    },
  })

  return {
    provide: {
      authedFetch,
    },
  }
})