// nuxt.config.ts
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',

  // ✅ Static SPA output for Azure Static Web Apps
  ssr: false,

  devtools: { enabled: process.env.NODE_ENV !== 'production' },

  runtimeConfig: {
    public: {
      /**
       * ✅ Preferred:
       * - In production on SWA, default to "/api" (built-in Functions route)
       * - In dev, default to your local API
       * - Allow override via NUXT_PUBLIC_API_BASE
       */
      apiBase:
        process.env.NUXT_PUBLIC_API_BASE ??
        (process.env.NODE_ENV === 'production' ? '/api' : 'http://localhost:5072'),
    },
  },

  nitro: {
    preset: 'static', // generates to .output/public with index.html
  },
})
