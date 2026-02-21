<template>
  <!-- If not logged in OR not Admin/Mortician: show nothing -->
  <div v-if="!canViewAnything"></div>

  <!-- Mortician sees a blank page -->
  <main v-else-if="isMortician && !isAdmin" class="wrap"></main>

  <!-- Admin UI -->
  <main v-else class="wrap">
    <h1>Account</h1>

    <div class="toolbar">
      <input
        v-model="query"
        class="search"
        type="text"
        placeholder="Search users by email or display name..."
      />
      <button class="btn" type="button" @click="loadUsers" :disabled="loading">
        {{ loading ? "Loading..." : "Refresh" }}
      </button>
    </div>

    <p v-if="error" class="error">{{ error }}</p>

    <div class="card" v-if="filteredUsers.length">
      <table class="table">
        <thead>
          <tr>
            <th>Email</th>
            <th>Display Name</th>
            <th>Status</th>
            <th>Roles</th>
            <th style="width: 260px;">Actions</th>
          </tr>
        </thead>

        <tbody>
          <tr v-for="u in filteredUsers" :key="u.id">
            <td>{{ u.email }}</td>
            <td>{{ u.displayName ?? "-" }}</td>

            <!-- ✅ Status column -->
            <td>
              <span :class="u.isDisabled ? 'badge disabled' : 'badge enabled'">
                {{ u.isDisabled ? "Disabled" : "Enabled" }}
              </span>
              <div v-if="u.isDisabled && u.lockoutEndUtc" class="muted small">
                Until: {{ formatUtc(u.lockoutEndUtc) }}
              </div>
            </td>

            <!-- Roles -->
            <td>
              <div class="roles">
                <label class="role">
                  <input type="checkbox" v-model="u.editRoles" value="Mortician" />
                  Mortician
                </label>
                <label class="role">
                  <input type="checkbox" v-model="u.editRoles" value="Admin" />
                  Admin
                </label>
              </div>
              <div class="muted small">
                Current: {{ (u.roles ?? []).join(", ") || "None" }}
              </div>
            </td>

            <!-- Actions -->
            <td>
              <div class="actions">
                <button class="btn" type="button" @click="saveRoles(u)" :disabled="u.saving">
                  {{ u.saving ? "Saving..." : "Save Roles" }}
                </button>

                <button
                  class="btn"
                  type="button"
                  @click="toggleEnabled(u)"
                  :disabled="u.toggling"
                >
                  {{ u.toggling ? "Working..." : u.isDisabled ? "Enable" : "Disable" }}
                </button>

                <button
                  class="btn danger"
                  type="button"
                  @click="deleteUser(u)"
                  :disabled="u.deleting"
                >
                  {{ u.deleting ? "Deleting..." : "Delete" }}
                </button>
              </div>

              <div v-if="u.saveError" class="error small">{{ u.saveError }}</div>
              <div v-if="u.saved" class="ok small">Saved</div>
              <div v-if="u.actionOk" class="ok small">{{ u.actionOk }}</div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <p v-else-if="!loading && !error" class="muted">No users found.</p>
  </main>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue"
import { $fetch } from "ofetch"
import { useRuntimeConfig } from "#imports"
import { useAuth } from "../../composables/useAuth"

type ApiUser = {
  id: string
  email: string
  displayName?: string | null
  roles: string[]
  isDisabled: boolean
  lockoutEndUtc?: string | null
}

type UiUser = ApiUser & {
  editRoles: string[]
  saving: boolean
  saved: boolean
  saveError: string | null
  toggling: boolean
  deleting: boolean
  actionOk: string | null
}

type SetRoleRequest = {
  userId: string
  role: "Admin" | "Mortician"
  enabled: boolean
}

type SetEnabledRequest = {
  userId: string
  enabled: boolean
}

const { user, roles, token, initAuth } = useAuth()

const isLoggedIn = computed(() => !!user.value)
const isAdmin = computed(() => roles.value?.includes("Admin"))
const isMortician = computed(() => roles.value?.includes("Mortician"))
const canViewAnything = computed(() => isLoggedIn.value && (isAdmin.value || isMortician.value))

const loading = ref(false)
const error = ref<string | null>(null)

const query = ref("")
const users = ref<UiUser[]>([])

const filteredUsers = computed(() => {
  const q = query.value.trim().toLowerCase()
  if (!q) return users.value
  return users.value.filter((u) => {
    const email = (u.email ?? "").toLowerCase()
    const name = (u.displayName ?? "").toLowerCase()
    return email.includes(q) || name.includes(q)
  })
})

function authHeaders(): Record<string, string> {
  const t = token.value
  if (!t) return {}
  return { Authorization: `Bearer ${t}` }
}

function normalizeRoles(rs: string[] | null | undefined): Array<"Admin" | "Mortician"> {
  const allowed = new Set(["Admin", "Mortician"] as const)
  return (rs ?? []).filter((r): r is "Admin" | "Mortician" => allowed.has(r as any))
}

function formatUtc(isoOrNull: string | null | undefined) {
  if (!isoOrNull) return ""
  const d = new Date(isoOrNull)
  if (isNaN(d.getTime())) return isoOrNull
  return d.toLocaleString()
}

/**
 * ✅ NEW: build URLs that work in BOTH environments
 * - DEV: apiBase="" -> "/api/..."
 * - PROD: apiBase="https://<appservice>" -> "https://.../api/..."
 */
const config = useRuntimeConfig()
const API_BASE = computed(() => (config.public as any)?.apiBase ?? "")

function apiUrl(path: string) {
  const p = path.startsWith("/") ? path : `/${path}`
  const base = (API_BASE.value ?? "").trim()
  if (!base) return p
  const b2 = base.endsWith("/") ? base.slice(0, -1) : base
  return `${b2}${p}`
}

async function loadUsers() {
  if (!isAdmin.value) return
  loading.value = true
  error.value = null

  try {
    const list = await $fetch<ApiUser[]>(apiUrl("/api/admin/users"), {
      headers: authHeaders(),
    })

    users.value = (list ?? []).map((u) => {
      const current = normalizeRoles(u.roles)
      return {
        ...u,
        roles: current,
        editRoles: [...current],

        saving: false,
        saved: false,
        saveError: null,

        toggling: false,
        deleting: false,
        actionOk: null,
      }
    })
  } catch (e: any) {
    error.value = e?.data?.message || e?.message || "Failed to load users."
  } finally {
    loading.value = false
  }
}

async function setRole(userId: string, role: "Admin" | "Mortician", enabled: boolean) {
  const payload: SetRoleRequest = { userId, role, enabled }

  return await $fetch(apiUrl("/api/admin/users/set-role"), {
    method: "POST",
    headers: authHeaders(),
    body: payload,
  })
}

async function saveRoles(u: UiUser) {
  u.saving = true
  u.saved = false
  u.actionOk = null
  u.saveError = null

  try {
    const current = new Set(normalizeRoles(u.roles))
    const desired = new Set(normalizeRoles(u.editRoles))

    const all: Array<"Admin" | "Mortician"> = ["Admin", "Mortician"]
    const changes = all.filter((r) => current.has(r) !== desired.has(r))

    if (changes.length === 0) {
      u.saved = true
      setTimeout(() => (u.saved = false), 1200)
      return
    }

    for (const r of changes) {
      await setRole(u.id, r, desired.has(r))
    }

    const newRoles = Array.from(desired)
    u.roles = newRoles
    u.editRoles = [...newRoles]

    u.saved = true
    setTimeout(() => (u.saved = false), 1200)
  } catch (e: any) {
    u.saveError = e?.data?.message || e?.message || "Failed to save roles."
  } finally {
    u.saving = false
  }
}

async function toggleEnabled(u: UiUser) {
  u.toggling = true
  u.saveError = null
  u.actionOk = null

  try {
    const payload: SetEnabledRequest = {
      userId: u.id,
      enabled: u.isDisabled, // if disabled -> enable; if enabled -> disable
    }

    const res = await $fetch<any>(apiUrl("/api/admin/users/set-enabled"), {
      method: "POST",
      headers: authHeaders(),
      body: payload,
    })

    u.isDisabled = !!(res?.isDisabled ?? res?.IsDisabled)
    u.lockoutEndUtc = (res?.lockoutEndUtc ?? res?.LockoutEndUtc ?? null) as any

    u.actionOk = u.isDisabled ? "User disabled." : "User enabled."
    setTimeout(() => (u.actionOk = null), 1200)
  } catch (e: any) {
    u.saveError = e?.data?.message || e?.message || "Failed to change enabled status."
  } finally {
    u.toggling = false
  }
}

async function deleteUser(u: UiUser) {
  const ok = window.confirm(`Delete user ${u.email}? This cannot be undone.`)
  if (!ok) return

  u.deleting = true
  u.saveError = null
  u.actionOk = null

  try {
    await $fetch(apiUrl(`/api/admin/users/${encodeURIComponent(u.id)}`), {
      method: "DELETE",
      headers: authHeaders(),
    })

    users.value = users.value.filter((x) => x.id !== u.id)
  } catch (e: any) {
    u.saveError = e?.data?.message || e?.message || "Failed to delete user."
  } finally {
    u.deleting = false
  }
}

onMounted(async () => {
  await initAuth()
  if (isAdmin.value) {
    await loadUsers()
  }
})
</script>

<style scoped>
.wrap {
  max-width: 1100px;
  margin: 0 auto;
  padding: 16px;
}

.toolbar {
  display: flex;
  gap: 12px;
  align-items: center;
  margin: 12px 0 16px;
}

.search {
  flex: 1;
  padding: 10px;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
}

.btn {
  padding: 10px 12px;
  border-radius: 8px;
  border: 1px solid #111827;
  background: white;
  cursor: pointer;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn.danger {
  border-color: #b00020;
  color: #b00020;
}

.card {
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  overflow: hidden;
}

.table {
  width: 100%;
  border-collapse: collapse;
}

thead th {
  text-align: left;
  background: #f8fafc;
  padding: 12px;
  border-bottom: 1px solid #e2e8f0;
}

tbody td {
  padding: 12px;
  border-bottom: 1px solid #e2e8f0;
  vertical-align: top;
}

.roles {
  display: flex;
  gap: 14px;
  flex-wrap: wrap;
}

.role {
  display: inline-flex;
  gap: 6px;
  align-items: center;
}

.actions {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
}

.badge {
  display: inline-block;
  padding: 4px 8px;
  border-radius: 999px;
  font-size: 12px;
  border: 1px solid #cbd5e1;
}

.enabled {
  color: #065f46;
  border-color: #10b981;
}

.disabled {
  color: #b00020;
  border-color: #b00020;
}

.error {
  color: #b00020;
  margin: 8px 0;
}

.ok {
  color: #067647;
}

.muted {
  opacity: 0.75;
}

.small {
  font-size: 12px;
  margin-top: 6px;
}
</style>