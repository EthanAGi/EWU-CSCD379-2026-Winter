<template>
  <main class="wrap">
    <h1>Public Case Board</h1>

    <p class="muted">
      Shows case status information from the database (public requirement).
    </p>

    <div v-if="pending">Loading…</div>

    <div v-else-if="errorMessage" class="error">
      {{ errorMessage }}
    </div>

    <table v-else class="table">
      <thead>
        <tr>
          <th>Case #</th>
          <th>Status</th>
          <th>Created</th>

          <!-- Admin/Mortician only -->
          <th v-if="canSeeVerbose">Decedent</th>
          <th v-if="canSeeVerbose">Status last changed</th>

          <!-- Optional “view details” action -->
          <th v-if="canSeeVerbose" style="width: 110px;">Details</th>
        </tr>
      </thead>

      <tbody>
        <tr v-for="c in cases" :key="c.caseNumber">
          <td>{{ c.caseNumber }}</td>
          <td>{{ c.status }}</td>
          <td>{{ formatDate(c.createdAt) }}</td>

          <!-- Admin/Mortician only -->
          <td v-if="canSeeVerbose">
            {{ verboseByCase[c.caseNumber]?.decedentName ?? "—" }}
          </td>

          <td v-if="canSeeVerbose">
            {{ formatDate(verboseByCase[c.caseNumber]?.statusChangedAt ?? null) }}
          </td>

          <td v-if="canSeeVerbose">
            <button class="btn" type="button" @click="toggleRow(c.caseNumber)">
              {{ openCaseNumber === c.caseNumber ? "Hide" : "Open" }}
            </button>
          </td>
        </tr>
      </tbody>
    </table>

    <!-- Expanded details preview (Admin/Mortician only) -->
    <div v-if="canSeeVerbose && openCaseNumber" class="details card">
      <div class="detailsHead">
        <h2 class="h2">Case Details: {{ openCaseNumber }}</h2>
        <button class="btn" type="button" @click="openCaseNumber = null">Close</button>
      </div>

      <div v-if="detailsError" class="error">{{ detailsError }}</div>
      <div v-else-if="detailsPending" class="muted">Loading details…</div>

      <div v-else-if="openDetails" class="stack">
        <div class="kv"><span class="k">Case #</span><span class="v">{{ openDetails.caseNumber }}</span></div>
        <div class="kv"><span class="k">Status</span><span class="v">{{ openDetails.status }}</span></div>
        <div class="kv"><span class="k">Created</span><span class="v">{{ formatDate(openDetails.createdAt) }}</span></div>
        <div class="kv"><span class="k">Next of Kin</span><span class="v">{{ openDetails.nextOfKinName || "—" }}</span></div>

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
 * Types (match your API output)
 * ----------------------------- */
type PublicCase = {
  caseNumber: string
  status: string // your API returns Status = c.Status.ToString()
  createdAt: string
}

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

  // future-ready: if you later add this to backend, we’ll use it
  statusChangedAt?: string | null
}

/** -----------------------------
 * Auth / role gating
 * ----------------------------- */
const { roles, token, initAuth } = useAuth()

const canSeeVerbose = computed(() => {
  const rs = roles.value ?? []
  return rs.includes("Admin") || rs.includes("Mortician")
})

function authHeaders(): Record<string, string> {
  const t = token.value
  return t ? { Authorization: `Bearer ${t}` } : {}
}

/** -----------------------------
 * Page state
 * ----------------------------- */
const cases = ref<PublicCase[]>([])
const pending = ref(true)
const errorMessage = ref<string | null>(null)

/**
 * For privileged users, we fetch details to show:
 * - decedent name
 * - status changed time (best effort unless backend provides it explicitly)
 */
const verboseByCase = ref<Record<string, { decedentName: string; statusChangedAt: string | null }>>({})

/** details panel (optional) */
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

/**
 * Since your DB/API does NOT currently track a real "status changed at",
 * we do a best-effort estimate from tasks:
 * - pick the latest of (completedAt, startedAt, createdAt) across all tasks
 * - fallback to case.createdAt
 * If you later add CaseFile.StatusChangedAt and return it as statusChangedAt,
 * this function will automatically use it.
 */
function computeStatusChangedAt(d: CaseDetailsDto): string | null {
  if (d.statusChangedAt) return d.statusChangedAt

  const stamps: number[] = []

  // include case created
  stamps.push(new Date(d.createdAt).getTime())

  for (const t of d.tasks ?? []) {
    if (t.completedAt) stamps.push(new Date(t.completedAt).getTime())
    if (t.startedAt) stamps.push(new Date(t.startedAt).getTime())
    if (t.createdAt) stamps.push(new Date(t.createdAt).getTime())
  }

  const max = Math.max(...stamps.filter((n) => !isNaN(n)))
  return isFinite(max) ? new Date(max).toISOString() : null
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
  } finally {
    pending.value = false
  }
}

async function loadVerboseExtras() {
  if (!canSeeVerbose.value) return

  // fetch details for each case number (simple approach; fine for small lists)
  const map: Record<string, { decedentName: string; statusChangedAt: string | null }> = {}

  await Promise.all(
    (cases.value ?? []).map(async (c) => {
      try {
        const d = await $fetch<CaseDetailsDto>(`/api/public/cases/${encodeURIComponent(c.caseNumber)}`, {
          headers: authHeaders(),
        })

        const first = d.decedent?.firstName?.trim() ?? ""
        const last = d.decedent?.lastName?.trim() ?? ""
        const name = (first + " " + last).trim()

        map[c.caseNumber] = {
          decedentName: name || "—",
          statusChangedAt: computeStatusChangedAt(d),
        }
      } catch {
        // If one fails (permissions/404/etc), don’t break the whole page.
        map[c.caseNumber] = {
          decedentName: "—",
          statusChangedAt: null,
        }
      }
    })
  )

  verboseByCase.value = map
}

/** details panel: load one case */
async function toggleRow(caseNumber: string) {
  if (openCaseNumber.value === caseNumber) {
    openCaseNumber.value = null
    openDetails.value = null
    detailsError.value = null
    detailsPending.value = false
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

onMounted(async () => {
  // populate roles/token on refresh if your composable supports it
  try {
    await initAuth()
  } catch {
    // if initAuth isn't implemented or fails, the public list still works
  }

  await loadPublicList()
  await loadVerboseExtras()
})
</script>

<style scoped>
.wrap {
  max-width: 1100px;
  margin: 0 auto;
  padding: 16px;
}
.muted {
  opacity: 0.7;
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

.small {
  font-size: 12px;
}
</style>