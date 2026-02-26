<template>
  <!-- ✅ Outer page wrapper -->
  <main class="page">
    <!-- ✅ Centered content container -->
    <div class="wrap">
      <h1>Public Case Board</h1>

      <p class="muted">
        Shows case status information from the database (public requirement). Admins and Morticians can view additional
        details.
      </p>

      <!-- ✅ Top loading bar (shows whenever the page is loading ANY data) -->
      <div class="loadingBar" :class="{ show: isBusy }" aria-hidden="true">
        <div class="loadingBarInner"></div>
      </div>

      <!-- Status card always renders (so layout doesn't jump) -->
      <div class="card statusCard">
        <div class="row statusRow">
          <div class="muted small statusText">
            Signed in:
            <strong>{{ isLoggedIn ? "Yes" : "No" }}</strong>
            <span v-if="isLoggedIn">
              • Roles: <strong>{{ (authRoles || []).join(", ") || "—" }}</strong>
            </span>
          </div>

          <button class="btn" type="button" @click="refreshAll" :disabled="isBusy">
            {{ isBusy ? "Loading..." : "Refresh" }}
          </button>
        </div>

        <div v-if="!canSeeVerbose" class="muted small" style="margin-top: 8px">
          Limited view: sign in as Admin or Mortician to see decedent and case details.
        </div>

        <div v-else class="muted small" style="margin-top: 8px">Verbose view enabled.</div>
      </div>

      <!-- ✅ Errors -->
      <div v-if="errorMessage" class="error" style="margin-top: 12px">
        {{ errorMessage }}
      </div>

      <!-- ✅ Page skeleton while loading initial list -->
      <div v-if="pending" class="tableWrap" aria-label="Loading cases">
        <table class="table" aria-label="Cases table loading">
          <thead>
            <tr>
              <th>Case #</th>
              <th>Status</th>
              <th>Created</th>

              <th v-if="canSeeVerbose">Decedent</th>
              <th v-if="canSeeVerbose">Assigned Mortician</th>

              <th v-if="canSeeVerbose" class="colDetails">Details</th>
            </tr>
          </thead>

          <tbody>
            <tr v-for="i in skeletonRows" :key="i">
              <td><div class="sk skText wSm"></div></td>
              <td><div class="sk skText wSm"></div></td>
              <td><div class="sk skText wMd"></div></td>

              <td v-if="canSeeVerbose"><div class="sk skText wLg"></div></td>
              <td v-if="canSeeVerbose"><div class="sk skText wLg"></div></td>

              <td v-if="canSeeVerbose">
                <div class="sk skBtn"></div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- ✅ Loaded content -->
      <template v-else>
        <!-- Responsive table wrapper to prevent horizontal overflow on mobile -->
        <div v-if="cases.length" class="tableWrap">
          <table class="table" aria-label="Cases table">
            <thead>
              <tr>
                <th>Case #</th>
                <th>Status</th>
                <th>Created</th>

                <th v-if="canSeeVerbose">Decedent</th>
                <th v-if="canSeeVerbose">Assigned Mortician</th>

                <th v-if="canSeeVerbose" class="colDetails">Details</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="c in cases" :key="c.caseNumber">
                <td class="nowrap">{{ c.caseNumber }}</td>
                <td class="nowrap">{{ c.status }}</td>
                <td class="nowrap">{{ formatDate(c.createdAt) }}</td>

                <td v-if="canSeeVerbose">
                  <template v-if="verbosePending && !verboseByCase[c.caseNumber]">
                    <div class="sk skText wLg" style="height: 14px"></div>
                  </template>
                  <template v-else>
                    {{ verboseByCase[c.caseNumber]?.decedentName ?? "—" }}
                  </template>
                </td>

                <td v-if="canSeeVerbose">
                  <template v-if="verbosePending && !verboseByCase[c.caseNumber]">
                    <div class="sk skText wLg" style="height: 14px"></div>
                  </template>
                  <template v-else>
                    {{ verboseByCase[c.caseNumber]?.assignedMorticianName ?? "—" }}
                  </template>
                </td>

                <td v-if="canSeeVerbose">
                  <button class="btn btnSmall" type="button" @click="toggleRow(c.caseNumber)" :disabled="detailsPending">
                    {{ openCaseNumber === c.caseNumber ? "Hide" : "Open" }}
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

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
                {{
                  openDetails.assignedMortician?.displayName ??
                  openDetails.assignedMortician?.email ??
                  "—"
                }}
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
                  <div class="break">{{ n.text }}</div>
                  <div class="muted small">{{ formatDate(n.createdAt) }}</div>
                </li>
              </ul>
            </div>

            <div class="section">
              <div class="sectionTitle">Equipment Checkouts ({{ openDetails.equipmentCheckouts?.length ?? 0 }})</div>
              <div v-if="!(openDetails.equipmentCheckouts?.length)" class="muted small">No equipment checkouts.</div>
              <ul v-else class="list">
                <li v-for="ec in openDetails.equipmentCheckouts" :key="ec.id">
                  <div class="break">
                    <strong>{{ ec.equipment?.name ?? "Equipment" }}</strong>
                    <span class="muted small" v-if="ec.equipment?.serialNumber">
                      ({{ ec.equipment.serialNumber }})
                    </span>
                  </div>
                  <div class="muted small">
                    Checked out: {{ formatDate(ec.checkedOutAt) }}
                    <span v-if="ec.returnedAt"> • Returned: {{ formatDate(ec.returnedAt) }}</span>
                  </div>
                  <div class="muted small break" v-if="ec.notes">Notes: {{ ec.notes }}</div>
                </li>
              </ul>
            </div>
          </div>

          <div v-else class="muted">No details loaded.</div>
        </div>
      </template>
    </div>
  </main>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue"
import { $fetch } from "ofetch"
import { useRuntimeConfig } from "#imports"
import { useAuth } from "../../composables/useAuth"

type PublicCase = { caseNumber: string; status: string; createdAt: string }
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

const config = useRuntimeConfig()
const apiBase = computed(() => {
  const raw = (config.public.apiBase as unknown as string) || ""
  return raw.replace(/\/+$/, "")
})

function apiUrl(path: string) {
  const p = path.startsWith("/") ? path : `/${path}`
  return apiBase.value ? `${apiBase.value}${p}` : p
}

const { token, roles, isLoggedIn, loadFromStorage, fetchMe } = useAuth()

const authRoles = computed(() => roles.value ?? [])

const canSeeVerbose = computed(() => {
  const rs = authRoles.value
  return rs.includes("Admin") || rs.includes("Mortician")
})

function authHeaders(): Record<string, string> {
  return token.value ? { Authorization: `Bearer ${token.value}` } : {}
}

const cases = ref<PublicCase[]>([])
const pending = ref(true)
const verbosePending = ref(false)
const errorMessage = ref<string | null>(null)

const skeletonRows = 8

const verboseByCase = ref<
  Record<
    string,
    {
      decedentName: string
      assignedMorticianName: string | null
      assignedMorticianUserId: string | null
      assignedMortician: MorticianLite | null
    }
  >
>({})

const openCaseNumber = ref<string | null>(null)
const openDetails = ref<CaseDetailsDto | null>(null)
const detailsPending = ref(false)
const detailsError = ref<string | null>(null)

const isBusy = computed(() => pending.value || verbosePending.value || detailsPending.value)

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

function morticianLabel(m: MorticianLite | null | undefined): string {
  if (!m) return "—"
  return (m.displayName || m.email || m.id || "—").trim() || "—"
}

async function loadPublicList() {
  pending.value = true
  errorMessage.value = null

  try {
    cases.value = await $fetch<PublicCase[]>(apiUrl("/api/public/cases"))
  } catch (e: any) {
    errorMessage.value = e?.data?.message || e?.message || "Failed to load cases."
    cases.value = []
  } finally {
    pending.value = false
  }
}

async function loadVerboseExtras() {
  if (!canSeeVerbose.value) {
    verboseByCase.value = {}
    closeDetails()
    return
  }

  verbosePending.value = true

  const map: Record<
    string,
    {
      decedentName: string
      assignedMorticianName: string | null
      assignedMorticianUserId: string | null
      assignedMortician: MorticianLite | null
    }
  > = {}

  try {
    await Promise.all(
      (cases.value ?? []).map(async (c) => {
        try {
          const d = await $fetch<CaseDetailsDto>(apiUrl(`/api/public/cases/${encodeURIComponent(c.caseNumber)}`), {
            headers: authHeaders(),
          })

          const first = d.decedent?.firstName?.trim() ?? ""
          const last = d.decedent?.lastName?.trim() ?? ""
          const decedentName = (first + " " + last).trim() || "—"

          let assigned: MorticianLite | null = null
          let assignedUserId: string | null = (d.assignedMorticianUserId ?? null) as any

          try {
            const fixed = await $fetch<{ assignedMortician: MorticianLite | null; assignedMorticianUserId?: string | null }>(
              apiUrl(`/api/cases/${encodeURIComponent(c.caseNumber)}`),
              { headers: authHeaders() }
            )
            assigned = fixed?.assignedMortician ?? null
            assignedUserId = (fixed?.assignedMorticianUserId ?? assignedUserId) as any
          } catch {
            assigned = d.assignedMortician ?? null
          }

          map[c.caseNumber] = {
            decedentName,
            assignedMortician: assigned,
            assignedMorticianName: morticianLabel(assigned),
            assignedMorticianUserId: assignedUserId,
          }
        } catch {
          map[c.caseNumber] = {
            decedentName: "—",
            assignedMortician: null,
            assignedMorticianName: null,
            assignedMorticianUserId: null,
          }
        }
      })
    )

    verboseByCase.value = map
  } finally {
    verbosePending.value = false
  }
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
    openDetails.value = await $fetch<CaseDetailsDto>(apiUrl(`/api/public/cases/${encodeURIComponent(caseNumber)}`), {
      headers: authHeaders(),
    })

    try {
      const fixed = await $fetch<{ assignedMortician: MorticianLite | null }>(apiUrl(`/api/cases/${encodeURIComponent(caseNumber)}`), {
        headers: authHeaders(),
      })
      if (openDetails.value) {
        openDetails.value.assignedMortician = fixed?.assignedMortician ?? openDetails.value.assignedMortician ?? null
      }
    } catch {
      // ignore
    }
  } catch (e: any) {
    detailsError.value = e?.data?.message || e?.message || "Failed to load case details."
  } finally {
    detailsPending.value = false
  }
}

async function refreshAll() {
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
/* ✅ Full-width page area (keeps content off the edges) */
.page {
  width: 100%;
}

/* ✅ Centered container */
.wrap {
  max-width: 980px; /* a bit narrower than before so it doesn't hug the edges */
  margin: 0 auto;
  padding: 24px 18px;
  width: 100%;
  box-sizing: border-box;
  overflow-x: clip;
  position: relative;
}

/* Common utilities */
.row {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
  min-width: 0;
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

/* Top loading bar */
.loadingBar {
  position: sticky;
  top: 0;
  height: 4px;
  width: 100%;
  border-radius: 999px;
  overflow: hidden;
  opacity: 0;
  transform: translateY(-6px);
  transition: opacity 180ms ease, transform 180ms ease;
  background: rgba(17, 24, 39, 0.08);
  z-index: 10;
  margin: 10px 0 6px;
}

.loadingBar.show {
  opacity: 1;
  transform: translateY(0);
}

.loadingBarInner {
  height: 100%;
  width: 40%;
  border-radius: 999px;
  background: rgba(17, 24, 39, 0.55);
  animation: loadingSlide 1.1s ease-in-out infinite;
}

@keyframes loadingSlide {
  0% {
    transform: translateX(-120%);
  }
  50% {
    transform: translateX(120%);
  }
  100% {
    transform: translateX(260%);
  }
}

/* Card */
.card {
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 12px;
  margin-top: 14px;
}

.statusCard {
  margin-bottom: 14px;
}

.statusRow {
  justify-content: space-between;
  align-items: center;
}

.statusText {
  min-width: 0;
  overflow-wrap: anywhere;
}

/* Table wrapper */
.tableWrap {
  width: 100%;
  max-width: 100%;
  overflow-x: auto;
  -webkit-overflow-scrolling: touch;
  border-radius: 12px;
}

.tableWrap::-webkit-scrollbar {
  height: 10px;
}
.tableWrap::-webkit-scrollbar-thumb {
  background: rgba(17, 24, 39, 0.25);
  border-radius: 999px;
}

.table {
  width: 100%;
  border-collapse: collapse;
  margin-top: 12px;
  min-width: 720px;
  background: white;
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

.colDetails {
  width: 110px;
}

.nowrap {
  white-space: nowrap;
}

/* Buttons */
.btn {
  padding: 6px 10px;
  border-radius: 6px;
  border: 1px solid #111827;
  background: white;
  cursor: pointer;
  flex: 0 0 auto;
}
.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
.btnSmall {
  padding: 6px 10px;
}

/* Details */
.detailsHead {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
  min-width: 0;
}

.h2 {
  margin: 0;
  font-size: 18px;
  min-width: 0;
  overflow-wrap: anywhere;
}

.stack {
  display: grid;
  gap: 10px;
  margin-top: 10px;
  min-width: 0;
}

.kv {
  display: grid;
  grid-template-columns: 180px 1fr;
  gap: 10px;
  min-width: 0;
}

.k {
  font-weight: 600;
}

.v {
  min-width: 0;
  overflow-wrap: anywhere;
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

.break {
  overflow-wrap: anywhere;
  word-break: break-word;
}

/* Skeleton */
.sk {
  position: relative;
  overflow: hidden;
  border-radius: 8px;
  background: rgba(17, 24, 39, 0.08);
}

.sk::after {
  content: "";
  position: absolute;
  inset: 0;
  transform: translateX(-120%);
  background: linear-gradient(
    90deg,
    rgba(255, 255, 255, 0) 0%,
    rgba(255, 255, 255, 0.55) 50%,
    rgba(255, 255, 255, 0) 100%
  );
  animation: shimmer 1.1s ease-in-out infinite;
}

@keyframes shimmer {
  0% {
    transform: translateX(-120%);
  }
  100% {
    transform: translateX(120%);
  }
}

.skText {
  height: 14px;
}
.skBtn {
  height: 30px;
  width: 72px;
  border-radius: 10px;
}
.wSm {
  width: 90px;
}
.wMd {
  width: 150px;
}
.wLg {
  width: 220px;
}

/* Mobile */
@media (max-width: 720px) {
  .wrap {
    padding: 14px 12px;
  }

  .statusRow {
    align-items: stretch;
    gap: 10px;
  }

  .btn {
    width: 100%;
    justify-content: center;
  }

  .table {
    min-width: 640px;
  }

  .kv {
    grid-template-columns: 1fr;
  }

  .k {
    opacity: 0.85;
  }

  .loadingBar {
    margin-top: 8px;
  }
}
</style>