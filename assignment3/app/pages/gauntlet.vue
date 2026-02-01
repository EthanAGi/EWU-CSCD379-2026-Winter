<script setup lang="ts">
import type { Animal, BattleMove, BattleState } from '../types/game'

const { player, addGold, healAnimalToFull } = usePlayerState()

// redirect if no player
watchEffect(() => {
  if (import.meta.client && !player.value) navigateTo('/')
})

const chosenId = ref<string | null>(null)
const choosing = computed(() => !battle.value && !result.value)

const result = ref<{ outcome: 'win' | 'loss'; message: string } | null>(null)

function bossForStage(stage: 1 | 2 | 3): Animal {
  // Bosses are "NPC" animals (ownerName = "Gauntlet")
  const base = {
    ownerPlayerId: 'npc',
    ownerName: 'Gauntlet',
  }

  if (stage === 1) {
    return {
      id: 'boss-1',
      ...base,
      name: 'Bristle Badger',
      kind: 'boar',
      stats: { attack: 6, defense: 4, affection: 0, hunger: 0, level: 1, hpMax: 28 },
      hpCurrent: 28,
    }
  }

  if (stage === 2) {
    return {
      id: 'boss-2',
      ...base,
      name: 'Talons the Owl',
      kind: 'owl',
      stats: { attack: 8, defense: 6, affection: 0, hunger: 0, level: 1, hpMax: 34 },
      hpCurrent: 34,
    }
  }

  return {
    id: 'boss-3',
    ...base,
    name: 'Vulpine Prime',
    kind: 'fox',
    stats: { attack: 10, defense: 7, affection: 0, hunger: 0, level: 1, hpMax: 40 },
    hpCurrent: 40,
  }
}

function rewardForStage(stage: 1 | 2 | 3): number {
  if (stage === 1) return 250
  if (stage === 2) return 500
  return 1000
}

const battle = ref<BattleState | null>(null)

const playerAnimal = computed(() => {
  if (!player.value || !battle.value) return null
  return player.value.animals.find(a => a.id === battle.value!.playerAnimalId) ?? null
})

function startGauntlet() {
  if (!player.value) return
  if (!chosenId.value) return

  battle.value = {
    stage: 1,
    round: 1,
    playerAnimalId: chosenId.value,
    enemy: bossForStage(1),
    playerDefending: false,
    enemyDefending: false,
    log: ['You entered the Gauntlet.'],
    ended: false,
  }
  result.value = null
}

function calcDamage(attackerAtk: number, defenderDef: number, defenderIsDefending: boolean): number {
  const raw = Math.max(1, attackerAtk - defenderDef)
  const reduced = defenderIsDefending ? Math.ceil(raw * 0.5) : raw
  return reduced
}

// Option B AI: defend when low hp (<= 35%), otherwise attack
function enemyChooseMove(enemy: Animal): BattleMove {
  const hpPct = enemy.hpCurrent / enemy.stats.hpMax
  if (hpPct <= 0.35) return 'defend'
  return 'attack'
}

function stepTurn(playerMove: BattleMove) {
  const b = battle.value
  const p = playerAnimal.value
  if (!b || !p || b.ended) return

  const enemyMove = enemyChooseMove(b.enemy)

  // reset defending flags each round
  b.playerDefending = playerMove === 'defend'
  b.enemyDefending = enemyMove === 'defend'

  b.log.unshift(`Round ${b.round}: You chose ${playerMove.toUpperCase()}, enemy chose ${enemyMove.toUpperCase()}.`)

  // If both defend, no damage dealt
  if (playerMove === 'defend' && enemyMove === 'defend') {
    b.log.unshift('Both defended. No damage dealt.')
    b.round++
    return
  }

  // Player attacks (if chose attack)
  if (playerMove === 'attack') {
    const dmg = calcDamage(p.stats.attack, b.enemy.stats.defense, b.enemyDefending)
    b.enemy.hpCurrent = Math.max(0, b.enemy.hpCurrent - dmg)
    b.log.unshift(`You hit ${b.enemy.name} for ${dmg}.`)
  }

  // Check enemy dead
  if (b.enemy.hpCurrent <= 0) {
    const reward = rewardForStage(b.stage)
    addGold(reward)
    b.log.unshift(`You defeated ${b.enemy.name}! +$${reward}`)

    if (b.stage === 3) {
      // ✅ NEW: heal after run ends (win)
      healAnimalToFull(b.playerAnimalId)

      b.ended = true
      b.outcome = 'win'
      b.rewardGold = reward
      result.value = { outcome: 'win', message: 'You conquered the Gauntlet!' }
      return
    }

    // Next stage
    const nextStage = (b.stage + 1) as 2 | 3
    b.stage = nextStage
    b.enemy = bossForStage(nextStage)
    b.round = 1
    b.playerDefending = false
    b.enemyDefending = false
    b.log.unshift(`Stage ${nextStage} begins!`)
    return
  }

  // Enemy attacks (if chose attack)
  if (enemyMove === 'attack') {
    const dmg = calcDamage(b.enemy.stats.attack, p.stats.defense, b.playerDefending)
    p.hpCurrent = Math.max(0, p.hpCurrent - dmg)
    b.log.unshift(`${b.enemy.name} hit you for ${dmg}.`)
  }

  // Check player dead
  if (p.hpCurrent <= 0) {
    // ✅ NEW: heal after run ends (loss)
    healAnimalToFull(b.playerAnimalId)

    b.ended = true
    b.outcome = 'loss'
    result.value = { outcome: 'loss', message: `Your animal fell in Stage ${b.stage}.` }
    b.log.unshift('You lost the Gauntlet run.')
    return
  }

  b.round++
}

function resetRun() {
  battle.value = null
  result.value = null
  chosenId.value = null
}
</script>

<template>
  <section class="card">
    <h1>Gauntlet</h1>
    <p class="muted">Choose an animal and fight 3 boss battles. Each turn: Attack or Defend.</p>

    <div v-if="choosing" class="panel">
      <h3>Choose Your Animal</h3>

      <div class="row">
        <button
          v-for="a in player?.animals ?? []"
          :key="a.id"
          class="animalBtn"
          :class="{ active: a.id === chosenId }"
          @click="chosenId = a.id"
        >
          <span class="name">{{ a.name }}</span>
          <span class="small">{{ a.kind.toUpperCase() }} • HP {{ a.hpCurrent }}/{{ a.stats.hpMax }}</span>
        </button>
      </div>

      <button class="btn primary" :disabled="!chosenId" @click="startGauntlet">
        Start Gauntlet
      </button>
    </div>

    <div v-else-if="battle && playerAnimal" class="row">
      <div class="panel col">
        <h3>Stage {{ battle.stage }} • Round {{ battle.round }}</h3>

        <div class="hpRow">
          <div class="hp">
            <div class="muted small">You</div>
            <div><b>{{ playerAnimal.name }}</b> (HP {{ playerAnimal.hpCurrent }}/{{ playerAnimal.stats.hpMax }})</div>
          </div>

          <div class="hp">
            <div class="muted small">Enemy</div>
            <div><b>{{ battle.enemy.name }}</b> (HP {{ battle.enemy.hpCurrent }}/{{ battle.enemy.stats.hpMax }})</div>
          </div>
        </div>

        <div class="actions" v-if="!battle.ended">
          <button class="btn primary" @click="stepTurn('attack')">Attack</button>
          <button class="btn" @click="stepTurn('defend')">Defend</button>
        </div>

        <div v-if="battle.ended" class="muted" style="margin-top:10px;">
          Battle ended: <b>{{ battle.outcome }}</b>
        </div>

        <button class="btn" style="margin-top:12px;" @click="resetRun">Reset</button>
      </div>

      <div class="panel col">
        <h3>Battle Log</h3>
        <div class="log">
          <div v-for="(line, i) in battle.log" :key="i" class="logLine">{{ line }}</div>
        </div>
      </div>
    </div>

    <div v-else-if="result" class="panel">
      <h3 v-if="result.outcome === 'win'">✅ Victory</h3>
      <h3 v-else>❌ Defeat</h3>
      <p class="muted">{{ result.message }}</p>
      <button class="btn primary" @click="resetRun">Try Again</button>
      <NuxtLink class="btn" to="/stable">Back to Stable</NuxtLink>
    </div>
  </section>
</template>

<style scoped>
.card{ border:1px solid rgba(255,255,255,.12); border-radius:18px; background:rgba(255,255,255,.06); box-shadow:0 12px 40px rgba(0,0,0,.35); padding:18px;}
.muted{ color:rgba(255,255,255,.70); }
.small{ font-size:12px; }
.row{ display:flex; gap:14px; flex-wrap:wrap; margin-top:14px;}
.col{ flex: 1 1 320px; min-width:280px; }
.panel{ padding:14px; border-radius:16px; border:1px solid rgba(255,255,255,.12); background:rgba(255,255,255,.04); }
.animalBtn{ width:100%; text-align:left; border-radius:14px; padding:10px 12px; margin-top:10px; border:1px solid rgba(255,255,255,.12); background:rgba(255,255,255,.05); color:rgba(255,255,255,.92); cursor:pointer;}
.animalBtn.active{ border:none; color:#0b1020; font-weight:900; background:linear-gradient(90deg,#7c5cff,#35d6c5); }
.name{ font-weight:900; display:block; }
.actions{ display:flex; gap:10px; flex-wrap:wrap; margin-top:12px; }
.btn{ border-radius:14px; padding:10px 14px; border:1px solid rgba(255,255,255,.12); background:rgba(255,255,255,.06); color:rgba(255,255,255,.92); cursor:pointer; text-decoration:none;}
.btn.primary{ border:none; background:linear-gradient(90deg,#7c5cff,#35d6c5); color:#0b1020; font-weight:900;}
.btn:disabled{ opacity:.5; cursor:not-allowed; }
.hpRow{ display:flex; gap:14px; flex-wrap:wrap; margin-top:10px; }
.hp{ flex:1 1 240px; padding:10px; border-radius:14px; border:1px solid rgba(255,255,255,.12); background:rgba(255,255,255,.04); }
.log{ margin-top:10px; max-height:360px; overflow:auto; padding-right:6px; }
.logLine{ padding:8px 10px; margin-top:8px; border-radius:12px; border:1px solid rgba(255,255,255,.10); background:rgba(0,0,0,.18); }
</style>
