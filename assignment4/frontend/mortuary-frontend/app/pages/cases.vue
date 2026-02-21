<template>
  <div class="page">
    <header class="topbar">
      <div>
        <h1>Cases</h1>
        <p class="muted">Create case files, review assigned cases, and progress workflow tasks.</p>
        <p class="muted small">
          Signed in: <strong>{{ isLoggedIn ? "Yes" : "No" }}</strong>
          <span v-if="isLoggedIn"> • Roles: <strong>{{ authRoles.join(", ") || "—" }}</strong> </span>
        </p>
      </div>

      <div class="actions">
        <button class="btn" @click="refreshAll" :disabled="loadingAny">
          {{ loadingAny ? "Refreshing..." : "Refresh" }}
        </button>
      </div>
    </header>

    <!-- Access gate -->
    <section v-if="!canUsePage" class="card">
      <h2>Access required</h2>
      <p class="muted">
        This page is only available to users with <strong>Mortician</strong> or <strong>Admin</strong> role.
      </p>
      <p class="muted small">
        If you just logged in, click Refresh. If it still fails, your account may not have the correct role.
      </p>
    </section>

    <template v-else>
      <!-- Mode chooser -->
      <section class="card">
        <div class="modeHeader">
          <h2>What would you like to do?</h2>
          <div class="modePills">
            <button class="chip" :class="{ active: viewMode === 'open' }" @click="viewMode = 'open'">
              Open Existing Case
            </button>
            <button class="chip" :class="{ active: viewMode === 'create' }" @click="viewMode = 'create'">
              Create New Case
            </button>
          </div>
        </div>

        <div v-if="viewMode === 'open'" class="muted">
          Morticians see their assigned cases. Admins can view all cases and assign/reassign morticians.
        </div>
        <div v-else class="muted">
          Enter case details below to create a new case file.
          <span v-if="isMortician" class="muted"> If you are a Mortician, you should be auto-assigned (server-side). </span>
        </div>
      </section>

      <!-- Create case -->
      <section v-if="viewMode === 'create'" class="card">
        <h2>Create case file</h2>

        <form class="formGrid" @submit.prevent="createCase">
          <label class="field">
            <span>Case Number *</span>
            <input v-model.trim="createForm.caseNumber" placeholder="CASE-1007" required />
          </label>

          <label class="field">
            <span>Next of Kin Name</span>
            <input v-model.trim="createForm.nextOfKinName" placeholder="Jane Doe" />
          </label>

          <div class="dividerRow">
            <strong>Decedent</strong>
            <span class="muted">(optional, but recommended)</span>
          </div>

          <label class="field">
            <span>First Name</span>
            <input v-model.trim="createForm.decedentFirstName" placeholder="John" />
          </label>

          <label class="field">
            <span>Last Name</span>
            <input v-model.trim="createForm.decedentLastName" placeholder="Doe" />
          </label>

          <label class="field">
            <span>Date of Birth</span>
            <input v-model="createForm.dateOfBirth" type="date" />
          </label>

          <label class="field">
            <span>Date of Death</span>
            <input v-model="createForm.dateOfDeath" type="date" />
          </label>

          <label class="field">
            <span>Place of Death</span>
            <input v-model.trim="createForm.placeOfDeath" placeholder="Spokane, WA" />
          </label>

          <label class="field">
            <span>Tag Number</span>
            <input v-model.trim="createForm.tagNumber" placeholder="TAG-01234" />
          </label>

          <label class="field">
            <span>Storage Location</span>
            <input v-model.trim="createForm.storageLocation" placeholder="Cooler A - Shelf 2" />
          </label>

          <div class="formActions">
            <button class="btn primary" type="submit" :disabled="creating || !createForm.caseNumber">
              {{ creating ? "Creating..." : "Create Case" }}
            </button>
            <button class="btn" type="button" @click="resetCreateForm" :disabled="creating">Clear</button>
            <button class="btn" type="button" @click="viewMode = 'open'" :disabled="creating">Back to list</button>
          </div>
        </form>

        <p v-if="createError" class="error">{{ createError }}</p>
        <p v-if="createOk" class="ok">{{ createOk }}</p>
      </section>

      <!-- Main workspace -->
      <main v-if="viewMode === 'open'" class="grid">
        <!-- LEFT -->
        <section class="card">
          <div class="listHeader">
            <h2>Case list</h2>

            <div class="chips">
              <button class="chip" :class="{ active: listMode === 'mine' }" @click="listMode = 'mine'">My Cases</button>

              <button
                v-if="isAdmin"
                class="chip"
                :class="{ active: listMode === 'unassigned' }"
                @click="listMode = 'unassigned'"
                title="Admin: only cases with no assigned mortician"
              >
                Unassigned
              </button>

              <button
                v-if="isAdmin"
                class="chip"
                :class="{ active: listMode === 'all' }"
                @click="listMode = 'all'"
                title="Admin: all cases"
              >
                All Cases
              </button>
            </div>
          </div>

          <div class="searchRow">
            <input v-model.trim="q" placeholder="Search case number..." />
          </div>

          <div v-if="loadingList" class="muted">Loading cases...</div>
          <p v-if="listError" class="error">{{ listError }}</p>

          <ul class="caseList" v-if="filteredCases.length">
            <li
              v-for="c in filteredCases"
              :key="c.caseNumber"
              class="caseRow"
              :class="{ selected: selectedCase?.caseNumber === c.caseNumber }"
              @click="selectCase(c.caseNumber)"
            >
              <div class="rowTop">
                <strong>{{ c.caseNumber }}</strong>
                <span class="badge" :class="caseStatusClass(c.status)">{{ c.status }}</span>
              </div>

              <div class="rowBottom">
                <span class="muted">Created: {{ formatDateTime(c.createdAt) }}</span>
                <span class="muted" v-if="c.decedentName">Decedent: {{ c.decedentName }}</span>
                <span class="muted" v-else>Decedent: —</span>
              </div>

              <div class="rowBottom">
                <span class="muted">Assigned: {{ morticianLabel(c.assignedMortician) }}</span>
              </div>

              <div v-if="isAdmin && !c.assignedMortician" class="muted small" style="margin-top: 6px">Needs assignment</div>
            </li>
          </ul>

          <div v-else class="muted">No cases found.</div>
        </section>

        <!-- RIGHT -->
        <section class="card">
          <h2>Case details</h2>

          <div v-if="!selectedCase" class="muted">Select a case to view details.</div>

          <div v-else>
            <div class="detailHeader">
              <div>
                <h3 class="caseTitle">{{ selectedCase.caseNumber }}</h3>

                <div class="detailMeta">
                  <span class="badge" :class="caseStatusClass(selectedCase.status)">{{ selectedCase.status }}</span>
                  <span class="muted">Created: {{ formatDateTime(selectedCase.createdAt) }}</span>
                </div>

                <div class="detailMeta">
                  <span class="muted">Next of Kin: {{ selectedCase.nextOfKinName || "—" }}</span>
                </div>

                <div class="detailMeta">
                  <span class="muted">Assigned: {{ morticianLabel(selectedCase.assignedMortician) }}</span>
                </div>
              </div>

              <div class="detailActions">
                <button class="btn" @click="reloadSelected" :disabled="loadingCase">
                  {{ loadingCase ? "Loading..." : "Reload" }}
                </button>

                <!-- Admin assignment UI -->
                <div v-if="isAdmin" class="assignBox">
                  <label class="miniField">
                    <span class="muted">Assign / Reassign mortician</span>
                    <select
                      :value="selectedMorticianId || ''"
                      @change="selectedMorticianId = ($event.target as HTMLSelectElement).value"
                      :disabled="assigning || morticianOptions.length === 0"
                    >
                      <option value="" disabled>Select mortician…</option>
                      <option v-for="m in morticianOptions" :key="m.id" :value="m.id">{{ morticianLabel(m) }}</option>
                    </select>
                  </label>

                  <div class="assignActions">
                    <button class="btn primary" @click="assignMorticianToSelected(selectedMorticianId)" :disabled="assigning || !selectedMorticianId">
                      {{ assigning ? "Saving..." : selectedCase.assignedMortician ? "Reassign" : "Assign" }}
                    </button>
                  </div>

                  <div class="muted small" v-if="morticianOptions.length === 0">No mortician users found.</div>
                </div>

                <!-- Admin complete button stays available (requires all done) -->
                <button
                  class="btn primary"
                  v-if="isAdmin && selectedCase.status !== 'Completed'"
                  @click="markCaseCompleted"
                  :disabled="completingCase || !allWorkflowDone"
                  title="Requires all workflow tasks to be Done"
                >
                  {{ completingCase ? "Completing..." : "Mark Completed" }}
                </button>
              </div>
            </div>

            <p v-if="assignError" class="error">{{ assignError }}</p>
            <p v-if="assignOk" class="ok">{{ assignOk }}</p>

            <div class="subCard" v-if="selectedCase.decedent">
              <div class="subTitle">Decedent</div>
              <div class="kv">
                <div><span class="muted">Name:</span> {{ decedentFullName(selectedCase.decedent) }}</div>
                <div><span class="muted">DOB:</span> {{ formatDate(selectedCase.decedent.dateOfBirth) }}</div>
                <div><span class="muted">DOD:</span> {{ formatDate(selectedCase.decedent.dateOfDeath) }}</div>
                <div><span class="muted">Place of Death:</span> {{ selectedCase.decedent.placeOfDeath ?? "—" }}</div>
                <div><span class="muted">Tag #:</span> {{ selectedCase.decedent.tagNumber ?? "—" }}</div>
                <div><span class="muted">Storage:</span> {{ selectedCase.decedent.storageLocation ?? "—" }}</div>
              </div>
            </div>
            <div class="subCard" v-else>
              <div class="muted">No decedent info on file.</div>
            </div>

            <hr class="sep" />

            <!-- =========================
                 WORKFLOW
                 Mortician: wizard (single step)
                 Admin: full list (unchanged)
                 ========================= -->

            <!-- Mortician wizard -->
            <div v-if="isMorticianWizard" class="wizard">
              <div class="tasksHeader">
                <h3>Workflow step</h3>
                <span class="muted">
                  Step {{ sortedTasks.length ? wizardStepIndex + 1 : 0 }} / {{ sortedTasks.length }} •
                  <span :class="{ okText: allWorkflowDone, warnText: !allWorkflowDone }">
                    {{ allWorkflowDone ? "All Done" : "In Progress" }}
                  </span>
                </span>
              </div>

              <p v-if="taskError" class="error">{{ taskError }}</p>

              <div v-if="!sortedTasks.length" class="muted">
                No workflow tasks exist for this case.
                <div class="muted">(Tip: seed WorkflowStepTemplate rows and auto-create tasks on case creation.)</div>
              </div>

              <div v-else-if="activeTask" class="wizardCard">
                <div class="wizardTop">
                  <div>
                    <div class="wizardTitle">
                      <strong>{{ activeTask.workflowStepTemplate.name }}</strong>
                      <span class="badge" style="margin-left: 8px">{{ activeTask.status }}</span>
                    </div>
                    <div class="muted" v-if="activeTask.workflowStepTemplate.description">
                      {{ activeTask.workflowStepTemplate.description }}
                    </div>
                    <div class="muted small">
                      Started: {{ formatDateTime(activeTask.startedAt) }} • Completed: {{ formatDateTime(activeTask.completedAt) }}
                    </div>
                  </div>

                  <div class="wizardNav">
                    <button class="btn" @click="prevStep" :disabled="wizardStepIndex <= 0 || updatingTaskId === activeTask.id">Prev</button>
                    <button class="btn" @click="nextStep" :disabled="wizardStepIndex >= sortedTasks.length - 1 || updatingTaskId === activeTask.id">
                      Next
                    </button>
                  </div>
                </div>

                <div class="wizardForm">
                  <label class="miniField">
                    <span class="muted">Status</span>
                    <select
                      :value="draftFor(activeTask).status"
                      :disabled="selectedCase.status === 'Completed' || updatingTaskId === activeTask.id"
                      @change="onTaskStatusChange(activeTask, ($event.target as HTMLSelectElement).value)"
                    >
                      <option value="Todo">Todo</option>
                      <option value="InProgress">InProgress</option>
                      <option value="Blocked">Blocked</option>
                      <option value="Done">Done</option>
                    </select>
                  </label>

                  <label class="miniField">
                    <span class="muted">Notes</span>
                    <textarea
                      :value="draftFor(activeTask).notes ?? ''"
                      :disabled="selectedCase.status === 'Completed' || updatingTaskId === activeTask.id"
                      rows="4"
                      placeholder="Add notes..."
                      @change="onTaskNotesChange(activeTask, ($event.target as HTMLTextAreaElement).value)"
                    />
                  </label>

                  <div class="wizardActions">
                    <button class="btn" :disabled="selectedCase.status === 'Completed' || updatingTaskId === activeTask.id" @click="saveTask(activeTask)">
                      {{ updatingTaskId === activeTask.id ? "Saving..." : "Save" }}
                    </button>

                    <button
                      class="btn primary"
                      v-if="selectedCase.status !== 'Completed'"
                      @click="completeIfAllDone"
                      :disabled="completingCase || !allWorkflowDone"
                      title="Enabled when all steps are Done"
                    >
                      {{ completingCase ? "Completing..." : "Complete Case" }}
                    </button>
                  </div>

                  <div class="muted small" v-if="!allWorkflowDone">
                    Complete all steps (set each to <strong>Done</strong>) to finish the case.
                  </div>
                </div>
              </div>

              <div v-else class="muted">No workflow tasks exist for this case.</div>
            </div>

            <!-- Admin (or non-wizard): full list -->
            <div v-else>
              <div class="tasksHeader">
                <h3>Workflow steps</h3>
                <span class="muted">
                  {{ tasks.length }} steps •
                  <span :class="{ okText: allWorkflowDone, warnText: !allWorkflowDone }">
                    {{ allWorkflowDone ? "All Done" : "Not Complete" }}
                  </span>
                </span>
              </div>

              <p v-if="taskError" class="error">{{ taskError }}</p>

              <ul class="taskList" v-if="tasks.length">
                <li v-for="t in tasks" :key="t.id" class="taskRow">
                  <div class="taskLeft">
                    <div class="taskName">
                      <strong>{{ t.workflowStepTemplate.name }}</strong>
                      <div class="muted" v-if="t.workflowStepTemplate.description">{{ t.workflowStepTemplate.description }}</div>
                      <div class="muted">Started: {{ formatDateTime(t.startedAt) }} • Completed: {{ formatDateTime(t.completedAt) }}</div>
                    </div>
                  </div>

                  <div class="taskRight">
                    <label class="miniField">
                      <span class="muted">Status</span>
                      <select
                        :value="draftFor(t).status"
                        :disabled="selectedCase.status === 'Completed' || updatingTaskId === t.id"
                        @change="onTaskStatusChange(t, ($event.target as HTMLSelectElement).value)"
                      >
                        <option value="Todo">Todo</option>
                        <option value="InProgress">InProgress</option>
                        <option value="Blocked">Blocked</option>
                        <option value="Done">Done</option>
                      </select>
                    </label>

                    <label class="miniField">
                      <span class="muted">Notes</span>
                      <textarea
                        :value="draftFor(t).notes ?? ''"
                        :disabled="selectedCase.status === 'Completed' || updatingTaskId === t.id"
                        rows="2"
                        placeholder="Add notes..."
                        @change="onTaskNotesChange(t, ($event.target as HTMLTextAreaElement).value)"
                      />
                    </label>

                    <button class="btn" :disabled="selectedCase.status === 'Completed' || updatingTaskId === t.id" @click="saveTask(t)">
                      {{ updatingTaskId === t.id ? "Saving..." : "Save" }}
                    </button>
                  </div>
                </li>
              </ul>

              <div v-else class="muted">
                No workflow tasks exist for this case.
                <div class="muted">(Tip: seed active WorkflowStepTemplate rows and auto-create tasks on case creation.)</div>
              </div>
            </div>

            <hr class="sep" />

            <!-- Case notes -->
            <div class="tasksHeader">
              <h3>Case notes</h3>
              <span class="muted">{{ notes.length }} notes</span>
            </div>

            <form class="noteCreate" @submit.prevent="addNote">
              <textarea v-model.trim="newNoteText" rows="3" placeholder="Write a case note..." :disabled="addingNote || selectedCase.status === 'Completed'" />
              <button class="btn primary" type="submit" :disabled="addingNote || !newNoteText || selectedCase.status === 'Completed'">
                {{ addingNote ? "Adding..." : "Add Note" }}
              </button>
            </form>

            <p v-if="noteError" class="error">{{ noteError }}</p>

            <ul class="noteList" v-if="notes.length">
              <li v-for="n in notes" :key="n.id" class="noteRow">
                <div class="noteText">{{ n.text }}</div>
                <div class="muted">Created: {{ formatDateTime(n.createdAt) }}</div>
              </li>
            </ul>
            <div v-else class="muted">No notes yet.</div>

            <p v-if="caseError" class="error">{{ caseError }}</p>
            <p v-if="caseOk" class="ok">{{ caseOk }}</p>
          </div>
        </section>
      </main>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from "vue"
import { useRuntimeConfig } from "#imports"
import { useAuth } from "../../composables/useAuth"

/** Auth */
const { token, roles, isLoggedIn, loadFromStorage, fetchMe } = useAuth()

const authRoles = computed(() => roles.value ?? [])
const isAdmin = computed(() => authRoles.value.includes("Admin"))
const isMortician = computed(() => authRoles.value.includes("Mortician"))
const canUsePage = computed(() => isAdmin.value || isMortician.value)

function authHeaders(): Record<string, string> {
  return token.value ? { Authorization: `Bearer ${token.value}` } : {}
}

const config = useRuntimeConfig()
const API_BASE = computed(() => (config.public as any)?.apiBase ?? "")

function joinUrl(base: string, path: string): string {
  const b = (base ?? "").trim()
  if (!b) return path
  const b2 = b.endsWith("/") ? b.slice(0, -1) : b
  const p2 = path.startsWith("/") ? path : `/${path}`
  return `${b2}${p2}`
}

/** Types */
type MorticianDto = { id: string; email: string | null; displayName: string | null }

type DecedentDto = {
  id: number
  firstName: string
  lastName: string
  dateOfBirth: string | null
  dateOfDeath: string | null
  placeOfDeath: string | null
  tagNumber: string | null
  storageLocation: string | null
}

type CaseListItemDto = {
  caseNumber: string
  status: string
  createdAt: string
  nextOfKinName: string
  decedentName: string | null
  assignedMortician: MorticianDto | null
}

type CaseDetailsDto = {
  id: number
  caseNumber: string
  status: string
  createdAt: string
  nextOfKinName: string
  assignedMortician: MorticianDto | null
  decedent: DecedentDto | null
}

type WorkflowStepTemplateDto = {
  id: number
  name: string
  description: string | null
  sortOrder: number
}

type CaseTaskStatus = "Todo" | "InProgress" | "Blocked" | "Done"

type CaseTaskDto = {
  id: number
  workflowStepTemplate: WorkflowStepTemplateDto
  status: CaseTaskStatus
  notes: string | null
  createdAt: string
  startedAt: string | null
  completedAt: string | null
}

type CaseNoteDto = {
  id: number
  text: string
  createdAt: string
}

/** UI State */
const viewMode = ref<"open" | "create">("open")
const listMode = ref<"mine" | "unassigned" | "all">("mine")

const q = ref<string>("")
const cases = ref<CaseListItemDto[]>([])
const selectedCase = ref<CaseDetailsDto | null>(null)
const tasks = ref<CaseTaskDto[]>([])
const notes = ref<CaseNoteDto[]>([])

const loadingList = ref(false)
const loadingCase = ref(false)
const creating = ref(false)
const completingCase = ref(false)
const addingNote = ref(false)
const assigning = ref(false)

const listError = ref("")
const caseError = ref("")
const caseOk = ref("")
const createError = ref("")
const createOk = ref("")
const noteError = ref("")
const taskError = ref("")
const assignError = ref("")
const assignOk = ref("")

const updatingTaskId = ref<number | null>(null)

/** Admin assignment state */
const morticianOptions = ref<MorticianDto[]>([])
const selectedMorticianId = ref<string>("")

/** Create form */
const createForm = reactive({
  caseNumber: "",
  nextOfKinName: "",
  decedentFirstName: "",
  decedentLastName: "",
  dateOfBirth: "",
  dateOfDeath: "",
  placeOfDeath: "",
  tagNumber: "",
  storageLocation: "",
})

/** Helpers */
const loadingAny = computed(() => loadingList.value || loadingCase.value || creating.value || assigning.value)

function morticianLabel(m: MorticianDto | null | undefined): string {
  if (!m) return "—"
  return (m.displayName || m.email || m.id || "—").trim() || "—"
}

function formatDateTime(s: string | null | undefined): string {
  if (!s) return "—"
  const d = new Date(s)
  return isNaN(d.getTime()) ? String(s) : d.toLocaleString()
}

function formatDate(s: string | null | undefined): string {
  if (!s) return "—"
  const d = new Date(s)
  return isNaN(d.getTime()) ? String(s) : d.toLocaleDateString()
}

function caseStatusClass(status: string): string {
  switch (status) {
    case "Completed":
      return "b-completed"
    case "ServiceScheduled":
      return "b-scheduled"
    case "ReadyForViewing":
      return "b-viewing"
    case "InPreparation":
      return "b-prep"
    case "Intake":
    default:
      return "b-intake"
  }
}

function decedentFullName(d: DecedentDto): string {
  return `${d.firstName ?? ""} ${d.lastName ?? ""}`.trim() || "—"
}

function resetCreateForm(): void {
  createForm.caseNumber = ""
  createForm.nextOfKinName = ""
  createForm.decedentFirstName = ""
  createForm.decedentLastName = ""
  createForm.dateOfBirth = ""
  createForm.dateOfDeath = ""
  createForm.placeOfDeath = ""
  createForm.tagNumber = ""
  createForm.storageLocation = ""
  createError.value = ""
  createOk.value = ""
}

function toNullableDateIso(yyyyMMdd: string): string | null {
  const s = (yyyyMMdd ?? "").trim()
  if (!s) return null
  const d = new Date(s)
  return isNaN(d.getTime()) ? null : d.toISOString()
}

function updateListAssignedMortician(caseNumber: string, m: MorticianDto | null): void {
  const idx = cases.value.findIndex((c) => c.caseNumber === caseNumber)
  if (idx < 0) return
  const row = cases.value[idx]
  if (!row) return
  row.assignedMortician = m
}

/** API helper */
async function apiFetch<T>(url: string, options: RequestInit = {}): Promise<T> {
  const headers: Record<string, string> = {
    ...(options.headers as Record<string, string> | undefined),
  }

  if (!(options.body instanceof FormData)) {
    headers["Content-Type"] = headers["Content-Type"] ?? "application/json"
  }

  const auth = authHeaders()
  if (auth.Authorization) headers["Authorization"] = auth.Authorization

  const fullUrl = joinUrl(API_BASE.value, url)
  const res = await fetch(fullUrl, { ...options, headers, cache: "no-store" })

  if (!res.ok) {
    const txt = await res.text()
    try {
      const j = JSON.parse(txt)
      throw new Error(j?.message ?? `Request failed (${res.status})`)
    } catch {
      throw new Error(txt || `Request failed (${res.status})`)
    }
  }

  if (res.status === 204) return null as unknown as T
  return (await res.json()) as T
}

/** Filtering */
const filteredCases = computed<CaseListItemDto[]>(() => {
  const needle = q.value.toLowerCase()
  return (cases.value ?? [])
    .filter((c) => c.caseNumber.toLowerCase().includes(needle))
    .sort((a, b) => (b.createdAt ?? "").localeCompare(a.createdAt ?? ""))
})

/** Sorted tasks for wizard */
const sortedTasks = computed<CaseTaskDto[]>(() => {
  const arr = tasks.value ?? []
  return [...arr].sort((a, b) => {
    const ao = a.workflowStepTemplate?.sortOrder ?? 9999
    const bo = b.workflowStepTemplate?.sortOrder ?? 9999
    if (ao !== bo) return ao - bo
    return (a.createdAt ?? "").localeCompare(b.createdAt ?? "")
  })
})

const allWorkflowDone = computed<boolean>(() => {
  if (!tasks.value.length) return false
  return tasks.value.every((t) => t.status === "Done")
})

/** Mortician wizard mode: Only morticians (and not admin) see the wizard. */
const isMorticianWizard = computed(() => isMortician.value && !isAdmin.value)

/** Wizard state */
const wizardStepIndex = ref(0)

/** ✅ FIX: activeTask can be null when there are no tasks */
const activeTask = computed<CaseTaskDto | null>(() => {
  const arr = sortedTasks.value
  if (!arr.length) return null
  const idx = Math.min(Math.max(wizardStepIndex.value, 0), arr.length - 1)
  return arr[idx] ?? null
})

function ensureWizardIndexValid() {
  const len = sortedTasks.value.length
  if (len <= 0) {
    wizardStepIndex.value = 0
    return
  }
  if (wizardStepIndex.value < 0) wizardStepIndex.value = 0
  if (wizardStepIndex.value > len - 1) wizardStepIndex.value = len - 1
}

function prevStep() {
  wizardStepIndex.value = Math.max(0, wizardStepIndex.value - 1)
}

function nextStep() {
  wizardStepIndex.value = Math.min(sortedTasks.value.length - 1, wizardStepIndex.value + 1)
}

/** Load cases */
async function loadCases(): Promise<void> {
  loadingList.value = true
  listError.value = ""
  try {
    if (isMortician.value && !isAdmin.value) {
      const mine = await apiFetch<CaseListItemDto[]>(`/api/cases?scope=mine`)
      cases.value = mine ?? []
      return
    }

    if (isAdmin.value) {
      if (listMode.value === "mine") {
        const mine = await apiFetch<CaseListItemDto[]>(`/api/cases?scope=mine`)
        cases.value = mine ?? []
        return
      }

      const all = await apiFetch<CaseListItemDto[]>(`/api/cases?scope=all`)
      if (listMode.value === "unassigned") {
        cases.value = (all ?? []).filter((c) => !c.assignedMortician)
      } else {
        cases.value = all ?? []
      }
      return
    }

    cases.value = []
  } catch (e: unknown) {
    listError.value = e instanceof Error ? e.message : "Failed to load cases."
    cases.value = []
  } finally {
    loadingList.value = false
  }
}

async function selectCase(caseNumber: string): Promise<void> {
  selectedCase.value = null
  tasks.value = []
  notes.value = []
  caseError.value = ""
  caseOk.value = ""
  assignError.value = ""
  assignOk.value = ""
  selectedMorticianId.value = ""
  wizardStepIndex.value = 0
  await loadCaseAndChildren(caseNumber)
}

async function loadCaseAndChildren(caseNumber: string): Promise<void> {
  loadingCase.value = true
  taskError.value = ""
  noteError.value = ""
  try {
    selectedCase.value = await apiFetch<CaseDetailsDto>(`/api/cases/${encodeURIComponent(caseNumber)}`)
    tasks.value = await apiFetch<CaseTaskDto[]>(`/api/cases/${encodeURIComponent(caseNumber)}/tasks`)
    notes.value = await apiFetch<CaseNoteDto[]>(`/api/cases/${encodeURIComponent(caseNumber)}/notes`)

    if (isAdmin.value && selectedCase.value) {
      selectedMorticianId.value = selectedCase.value.assignedMortician?.id ?? ""
    }

    if (selectedCase.value?.assignedMortician) {
      updateListAssignedMortician(selectedCase.value.caseNumber, selectedCase.value.assignedMortician)
    }

    // Wizard starts at the first NOT done step
    if (isMorticianWizard.value) {
      const arr = [...(tasks.value ?? [])].sort((a, b) => {
        const ao = a.workflowStepTemplate?.sortOrder ?? 9999
        const bo = b.workflowStepTemplate?.sortOrder ?? 9999
        if (ao !== bo) return ao - bo
        return (a.createdAt ?? "").localeCompare(b.createdAt ?? "")
      })
      const idx = arr.findIndex((t) => t.status !== "Done")
      wizardStepIndex.value = idx >= 0 ? idx : Math.max(arr.length - 1, 0)
      ensureWizardIndexValid()
    }
  } catch (e: unknown) {
    caseError.value = e instanceof Error ? e.message : "Failed to load case."
  } finally {
    loadingCase.value = false
  }
}

async function reloadSelected(): Promise<void> {
  const sc = selectedCase.value
  if (!sc) return
  await loadCaseAndChildren(sc.caseNumber)
}

/** Create case */
async function createCase(): Promise<void> {
  creating.value = true
  createError.value = ""
  createOk.value = ""
  try {
    const payload = {
      caseNumber: createForm.caseNumber.trim(),
      nextOfKinName: createForm.nextOfKinName.trim() || null,
      decedent: {
        firstName: createForm.decedentFirstName.trim() || null,
        lastName: createForm.decedentLastName.trim() || null,
        dateOfBirth: createForm.dateOfBirth ? toNullableDateIso(createForm.dateOfBirth) : null,
        dateOfDeath: createForm.dateOfDeath ? toNullableDateIso(createForm.dateOfDeath) : null,
        placeOfDeath: createForm.placeOfDeath.trim() || null,
        tagNumber: createForm.tagNumber.trim() || null,
        storageLocation: createForm.storageLocation.trim() || null,
      },
    }

    const created = await apiFetch<{ caseNumber: string }>(`/api/cases`, {
      method: "POST",
      body: JSON.stringify(payload),
    })

    createOk.value = `Created ${created.caseNumber}`
    await loadCases()
    await selectCase(created.caseNumber)
    viewMode.value = "open"
    resetCreateForm()
  } catch (e: unknown) {
    createError.value = e instanceof Error ? e.message : "Failed to create case."
  } finally {
    creating.value = false
  }
}

/** Admin: load morticians */
type AdminUserDtoLoosely = any

function normalizeAdminUser(u: AdminUserDtoLoosely): MorticianDto & { roles: string[] } {
  const id = String(u?.id ?? u?.Id ?? u?.userId ?? u?.UserId ?? "")
  const email = (u?.email ?? u?.Email ?? null) as string | null
  const displayName = (u?.displayName ?? u?.DisplayName ?? null) as string | null
  const rolesArr = (u?.roles ?? u?.Roles ?? []) as unknown
  const rolesNorm = Array.isArray(rolesArr) ? rolesArr.map(String) : []
  return { id, email, displayName, roles: rolesNorm }
}

async function loadMorticiansIfAdmin(): Promise<void> {
  morticianOptions.value = []
  if (!isAdmin.value) return

  try {
    const raw = await apiFetch<any>(`/api/admin/users`)
    const arr: any[] = Array.isArray(raw) ? raw : []

    if (arr.length && typeof arr[0] === "string") {
      morticianOptions.value = arr.map((id: string) => ({ id, email: null, displayName: id }))
      return
    }

    const normalized = arr.map(normalizeAdminUser).filter((u) => !!u.id)

    morticianOptions.value = normalized
      .filter((u) => (u.roles ?? []).includes("Mortician"))
      .map((u) => ({
        id: u.id,
        email: u.email ?? null,
        displayName: u.displayName ?? u.email ?? u.id,
      }))
      .sort((a, b) => (morticianLabel(a) || "").localeCompare(morticianLabel(b) || ""))
  } catch {
    morticianOptions.value = []
  }
}

/** Admin: assign/reassign mortician */
type AssignMorticianResponse = {
  message?: string
  caseNumber?: string
  assignedMortician?: { id: string; email: string | null; displayName: string | null }
}

async function assignMorticianToSelected(userId: string): Promise<void> {
  if (!isAdmin.value) return
  const sc = selectedCase.value
  if (!sc) return

  const id = (userId ?? "").trim()
  if (!id) return

  assigning.value = true
  assignError.value = ""
  assignOk.value = ""

  try {
    const resp = await apiFetch<AssignMorticianResponse>(`/api/cases/${encodeURIComponent(sc.caseNumber)}/assign-mortician`, {
      method: "POST",
      body: JSON.stringify({ userId: id }),
    })

    if (resp?.assignedMortician) {
      const m: MorticianDto = {
        id: resp.assignedMortician.id,
        email: resp.assignedMortician.email ?? null,
        displayName: resp.assignedMortician.displayName ?? null,
      }

      sc.assignedMortician = m
      updateListAssignedMortician(sc.caseNumber, m)
    }

    assignOk.value = "Assignment saved."
    await reloadSelected()
    await loadCases()
  } catch (e: unknown) {
    assignError.value = e instanceof Error ? e.message : "Failed to assign mortician."
  } finally {
    assigning.value = false
  }
}

/** Tasks */
type TaskDraft = { status: CaseTaskStatus; notes: string | null }
const taskDraft = reactive<Record<number, TaskDraft | undefined>>({})

function draftFor(t: CaseTaskDto): TaskDraft {
  const existing = taskDraft[t.id]
  if (existing) return existing
  const created: TaskDraft = { status: t.status, notes: t.notes ?? null }
  taskDraft[t.id] = created
  return created
}

function onTaskStatusChange(t: CaseTaskDto, status: string): void {
  const d = draftFor(t)
  d.status = status as CaseTaskStatus
}

function onTaskNotesChange(t: CaseTaskDto, notesText: string): void {
  const d = draftFor(t)
  const trimmed = (notesText ?? "").trim()
  d.notes = trimmed ? trimmed : null
}

async function saveTask(t: CaseTaskDto): Promise<void> {
  const sc = selectedCase.value
  if (!sc) return

  updatingTaskId.value = t.id
  taskError.value = ""

  try {
    const d = draftFor(t)

    await apiFetch<void>(`/api/cases/${encodeURIComponent(sc.caseNumber)}/tasks/${t.id}`, {
      method: "PATCH",
      body: JSON.stringify({ status: d.status, notes: d.notes }),
    })

    tasks.value = await apiFetch<CaseTaskDto[]>(`/api/cases/${encodeURIComponent(sc.caseNumber)}/tasks`)
    delete taskDraft[t.id]

    ensureWizardIndexValid()

    if (isMorticianWizard.value && tasks.value.length && tasks.value.every((x) => x.status === "Done")) {
      await completeIfAllDone()
    }
  } catch (e: unknown) {
    taskError.value = e instanceof Error ? e.message : "Failed to update task."
  } finally {
    updatingTaskId.value = null
  }
}

/** Complete case */
async function markCaseCompleted(): Promise<void> {
  const sc = selectedCase.value
  if (!sc) return

  caseError.value = ""
  caseOk.value = ""
  completingCase.value = true

  try {
    await apiFetch<void>(`/api/cases/${encodeURIComponent(sc.caseNumber)}/complete`, { method: "PATCH" })
    caseOk.value = "Case marked Completed."
    await reloadSelected()
    await loadCases()
  } catch (e: unknown) {
    caseError.value = e instanceof Error ? e.message : "Failed to complete case."
  } finally {
    completingCase.value = false
  }
}

async function completeIfAllDone(): Promise<void> {
  const sc = selectedCase.value
  if (!sc) return
  if (!allWorkflowDone.value) return

  completingCase.value = true
  caseError.value = ""
  caseOk.value = ""

  try {
    await apiFetch<void>(`/api/cases/${encodeURIComponent(sc.caseNumber)}/complete`, { method: "PATCH" })

    cases.value = (cases.value ?? []).filter((c) => c.caseNumber !== sc.caseNumber)

    selectedCase.value = null
    tasks.value = []
    notes.value = []
    wizardStepIndex.value = 0

    await loadCases()

    const next = (filteredCases.value ?? [])[0]
    if (next) {
      await selectCase(next.caseNumber)
    }

    caseOk.value = "Case completed and removed from your list."
  } catch (e: unknown) {
    caseError.value = e instanceof Error ? e.message : "Failed to complete case."
  } finally {
    completingCase.value = false
  }
}

/** Notes */
const newNoteText = ref<string>("")

async function addNote(): Promise<void> {
  const sc = selectedCase.value
  if (!sc) return

  addingNote.value = true
  noteError.value = ""
  try {
    await apiFetch<void>(`/api/cases/${encodeURIComponent(sc.caseNumber)}/notes`, {
      method: "POST",
      body: JSON.stringify({ text: newNoteText.value }),
    })
    newNoteText.value = ""
    notes.value = await apiFetch<CaseNoteDto[]>(`/api/cases/${encodeURIComponent(sc.caseNumber)}/notes`)
  } catch (e: unknown) {
    noteError.value = e instanceof Error ? e.message : "Failed to add note."
  } finally {
    addingNote.value = false
  }
}

/** Refresh / init */
async function refreshAll(): Promise<void> {
  loadFromStorage()
  if (token.value) await fetchMe()

  if (!canUsePage.value) return

  if (isAdmin.value) {
    if (listMode.value === "mine") listMode.value = "unassigned"
  } else {
    listMode.value = "mine"
  }

  await loadMorticiansIfAdmin()
  await loadCases()
  if (selectedCase.value) await reloadSelected()
}

watch(listMode, async () => {
  if (!canUsePage.value) return
  if (!isAdmin.value && listMode.value !== "mine") listMode.value = "mine"
  await loadCases()
})

onMounted(async () => {
  await refreshAll()
})
</script>

<style scoped>
/* (unchanged) */
.page { max-width: 1200px; margin: 0 auto; padding: 16px; }
.topbar { display: flex; justify-content: space-between; gap: 12px; align-items: flex-start; margin-bottom: 16px; }
.actions { display: flex; gap: 8px; }
.small { font-size: 12px; }
.grid { display: grid; grid-template-columns: 1fr; gap: 16px; margin-top: 16px; }
@media (min-width: 980px) { .grid { grid-template-columns: 420px 1fr; } }
.card { background: rgba(255,255,255,0.06); border: 1px solid rgba(255,255,255,0.10); border-radius: 16px; padding: 16px; backdrop-filter: blur(10px); }
.modeHeader{ display:flex; justify-content: space-between; align-items: center; gap: 10px; margin-bottom: 8px; }
.modePills{ display:flex; gap:8px; flex-wrap: wrap; }
.subCard { margin-top: 12px; padding: 12px; border-radius: 14px; border: 1px solid rgba(255,255,255,0.10); background: rgba(0,0,0,0.14); }
.subTitle { font-weight: 700; margin-bottom: 6px; }
h1 { margin: 0; font-size: 28px; }
h2 { margin: 0 0 10px 0; font-size: 18px; }
h3 { margin: 0; }
.muted { opacity: 0.75; }
.error { margin-top: 10px; color: #ffb3b3; }
.ok { margin-top: 10px; color: #b8ffb8; }
.okText { color: #b8ffb8; }
.warnText { color: #ffd28a; }
.btn { border: 1px solid rgba(255,255,255,0.20); background: rgba(255,255,255,0.08); color: inherit; padding: 10px 12px; border-radius: 12px; cursor: pointer; }
.btn:disabled { opacity: 0.6; cursor: not-allowed; }
.btn.primary { background: rgba(255,255,255,0.18); border-color: rgba(255,255,255,0.30); }
.field { display: grid; gap: 6px; }
.formGrid { display: grid; grid-template-columns: 1fr; gap: 10px; }
@media (min-width: 760px) { .formGrid { grid-template-columns: 1fr 1fr; } }
.dividerRow { grid-column: 1 / -1; display: flex; gap: 10px; align-items: baseline; margin-top: 6px; }
.formActions { grid-column: 1 / -1; display: flex; gap: 10px; align-items: center; flex-wrap: wrap; }
input, select, textarea { width: 100%; padding: 10px 12px; border-radius: 12px; border: 1px solid rgba(255,255,255,0.20); background: rgba(0,0,0,0.25); color: inherit; outline: none; }
.listHeader { display: flex; justify-content: space-between; align-items: center; gap: 10px; margin-bottom: 10px; }
.chips { display: flex; gap: 8px; flex-wrap: wrap; }
.chip { padding: 8px 10px; border-radius: 999px; border: 1px solid rgba(255,255,255,0.18); background: rgba(255,255,255,0.06); cursor: pointer; }
.chip.active { background: rgba(255,255,255,0.16); border-color: rgba(255,255,255,0.28); }
.searchRow { margin-bottom: 10px; }
.caseList { list-style: none; padding: 0; margin: 0; display: grid; gap: 10px; }
.caseRow { padding: 12px; border-radius: 14px; border: 1px solid rgba(255,255,255,0.10); background: rgba(0,0,0,0.14); cursor: pointer; }
.caseRow.selected { border-color: rgba(255,255,255,0.28); background: rgba(255,255,255,0.06); }
.rowTop { display: flex; justify-content: space-between; align-items: center; gap: 10px; }
.rowBottom { display: flex; justify-content: space-between; gap: 10px; flex-wrap: wrap; margin-top: 8px; }
.badge { padding: 4px 8px; border-radius: 999px; font-size: 12px; border: 1px solid rgba(255,255,255,0.18); background: rgba(255,255,255,0.06); }
.b-intake { background: rgba(255,255,255,0.06); }
.b-prep { background: rgba(255,255,255,0.10); }
.b-viewing { background: rgba(120,200,255,0.12); }
.b-scheduled { background: rgba(255,210,120,0.12); }
.b-completed { background: rgba(0,255,0,0.10); border-color: rgba(0,255,0,0.25); }
.detailHeader { display: flex; justify-content: space-between; gap: 12px; align-items: flex-start; }
.caseTitle { font-size: 22px; }
.detailMeta { display: flex; gap: 10px; flex-wrap: wrap; margin-top: 6px; align-items: center; }
.detailActions { display: flex; gap: 10px; flex-wrap: wrap; justify-content: flex-end; align-items: flex-end; }
.assignBox { display: grid; gap: 8px; padding: 10px; border-radius: 14px; border: 1px solid rgba(255,255,255,0.12); background: rgba(0,0,0,0.12); min-width: 280px; }
.assignActions{ display:flex; gap: 8px; flex-wrap: wrap; justify-content: flex-end; }
.miniField { display: grid; gap: 6px; }
.sep { border: none; border-top: 1px solid rgba(255,255,255,0.10); margin: 16px 0; }
.tasksHeader { display: flex; justify-content: space-between; align-items: baseline; gap: 10px; }
.taskList { list-style: none; padding: 0; margin: 10px 0 0 0; display: grid; gap: 10px; }
.taskRow { padding: 12px; border-radius: 14px; border: 1px solid rgba(255,255,255,0.10); background: rgba(0,0,0,0.14); display: grid; grid-template-columns: 1fr; gap: 12px; }
@media (min-width: 980px) { .taskRow { grid-template-columns: 1fr 380px; } }
.taskName { display: grid; gap: 6px; }
.taskRight { display: grid; gap: 10px; }
.kv { display: grid; gap: 6px; margin-top: 8px; }
.noteCreate { margin-top: 10px; display: grid; gap: 10px; }
.noteList { list-style: none; padding: 0; margin: 10px 0 0 0; display: grid; gap: 10px; }
.noteRow { padding: 12px; border-radius: 14px; border: 1px solid rgba(255,255,255,0.10); background: rgba(0,0,0,0.14); }
.noteText { white-space: pre-wrap; }
.wizardCard { margin-top: 10px; padding: 12px; border-radius: 14px; border: 1px solid rgba(255,255,255,0.10); background: rgba(0,0,0,0.14); display: grid; gap: 12px; }
.wizardTop { display: flex; justify-content: space-between; gap: 10px; align-items: flex-start; flex-wrap: wrap; }
.wizardTitle { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.wizardNav { display: flex; gap: 10px; flex-wrap: wrap; }
.wizardForm { display: grid; gap: 10px; }
.wizardActions { display: flex; gap: 10px; flex-wrap: wrap; align-items: center; }
</style>