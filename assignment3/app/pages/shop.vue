<template>
  <section class="card">
    <h1>Shop</h1>
    <p class="muted">
      This is where you buy upgrades/items. Later we can GET/POST to the server for inventory, purchases, etc.
    </p>

    <div class="row">
      <div class="panel">
        <h3>Items</h3>
        <p class="muted">Placeholder for store listings.</p>
      </div>

      <div class="panel">
        <h3>Your Gold</h3>
        <p class="muted">Placeholder for player currency.</p>
        <NuxtLink class="btn" to="/stable">Back to Stable</NuxtLink>
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
  border-radius: 14px;
  padding: 10px 14px;
  border: 1px solid rgba(255,255,255,0.12);
  background: rgba(255,255,255,0.06);
  color: rgba(255,255,255,0.92);
  cursor: pointer;
  text-decoration: none;
  margin-top: 10px;
}
</style>
