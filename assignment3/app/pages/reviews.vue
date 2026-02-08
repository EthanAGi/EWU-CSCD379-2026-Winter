<script setup lang="ts">
type ReviewDto = {
  id: number
  playerName: string
  body: string
  rating: number
  createdAtUtc: string
}

const { player } = usePlayerState()

/**
 * ✅ IMPORTANT:
 * Single Azure App Service (Nuxt/Nitro) deployment.
 * Therefore ALL client calls should use SAME-ORIGIN Nuxt server routes:
 *   /api/...
 * NOT http://localhost:5072 and NOT runtimeConfig apiBase.
 */

const loading = ref(false)
const errorMsg = ref<string | null>(null)
const reviews = ref<ReviewDto[]>([])

const formName = ref('')
const formRating = ref(5)
const formBody = ref('')
const submitting = ref(false)
const submitMsg = ref<string | null>(null)

watchEffect(() => {
  if (!formName.value && player.value?.name) {
    formName.value = player.value.name
  }
})

async function loadReviews() {
  loading.value = true
  errorMsg.value = null
  try {
    // ✅ same-origin (Nitro route)
    reviews.value = await $fetch<ReviewDto[]>(`/api/reviews?take=50`)
  } catch (e: any) {
    console.error(e)
    errorMsg.value = e?.message ?? 'Failed to load reviews.'
  } finally {
    loading.value = false
  }
}

function fmtDate(iso: string) {
  return new Date(iso).toLocaleString()
}

async function submitReview() {
  submitMsg.value = null
  errorMsg.value = null

  const name = formName.value.trim()
  const body = formBody.value.trim()
  const rating = Number(formRating.value)

  if (!name) {
    errorMsg.value = 'Name is required.'
    return
  }
  if (body.length < 5) {
    errorMsg.value = 'Review must be at least 5 characters.'
    return
  }
  if (!(rating >= 1 && rating <= 5)) {
    errorMsg.value = 'Rating must be 1–5.'
    return
  }

  submitting.value = true
  try {
    // ✅ same-origin (Nitro route)
    const created = await $fetch<ReviewDto>(`/api/reviews`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: { playerName: name, body, rating },
    })

    reviews.value = [created, ...reviews.value]
    formBody.value = ''
    submitMsg.value = 'Review submitted ✅'
  } catch (e: any) {
    console.error(e)
    errorMsg.value = e?.data?.message ?? e?.message ?? 'Failed to submit review.'
  } finally {
    submitting.value = false
  }
}

onMounted(loadReviews)
</script>

<template>
  <section class="page">
    <div class="wrap">
      <div class="topRow">
        <div>
          <h1 class="title">Reviews</h1>
          <p class="muted">Please, give me some feedback!.</p>
        </div>
      </div>

      <div class="grid">
        <!-- Submit -->
        <div class="card">
          <h2 class="cardTitle">Write a review</h2>

          <div class="row">
            <label class="label">
              Name
              <input v-model="formName" class="input" type="text" maxlength="64" />
            </label>

            <label class="label">
              Rating
              <select v-model="formRating" class="input">
                <option :value="5">5 - Loved it</option>
                <option :value="4">4 - Great</option>
                <option :value="3">3 - Good</option>
                <option :value="2">2 - Meh</option>
                <option :value="1">1 - Didn’t like it</option>
              </select>
            </label>
          </div>

          <label class="label">
            Review
            <textarea v-model="formBody" class="textarea" rows="6" maxlength="1200" />
          </label>

          <div class="actions">
            <button class="btn primary" :disabled="submitting" @click="submitReview" type="button">
              {{ submitting ? 'Submitting…' : 'Submit Review' }}
            </button>
            <button class="btn" :disabled="loading" @click="loadReviews" type="button">
              Refresh
            </button>
          </div>

          <p v-if="submitMsg" class="muted">{{ submitMsg }}</p>
          <p v-if="errorMsg" class="err">{{ errorMsg }}</p>
        </div>

        <!-- List -->
        <div class="card">
          <h2 class="cardTitle">Latest reviews</h2>

          <div v-if="loading" class="muted">Loading…</div>
          <div v-else-if="reviews.length === 0" class="muted">No reviews yet. Be the first!</div>

          <div v-else class="list">
            <article v-for="r in reviews" :key="r.id" class="review">
              <div class="reviewTop">
                <div class="who">
                  <span class="name">{{ r.playerName }}</span>
                  <span class="stars">
                    {{ '★'.repeat(r.rating) }}<span class="dim">{{ '★'.repeat(5 - r.rating) }}</span>
                  </span>
                </div>
                <div class="date muted">{{ fmtDate(r.createdAtUtc) }}</div>
              </div>
              <p class="body">{{ r.body }}</p>
            </article>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
.page { width: 100%; }
.wrap { width: min(1100px, calc(100% - 32px)); margin: 0 auto; padding: 18px 0 40px; }

.topRow { display: flex; justify-content: space-between; gap: 16px; flex-wrap: wrap; align-items: flex-end; }
.title { font-size: 28px; font-weight: 1000; margin: 0; }
.muted { color: rgba(255,255,255,0.72); }
.err { color: rgba(255, 120, 120, 0.95); margin-top: 10px; }

.grid { display: grid; grid-template-columns: 1fr 1.2fr; gap: 16px; margin-top: 16px; }
@media (max-width: 900px) { .grid { grid-template-columns: 1fr; } }

.card {
  border-radius: 18px;
  border: 1px solid rgba(255,255,255,0.12);
  background: rgba(15, 18, 30, 0.7);
  box-shadow: 0 18px 60px rgba(0,0,0,0.45);
  padding: 14px;
  backdrop-filter: blur(8px);
}
.cardTitle { font-weight: 1000; margin: 0 0 12px; font-size: 18px; }

.row { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; }
@media (max-width: 520px) { .row { grid-template-columns: 1fr; } }

.label { display: grid; gap: 6px; font-size: 12px; color: rgba(255,255,255,0.82); }
.input, .textarea {
  border-radius: 14px;
  border: 1px solid rgba(255,255,255,0.12);
  background: rgba(0,0,0,0.22);
  color: rgba(255,255,255,0.92);
  padding: 10px 12px;
}
.textarea { resize: vertical; }

.actions { display: flex; gap: 10px; flex-wrap: wrap; margin-top: 12px; }

.btn {
  border-radius: 14px;
  padding: 10px 14px;
  border: 1px solid rgba(255,255,255,0.12);
  background: rgba(255,255,255,0.08);
  color: rgba(255,255,255,0.92);
  cursor: pointer;
}
.btn.primary {
  border: none;
  background: linear-gradient(90deg, #7c5cff, #35d6c5);
  color: #0b1020;
  font-weight: 900;
}
.btn:disabled { opacity: 0.55; cursor: not-allowed; }

.list { display: grid; gap: 10px; }
.review {
  border-radius: 14px;
  border: 1px solid rgba(255,255,255,0.10);
  background: rgba(255,255,255,0.05);
  padding: 12px;
}
.reviewTop { display: flex; justify-content: space-between; gap: 10px; align-items: baseline; flex-wrap: wrap; }
.who { display: flex; gap: 10px; align-items: baseline; flex-wrap: wrap; }
.name { font-weight: 900; }
.stars { letter-spacing: 1px; }
.dim { opacity: 0.35; }
.body { margin: 8px 0 0; white-space: pre-wrap; }
</style>
