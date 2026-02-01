<script setup lang="ts">
function goToGauntlet() {
  navigateTo('/gauntlet')
}
</script>

<template>
  <section class="card">
    <h1>Stable</h1>
    <p class="muted">
      This is your hub page. You’ll manage your “run” here: roster, upgrades, heals, etc.
    </p>

    <div class="row">
      <div class="panel">
        <h3>Quick Actions</h3>
        <button class="btn primary" @click="goToGauntlet">Enter Gauntlet</button>
        <NuxtLink class="btn" to="/shop">Visit Shop</NuxtLink>
      </div>

      <div class="panel">
        <h3>Run Status</h3>
        <p class="muted">Placeholder for stats (gold, day, difficulty, etc.).</p>
      </div>
    </div>
  </section>
</template>

<style scoped>
.card {
  border: 1px solid rgba(255,255,255,0.12);
  border-radius: 18px;
  background: rgba(255,255,255,0.06);
  box-shadow: 0 12px 40px rgba(0,0,0,0.35);
  padding: 18px;
}
.muted { color: rgba(255,255,255,0.70); }
.row { display: flex; gap: 14px; flex-wrap: wrap; margin-top: 14px; }
.panel {
  flex: 1 1 280px;
  min-width: 260px;
  padding: 14px;
  border-radius: 16px;
  border: 1px solid rgba(255,255,255,0.12);
  background: rgba(255,255,255,0.04);
}
.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
  border-radius: 14px;
  padding: 10px 14px;
  border: 1px solid rgba(255,255,255,0.12);
  background: rgba(255,255,255,0.06);
  color: rgba(255,255,255,0.92);
  cursor: pointer;
  text-decoration: none;
  margin-right: 10px;
  margin-top: 10px;
}
.btn.primary {
  border: none;
  background: linear-gradient(90deg, #7c5cff, #35d6c5);
  color: #0b1020;
  font-weight: 800;
}
</style>
