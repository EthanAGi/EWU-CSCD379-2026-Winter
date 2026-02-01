<script setup lang="ts">
const route = useRoute()

const links = [
  { to: '/stable', label: 'Stable' },
  { to: '/gauntlet', label: 'Gauntlet' },
  { to: '/shop', label: 'Shop' },
]

function isActive(to: string) {
  // exact match only keeps it simple
  return route.path === to
}
</script>

<template>
  <div class="page">
    <header class="nav">
      <NuxtLink to="/stable" class="brand" aria-label="Go to Stable">
        <span class="brandMark" aria-hidden="true" />
        <span class="brandText">Stable Run</span>
      </NuxtLink>

      <nav class="links" aria-label="Primary navigation">
        <NuxtLink
          v-for="l in links"
          :key="l.to"
          :to="l.to"
          class="link"
          :class="{ active: isActive(l.to) }"
        >
          {{ l.label }}
        </NuxtLink>
      </nav>
    </header>

    <main class="container">
      <slot />
    </main>
  </div>
</template>

<style scoped>
.page {
  min-height: 100vh;
  background:
    radial-gradient(900px 500px at 20% 10%, rgba(124, 92, 255, 0.22), transparent 60%),
    radial-gradient(900px 500px at 80% 30%, rgba(53, 214, 197, 0.18), transparent 60%),
    #0b1020;
  color: rgba(255,255,255,0.92);
}

.nav {
  position: sticky;
  top: 0;
  z-index: 20;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 14px;
  padding: 14px 16px;
  border-bottom: 1px solid rgba(255,255,255,0.12);
  background: rgba(11,16,32,0.72);
  backdrop-filter: blur(10px);
}

.brand {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  text-decoration: none;
  color: inherit;
  font-weight: 800;
  letter-spacing: 0.2px;
}

.brandMark {
  width: 28px;
  height: 28px;
  border-radius: 10px;
  background: linear-gradient(135deg, #7c5cff, #35d6c5);
  box-shadow: 0 10px 28px rgba(124, 92, 255, 0.25);
}

.brandText {
  font-size: 14px;
  opacity: 0.95;
}

.links {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
  justify-content: flex-end;
}

.link {
  text-decoration: none;
  color: rgba(255,255,255,0.82);
  padding: 8px 12px;
  border-radius: 999px;
  border: 1px solid rgba(255,255,255,0.12);
  background: rgba(255,255,255,0.04);
  transition: transform 0.08s ease, background 0.15s ease, border-color 0.15s ease;
}

.link:hover {
  background: rgba(255,255,255,0.08);
  border-color: rgba(255,255,255,0.18);
}

.link:active {
  transform: translateY(1px);
}

.link.active {
  color: #0b1020;
  font-weight: 800;
  border: none;
  background: linear-gradient(90deg, #7c5cff, #35d6c5);
}

.container {
  width: min(980px, calc(100% - 32px));
  margin: 0 auto;
  padding: 22px 0 42px;
}

@media (max-width: 520px) {
  .brandText { display: none; }
}
</style>
