// nuxt.config.ts
import { defineNuxtConfig } from 'nuxt/config'

export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: { enabled: true },

  // ✅ Static site for Azure Static Web Apps
  ssr: false,

  // Make sure runtime config is available
  runtimeConfig: {
    public: {
      // Set this in Azure Static Web App settings:
      // NUXT_PUBLIC_API_BASE = https://mortuaryassist-api-bgb7g0ffbtd2hpe9.eastus2-01.azurewebsites.net
      apiBase: process.env.NUXT_PUBLIC_API_BASE || '',
    },
  },

  /**
   * ✅ Local DEV only
   * Lets you call /api/* locally without CORS.
   * In production, apiBase should be set and the app will call the full API host.
   */
  vite: {
    server: {
      proxy: {
        '/api': {
          target: 'http://localhost:5112',
          changeOrigin: true,
          secure: false,
        },
      },
    },
  },

  // Optional but often helpful for static hosting
  app: {
    // If you later host under a subpath, you'd set baseURL here.
    baseURL: '/',
  },
})