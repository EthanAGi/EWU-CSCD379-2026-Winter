// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',

  // We are deploying as a Node server (Azure App Service), not static.
  ssr: true,

  // Avoid shipping devtools in production
  devtools: { enabled: process.env.NODE_ENV !== 'production' },

  runtimeConfig: {
    /**
     * Public values (exposed to the browser).
     *
     * In production, you MUST set this in Azure App Service → Configuration → Application settings:
     *   NUXT_PUBLIC_API_BASE = https://<your-backend-api-host>
     *
     * Local dev fallback:
     *   http://localhost:5072
     */
    public: {
      apiBase: process.env.NUXT_PUBLIC_API_BASE || 'http://localhost:5072',

      /**
       * This is what your FRONTEND should use when calling the API.
       * We want same-origin calls in production to avoid CORS:
       *   browser -> https://assignment3.../api/...
       *
       * In local dev, you can still use the backend directly if you want.
       */
      apiClientBase:
        process.env.NODE_ENV === 'production' ? '' : (process.env.NUXT_PUBLIC_API_BASE || 'http://localhost:5072'),
    },
  },

  /**
   * Proxy API calls through Nuxt to avoid CORS:
   * Browser calls /api/... (same origin)
   * Nuxt forwards to your backend at NUXT_PUBLIC_API_BASE
   */
  nitro: {
    routeRules: {
      '/api/**': {
        proxy: `${process.env.NUXT_PUBLIC_API_BASE || 'http://localhost:5072'}/api/**`,
      },
    },
  },
})
