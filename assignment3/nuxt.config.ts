// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',

  // We are deploying as a Node server (Azure App Service), not static.
  ssr: true,

  // Avoid shipping devtools in production
  devtools: { enabled: process.env.NODE_ENV !== 'production' },

  // Put runtime config here so Azure can inject values as environment variables.
  runtimeConfig: {
    /**
     * Server-only values (NOT exposed to the browser).
     * Put secrets here if you ever need them in SSR routes/server routes.
     */
    // apiSecret: process.env.API_SECRET,

    /**
     * Public values (exposed to the browser).
     * Azure App Service → Configuration → Application settings:
     *   NUXT_PUBLIC_API_BASE = https://<your-api>.azurewebsites.net
     *
     * In dev you can set:
     *   NUXT_PUBLIC_API_BASE = http://localhost:5000
     */
    public: {
      apiBase: process.env.NUXT_PUBLIC_API_BASE || 'http://localhost:5000'
    }
  },

  /**
   * Azure runs behind a proxy and terminates HTTPS before forwarding to your node app.
   * This helps Nuxt generate correct absolute URLs (and not think it's http).
   */
  nitro: {
    // Trust X-Forwarded-* headers set by Azure
    routeRules: {},
  },

  /**
   * These headers can be useful if you later add cookies/auth.
   * Nuxt/Nitro generally handles this well, but keeping it explicit is nice.
   */
  app: {
    // If you later deploy under a sub-path, set baseURL here.
    // baseURL: '/'
  }
})
