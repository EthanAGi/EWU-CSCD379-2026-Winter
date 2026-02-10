<script setup lang="ts">
const route = useRoute()
const { player } = usePlayerState()

// ✅ Mobile menu state (MUST be declared before watch uses it)
const menuOpen = ref(false)
function toggleMenu() {
  menuOpen.value = !menuOpen.value
}
function closeMenu() {
  menuOpen.value = false
}

// ✅ BGM controller (persists because layout stays mounted)
const { initOnce, setZoneFromRoute, setEnabled, enabled, switching, volume, setVolume } = useBgm()

onMounted(() => {
  // sets up the browser “unlock audio on first click/keydown”
  initOnce()
})

// Watch navigation and switch ONLY when route demands it (stable/gauntlet)
watch(
  () => route.path,
  (p) => {
    setZoneFromRoute(p)
    // ✅ close mobile menu on navigation
    menuOpen.value = false
  },
  { immediate: true }
)

// ✅ No Home link anywhere.
// ✅ Only show game pages when a player exists (prevents weird navigation before onboarding)
const links = computed(() => {
  const base = [{ to: '/reviews', label: 'Reviews' }]
  const game = [
    { to: '/stable', label: 'Stable' },
    { to: '/gauntlet', label: 'Gauntlet' },
    { to: '/shop', label: 'Shop' },
  ]
  return player.value ? [...base, ...game] : base
})

const gold = computed(() => player.value?.gold ?? 0)

function isActive(to: string) {
  // ✅ make active highlight work for nested routes too (ex: /shop/whatever)
  return route.path === to || route.path.startsWith(to + '/')
}

function onBrandClick(e: MouseEvent) {
  // ✅ Prevent any navigation to "/" via the brand.
  e.preventDefault()
  // If a player exists, take them to Stable. Otherwise keep them on the current page.
  if (player.value) navigateTo('/stable')
}

function toggleMusic() {
  setEnabled(!enabled.value)
}

function onVolumeInput(v: number) {
  setVolume(v)
}
</script>

<template>
  <div class="page">
    <header class="nav">
      <!-- ✅ Brand no longer routes to Home -->
      <NuxtLink to="/stable" class="brand" @click="onBrandClick">
        <!-- ✅ Replace the old gradient square with your existing favicon asset from /public -->
        <!-- Change this to whatever file you actually have: /favicon.ico, /favicon.svg, etc -->
        <img
          src="/favicon.ico"
          alt="Stable Run"
          class="brandIcon"
        />
        <span class="title">Stable Run</span>
      </NuxtLink>

      <div class="right">
        <span v-if="player" class="pill goldPill">💰 {{ gold }}</span>

        <!-- ✅ Minimal music controls -->
        <div class="audio" aria-label="Music controls">
          <button
            class="iconBtn"
            type="button"
            @click="toggleMusic"
            :aria-label="enabled ? 'Mute music' : 'Unmute music'"
            :title="enabled ? 'Mute' : 'Unmute'"
          >
            <span class="icon" aria-hidden="true">{{ enabled ? '🔊' : '🔇' }}</span>
          </button>

          <input
            class="slider"
            type="range"
            min="0"
            max="1"
            step="0.01"
            :value="volume"
            @input="onVolumeInput(parseFloat(($event.target as HTMLInputElement).value))"
            aria-label="Music volume"
          />

          <span v-if="switching" class="dot" aria-hidden="true">•</span>
        </div>

        <!-- ✅ Desktop links -->
        <nav class="links desktopLinks" aria-label="Primary navigation">
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

        <!-- ✅ Mobile hamburger -->
        <button
          class="hamburger"
          type="button"
          @click="toggleMenu"
          :aria-expanded="menuOpen ? 'true' : 'false'"
          aria-label="Toggle menu"
          title="Menu"
        >
          <span class="hamburgerLines" aria-hidden="true">
            <span />
            <span />
            <span />
          </span>
        </button>
      </div>

      <!-- ✅ Mobile dropdown menu -->
      <Transition name="menu">
        <div v-if="menuOpen" class="mobileMenuWrap">
          <button class="backdrop" type="button" @click="closeMenu" aria-label="Close menu" />

          <nav class="mobileMenu" aria-label="Mobile navigation">
            <NuxtLink
              v-for="l in links"
              :key="l.to"
              :to="l.to"
              class="mobileLink"
              :class="{ active: isActive(l.to) }"
              @click="closeMenu"
            >
              {{ l.label }}
            </NuxtLink>
          </nav>
        </div>
      </Transition>
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
  display:flex;
  justify-content:space-between;
  align-items:center;
  gap:12px;
  padding:12px 14px;

  border-bottom: none;

  background: rgba(11,16,32,.72);
  backdrop-filter: blur(10px);
}

/* Brand */
.brand{
  display:inline-flex;
  align-items:center;
  gap:10px;
  text-decoration:none;
  color:inherit;
  font-weight:900;
  min-width: 0;
}

/* ✅ favicon image used as brand mark */
.brandIcon{
  width:28px;
  height:28px;
  border-radius:10px;
  object-fit:contain;
  flex: 0 0 auto;
}

.title{
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* Right group */
.right{
  display:flex;
  gap:10px;
  align-items:center;
  flex-wrap:wrap;
  justify-content:flex-end;
}

/* Pills */
.pill{
  border:1px solid rgba(255,255,255,.12);
  background: rgba(255,255,255,.05);
  padding:6px 10px;
  border-radius:999px;
  font-size:12px;
  color: rgba(255,255,255,.82);
  display:inline-flex;
  align-items:center;
  gap:8px;
}
.goldPill{ padding:6px 10px; }

/* ✅ Minimal audio group */
.audio{
  display:inline-flex;
  align-items:center;
  gap:8px;
  padding:6px 8px;
  border-radius:999px;
  border:1px solid rgba(255,255,255,.12);
  background: rgba(255,255,255,.05);
}

.iconBtn{
  width:34px;
  height:34px;
  border-radius:999px;
  border:1px solid rgba(255,255,255,.12);
  background: rgba(255,255,255,.04);
  color: rgba(255,255,255,.88);
  display:inline-flex;
  align-items:center;
  justify-content:center;
  cursor:pointer;
  padding:0;
  transition: transform .08s ease, background .15s ease, border-color .15s ease;
}
.iconBtn:hover{
  background: rgba(255,255,255,.06);
  border-color: rgba(255,255,255,.16);
}
.iconBtn:active{ transform: translateY(1px); }

.icon{ filter: saturate(0.9); }

.slider{
  width:120px;
  accent-color: rgba(255,255,255,.85);
  background: transparent;
}

.dot{
  opacity:.7;
  margin-left:2px;
}

/* Links */
.links{
  display:flex;
  gap:8px;
  flex-wrap:wrap;
}

.link{
  text-decoration:none;
  color: rgba(255,255,255,.82);
  padding:8px 12px;
  border-radius:999px;
  border:1px solid rgba(255,255,255,.12);
  background: rgba(255,255,255,.04);
  transition: background .15s ease, border-color .15s ease, transform .08s ease;
}
.link:hover{
  background: rgba(255,255,255,.06);
  border-color: rgba(255,255,255,.16);
}
.link:active{ transform: translateY(1px); }

.link.active{
  color:#0b1020;
  font-weight:900;
  border:none;
  background: linear-gradient(90deg,#7c5cff,#35d6c5);
}

/* ✅ Hamburger (hidden on desktop by default) */
.hamburger{
  display:none;
  width:40px;
  height:40px;
  border-radius:999px;
  border:1px solid rgba(255,255,255,.12);
  background: rgba(255,255,255,.05);
  cursor:pointer;
  padding:0;
  align-items:center;
  justify-content:center;
  transition: background .15s ease, border-color .15s ease, transform .08s ease;
}
.hamburger:hover{
  background: rgba(255,255,255,.08);
  border-color: rgba(255,255,255,.16);
}
.hamburger:active{ transform: translateY(1px); }

.hamburgerLines{
  display:inline-flex;
  flex-direction:column;
  gap:5px;
}
.hamburgerLines > span{
  width:18px;
  height:2px;
  border-radius:999px;
  background: rgba(255,255,255,.9);
  display:block;
  opacity:.9;
}

/* ✅ Mobile menu overlay */
.mobileMenuWrap{
  position:fixed;
  inset:0;
  z-index:50;
}
.backdrop{
  position:absolute;
  inset:0;
  border:none;
  background: rgba(0,0,0,.35);
  cursor:pointer;
}
.mobileMenu{
  position:absolute;
  top:64px;
  right:12px;
  width:min(260px, calc(100vw - 24px));
  padding:10px;
  border-radius:16px;
  border:1px solid rgba(255,255,255,.12);
  background: rgba(11,16,32,.92);
  backdrop-filter: blur(10px);
  box-shadow: 0 12px 30px rgba(0,0,0,.35);
  display:flex;
  flex-direction:column;
  gap:8px;
}

.mobileLink{
  text-decoration:none;
  color: rgba(255,255,255,.86);
  padding:10px 12px;
  border-radius:12px;
  border:1px solid rgba(255,255,255,.10);
  background: rgba(255,255,255,.04);
}
.mobileLink.active{
  color:#0b1020;
  font-weight:900;
  border:none;
  background: linear-gradient(90deg,#7c5cff,#35d6c5);
}

/* Menu animation */
.menu-enter-active, .menu-leave-active{
  transition: opacity .14s ease;
}
.menu-enter-from, .menu-leave-to{
  opacity: 0;
}

/* Main container */
.container{
  width:min(980px, calc(100% - 32px));
  margin:0 auto;
  padding:22px 0 42px;
}

/* -----------------------
   Mobile behavior
   ----------------------- */
@media (max-width: 640px){
  .nav{
    padding:10px 12px;
    gap:10px;
  }

  .brandIcon{ width:24px; height:24px; border-radius:9px; }
  .title{ max-width: 140px; font-size: 14px; }

  .right{ gap:8px; }

  .audio{ gap:6px; padding:6px 8px; }
  .iconBtn{ width:32px; height:32px; }
  .slider{ width:88px; }

  /* ✅ Hide desktop links; show hamburger */
  .desktopLinks{ display:none; }
  .hamburger{ display:inline-flex; }
}

@media (max-width: 380px){
  .goldPill{ display:none; }
  .title{ max-width: 110px; }
  .slider{ width:72px; }
}
</style>
