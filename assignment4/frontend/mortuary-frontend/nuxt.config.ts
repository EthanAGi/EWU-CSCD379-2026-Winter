// nuxt.config.ts
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: { enabled: true },

  // ✅ Recommended: Nitro dev proxy (Nuxt-side) for /api/* -> ASP.NET backend
  nitro: {
    devProxy: {
      '/api': {
        target: 'http://localhost:5112',
        changeOrigin: true,
        secure: false
      }
    }
  },

  // ✅ Optional: keep Vite proxy too (mostly helps if you directly call from browser/client)
  vite: {
    server: {
      proxy: {
        '/api': {
          target: 'http://localhost:5112',
          changeOrigin: true,
          secure: false
        }
      }
    }
  }
})