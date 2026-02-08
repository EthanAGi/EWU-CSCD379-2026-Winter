// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  ssr: true,
  devtools: { enabled: process.env.NODE_ENV !== 'production' },

  runtimeConfig: {
    // server-only secrets go here (NOT public)
    // e.g. AZURE_SQL_CONNECTION_STRING is read directly from process.env in db.ts
    public: {},
  },

  // ✅ IMPORTANT:
  // Do NOT proxy /api/** when you are using Nitro server routes in server/api/**.
  nitro: {
    routeRules: {
      // Optional: prevent caching
      '/api/**': { cors: true },
    },
  },
})
