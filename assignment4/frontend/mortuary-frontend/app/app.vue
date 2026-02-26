<template>
  <div>
    <nav class="navbar">
      <div class="nav-left">
        <!-- ✅ Brand goes to Public board (not "/") -->
        <NuxtLink to="/public" class="brand" @click="closeMobileMenu">MortuaryAssist</NuxtLink>
      </div>

      <!-- Desktop links (unchanged look) -->
      <div class="nav-links desktop">
        <NuxtLink to="/public" class="nav-item">Home</NuxtLink>
        <NuxtLink to="/public" class="nav-item">Public Board</NuxtLink>

        <NuxtLink v-if="canSeeCases" to="/cases" class="nav-item">Cases</NuxtLink>
        <NuxtLink v-if="canSeeAccount" to="/account" class="nav-item">Account</NuxtLink>

        <!-- 🔊 Audio Control (desktop hover popover) -->
        <div class="audio-wrap">
          <button
            class="nav-item icon-btn"
            type="button"
            :aria-label="muted ? 'Unmute' : 'Mute'"
            :title="muted ? 'Unmute' : 'Mute'"
            @click="toggleMute"
          >
            <!-- Unmuted -->
            <svg
              v-if="!muted"
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 24 24"
              class="icon"
              fill="currentColor"
            >
              <path
                d="M11 5.414 6.707 9.707A1 1 0 0 1 6 10H3a1 1 0 0 0-1 1v2a1 1 0 0 0 1 1h3a1 1 0 0 1 .707.293L11 18.586A1 1 0 0 0 13 17.879V6.121a1 1 0 0 0-1.707-.707Z"
              />
              <path
                d="M16.5 8.5a1 1 0 0 1 1.414 0 6 6 0 0 1 0 8.485 1 1 0 1 1-1.414-1.414 4 4 0 0 0 0-5.657 1 1 0 0 1 0-1.414Z"
              />
            </svg>

            <!-- Muted -->
            <svg
              v-else
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 24 24"
              class="icon"
              fill="currentColor"
            >
              <path
                d="M11 5.414 6.707 9.707A1 1 0 0 1 6 10H3a1 1 0 0 0-1 1v2a1 1 0 0 0 1 1h3a1 1 0 0 1 .707.293L11 18.586A1 1 0 0 0 13 17.879V6.121a1 1 0 0 0-1.707-.707Z"
              />
              <path d="M16 8l6 6M22 8l-6 6" stroke="white" stroke-width="2" />
            </svg>
          </button>

          <!-- Popover -->
          <div class="vol-pop" @click.stop>
            <div class="vol-rail" aria-hidden="true">
              <input
                class="vol-rotated"
                type="range"
                min="0"
                max="1"
                step="0.01"
                v-model.number="volume"
                aria-label="Volume"
              />
            </div>

            <div class="vol-label">{{ Math.round(volume * 100) }}%</div>
          </div>
        </div>

        <NuxtLink v-if="!isLoggedIn" to="/login" class="nav-item">Login</NuxtLink>

        <button v-else class="nav-item logout-btn" type="button" @click="handleLogout">
          Log Out
        </button>
      </div>

      <!-- Mobile hamburger -->
      <div class="mobile">
        <button
          class="hamburger"
          type="button"
          aria-label="Open menu"
          :aria-expanded="mobileOpen ? 'true' : 'false'"
          @click="toggleMobileMenu"
        >
          <span class="hamburger-lines" aria-hidden="true">
            <span class="line" :class="{ open: mobileOpen }"></span>
            <span class="line" :class="{ open: mobileOpen }"></span>
            <span class="line" :class="{ open: mobileOpen }"></span>
          </span>
        </button>
      </div>
    </nav>

    <!-- Mobile menu panel -->
    <div
      v-if="mobileOpen"
      class="mobile-overlay"
      @click="closeMobileMenu"
      aria-hidden="true"
    ></div>

    <aside
      class="mobile-panel"
      :class="{ open: mobileOpen }"
      aria-label="Mobile navigation"
    >
      <div class="mobile-panel-head">
        <NuxtLink to="/public" class="mobile-brand" @click="closeMobileMenu">
          MortuaryAssist
        </NuxtLink>

        <button class="close-btn" type="button" aria-label="Close menu" @click="closeMobileMenu">
          ✕
        </button>
      </div>

      <div class="mobile-links">
        <NuxtLink to="/public" class="mobile-item" @click="closeMobileMenu">Home</NuxtLink>
        <NuxtLink to="/public" class="mobile-item" @click="closeMobileMenu">Public Board</NuxtLink>

        <NuxtLink
          v-if="canSeeCases"
          to="/cases"
          class="mobile-item"
          @click="closeMobileMenu"
        >
          Cases
        </NuxtLink>

        <NuxtLink
          v-if="canSeeAccount"
          to="/account"
          class="mobile-item"
          @click="closeMobileMenu"
        >
          Account
        </NuxtLink>

        <div class="mobile-divider"></div>

        <!-- Mobile audio: no hover; show slider inline -->
        <div class="mobile-audio">
          <div class="mobile-audio-row">
            <button
              class="mobile-icon-btn"
              type="button"
              :aria-label="muted ? 'Unmute' : 'Mute'"
              @click="toggleMute"
            >
              <svg
                v-if="!muted"
                xmlns="http://www.w3.org/2000/svg"
                viewBox="0 0 24 24"
                class="icon"
                fill="currentColor"
              >
                <path
                  d="M11 5.414 6.707 9.707A1 1 0 0 1 6 10H3a1 1 0 0 0-1 1v2a1 1 0 0 0 1 1h3a1 1 0 0 1 .707.293L11 18.586A1 1 0 0 0 13 17.879V6.121a1 1 0 0 0-1.707-.707Z"
                />
                <path
                  d="M16.5 8.5a1 1 0 0 1 1.414 0 6 6 0 0 1 0 8.485 1 1 0 1 1-1.414-1.414 4 4 0 0 0 0-5.657 1 1 0 0 1 0-1.414Z"
                />
              </svg>

              <svg
                v-else
                xmlns="http://www.w3.org/2000/svg"
                viewBox="0 0 24 24"
                class="icon"
                fill="currentColor"
              >
                <path
                  d="M11 5.414 6.707 9.707A1 1 0 0 1 6 10H3a1 1 0 0 0-1 1v2a1 1 0 0 0 1 1h3a1 1 0 0 1 .707.293L11 18.586A1 1 0 0 0 13 17.879V6.121a1 1 0 0 0-1.707-.707Z"
                />
                <path d="M16 8l6 6M22 8l-6 6" stroke="white" stroke-width="2" />
              </svg>
            </button>

            <div class="mobile-vol-label">{{ Math.round(volume * 100) }}%</div>
          </div>

          <input
            class="mobile-slider"
            type="range"
            min="0"
            max="1"
            step="0.01"
            v-model.number="volume"
            aria-label="Volume"
          />
        </div>

        <div class="mobile-divider"></div>

        <NuxtLink
          v-if="!isLoggedIn"
          to="/login"
          class="mobile-item"
          @click="closeMobileMenu"
        >
          Login
        </NuxtLink>

        <button v-else class="mobile-item mobile-logout" type="button" @click="handleLogout">
          Log Out
        </button>
      </div>
    </aside>

    <NuxtPage />

    <audio ref="audioEl" :src="audioSrc" loop preload="auto" />
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from "vue"
import { useRouter } from "vue-router"
import { useAuth } from "../composables/useAuth"

const router = useRouter()

const { roles, token, logout, initAuth } = useAuth()

onMounted(async () => {
  await initAuth()
})

const isLoggedIn = computed(() => !!token.value)

const isAdmin = computed(() => roles.value?.includes("Admin"))
const isMortician = computed(() => roles.value?.includes("Mortician"))

const canSeeAccount = computed(() => isLoggedIn.value && (isAdmin.value || isMortician.value))
const canSeeCases = canSeeAccount

async function handleLogout() {
  logout()
  closeMobileMenu()
  await router.push("/login")
}

/* 📱 MOBILE MENU */
const mobileOpen = ref(false)

function openMobileMenu() {
  mobileOpen.value = true
  if (import.meta.client) document.documentElement.style.overflow = "hidden"
}
function closeMobileMenu() {
  mobileOpen.value = false
  if (import.meta.client) document.documentElement.style.overflow = ""
}
function toggleMobileMenu() {
  mobileOpen.value ? closeMobileMenu() : openMobileMenu()
}

function onKeydown(e: KeyboardEvent) {
  if (e.key === "Escape") closeMobileMenu()
}
onMounted(() => {
  if (import.meta.client) window.addEventListener("keydown", onKeydown)
})
onBeforeUnmount(() => {
  if (import.meta.client) window.removeEventListener("keydown", onKeydown)
})

/* 🔊 AUDIO */
const audioSrc = "/TwinPeak.mp3"
const audioEl = ref<HTMLAudioElement | null>(null)

const volume = ref(0.35)
const muted = ref(false)

function applyAudio() {
  if (!audioEl.value) return
  audioEl.value.volume = Math.max(0, Math.min(1, volume.value))
  audioEl.value.muted = muted.value
}

function ensurePlaying() {
  audioEl.value?.play().catch(() => {})
}

function toggleMute() {
  muted.value = !muted.value
  applyAudio()
  ensurePlaying()
}

watch(volume, () => {
  if (volume.value > 0 && muted.value) muted.value = false
  applyAudio()
  ensurePlaying()
})

onMounted(() => {
  applyAudio()
})
</script>

<style scoped>
.navbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px;
  background-color: #111827;
  color: white;
  position: sticky;
  top: 0;
  z-index: 60;
}

.brand {
  font-weight: bold;
  font-size: 18px;
  text-decoration: none;
  color: white;
  white-space: nowrap;
}

.nav-links {
  display: flex;
  gap: 16px;
  align-items: center;
}

.nav-item {
  text-decoration: none;
  color: white;
  padding: 6px 10px;
  border-radius: 6px;
  background: none;
  border: none;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  line-height: 1;
}

.nav-item:hover {
  background-color: #1f2937;
}

.logout-btn {
  font: inherit;
}

.icon {
  width: 18px;
  height: 18px;
  display: block;
}

/* AUDIO CONTROL (desktop hover popover) */
.audio-wrap {
  position: relative;
  display: inline-flex;
  align-items: center;
}

.audio-wrap:hover .vol-pop,
.audio-wrap:focus-within .vol-pop {
  opacity: 1;
  pointer-events: auto;
  transform: translateX(50%) translateY(0);
}

.vol-pop {
  position: absolute;
  right: 50%;
  top: 115%;
  transform: translateX(50%) translateY(-6px);
  padding: 14px 12px;
  background: rgba(17, 24, 39, 0.98);
  border-radius: 14px;
  border: 1px solid rgba(255, 255, 255, 0.08);
  box-shadow: 0 15px 35px rgba(0, 0, 0, 0.45);
  opacity: 0;
  pointer-events: none;
  transition: opacity 150ms ease, transform 150ms ease;
  display: flex;
  flex-direction: column;
  align-items: center;
  z-index: 50;
}

.vol-pop::before {
  content: "";
  position: absolute;
  top: -10px;
  left: 0;
  right: 0;
  height: 12px;
}

.vol-rail {
  position: relative;
  width: 40px;
  height: 160px;
  border-radius: 14px;
  background: rgba(255, 255, 255, 0.06);
  border: 1px solid rgba(255, 255, 255, 0.08);
  display: grid;
  place-items: center;
  overflow: hidden;
}

.vol-rotated {
  position: absolute;
  left: 50%;
  top: 50%;
  width: 140px;
  height: 28px;
  transform: translate(-50%, -50%) rotate(-90deg);
  transform-origin: center;

  appearance: none;
  -webkit-appearance: none;
  background: transparent;
  cursor: pointer;
  outline: none;
  padding: 0;
  margin: 0;
}

.vol-rotated::-webkit-slider-runnable-track {
  height: 10px;
  background: #374151;
  border-radius: 999px;
}

.vol-rotated::-webkit-slider-thumb {
  -webkit-appearance: none;
  width: 16px;
  height: 16px;
  background: white;
  border-radius: 50%;
  margin-top: -3px;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.35);
}

.vol-rotated::-moz-range-track {
  height: 10px;
  background: #374151;
  border-radius: 999px;
}

.vol-rotated::-moz-range-thumb {
  width: 16px;
  height: 16px;
  background: white;
  border: none;
  border-radius: 50%;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.35);
}

.vol-label {
  margin-top: 10px;
  font-size: 12px;
  opacity: 0.8;
}

/* Responsive: hide/show desktop vs mobile */
.desktop {
  display: flex;
}
.mobile {
  display: none;
}

/* Hamburger */
.hamburger {
  background: transparent;
  border: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: 10px;
  width: 42px;
  height: 38px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  color: white;
}
.hamburger:hover {
  background: rgba(255, 255, 255, 0.06);
}
.hamburger-lines {
  width: 18px;
  height: 14px;
  display: inline-flex;
  flex-direction: column;
  justify-content: space-between;
}
.line {
  height: 2px;
  width: 100%;
  background: white;
  border-radius: 99px;
  transition: transform 160ms ease, opacity 160ms ease;
}
.line.open:nth-child(1) {
  transform: translateY(6px) rotate(45deg);
}
.line.open:nth-child(2) {
  opacity: 0;
}
.line.open:nth-child(3) {
  transform: translateY(-6px) rotate(-45deg);
}

/* Mobile panel + overlay */
.mobile-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.45);
  z-index: 59;
}

.mobile-panel {
  position: fixed;
  top: 0;
  right: 0;
  height: 100vh;
  width: min(86vw, 320px);
  background: #0b1220;
  border-left: 1px solid rgba(255, 255, 255, 0.08);
  box-shadow: -18px 0 45px rgba(0, 0, 0, 0.45);
  transform: translateX(110%);
  transition: transform 180ms ease;
  z-index: 60;
  display: flex;
  flex-direction: column;
}
.mobile-panel.open {
  transform: translateX(0);
}

.mobile-panel-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 14px 14px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
}

.mobile-brand {
  color: white;
  text-decoration: none;
  font-weight: 700;
}

.close-btn {
  background: transparent;
  border: 1px solid rgba(255, 255, 255, 0.12);
  color: white;
  border-radius: 10px;
  width: 38px;
  height: 34px;
  cursor: pointer;
}
.close-btn:hover {
  background: rgba(255, 255, 255, 0.06);
}

.mobile-links {
  padding: 10px 10px 18px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  overflow: auto;
}

.mobile-item {
  width: 100%;
  text-align: left;
  padding: 12px 12px;
  border-radius: 12px;
  border: 1px solid rgba(255, 255, 255, 0.08);
  background: rgba(255, 255, 255, 0.03);
  color: white;
  text-decoration: none;
  font: inherit;
  cursor: pointer;
}
.mobile-item:hover {
  background: rgba(255, 255, 255, 0.06);
}

.mobile-divider {
  height: 1px;
  margin: 6px 2px;
  background: rgba(255, 255, 255, 0.08);
}

.mobile-audio {
  border: 1px solid rgba(255, 255, 255, 0.08);
  background: rgba(255, 255, 255, 0.03);
  border-radius: 12px;
  padding: 12px;
}

.mobile-audio-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}

.mobile-icon-btn {
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(255, 255, 255, 0.1);
  color: white;
  border-radius: 10px;
  width: 44px;
  height: 38px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
}
.mobile-icon-btn:hover {
  background: rgba(255, 255, 255, 0.07);
}

.mobile-vol-label {
  font-size: 12px;
  opacity: 0.85;
}

.mobile-slider {
  width: 100%;
  margin-top: 10px;
}

/* Breakpoint */
@media (max-width: 720px) {
  .desktop {
    display: none;
  }
  .mobile {
    display: flex;
    align-items: center;
    gap: 10px;
  }

  .navbar {
    padding: 12px 14px;
  }
}
</style>