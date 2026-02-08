// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',

  // We are deploying as a Node server (Azure App Service), not static.
  ssr: true,

  // Avoid shipping devtools in production
  devtools: { enabled: process.env.NODE_ENV !== 'production' },

  // Runtime config allows Azure + local dev to inject environment variables
  runtimeConfig: {
    /**
     * Server-only values (NOT exposed to the browser).
     * Put secrets here if you ever need them in SSR routes/server routes.
     */
    // apiSecret: process.env.API_SECRET,

    /**
     * Public values (exposed to the browser).
     *
     * Azure App Service → Configuration → Application settings:
     *   NUXT_PUBLIC_API_BASE = https://<your-api>.azurewebsites.net
     *
     * Local development default:
     *   http://localhost:5072
     */
    public: {
      apiBase: process.env.NUXT_PUBLIC_API_BASE || 'http://localhost:5072',
    },
  },

  /**
   * Azure runs behind a proxy and terminates HTTPS before forwarding to your node app.
   * This ensures Nuxt generates correct absolute URLs.
   */
  nitro: {
    /**
     * OPTIONAL BUT STRONGLY RECOMMENDED:
     * Proxy `/api/**` requests from Nuxt → .NET API.
     *
     * Result:
     *   Browser → Nuxt (3000) → .NET API (5072)
     *
     * This avoids CORS entirely.
     */
    routeRules: {
      '/api/**': {
        proxy: `${process.env.NUXT_PUBLIC_API_BASE || 'http://localhost:5072'}/api/**`,
      },
    },
  },

  /**
   * App-level configuration
   */
  app: {
    // If you later deploy under a sub-path, set baseURL here.
    // baseURL: '/'
  },
})
