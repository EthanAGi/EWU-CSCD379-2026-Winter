<template>
  <main class="wrap">
    <h1>Public Case Board</h1>

    <p class="muted">
      Shows case status information from the database (public requirement). Admins and Morticians can view
      additional details.
    </p>

    <div v-if="pending">Loading…</div>

    <div v-else-if="errorMessage" class="error">
      {{ errorMessage }}
    </div>

    <div v-else class="card" style="margin-bottom: 14px">
      <div class="row" style="justify-content: space-between; align-items: center">
        <div class="muted small">
          Signed in:
          <strong>{{ isLoggedIn ? "Yes" : "No" }}</strong>
          <span v-if="isLoggedIn"> • Roles: <strong>{{ (authRoles || []).join(", ") || "—" }}</strong></span>
        </div>

        <button class="btn" type="button" @click="refreshAll" :disabled="pending">
          Refresh
        </button>
      </div>

      <div v-if="!canSeeVerbose" class="muted small" style="margin-top: 8px">
        Limited view: sign in as Admin or Mortician to see decedent and case details.
      </div>

      <div v-else class="muted small" style="margin-top: 8px">
        Verbose view enabled.
      </div>
    </div>

    <table v-if="cases.length" class="table">
      <thead>
        <tr>
          <th>Case #</th>
          <th>Status</th>
          <th>Created</th>

          <th v-if="canSeeVerbose">Decedent</th>
          <th v-if="canSeeVerbose">Assigned Mortician</th>

          <th v-if="canSeeVerbose" style="width: 110px">Details</th>
        </tr>
      </thead>

      <tbody>
        <tr v-for="c in cases" :key="c.caseNumber">
          <td>{{ c.caseNumber }}</td>
          <td>{{ c.status }}</td>
          <td>{{ formatDate(c.createdAt) }}</td>

          <td v-if="canSeeVerbose">
            {{ verboseByCase[c.caseNumber]?.decedentName ?? "—" }}
          </td>

          <td v-if="canSeeVerbose">
            {{ verboseByCase[c.caseNumber]?.assignedMorticianName ?? "—" }}
          </td>

          <td v-if="canSeeVerbose">
            <button class="btn" type="button" @click="toggleRow(c.caseNumber)">
              {{ openCaseNumber === c.caseNumber ? "Hide" : "Open" }}
            </button>
          </td>
        </tr>
      </tbody>
    </table>

    <div v-else class="muted">No cases found.</div>

    <!-- Expanded details preview (Admin/Mortician only) -->
    <div v-if="canSeeVerbose && openCaseNumber" class="details card">
      <div class="detailsHead">
        <h2 class="h2">Case Details: {{ openCaseNumber }}</h2>
        <button class="btn" type="button" @click="closeDetails">Close</button>
      </div>

      <div v-if="detailsError" class="error">{{ detailsError }}</div>
      <div v-else-if="detailsPending" class="muted">Loading details…</div>

      <div v-else-if="openDetails" class="stack">
        <div class="kv"><span class="k">Case #</span><span class="v">{{ openDetails.caseNumber }}</span></div>
        <div class="kv"><span class="k">Status</span><span class="v">{{ openDetails.status }}</span></div>
        <div class="kv"><span class="k">Created</span><span class="v">{{ formatDate(openDetails.createdAt) }}</span></div>
        <div class="kv"><span class="k">Next of Kin</span><span class="v">{{ openDetails.nextOfKinName || "—" }}</span></div>

        <div class="kv">
          <span class="k">Assigned Mortician</span>
          <span class="v">
            {{ openDetails.assignedMortician?.displayName ?? openDetails.assignedMortician?.email ?? "—" }}
          </span>
        </div>

        <div class="section">
          <div class="sectionTitle">Decedent</div>
          <div v-if="!openDetails.decedent" class="muted small">No decedent on file.</div>
          <div v-else class="stack">
            <div class="kv">
              <span class="k">Name</span>
              <span class="v">
                {{ (openDetails.decedent.firstName || "") + " " + (openDetails.decedent.lastName || "") }}
              </span>
            </div>
            <div class="kv"><span class="k">DOB</span><span class="v">{{ formatDate(openDetails.decedent.dateOfBirth) }}</span></div>
            <div class="kv"><span class="k">DOD</span><span class="v">{{ formatDate(openDetails.decedent.dateOfDeath) }}</span></div>
            <div class="kv"><span class="k">Place of death</span><span class="v">{{ openDetails.decedent.placeOfDeath || "—" }}</span></div>
            <div class="kv"><span class="k">Tag #</span><span class="v">{{ openDetails.decedent.tagNumber || "—" }}</span></div>
            <div class="kv"><span class="k">Storage</span><span class="v">{{ openDetails.decedent.storageLocation || "—" }}</span></div>
          </div>
        </div>

        <div class="section">
          <div class="sectionTitle">Tasks ({{ openDetails.tasks?.length ?? 0 }})</div>
          <div v-if="!(openDetails.tasks?.length)" class="muted small">No tasks.</div>
          <ul v-else class="list">
            <li v-for="t in openDetails.tasks" :key="t.id">
              <div class="muted small">
                {{ t.workflowStepTemplate?.sortOrder ?? "?" }} —
                <strong>{{ t.workflowStepTemplate?.name ?? "Step" }}</strong>
                ({{ t.status }})
              </div>
              <div class="muted small" v-if="t.workflowStepTemplate?.description">
                {{ t.workflowStepTemplate.description }}
              </div>
              <div class="muted small">
                Created: {{ formatDate(t.createdAt) }}
                <span v-if="t.startedAt"> • Started: {{ formatDate(t.startedAt) }}</span>
                <span v-if="t.completedAt"> • Done: {{ formatDate(t.completedAt) }}</span>
              </div>
              <div v-if="t.notes" class="muted small">Notes: {{ t.notes }}</div>
            </li>
          </ul>
        </div>

        <div class="section">
          <div class="sectionTitle">Notes ({{ openDetails.notes?.length ?? 0 }})</div>
          <div v-if="!(openDetails.notes?.length)" class="muted small">No notes.</div>
          <ul v-else class="list">
            <li v-for="n in openDetails.notes" :key="n.id">
              <div>{{ n.text }}</div>
              <div class="muted small">{{ formatDate(n.createdAt) }}</div>
            </li>
          </ul>
        </div>

        <div class="section">
          <div class="sectionTitle">Equipment Checkouts ({{ openDetails.equipmentCheckouts?.length ?? 0 }})</div>
          <div v-if="!(openDetails.equipmentCheckouts?.length)" class="muted small">No equipment checkouts.</div>
          <ul v-else class="list">
            <li v-for="ec in openDetails.equipmentCheckouts" :key="ec.id">
              <div>
                <strong>{{ ec.equipment?.name ?? "Equipment" }}</strong>
                <span class="muted small" v-if="ec.equipment?.serialNumber">
                  ({{ ec.equipment.serialNumber }})
                </span>
              </div>
              <div class="muted small">
                Checked out: {{ formatDate(ec.checkedOutAt) }}
                <span v-if="ec.returnedAt"> • Returned: {{ formatDate(ec.returnedAt) }}</span>
              </div>
              <div class="muted small" v-if="ec.notes">Notes: {{ ec.notes }}</div>
            </li>
          </ul>
        </div>
      </div>

      <div v-else class="muted">No details loaded.</div>
    </div>
  </main>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue"
import { $fetch } from "ofetch"
import { useAuth } from "../../composables/useAuth"

/** -----------------------------
 * Types
 * ----------------------------- */
type PublicCase = {
  caseNumber: string
  status: string
  createdAt: string
}

type MorticianLite = { id: string; email: string | null; displayName: string | null }

type WorkflowStepTemplateDto = {
  id: number
  name: string
  description?: string | null
  sortOrder: number
  isActive: boolean
}

type CaseTaskDto = {
  id: number
  caseFileId: number
  workflowStepTemplateId: number
  workflowStepTemplate?: WorkflowStepTemplateDto | null
  status: string
  notes?: string | null
  assignedToUserId?: string | null
  createdAt: string
  startedAt?: string | null
  completedAt?: string | null
}

type CaseNoteDto = {
  id: number
  caseFileId: number
  text: string
  createdByUserId?: string | null
  createdAt: string
}

type DecedentDto = {
  id: number
  caseFileId: number
  firstName: string
  lastName: string
  dateOfBirth?: string | null
  dateOfDeath?: string | null
  placeOfDeath?: string | null
  tagNumber?: string | null
  storageLocation?: string | null
}

type EquipmentDto = {
  id: number
  name: string
  serialNumber?: string | null
  status: string
  location?: string | null
  isActive: boolean
}

type EquipmentCheckoutDto = {
  id: number
  equipmentId: number
  equipment?: EquipmentDto | null
  caseFileId?: number | null
  checkedOutByUserId: string
  checkedOutAt: string
  returnedAt?: string | null
  notes?: string | null
}

type CaseDetailsDto = {
  id: number
  caseNumber: string
  status: string
  createdAt: string
  nextOfKinName: string
  decedent?: DecedentDto | null
  tasks: CaseTaskDto[]
  notes: CaseNoteDto[]
  equipmentCheckouts: EquipmentCheckoutDto[]
  assignedMortician?: MorticianLite | null
  assignedMorticianUserId?: string | null
}

/** -----------------------------
 * Auth (from your composable)
 * ----------------------------- */
const { token, roles, isLoggedIn, loadFromStorage, fetchMe } = useAuth()

// expose roles for template (avoid confusion with local variables)
const authRoles = computed(() => roles.value ?? [])

// Use roles to gate verbose content
const canSeeVerbose = computed(() => {
  const rs = authRoles.value
  return rs.includes("Admin") || rs.includes("Mortician")
})

// Helper for protected calls
function authHeaders(): Record<string, string> {
  return token.value ? { Authorization: `Bearer ${token.value}` } : {}
}

/** -----------------------------
 * Page state
 * ----------------------------- */
const cases = ref<PublicCase[]>([])
const pending = ref(true)
const errorMessage = ref<string | null>(null)

const verboseByCase = ref<
  Record<string, { decedentName: string; assignedMorticianName: string | null; assignedMorticianUserId: string | null }>
>({})

const openCaseNumber = ref<string | null>(null)
const openDetails = ref<CaseDetailsDto | null>(null)
const detailsPending = ref(false)
const detailsError = ref<string | null>(null)

/** -----------------------------
 * Helpers
 * ----------------------------- */
function formatDate(iso: string | null | undefined) {
  if (!iso) return "—"
  const d = new Date(iso)
  return isNaN(d.getTime()) ? "—" : d.toLocaleString()
}

function closeDetails() {
  openCaseNumber.value = null
  openDetails.value = null
  detailsError.value = null
  detailsPending.value = false
}

/** -----------------------------
 * Data loading
 * ----------------------------- */
async function loadPublicList() {
  pending.value = true
  errorMessage.value = null

  try {
    cases.value = await $fetch<PublicCase[]>("/api/public/cases")
  } catch (e: any) {
    errorMessage.value = e?.data?.message || e?.message || "Failed to load cases."
    cases.value = []
  } finally {
    pending.value = false
  }
}

/**
 * Load decedent + assigned mortician for the table (verbose users only)
 * Uses the protected endpoint; will fail if you aren’t Admin/Mortician (which is fine).
 */
async function loadVerboseExtras() {
  if (!canSeeVerbose.value) {
    verboseByCase.value = {}
    closeDetails()
    return
  }

  const map: Record<
    string,
    { decedentName: string; assignedMorticianName: string | null; assignedMorticianUserId: string | null }
  > = {}

  await Promise.all(
    (cases.value ?? []).map(async (c) => {
      try {
        const d = await $fetch<CaseDetailsDto>(`/api/public/cases/${encodeURIComponent(c.caseNumber)}`, {
          headers: authHeaders(),
        })

        const first = d.decedent?.firstName?.trim() ?? ""
        const last = d.decedent?.lastName?.trim() ?? ""
        const decedentName = (first + " " + last).trim() || "—"

        const assignedName = d.assignedMortician?.displayName ?? d.assignedMortician?.email ?? null

        map[c.caseNumber] = {
          decedentName,
          assignedMorticianName: assignedName,
          assignedMorticianUserId: d.assignedMorticianUserId ?? null,
        }
      } catch {
        map[c.caseNumber] = {
          decedentName: "—",
          assignedMorticianName: null,
          assignedMorticianUserId: null,
        }
      }
    })
  )

  verboseByCase.value = map
}

async function toggleRow(caseNumber: string) {
  if (!canSeeVerbose.value) return

  if (openCaseNumber.value === caseNumber) {
    closeDetails()
    return
  }

  openCaseNumber.value = caseNumber
  detailsPending.value = true
  detailsError.value = null
  openDetails.value = null

  try {
    openDetails.value = await $fetch<CaseDetailsDto>(`/api/public/cases/${encodeURIComponent(caseNumber)}`, {
      headers: authHeaders(),
    })
  } catch (e: any) {
    detailsError.value = e?.data?.message || e?.message || "Failed to load case details."
  } finally {
    detailsPending.value = false
  }
}

async function refreshAll() {
  // Restore token from localStorage (ma_token) + populate roles via /me
  loadFromStorage()
  if (token.value) {
    await fetchMe()
  }

  await loadPublicList()
  await loadVerboseExtras()
}

onMounted(async () => {
  await refreshAll()
})
</script>

<style scoped>
.wrap {
  max-width: 1100px;
  margin: 0 auto;
  padding: 16px;
}

.row {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
}

.muted {
  opacity: 0.7;
}
.small {
  font-size: 12px;
}

.error {
  color: #b00020;
}

.table {
  width: 100%;
  border-collapse: collapse;
  margin-top: 12px;
}
.table th,
.table td {
  border: 1px solid #ddd;
  padding: 8px;
  text-align: left;
  vertical-align: top;
}
.table th {
  background: #f6f6f6;
}

.btn {
  padding: 6px 10px;
  border-radius: 6px;
  border: 1px solid #111827;
  background: white;
  cursor: pointer;
}
.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.card {
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 12px;
  margin-top: 14px;
}

.detailsHead {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 10px;
}

.h2 {
  margin: 0;
  font-size: 18px;
}

.stack {
  display: grid;
  gap: 10px;
  margin-top: 10px;
}

.kv {
  display: grid;
  grid-template-columns: 180px 1fr;
  gap: 10px;
}

.k {
  font-weight: 600;
}

.section {
  margin-top: 6px;
  padding-top: 10px;
  border-top: 1px solid #e2e8f0;
}

.sectionTitle {
  font-weight: 700;
  margin-bottom: 6px;
}

.list {
  margin: 0;
  padding-left: 18px;
}
</style>