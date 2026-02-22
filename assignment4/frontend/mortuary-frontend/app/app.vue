<template>
  <div>
    <nav class="navbar">
      <div class="nav-left">
        <!-- ✅ Brand goes to Public board (not "/") -->
        <NuxtLink to="/public" class="brand">MortuaryAssist</NuxtLink>
      </div>

      <div class="nav-links">
        <!-- ✅ "Home" points to public board so you never get stuck on "/" -->
        <NuxtLink to="/public" class="nav-item">Home</NuxtLink>

        <NuxtLink to="/public" class="nav-item">Public Board</NuxtLink>

        <NuxtLink v-if="canSeeCases" to="/cases" class="nav-item">Cases</NuxtLink>
        <NuxtLink v-if="canSeeAccount" to="/account" class="nav-item">Account</NuxtLink>

        <!-- 🔊 Audio Control -->
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

        <!-- ✅ Login always goes to /login -->
        <NuxtLink v-if="!isLoggedIn" to="/login" class="nav-item">Login</NuxtLink>

        <!-- ✅ Logout returns user to /login (not "/") -->
        <button v-else class="nav-item logout-btn" type="button" @click="handleLogout">
          Log Out
        </button>
      </div>
    </nav>

    <NuxtPage />

    <audio ref="audioEl" :src="audioSrc" loop preload="auto" />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue"
import { useRouter } from "vue-router"
import { useAuth } from "../composables/useAuth"

const router = useRouter()

// ✅ Use token-based logged in state (more reliable than user on first paint)
const { user, roles, token, logout, initAuth } = useAuth()

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
  // ✅ After logout, go to login (prevents landing on a bad "/" route)
  await router.push("/login")
}

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
}

.brand {
  font-weight: bold;
  font-size: 18px;
  text-decoration: none;
  color: white;
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

/* AUDIO CONTROL */
.audio-wrap {
  position: relative;
  display: inline-flex;
  align-items: center;
}

/* show popover on hover OR while interacting (focus within) */
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

/* hover bridge */
.vol-pop::before {
  content: "";
  position: absolute;
  top: -10px;
  left: 0;
  right: 0;
  height: 12px;
}

/* Rail */
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
</style>