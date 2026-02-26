<template>
  <!-- If not logged in OR not Admin/Mortician: show nothing -->
  <div v-if="!canViewAnything"></div>

  <!-- Mortician sees a blank page -->
  <main v-else-if="isMortician && !isAdmin" class="page">
    <div class="wrap"></div>
  </main>

  <!-- Admin UI -->
  <main v-else class="page">
    <div class="wrap">
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

      <div class="card tableCard" v-if="filteredUsers.length">
        <!-- ✅ Desktop table (NO scrollbars) -->
        <div class="tableWrap" aria-label="Users table">
          <table class="table">
            <thead>
              <tr>
                <th>Email</th>
                <th>Display Name</th>
                <th>Status</th>
                <th class="colRole">Role</th>
                <th class="colActions">Actions</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="u in filteredUsers" :key="u.id">
                <td class="break">{{ u.email }}</td>
                <td class="break">{{ u.displayName ?? "-" }}</td>

                <td>
                  <span :class="u.isDisabled ? 'badge disabled' : 'badge enabled'">
                    {{ u.isDisabled ? "Disabled" : "Enabled" }}
                  </span>
                  <div v-if="u.isDisabled && u.lockoutEndUtc" class="muted small">
                    Until: {{ formatUtc(u.lockoutEndUtc) }}
                  </div>
                </td>

                <td>
                  <select class="select" v-model="u.editRole">
                    <option value="">None</option>
                    <option value="Mortician">Mortician</option>
                    <option value="Admin">Admin</option>
                  </select>
                  <div class="muted small">Current: {{ u.role || "None" }}</div>
                </td>

                <td>
                  <div class="actions">
                    <button class="btn" type="button" @click="saveRole(u)" :disabled="u.saving">
                      {{ u.saving ? "Saving..." : "Save Role" }}
                    </button>

                    <!-- ✅ Removed Enable/Disable button per request -->

                    <button class="btn danger" type="button" @click="deleteUser(u)" :disabled="u.deleting">
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

        <!-- ✅ Mobile cards -->
        <div class="mobileList">
          <article v-for="u in filteredUsers" :key="u.id" class="userCard">
            <div class="userTop">
              <div class="userIdentity">
                <div class="userEmail break">{{ u.email }}</div>
                <div class="muted small break">{{ u.displayName ?? "-" }}</div>
              </div>

              <div class="userStatus">
                <span :class="u.isDisabled ? 'badge disabled' : 'badge enabled'">
                  {{ u.isDisabled ? "Disabled" : "Enabled" }}
                </span>
                <div v-if="u.isDisabled && u.lockoutEndUtc" class="muted small">
                  Until: {{ formatUtc(u.lockoutEndUtc) }}
                </div>
              </div>
            </div>

            <div class="userRow">
              <div class="muted small">Role</div>
              <select class="select" v-model="u.editRole">
                <option value="">None</option>
                <option value="Mortician">Mortician</option>
                <option value="Admin">Admin</option>
              </select>
              <div class="muted small">Current: {{ u.role || "None" }}</div>
            </div>

            <div class="userActions">
              <button class="btn" type="button" @click="saveRole(u)" :disabled="u.saving">
                {{ u.saving ? "Saving..." : "Save Role" }}
              </button>

              <!-- ✅ Removed Enable/Disable button per request -->

              <button class="btn danger" type="button" @click="deleteUser(u)" :disabled="u.deleting">
                {{ u.deleting ? "Deleting..." : "Delete" }}
              </button>
            </div>

            <div v-if="u.saveError" class="error small" style="margin-top: 8px">{{ u.saveError }}</div>
            <div v-if="u.saved" class="ok small" style="margin-top: 8px">Saved</div>
            <div v-if="u.actionOk" class="ok small" style="margin-top: 8px">{{ u.actionOk }}</div>
          </article>
        </div>
      </div>

      <p v-else-if="!loading && !error" class="muted">No users found.</p>
    </div>
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

type Role = "" | "Admin" | "Mortician"

type UiUser = ApiUser & {
  role: Role
  editRole: Role
  saving: boolean
  saved: boolean
  saveError: string | null
  deleting: boolean
  actionOk: string | null
}

type SetRoleRequest = {
  userId: string
  role: "Admin" | "Mortician"
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

function pickSingleRole(rs: string[] | null | undefined): Role {
  const set = new Set((rs ?? []).map((x) => (x ?? "").trim()))
  if (set.has("Admin")) return "Admin"
  if (set.has("Mortician")) return "Mortician"
  return ""
}

function formatUtc(isoOrNull: string | null | undefined) {
  if (!isoOrNull) return ""
  const d = new Date(isoOrNull)
  if (isNaN(d.getTime())) return isoOrNull
  return d.toLocaleString()
}

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
      const role = pickSingleRole(u.roles)
      return {
        ...u,
        role,
        editRole: role,
        saving: false,
        saved: false,
        saveError: null,
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

async function saveRole(u: UiUser) {
  u.saving = true
  u.saved = false
  u.actionOk = null
  u.saveError = null

  const desired = u.editRole
  const current = u.role

  try {
    if (desired === current) {
      u.saved = true
      setTimeout(() => (u.saved = false), 1200)
      return
    }

    // remove both roles, then add desired role (if any)
    await setRole(u.id, "Admin", false)
    await setRole(u.id, "Mortician", false)

    if (desired === "Admin") await setRole(u.id, "Admin", true)
    if (desired === "Mortician") await setRole(u.id, "Mortician", true)

    u.role = desired
    u.roles = desired ? [desired] : []
    u.editRole = desired

    u.saved = true
    setTimeout(() => (u.saved = false), 1200)
  } catch (e: any) {
    u.saveError = e?.data?.message || e?.message || "Failed to save role."
  } finally {
    u.saving = false
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
.page {
  width: 100%;
}

.wrap {
  max-width: 1100px;
  margin: 0 auto;
  padding: 24px 18px;
  width: 100%;
  box-sizing: border-box;
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
  border-radius: 10px;
  min-width: 0;
}

.select {
  width: 100%;
  padding: 10px;
  border: 1px solid #cbd5e1;
  border-radius: 10px;
  background: white;
}

.btn {
  padding: 10px 12px;
  border-radius: 10px;
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
  border-radius: 14px;
  overflow: hidden;
  background: white;
}

.tableCard {
  margin-top: 10px;
}

/* ✅ Kill any horizontal scrollbars at this level */
.tableWrap {
  width: 100%;
  overflow: hidden; /* ✅ prevents scrollbars */
}

/* ✅ Make the table fit the container */
.table {
  width: 100%;
  border-collapse: collapse;
  table-layout: auto; /* ✅ allow browser to size columns naturally */
}

/* Keep headers readable */
thead th {
  text-align: left;
  background: #f8fafc;
  padding: 12px;
  border-bottom: 1px solid #e2e8f0;
  white-space: nowrap;
}

tbody td {
  padding: 12px;
  border-bottom: 1px solid #e2e8f0;
  vertical-align: top;
}

/* Column sizing */
.colRole {
  width: 220px;
}
.colActions {
  width: 280px; /* slightly smaller since we removed a button */
}

/* Actions wrap */
.actions {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
}

/* Badges */
.badge {
  display: inline-block;
  padding: 4px 8px;
  border-radius: 999px;
  font-size: 12px;
  border: 1px solid #cbd5e1;
  white-space: nowrap;
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

.break {
  overflow-wrap: anywhere;
  word-break: break-word;
}

/* Mobile list */
.mobileList {
  display: none;
  padding: 12px;
  gap: 12px;
}

.userCard {
  border: 1px solid #e2e8f0;
  border-radius: 14px;
  padding: 12px;
  display: grid;
  gap: 10px;
  background: white;
}

.userTop {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  flex-wrap: wrap;
}

.userIdentity {
  min-width: 0;
}

.userEmail {
  font-weight: 700;
}

.userStatus {
  text-align: right;
}

.userRow {
  display: grid;
  gap: 6px;
}

.userActions {
  display: grid;
  gap: 10px;
}

@media (max-width: 720px) {
  .wrap {
    padding: 14px 12px;
  }

  .toolbar {
    flex-direction: column;
    align-items: stretch;
  }

  .btn {
    width: 100%;
    justify-content: center;
  }

  /* Hide table on mobile, show cards */
  .tableWrap {
    display: none;
  }

  .mobileList {
    display: grid;
  }

  .userStatus {
    text-align: left;
  }
}
</style>