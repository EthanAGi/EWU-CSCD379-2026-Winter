// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',

  // Azure Static Web Apps expects a static client build
  ssr: false,

  devtools: { enabled: process.env.NODE_ENV !== 'production' },

  runtimeConfig: {
    public: {
      // One env var for both dev + prod.
      // Locally: NUXT_PUBLIC_API_BASE=http://localhost:5072
      // In Azure SWA: NUXT_PUBLIC_API_BASE=https://assignment3-b6dfeygfb0bgfgbr.eastus2-01.azurewebsites.net
      apiBase: process.env.NUXT_PUBLIC_API_BASE || 'http://localhost:5072',
    },
  },

  // Generate static output for SWA (outputs to .output/public)
  nitro: {
    preset: 'static',
  },
})
