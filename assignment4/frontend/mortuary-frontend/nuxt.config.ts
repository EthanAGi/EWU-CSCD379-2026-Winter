// nuxt.config.ts
import { defineNuxtConfig } from 'nuxt/config'

export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: { enabled: true },

  // ✅ Azure Static Web Apps: generate static site
  ssr: false,

  // ✅ Use this in your API calls in production:
  // const api = useRuntimeConfig().public.apiBase
  runtimeConfig: {
    public: {
      apiBase: process.env.NUXT_PUBLIC_API_BASE || '',
    },
  },

  /**
   * ✅ DEV proxy only (fixes CORS during local dev)
   * This is enough for your setup and avoids the 'nitro' type error.
   */
  vite: {
    server: {
      proxy: {
        '/api': {
          target: 'http://localhost:5112',
          changeOrigin: true,
          secure: false,
        },
      },
    },
  },
})