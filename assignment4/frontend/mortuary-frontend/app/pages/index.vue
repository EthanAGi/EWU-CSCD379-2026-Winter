<template>
  <main class="wrap">
    <h1>Mortuary Assistant</h1>
    <p>Welcome. Go to the public case board:</p>

    <NuxtLink class="link" to="/public">Open Public Case Board →</NuxtLink>
  </main>
</template>

<style scoped>
.wrap { max-width: 900px; margin: 0 auto; padding: 16px; }
.link { display: inline-block; margin-top: 12px; text-decoration: underline; }
</style>
