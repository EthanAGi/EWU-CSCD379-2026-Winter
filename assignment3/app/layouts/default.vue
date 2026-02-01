<script setup lang="ts">
const route = useRoute()
const { player } = usePlayerState()

const links = [
  { to: '/stable', label: 'Stable' },
  { to: '/gauntlet', label: 'Gauntlet' },
  { to: '/shop', label: 'Shop' },
]

const gold = computed(() => player.value?.gold ?? 0)
function isActive(to: string) { return route.path === to }
</script>

<template>
  <div class="page">
    <header class="nav">
      <NuxtLink to="/stable" class="brand">
        <span class="mark" aria-hidden="true" />
        <span class="title">Stable Run</span>
      </NuxtLink>

      <div class="right">
        <span v-if="player" class="pill">💰 {{ gold }}</span>

        <nav class="links">
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
      </div>
    </header>

    <main class="container">
      <slot />
    </main>
  </div>
</template>

<style scoped>
.page{
  min-height:100vh;
  background:
    radial-gradient(900px 500px at 20% 10%, rgba(124,92,255,.22), transparent 60%),
    radial-gradient(900px 500px at 80% 30%, rgba(53,214,197,.18), transparent 60%),
    #0b1020;
  color: rgba(255,255,255,.92);
}
.nav{
  position:sticky; top:0; z-index:20;
  display:flex; justify-content:space-between; align-items:center; gap:12px;
  padding:14px 16px;
  border-bottom:1px solid rgba(255,255,255,.12);
  background: rgba(11,16,32,.72);
  backdrop-filter: blur(10px);
}
.brand{ display:inline-flex; align-items:center; gap:10px; text-decoration:none; color:inherit; font-weight:900; }
.mark{ width:28px; height:28px; border-radius:10px; background: linear-gradient(135deg,#7c5cff,#35d6c5); }
.right{ display:flex; gap:10px; align-items:center; flex-wrap:wrap; justify-content:flex-end; }
.pill{
  border:1px solid rgba(255,255,255,.12);
  background: rgba(255,255,255,.05);
  padding:6px 10px; border-radius:999px; font-size:12px;
  color: rgba(255,255,255,.8);
}
.links{ display:flex; gap:8px; flex-wrap:wrap; }
.link{
  text-decoration:none;
  color: rgba(255,255,255,.82);
  padding:8px 12px;
  border-radius:999px;
  border:1px solid rgba(255,255,255,.12);
  background: rgba(255,255,255,.04);
}
.link.active{
  color:#0b1020; font-weight:900; border:none;
  background: linear-gradient(90deg,#7c5cff,#35d6c5);
}
.container{ width:min(980px, calc(100% - 32px)); margin:0 auto; padding:22px 0 42px; }
</style>
