// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',

  // Azure App Service runs a Node server (not static hosting)
  ssr: true,

  // Avoid shipping devtools in production
  devtools: { enabled: process.env.NODE_ENV !== 'production' },

  runtimeConfig: {
    /**
     * Private values (server-only) could go here if needed
     * (ex: DB connection string). Do NOT put secrets in public.
     */
    public: {
      /**
       * Keep this only if you still use it somewhere (e.g. for non-/api calls).
       * Your browser code should prefer SAME-ORIGIN calls like:
       *   $fetch('/api/...')
       */
      apiBase: process.env.NUXT_PUBLIC_API_BASE || '',
    },
  },

  /**
   * ✅ IMPORTANT:
   * Do NOT proxy /api/** if you are using Nuxt server routes in server/api/**.
   * If you proxy /api/**, Nitro never runs your server/api handlers and you get 502.
   */
  nitro: {
    preset: 'node-server',
    routeRules: {
      // leave empty (or put non-/api rules here)
    },
  },
})
