// types/nuxt.d.ts
import type { $Fetch } from 'ofetch'

declare module '#app' {
  interface NuxtApp {
    $authedFetch: $Fetch
  }
}

declare module 'vue' {
  interface ComponentCustomProperties {
    $authedFetch: $Fetch
  }
}

export {}