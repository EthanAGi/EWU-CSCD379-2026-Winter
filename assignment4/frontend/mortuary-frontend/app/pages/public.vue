<template>
  <main class="wrap">
    <h1>Public Case Board</h1>

    <p class="muted">
      Shows case status information from the database (public requirement).
    </p>

    <div v-if="pending">Loading…</div>

    <div v-else-if="error" class="error">
      {{ errorMessage }}
    </div>

    <table v-else class="table">
      <thead>
        <tr>
          <th>Case #</th>
          <th>Status</th>
          <th>Created</th>
        </tr>
      </thead>

      <tbody>
        <tr v-for="c in cases" :key="c.caseNumber">
          <td>{{ c.caseNumber }}</td>
          <td>{{ statusLabel(c.status) }}</td>
          <td>{{ new Date(c.createdAt).toLocaleString() }}</td>
        </tr>
      </tbody>
    </table>
  </main>
</template>

<script setup lang="ts">
type PublicCase = {
  caseNumber: string
  status: number
  createdAt: string
}

const { data, pending, error } = await useFetch<PublicCase[]>('/api/public/cases')

const cases = computed(() => data.value ?? [])

const errorMessage = computed(() => {
  const e: any = error.value
  return e?.data?.message || e?.message || 'Failed to load cases.'
})

function statusLabel(s: number) {
  switch (s) {
    case 0: return 'Intake'
    case 1: return 'In Preparation'
    case 2: return 'Ready For Viewing'
    case 3: return 'Service Scheduled'
    case 4: return 'Completed'
    default: return `Unknown (${s})`
  }
}
</script>

<style scoped>
.wrap { max-width: 900px; margin: 0 auto; padding: 16px; }
.muted { opacity: 0.7; }
.error { color: #b00020; }
.table { width: 100%; border-collapse: collapse; margin-top: 12px; }
.table th, .table td { border: 1px solid #ddd; padding: 8px; text-align: left; }
.table th { background: #f6f6f6; }
</style>