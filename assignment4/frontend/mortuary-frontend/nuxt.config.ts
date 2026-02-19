// nuxt.config.ts
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: { enabled: true },

  // ✅ Dev-only proxy (Vite): /api/* -> ASP.NET backend on 5112
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