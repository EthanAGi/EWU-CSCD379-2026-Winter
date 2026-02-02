<script setup lang="ts">
import type { Animal, BattleMove, BattleState, ItemKind } from '../types/game'

const { player, addGold, healAnimalToFull } = usePlayerState()

// redirect if no player
watchEffect(() => {
  if (import.meta.client && !player.value) navigateTo('/')
})

const chosenId = ref<string | null>(null)
const choosing = computed(() => !battle.value && !result.value && !stageOverlay.value)

const result = ref<{ outcome: 'win' | 'loss'; message: string } | null>(null)

/**
 * After beating a boss (stage 1 or 2), we pause and show a modal:
 * - Continue to next stage
 * - Drop out with winnings so far
 */
const stageOverlay = ref<{
  stageCleared: 1 | 2 | 3
  stageReward: number
  totalRunGold: number
} | null>(null)

function bossForStage(stage: 1 | 2 | 3): Animal {
  const base = { ownerPlayerId: 'npc', ownerName: 'Gauntlet' }

  if (stage === 1) {
    return {
      id: 'boss-1',
      ...base,
      name: 'Bristle Badger',
      kind: 'boar',
      stats: { attack: 6, defense: 4, affection: 0, level: 1, hpMax: 28 },
      hpCurrent: 28,
    }
  }

  if (stage === 2) {
    return {
      id: 'boss-2',
      ...base,
      name: 'Talons the Owl',
      kind: 'owl',
      stats: { attack: 8, defense: 6, affection: 0, level: 1, hpMax: 34 },
      hpCurrent: 34,
    }
  }

  return {
    id: 'boss-3',
    ...base,
    name: 'Vulpine Prime',
    kind: 'fox',
    stats: { attack: 10, defense: 7, affection: 0, level: 1, hpMax: 40 },
    hpCurrent: 40,
  }
}

function rewardForStage(stage: 1 | 2 | 3): number {
  if (stage === 1) return 250
  if (stage === 2) return 500
  return 1000
}

const battle = ref<BattleState | null>(null)

/** Total gold earned THIS gauntlet run (not lifetime) */
const runGold = ref(0)

const playerAnimal = computed(() => {
  if (!player.value || !battle.value) return null
  return player.value.animals.find(a => a.id === battle.value!.playerAnimalId) ?? null
})

/** -------- Battle item state (wears off after each boss fight) -------- */
const atkMult = ref(1)
const defMult = ref(1)

function resetBattleBuffs() {
  atkMult.value = 1
  defMult.value = 1
}

function startGauntlet() {
  if (!player.value) return
  if (!chosenId.value) return

  runGold.value = 0
  stageOverlay.value = null
  result.value = null

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

  // reset visuals + buffs
  playerHit.value = false
  enemyHit.value = false
  playerAttacking.value = false
  enemyAttacking.value = false
  isAnimating.value = false
  resetBattleBuffs()
}

function calcDamage(attackerAtk: number, defenderDef: number, defenderIsDefending: boolean): number {
  const raw = Math.max(1, attackerAtk - defenderDef)
  return defenderIsDefending ? Math.ceil(raw * 0.5) : raw
}

// Option B AI: defend when low hp (<= 35%), otherwise attack
function enemyChooseMove(enemy: Animal): BattleMove {
  const hpPct = enemy.hpCurrent / enemy.stats.hpMax
  return hpPct <= 0.35 ? 'defend' : 'attack'
}

/** -------- Sprites + HP helpers -------- */
function spriteUrl(kind: string) {
  return `/sprites/${kind}.png`
}

function hpPercent(current: number, max: number): number {
  if (max <= 0) return 0
  return Math.max(0, Math.min(100, (current / max) * 100))
}

/** -------- Hit (flash red) toggles -------- */
const playerHit = ref(false)
const enemyHit = ref(false)
let playerHitTimeout: number | null = null
let enemyHitTimeout: number | null = null

function flashPlayerHit() {
  playerHit.value = true
  if (playerHitTimeout !== null) window.clearTimeout(playerHitTimeout)
  playerHitTimeout = window.setTimeout(() => (playerHit.value = false), 170)
}

function flashEnemyHit() {
  enemyHit.value = true
  if (enemyHitTimeout !== null) window.clearTimeout(enemyHitTimeout)
  enemyHitTimeout = window.setTimeout(() => (enemyHit.value = false), 170)
}

onBeforeUnmount(() => {
  if (playerHitTimeout !== null) window.clearTimeout(playerHitTimeout)
  if (enemyHitTimeout !== null) window.clearTimeout(enemyHitTimeout)
})

/** -------- Attack lunge animation toggles --------
 *  - If attack: add class for quick lunge forward, then return.
 *  - If defend / item: no movement.
 */
const isAnimating = ref(false)
const playerAttacking = ref(false)
const enemyAttacking = ref(false)
let animTimeout: number | null = null

function clearAnimTimeout() {
  if (animTimeout !== null) window.clearTimeout(animTimeout)
  animTimeout = null
}

function playAttackAnimation(playerMove: BattleMove | 'item', enemyMove: BattleMove) {
  playerAttacking.value = playerMove === 'attack'
  enemyAttacking.value = enemyMove === 'attack'
  isAnimating.value = playerAttacking.value || enemyAttacking.value

  clearAnimTimeout()
  if (isAnimating.value) {
    animTimeout = window.setTimeout(() => {
      playerAttacking.value = false
      enemyAttacking.value = false
      isAnimating.value = false
      animTimeout = null
    }, 260) // must match CSS animation duration
  }
}

onBeforeUnmount(() => {
  clearAnimTimeout()
})

/** -------- Inventory helpers (persist by leveraging addGold(0)) -------- */
const battleItemDefs = [
  {
    kind: 'bandage' as ItemKind,
    name: 'Bandage',
    desc: 'Heals 30% HP (keeps the health).',
  },
  {
    kind: 'medkit' as ItemKind,
    name: 'Medkit',
    desc: 'Heals 70% HP (keeps the health).',
  },
  {
    kind: 'attackPill' as ItemKind,
    name: 'Attack Boost Pill',
    desc: 'Doubles attack for this boss fight only.',
  },
  {
    kind: 'defensePill' as ItemKind,
    name: 'Defense Pill',
    desc: 'Doubles defense for this boss fight only.',
  },
] as const

const usableBattleItems = computed(() => {
  const inv = player.value?.inventory
  if (!inv) return []
  return battleItemDefs.filter(d => (inv[d.kind] ?? 0) > 0)
})

const selectedBattleItem = ref<ItemKind | ''>('')

watchEffect(() => {
  const list = usableBattleItems.value

  if (list.length === 0) {
    selectedBattleItem.value = ''
    return
  }

  const stillValid = list.some(x => x.kind === selectedBattleItem.value)
  if (!stillValid) {
    const first = list[0]
    if (first) selectedBattleItem.value = first.kind
  }
})


function consumeItem(kind: ItemKind): boolean {
  if (!player.value) return false
  const have = player.value.inventory[kind] ?? 0
  if (have <= 0) return false
  player.value.inventory[kind] = have - 1

  // Persist without changing gold:
  // addGold always calls save() in your composable, so addGold(0) writes inventory to localStorage.
  addGold(0)
  return true
}

/** -------- Final result overlay helpers -------- */
const showOverlay = computed(() => !!result.value)

const overlayTitle = computed(() => {
  if (!result.value) return ''
  return result.value.outcome === 'win' ? 'WON' : 'LOST'
})

const overlaySpriteKind = computed(() => {
  if (!result.value || !battle.value || !playerAnimal.value) return null
  // if you WON => your animal, if you LOST => enemy
  return result.value.outcome === 'win' ? playerAnimal.value.kind : battle.value.enemy.kind
})

const overlaySpriteAlt = computed(() => {
  if (!result.value || !battle.value || !playerAnimal.value) return ''
  return result.value.outcome === 'win' ? playerAnimal.value.name : battle.value.enemy.name
})

function closeOverlayAndReset() {
  resetRun()
}

function closeOverlayBackToStable() {
  resetRun()
  navigateTo('/stable')
}

/** -------- Stage overlay actions (continue / drop out) -------- */

const stageOverlaySpriteKind = computed(() => {
  if (!stageOverlay.value || !playerAnimal.value) return null
  // winner is always the player animal for stage cleared
  return playerAnimal.value.kind
})

const stageOverlaySpriteAlt = computed(() => {
  if (!stageOverlay.value || !playerAnimal.value) return ''
  return playerAnimal.value.name
})

function continueToNextStage() {
  const b = battle.value
  if (!b || !stageOverlay.value) return

  // ✅ Pills wear off after the boss fight ends
  resetBattleBuffs()

  // move to next stage
  const nextStage = (b.stage + 1) as 2 | 3
  b.stage = nextStage
  b.enemy = bossForStage(nextStage)
  b.round = 1
  b.playerDefending = false
  b.enemyDefending = false
  b.log.unshift(`Stage ${nextStage} begins!`)

  stageOverlay.value = null
}

function dropOutWithWinnings() {
  const b = battle.value
  if (!b || !playerAnimal.value) return

  // ✅ run ends => buffs cleared
  resetBattleBuffs()

  // Heal because run ends
  healAnimalToFull(b.playerAnimalId)

  const total = runGold.value
  stageOverlay.value = null

  // show final WIN overlay with total gold message
  result.value = {
    outcome: 'win',
    message: `You dropped out safely with $${total} from this run.`,
  }

  // Mark run ended (optional, mostly for safety)
  b.ended = true
  b.outcome = 'win'
}

/** -------- Apply battle item effects -------- */
function applyBattleItem(kind: ItemKind) {
  const b = battle.value
  const p = playerAnimal.value
  if (!b || !p) return

  // Consume first so you can't use items you don't have
  const ok = consumeItem(kind)
  if (!ok) {
    b.log.unshift('No item available.')
    return
  }

  if (kind === 'bandage') {
    const heal = Math.ceil(p.stats.hpMax * 0.3)
    p.hpCurrent = Math.min(p.stats.hpMax, p.hpCurrent + heal)
    b.log.unshift(`You used a Bandage and healed ${heal} HP.`)
    return
  }

  if (kind === 'medkit') {
    const heal = Math.ceil(p.stats.hpMax * 0.7)
    p.hpCurrent = Math.min(p.stats.hpMax, p.hpCurrent + heal)
    b.log.unshift(`You used a Medkit and healed ${heal} HP.`)
    return
  }

  if (kind === 'attackPill') {
    atkMult.value = 2
    b.log.unshift('You used an Attack Boost Pill. Your attack is doubled for this boss fight.')
    return
  }

  if (kind === 'defensePill') {
    defMult.value = 2
    b.log.unshift('You used a Defense Pill. Your defense is doubled for this boss fight.')
    return
  }

  // If new kinds get added later:
  b.log.unshift('That item has no effect yet.')
}

/** -------- Main turn step (attack/defend) -------- */
function stepTurn(playerMove: BattleMove) {
  const b = battle.value
  const p = playerAnimal.value
  if (!b || !p || b.ended) return
  if (stageOverlay.value) return
  if (isAnimating.value) return

  const enemyMove = enemyChooseMove(b.enemy)

  playAttackAnimation(playerMove, enemyMove)

  b.playerDefending = playerMove === 'defend'
  b.enemyDefending = enemyMove === 'defend'

  b.log.unshift(`Round ${b.round}: You chose ${playerMove.toUpperCase()}, enemy chose ${enemyMove.toUpperCase()}.`)

  if (playerMove === 'defend' && enemyMove === 'defend') {
    b.log.unshift('Both defended. No damage dealt.')
    b.round++
    return
  }

  // Player attacks (apply pill multiplier)
  if (playerMove === 'attack') {
    const effectiveAtk = Math.round(p.stats.attack * atkMult.value)
    const dmg = calcDamage(effectiveAtk, b.enemy.stats.defense, b.enemyDefending)
    b.enemy.hpCurrent = Math.max(0, b.enemy.hpCurrent - dmg)
    b.log.unshift(`You hit ${b.enemy.name} for ${dmg}.`)
    flashEnemyHit()
  }

  // Enemy dead?
  if (b.enemy.hpCurrent <= 0) {
    const stageReward = rewardForStage(b.stage)
    addGold(stageReward)
    runGold.value += stageReward

    b.log.unshift(`You defeated ${b.enemy.name}! +$${stageReward} (Run total: $${runGold.value})`)

    // Stage 3 => run complete
    if (b.stage === 3) {
      // ✅ fight ends => pills wear off
      resetBattleBuffs()

      healAnimalToFull(b.playerAnimalId)
      b.ended = true
      b.outcome = 'win'
      b.rewardGold = stageReward
      result.value = { outcome: 'win', message: `You conquered the Gauntlet! Total winnings: $${runGold.value}` }
      return
    }

    // ✅ fight ends (stage 1 or 2) => pills wear off BEFORE next stage
    resetBattleBuffs()

    // Stage 1 or 2 => show stage overlay with Continue/Drop Out
    stageOverlay.value = {
      stageCleared: b.stage,
      stageReward,
      totalRunGold: runGold.value,
    }
    return
  }

  // Enemy attacks (apply defense pill multiplier)
  if (enemyMove === 'attack') {
    const effectiveDef = Math.round(p.stats.defense * defMult.value)
    const dmg = calcDamage(b.enemy.stats.attack, effectiveDef, b.playerDefending)
    p.hpCurrent = Math.max(0, p.hpCurrent - dmg)
    b.log.unshift(`${b.enemy.name} hit you for ${dmg}.`)
    flashPlayerHit()
  }

  // Player dead?
  if (p.hpCurrent <= 0) {
    // ✅ fight ends => pills wear off
    resetBattleBuffs()

    healAnimalToFull(b.playerAnimalId)
    b.ended = true
    b.outcome = 'loss'
    b.log.unshift('You lost the Gauntlet run.')
    result.value = { outcome: 'loss', message: `Your animal fell in Stage ${b.stage}.` }
    return
  }

  b.round++
}

/** -------- Turn step (use item) --------
 * Item use consumes your turn; enemy still acts.
 */
function stepUseItem() {
  const b = battle.value
  const p = playerAnimal.value
  if (!b || !p || b.ended) return
  if (stageOverlay.value) return
  if (isAnimating.value) return
  if (!selectedBattleItem.value) return

  const enemyMove = enemyChooseMove(b.enemy)

  // item => player doesn't lunge; enemy might
  playAttackAnimation('item', enemyMove)

  b.playerDefending = false
  b.enemyDefending = enemyMove === 'defend'

  b.log.unshift(`Round ${b.round}: You used an item, enemy chose ${enemyMove.toUpperCase()}.`)

  // Apply the item effect immediately
  applyBattleItem(selectedBattleItem.value)

  // Enemy might still attack after you use item
  if (enemyMove === 'attack') {
    const effectiveDef = Math.round(p.stats.defense * defMult.value)
    const dmg = calcDamage(b.enemy.stats.attack, effectiveDef, b.playerDefending)
    p.hpCurrent = Math.max(0, p.hpCurrent - dmg)
    b.log.unshift(`${b.enemy.name} hit you for ${dmg}.`)
    flashPlayerHit()
  } else {
    b.log.unshift(`${b.enemy.name} defended.`)
  }

  // Player dead?
  if (p.hpCurrent <= 0) {
    // ✅ fight ends => pills wear off
    resetBattleBuffs()

    healAnimalToFull(b.playerAnimalId)
    b.ended = true
    b.outcome = 'loss'
    b.log.unshift('You lost the Gauntlet run.')
    result.value = { outcome: 'loss', message: `Your animal fell in Stage ${b.stage}.` }
    return
  }

  b.round++
}

function resetRun() {
  battle.value = null
  result.value = null
  stageOverlay.value = null
  chosenId.value = null
  runGold.value = 0
  playerHit.value = false
  enemyHit.value = false
  playerAttacking.value = false
  enemyAttacking.value = false
  isAnimating.value = false
  clearAnimTimeout()
  resetBattleBuffs()
}
</script>

<template>
  <section class="card">
    <h1>Gauntlet</h1>
    <p class="muted">Choose an animal and fight 3 boss battles. Each turn: Attack, Defend, or Use Item.</p>

    <!-- Choosing screen -->
    <div v-if="choosing" class="panel">
      <h3>Choose Your Animal</h3>

      <div class="row">
        <button
          v-for="a in player?.animals ?? []"
          :key="a.id"
          class="animalBtn"
          :class="{ active: a.id === chosenId }"
          @click="chosenId = a.id"
          type="button"
        >
          <span class="name">{{ a.name }}</span>
          <span class="small">{{ a.kind.toUpperCase() }} • HP {{ a.hpCurrent }}/{{ a.stats.hpMax }}</span>
        </button>
      </div>

      <button class="btn primary" :disabled="!chosenId" @click="startGauntlet" type="button">
        Start Gauntlet
      </button>
    </div>

    <!-- Battle screen -->
    <div v-else-if="battle && playerAnimal" class="battleLayout">
      <!-- TOP: Battle / Arena -->
      <div class="panel battlePanel">
        <div class="battleHeader">
          <h3>Stage {{ battle.stage }} • Round {{ battle.round }}</h3>

          <div class="rightHeader">
            <div class="pill muted small">Run: <b>${{ runGold }}</b></div>

            <div class="pill muted small" v-if="atkMult > 1 || defMult > 1">
              Buffs:
              <span v-if="atkMult > 1"><b>ATK×{{ atkMult }}</b></span>
              <span v-if="atkMult > 1 && defMult > 1"> • </span>
              <span v-if="defMult > 1"><b>DEF×{{ defMult }}</b></span>
              <span class="muted"> (ends after this boss)</span>
            </div>

            <button class="btn" @click="resetRun" type="button">Reset</button>
          </div>
        </div>

        <div class="arena">
          <!-- Player (LEFT) -->
          <div class="fighter left">
            <div class="label muted small">You</div>

            <!-- 🚀 lunge wrapper -->
            <div class="lungeWrap" :class="{ lungeRight: playerAttacking }">
              <div class="spriteBlock" :class="{ hit: playerHit }">
                <img class="spriteImg" :src="spriteUrl(playerAnimal.kind)" :alt="playerAnimal.name" draggable="false" />
              </div>
            </div>

            <div class="hpBar">
              <div class="hpFill" :style="{ width: hpPercent(playerAnimal.hpCurrent, playerAnimal.stats.hpMax) + '%' }" />
            </div>

            <div class="hpText muted small">
              {{ playerAnimal.name }} • {{ playerAnimal.hpCurrent }}/{{ playerAnimal.stats.hpMax }}
            </div>
          </div>

          <div class="vs">VS</div>

          <!-- Enemy (RIGHT) -->
          <div class="fighter right">
            <div class="label muted small">Enemy</div>

            <!-- 🚀 lunge wrapper -->
            <div class="lungeWrap" :class="{ lungeLeft: enemyAttacking }">
              <div class="spriteBlock enemy" :class="{ hit: enemyHit }">
                <img class="spriteImg flip" :src="spriteUrl(battle.enemy.kind)" :alt="battle.enemy.name" draggable="false" />
              </div>
            </div>

            <div class="hpBar">
              <div class="hpFill" :style="{ width: hpPercent(battle.enemy.hpCurrent, battle.enemy.stats.hpMax) + '%' }" />
            </div>

            <div class="hpText muted small">
              {{ battle.enemy.name }} • {{ battle.enemy.hpCurrent }}/{{ battle.enemy.stats.hpMax }}
            </div>
          </div>
        </div>

        <!-- Controls -->
        <div class="actions" v-if="!battle.ended">
          <button class="btn primary" @click="stepTurn('attack')" type="button" :disabled="isAnimating">
            Attack
          </button>

          <button class="btn" @click="stepTurn('defend')" type="button" :disabled="isAnimating">
            Defend
          </button>

          <!-- ✅ Use Item (3rd option) -->
          <div class="itemControls">
            <select class="select" v-model="selectedBattleItem" :disabled="isAnimating || usableBattleItems.length === 0">
              <option value="" disabled>
                {{ usableBattleItems.length === 0 ? 'No battle items' : 'Choose battle item' }}
              </option>

              <option v-for="it in usableBattleItems" :key="it.kind" :value="it.kind">
                {{ it.name }} (x{{ player?.inventory?.[it.kind] ?? 0 }})
              </option>
            </select>

            <button
              class="btn"
              @click="stepUseItem"
              type="button"
              :disabled="isAnimating || !selectedBattleItem || usableBattleItems.length === 0"
            >
              Use Item
            </button>
          </div>
        </div>

        <div class="muted small itemHint" v-if="!battle.ended">
          Bandage/Medkit keep the HP. Attack/Defense pills wear off after this boss fight ends.
        </div>
      </div>

      <!-- BOTTOM: Battle log full width -->
      <div class="panel logPanel">
        <h3>Battle Log</h3>
        <div class="log">
          <div v-for="(line, i) in battle.log" :key="i" class="logLine">{{ line }}</div>
        </div>
      </div>
    </div>

    <!-- Stage Win overlay (after stage 1 or 2) -->
    <div v-if="stageOverlay" class="overlay" role="dialog" aria-modal="true">
      <div class="overlayCard">
        <div class="overlayTop">
          <div class="overlayBadge win">STAGE CLEARED</div>
        </div>

        <div class="overlaySprite" v-if="stageOverlaySpriteKind">
          <img class="overlayImg" :src="spriteUrl(stageOverlaySpriteKind)" :alt="stageOverlaySpriteAlt" draggable="false" />
        </div>

        <div class="overlayText">
          <div class="overlayHeadline">Boss Defeated!</div>
          <div class="muted">
            Stage reward: <b>${{ stageOverlay.stageReward }}</b><br />
            Run total: <b>${{ stageOverlay.totalRunGold }}</b>
          </div>
        </div>

        <div class="overlayActions">
          <button class="btn primary" @click="continueToNextStage" type="button">Continue</button>
          <button class="btn" @click="dropOutWithWinnings" type="button">Drop Out</button>
        </div>
      </div>
    </div>

    <!-- Final Result overlay -->
    <div v-if="showOverlay" class="overlay" role="dialog" aria-modal="true">
      <div class="overlayCard">
        <div class="overlayTop">
          <div class="overlayBadge" :class="{ win: result?.outcome === 'win', loss: result?.outcome === 'loss' }">
            {{ overlayTitle }}
          </div>
        </div>

        <div class="overlaySprite" v-if="overlaySpriteKind">
          <img class="overlayImg" :src="spriteUrl(overlaySpriteKind)" :alt="overlaySpriteAlt" draggable="false" />
        </div>

        <div class="overlayText">
          <div class="overlayHeadline">You {{ result?.outcome === 'win' ? 'Won!' : 'Lost!' }}</div>
          <div class="muted">{{ result?.message }}</div>
        </div>

        <div class="overlayActions">
          <button class="btn primary" @click="closeOverlayAndReset" type="button">Try Again</button>
          <button class="btn" @click="closeOverlayBackToStable" type="button">Back to Stable</button>
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
.card {
  border: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: 18px;
  background: rgba(255, 255, 255, 0.06);
  box-shadow: 0 12px 40px rgba(0, 0, 0, 0.35);
  padding: 18px;
}

.muted { color: rgba(255, 255, 255, 0.70); }
.small { font-size: 12px; }

.panel {
  padding: 14px;
  border-radius: 16px;
  border: 1px solid rgba(255,255,255,.12);
  background: rgba(255,255,255,.04);
}

.row { display:flex; gap:14px; flex-wrap:wrap; margin-top:14px; }

.animalBtn{
  width:100%;
  text-align:left;
  border-radius:14px;
  padding:10px 12px;
  margin-top:10px;
  border:1px solid rgba(255,255,255,.12);
  background:rgba(255,255,255,.05);
  color:rgba(255,255,255,.92);
  cursor:pointer;
}
.animalBtn.active{
  border:none;
  color:#0b1020;
  font-weight:900;
  background:linear-gradient(90deg,#7c5cff,#35d6c5);
}
.name{ font-weight:900; display:block; }

.btn{
  border-radius:14px;
  padding:10px 14px;
  border:1px solid rgba(255,255,255,.12);
  background:rgba(255,255,255,.06);
  color:rgba(255,255,255,.92);
  cursor:pointer;
  text-decoration:none;
  max-width: 100%;
}
.btn.primary{
  border:none;
  background:linear-gradient(90deg,#7c5cff,#35d6c5);
  color:#0b1020;
  font-weight:900;
}
.btn:disabled{ opacity:.5; cursor:not-allowed; }

.actions{
  display:flex;
  gap:10px;
  flex-wrap:wrap;
  margin-top:12px;
  align-items: center;
}

/* ✅ Item controls */
.itemControls{
  display:flex;
  gap:10px;
  flex-wrap:wrap;
  align-items:center;
}

.select{
  padding:10px 12px;
  border-radius:14px;
  border:1px solid rgba(255,255,255,.12);
  background:rgba(0,0,0,.18);
  color:rgba(255,255,255,.92);
}

.itemHint{
  margin-top: 10px;
}

/* -------- layout -------- */
.battleLayout{
  display: grid;
  gap: 14px;
  margin-top: 14px;
}

.battlePanel{ width: 100%; }

.battleHeader{
  display:flex;
  align-items:center;
  justify-content: space-between;
  gap: 12px;
  flex-wrap: wrap;
}

.rightHeader{
  display:flex;
  gap:10px;
  align-items:center;
  flex-wrap: wrap;
  max-width: 100%;
}

.pill{
  padding: 8px 12px;
  border-radius: 999px;
  border: 1px solid rgba(255,255,255,.12);
  background: rgba(255,255,255,.06);
}

/* -------- Arena -------- */
.arena{
  margin-top: 12px;
  border-radius: 16px;
  border: 1px solid rgba(255,255,255,.10);
  background:
    radial-gradient(700px 320px at 25% 10%, rgba(124, 92, 255, .16), transparent 55%),
    radial-gradient(700px 320px at 80% 35%, rgba(53, 214, 197, .12), transparent 55%),
    rgba(0,0,0,.14);
  padding: 14px;

  display: grid;
  grid-template-columns: 1fr auto 1fr;
  align-items: center;
  gap: 12px;
  overflow: hidden;
  min-height: 240px;
}

.fighter{
  min-width: 0;
  display:flex;
  flex-direction:column;
  gap:8px;
}

.fighter.left { align-items: flex-start; justify-self: start; }
.fighter.right { align-items: flex-end; justify-self: end; }

.vs{
  justify-self: center;
  font-weight: 900;
  letter-spacing: 1px;
  opacity: .85;
  padding: 6px 10px;
  border-radius: 999px;
  border: 1px solid rgba(255,255,255,.12);
  background: rgba(255,255,255,.06);
}

/* ---------------- Attack lunge animation ----------------
   We animate a wrapper so it doesn't fight with spriteBlock hit transform.
*/
.lungeWrap{
  display: inline-block;
  will-change: transform;
}

/* Player lunges right (toward enemy) */
.lungeWrap.lungeRight{
  animation: lungeRight 260ms ease-in-out;
}

/* Enemy lunges left (toward player) */
.lungeWrap.lungeLeft{
  animation: lungeLeft 260ms ease-in-out;
}

/* Desktop distance */
@keyframes lungeRight{
  0%   { transform: translateX(0); }
  45%  { transform: translateX(46px); }
  100% { transform: translateX(0); }
}
@keyframes lungeLeft{
  0%   { transform: translateX(0); }
  45%  { transform: translateX(-46px); }
  100% { transform: translateX(0); }
}

/* sprite container */
.spriteBlock{
  width: 140px;
  height: 140px;
  display:grid;
  place-items:center;
  border-radius:18px;
  border:1px solid rgba(255,255,255,.12);
  background: rgba(255,255,255,.05);
  box-shadow: 0 12px 30px rgba(0,0,0,.25);
  transition: transform 120ms ease;
  position: relative;
  overflow: hidden;
  max-width: 100%;
}

.spriteImg{
  width: 110px;
  height: 110px;
  object-fit: contain;
  user-select:none;
  -webkit-user-drag:none;
  animation: bob 1.8s ease-in-out infinite;
  max-width: 100%;
}

@keyframes bob{
  0%,100%{ transform: translateY(0px); }
  50%{ transform: translateY(-6px); }
}

.flip{ transform: scaleX(-1); }

/* hit red flash */
.spriteBlock.hit { transform: translateX(-6px); }
.spriteBlock.enemy.hit { transform: translateX(6px); }

.spriteBlock.hit::after,
.spriteBlock.enemy.hit::after{
  content:'';
  position:absolute;
  inset:0;
  background: rgba(255, 40, 60, 0.35);
  mix-blend-mode: screen;
  animation: hitflash 170ms ease-out;
}

@keyframes hitflash {
  0% { opacity: 0.9; }
  100% { opacity: 0; }
}

/* HP */
.hpBar{
  width: 180px;
  height: 12px;
  border-radius: 999px;
  border: 1px solid rgba(255,255,255,.14);
  background: rgba(0,0,0,.22);
  overflow: hidden;
  max-width: 100%;
}

.hpFill{
  height: 100%;
  width: 100%;
  border-radius: 999px;
  background: linear-gradient(90deg, rgba(53,214,197,.95), rgba(124,92,255,.95));
  transition: width 180ms ease;
}

.hpText{
  width: 180px;
  text-align: center;
  max-width: 100%;
  overflow-wrap: anywhere;
}

/* Log */
.logPanel{ width: 100%; }
.log{
  margin-top: 10px;
  max-height: 260px;
  overflow: auto;
  padding-right: 6px;
}

.logLine{
  padding:8px 10px;
  margin-top:8px;
  border-radius:12px;
  border:1px solid rgba(255,255,255,.10);
  background:rgba(0,0,0,.18);
  overflow-wrap: anywhere;
}

/* Mobile arena adjustments */
@media (max-width: 560px){
  .card{ padding: 14px; }

  .arena{
    grid-template-columns: 1fr;
    grid-template-rows: auto auto auto;
    justify-items: center;
    min-height: 420px;
    padding: 12px;
  }

  .fighter.left, .fighter.right{
    align-items: center;
    justify-self: center;
  }

  .spriteBlock{
    width: 118px;
    height: 118px;
  }

  .spriteImg{
    width: 92px;
    height: 92px;
  }

  .hpBar, .hpText{
    width: min(220px, 88vw);
  }

  .actions{
    justify-content: center;
  }

  /* Shorter lunge distance on mobile */
  @keyframes lungeRight{
    0%   { transform: translateX(0); }
    45%  { transform: translateX(26px); }
    100% { transform: translateX(0); }
  }
  @keyframes lungeLeft{
    0%   { transform: translateX(0); }
    45%  { transform: translateX(-26px); }
    100% { transform: translateX(0); }
  }
}

/* -------- Overlay modal -------- */
.overlay{
  position: fixed;
  inset: 0;
  background: rgba(0,0,0,0.55);
  backdrop-filter: blur(6px);
  display: grid;
  place-items: center;
  z-index: 999;
  padding: 12px;
  overflow: auto;
  overscroll-behavior: contain;
}

.overlayCard{
  width: min(520px, 94vw);
  max-width: 94vw;
  max-height: 92vh;
  overflow: auto;
  border-radius: 20px;
  border: 1px solid rgba(255,255,255,.14);
  background:
    radial-gradient(600px 260px at 30% 10%, rgba(124,92,255,.18), transparent 55%),
    radial-gradient(600px 260px at 80% 35%, rgba(53, 214, 197, .14), transparent 55%),
    rgba(15, 18, 30, 0.92);
  box-shadow: 0 24px 80px rgba(0,0,0,.55);
  padding: 16px;
  box-sizing: border-box;
}

.overlayTop{ display:flex; justify-content:flex-end; }

.overlayBadge{
  padding: 8px 12px;
  border-radius: 999px;
  font-weight: 900;
  letter-spacing: 1px;
  border: 1px solid rgba(255,255,255,.14);
  background: rgba(255,255,255,.06);
  max-width: 100%;
  overflow-wrap: anywhere;
}
.overlayBadge.win{ background: rgba(53,214,197,.18); }
.overlayBadge.loss{ background: rgba(255, 70, 90, .16); }

.overlaySprite{ margin-top: 10px; display:flex; justify-content:center; }

.overlayImg{
  width: 140px;
  height: 140px;
  object-fit: contain;
  animation: bob 1.8s ease-in-out infinite;
  user-select:none;
  -webkit-user-drag:none;
  max-width: 100%;
}

.overlayText{ margin-top: 8px; text-align:center; }
.overlayHeadline{
  font-size: 28px;
  font-weight: 1000;
  margin-bottom: 6px;
  overflow-wrap: anywhere;
}

.overlayActions{
  margin-top: 14px;
  display:flex;
  gap: 10px;
  justify-content:center;
  flex-wrap:wrap;
}

/* Mobile overlay adjustments */
@media (max-width: 420px){
  .overlayCard{
    width: 96vw;
    padding: 14px;
  }

  .overlayImg{
    width: 120px;
    height: 120px;
  }

  .overlayHeadline{
    font-size: 22px;
  }

  .overlayActions{
    gap: 8px;
  }

  .overlayActions .btn{
    flex: 1 1 140px;
    max-width: 100%;
  }
}
</style>
