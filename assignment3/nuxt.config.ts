// nuxt.config.ts
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  ssr: false,

  devtools: { enabled: process.env.NODE_ENV !== 'production' },

  runtimeConfig: {
    public: {
      apiBase:
        process.env.NUXT_PUBLIC_API_BASE ??
        (process.env.NODE_ENV === 'production' ? '' : 'http://localhost:5072'),
    },
  },

  nitro: {
    preset: 'static',
  },
})
