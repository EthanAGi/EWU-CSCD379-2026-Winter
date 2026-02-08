<script setup lang="ts">
import type { Animal, BattleMove, BattleState, ItemKind } from '../types/game'
import BattlePanel from '~/components/BattlePanel.vue'

const { player, addGold, healAnimalToFull } = usePlayerState()

/**
 * ✅ IMPORTANT:
 * Single Azure App Service (Nuxt/Nitro).
 * ALL client calls should hit SAME-ORIGIN Nuxt server routes: /api/...
 */

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

/** -------------------------------------------------------
 *  Pull possible opponents from the DATABASE (Nuxt server)
 *  - Base animals (templates)   GET /api/animals/templates
 *  - Player animals (your DB)   GET /api/animals/player/{playerId}
 *  ------------------------------------------------------*/

type AnimalTemplateDto = {
  id: number
  kind: string
  attack: number
  defense: number
  affection: number
  level: number
  hpMax: number
}

type PlayerAnimalDto = {
  id: number
  ownerPlayerId: string
  ownerName: string
  name: string
  kind: string
  attack: number
  defense: number
  affection: number
  level: number
  hpMax: number
  hpCurrent: number
  createdAt: string
  templateId: number
}

const templatesDb = ref<AnimalTemplateDto[]>([])
const playerAnimalsDb = ref<PlayerAnimalDto[]>([])
const opponentsPool = ref<Animal[]>([])
const opponentsLoaded = ref(false)
const opponentsLoadErr = ref<string | null>(null)

function titleCase(s: string) {
  if (!s) return s
  return s.charAt(0).toUpperCase() + s.slice(1)
}

function dtoToAnimalFromTemplate(t: AnimalTemplateDto): Animal {
  const base = { ownerPlayerId: 'npc', ownerName: 'Wilds' }
  return {
    id: `tpl-${t.id}`,
    ...base,
    name: titleCase(t.kind),
    kind: t.kind as any,
    stats: { attack: t.attack, defense: t.defense, affection: t.affection, level: t.level, hpMax: t.hpMax },
    hpCurrent: t.hpMax,
  }
}

function dtoToAnimalFromPlayer(a: PlayerAnimalDto): Animal {
  return {
    id: `pa-${a.id}`,
    ownerPlayerId: a.ownerPlayerId,
    ownerName: a.ownerName,
    name: a.name,
    kind: a.kind as any,
    stats: { attack: a.attack, defense: a.defense, affection: a.affection, level: a.level, hpMax: a.hpMax },
    hpCurrent: a.hpCurrent,
  }
}

function cloneAnimal(a: Animal): Animal {
  return { ...a, stats: { ...a.stats } }
}

/** Load DB animals once the player exists */
async function loadOpponentsFromDb() {
  if (!player.value) return
  if (opponentsLoaded.value) return

  opponentsLoadErr.value = null

  try {
    // ✅ SAME-ORIGIN calls to Nitro server routes
    const templatesUrl = `/api/animals/templates`
    const playerAnimalsUrl = `/api/animals/player/${encodeURIComponent(player.value.id)}`

    const [tpls, pAnimals] = await Promise.all([
      $fetch<AnimalTemplateDto[]>(templatesUrl),
      $fetch<PlayerAnimalDto[]>(playerAnimalsUrl),
    ])

    templatesDb.value = Array.isArray(tpls) ? tpls : []
    playerAnimalsDb.value = Array.isArray(pAnimals) ? pAnimals : []

    opponentsPool.value = [
      ...templatesDb.value.map(dtoToAnimalFromTemplate),
      ...playerAnimalsDb.value.map(dtoToAnimalFromPlayer),
    ]

    opponentsLoaded.value = true
  } catch (e: any) {
    console.error('Failed to load opponents from DB:', e)
    opponentsLoadErr.value = 'Failed to load opponents from the database. Using local fallback enemies.'
    opponentsPool.value = []
    opponentsLoaded.value = true // prevent infinite retry spam; refresh page to retry
  }
}

/**
 * Pick a stage-appropriate random opponent from the pool.
 * If DB pool is empty, use local fallback enemies.
 */
function pickRandomOpponent(stage: 1 | 2 | 3, usedIds: Set<string>, avoidKind?: string, avoidId?: string): Animal {
  const pool = opponentsPool.value

  if (!pool || pool.length === 0) {
    const base = { ownerPlayerId: 'npc', ownerName: 'Wilds' }
    const fallback: Animal[] = [
      { id: 'fb-dog', ...base, name: 'Dog', kind: 'dog', stats: { attack: 5, defense: 4, affection: 0, level: 1, hpMax: 30 }, hpCurrent: 30 },
      { id: 'fb-cat', ...base, name: 'Cat', kind: 'cat', stats: { attack: 6, defense: 3, affection: 0, level: 1, hpMax: 28 }, hpCurrent: 28 },
      { id: 'fb-hamster', ...base, name: 'Hamster', kind: 'hamster', stats: { attack: 4, defense: 5, affection: 0, level: 1, hpMax: 32 }, hpCurrent: 32 },
      { id: 'fb-owl', ...base, name: 'Owl', kind: 'owl', stats: { attack: 7, defense: 6, affection: 0, level: 1, hpMax: 36 }, hpCurrent: 36 },
      { id: 'fb-fox', ...base, name: 'Fox', kind: 'fox', stats: { attack: 8, defense: 5, affection: 0, level: 1, hpMax: 34 }, hpCurrent: 34 },
      { id: 'fb-boar', ...base, name: 'Boar', kind: 'boar', stats: { attack: 6, defense: 8, affection: 0, level: 1, hpMax: 40 }, hpCurrent: 40 },
    ]
    return cloneAnimal(fallback[Math.floor(Math.random() * fallback.length)]!)
  }

  const stageFilter = (a: Animal) => {
    const hp = a.stats.hpMax
    if (stage === 1) return hp <= 32
    if (stage === 2) return hp <= 36
    return true
  }

  const baseFilter = (a: Animal) => {
    if (usedIds.has(a.id)) return false
    if (avoidId && a.id === avoidId) return false
    if (avoidKind && a.kind === (avoidKind as any)) return false
    return true
  }

  const stageCandidates = pool.filter(a => baseFilter(a) && stageFilter(a))
  const anyCandidates = pool.filter(a => baseFilter(a))
  const candidates = stageCandidates.length > 0 ? stageCandidates : anyCandidates

  const chosen = candidates[Math.floor(Math.random() * candidates.length)]!
  const fresh = cloneAnimal(chosen)
  fresh.hpCurrent = fresh.stats.hpMax
  return fresh
}

function rewardForStage(stage: 1 | 2 | 3): number {
  if (stage === 1) return 250
  if (stage === 2) return 500
  return 1000
}

const battle = ref<BattleState | null>(null)
const runGold = ref(0)

const playerAnimal = computed(() => {
  if (!player.value || !battle.value) return null
  return player.value.animals.find(a => a.id === battle.value!.playerAnimalId) ?? null
})

/** Battle item state */
const atkMult = ref(1)
const defMult = ref(1)

function resetBattleBuffs() {
  atkMult.value = 1
  defMult.value = 1
}

/** Store the 3 opponents for this run */
const runOpponents = ref<Animal[]>([])

async function startGauntlet() {
  if (!player.value) return
  if (!chosenId.value) return

  await loadOpponentsFromDb()

  runGold.value = 0
  stageOverlay.value = null
  result.value = null

  const chosen = player.value.animals.find(a => a.id === chosenId.value) ?? null
  const avoidKind = chosen?.kind
  const avoidId = chosen?.id

  const used = new Set<string>()
  const opp1 = pickRandomOpponent(1, used, avoidKind as any, avoidId)
  used.add(opp1.id)
  const opp2 = pickRandomOpponent(2, used, avoidKind as any, avoidId)
  used.add(opp2.id)
  const opp3 = pickRandomOpponent(3, used, avoidKind as any, avoidId)
  used.add(opp3.id)

  runOpponents.value = [opp1, opp2, opp3]

  battle.value = {
    stage: 1,
    round: 1,
    playerAnimalId: chosenId.value,
    enemy: cloneAnimal(runOpponents.value[0]!),
    playerDefending: false,
    enemyDefending: false,
    log: ['You entered the Gauntlet.'],
    ended: false,
  }

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

// Simple AI
function enemyChooseMove(enemy: Animal): BattleMove {
  const hpPct = enemy.hpCurrent / enemy.stats.hpMax
  return hpPct <= 0.35 ? 'defend' : 'attack'
}

/** Sprites */
function spriteUrl(kind: string) {
  return `/sprites/${kind}.png`
}

/** Hit flash */
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

/** Attack animation toggles */
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
    }, 260)
  }
}

onBeforeUnmount(() => {
  clearAnimTimeout()
})

/** Inventory helpers */
const battleItemDefs = [
  { kind: 'bandage' as ItemKind, name: 'Bandage', desc: 'Heals 30% HP (keeps the health).' },
  { kind: 'medkit' as ItemKind, name: 'Medkit', desc: 'Heals 70% HP (keeps the health).' },
  { kind: 'attackPill' as ItemKind, name: 'Attack Boost Pill', desc: 'Doubles attack for this boss fight only.' },
  { kind: 'defensePill' as ItemKind, name: 'Defense Pill', desc: 'Doubles defense for this boss fight only.' },
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
  addGold(0)
  return true
}

/** Overlays */
const showOverlay = computed(() => !!result.value)

const overlayTitle = computed(() => {
  if (!result.value) return ''
  return result.value.outcome === 'win' ? 'WON' : 'LOST'
})

const overlaySpriteKind = computed(() => {
  if (!result.value || !battle.value || !playerAnimal.value) return null
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

const stageOverlaySpriteKind = computed(() => {
  if (!stageOverlay.value || !playerAnimal.value) return null
  return playerAnimal.value.kind
})

const stageOverlaySpriteAlt = computed(() => {
  if (!stageOverlay.value || !playerAnimal.value) return ''
  return playerAnimal.value.name
})

function continueToNextStage() {
  const b = battle.value
  if (!b || !stageOverlay.value) return

  resetBattleBuffs()

  const nextStage = (b.stage + 1) as 2 | 3
  b.stage = nextStage

  const nextEnemy = runOpponents.value[nextStage - 1]
  if (nextEnemy) {
    const fresh = cloneAnimal(nextEnemy)
    fresh.hpCurrent = fresh.stats.hpMax
    b.enemy = fresh
  }

  b.round = 1
  b.playerDefending = false
  b.enemyDefending = false
  b.log.unshift(`Stage ${nextStage} begins!`)

  stageOverlay.value = null
}

function dropOutWithWinnings() {
  const b = battle.value
  if (!b || !playerAnimal.value) return

  resetBattleBuffs()
  healAnimalToFull(b.playerAnimalId)

  const total = runGold.value
  stageOverlay.value = null

  result.value = {
    outcome: 'win',
    message: `You dropped out safely with $${total} from this run.`,
  }

  b.ended = true
  b.outcome = 'win'
}

/** Apply battle item effects */
function applyBattleItem(kind: ItemKind) {
  const b = battle.value
  const p = playerAnimal.value
  if (!b || !p) return

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

  b.log.unshift('That item has no effect yet.')
}

/** Main turn step */
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

  if (playerMove === 'attack') {
    const effectiveAtk = Math.round(p.stats.attack * atkMult.value)
    const dmg = calcDamage(effectiveAtk, b.enemy.stats.defense, b.enemyDefending)
    b.enemy.hpCurrent = Math.max(0, b.enemy.hpCurrent - dmg)
    b.log.unshift(`You hit ${b.enemy.name} for ${dmg}.`)
    flashEnemyHit()
  }

  if (b.enemy.hpCurrent <= 0) {
    const stageReward = rewardForStage(b.stage)
    addGold(stageReward)
    runGold.value += stageReward

    b.log.unshift(`You defeated ${b.enemy.name}! +$${stageReward} (Run total: $${runGold.value})`)

    if (b.stage === 3) {
      resetBattleBuffs()
      healAnimalToFull(b.playerAnimalId)
      b.ended = true
      b.outcome = 'win'
      b.rewardGold = stageReward
      result.value = { outcome: 'win', message: `You conquered the Gauntlet! Total winnings: $${runGold.value}` }
      return
    }

    resetBattleBuffs()

    stageOverlay.value = {
      stageCleared: b.stage,
      stageReward,
      totalRunGold: runGold.value,
    }
    return
  }

  if (enemyMove === 'attack') {
    const effectiveDef = Math.round(p.stats.defense * defMult.value)
    const dmg = calcDamage(b.enemy.stats.attack, effectiveDef, b.playerDefending)
    p.hpCurrent = Math.max(0, p.hpCurrent - dmg)
    b.log.unshift(`${b.enemy.name} hit you for ${dmg}.`)
    flashPlayerHit()
  }

  if (p.hpCurrent <= 0) {
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

/** Use item */
function stepUseItem() {
  const b = battle.value
  const p = playerAnimal.value
  if (!b || !p || b.ended) return
  if (stageOverlay.value) return
  if (isAnimating.value) return
  if (!selectedBattleItem.value) return

  const enemyMove = enemyChooseMove(b.enemy)
  playAttackAnimation('item', enemyMove)

  b.playerDefending = false
  b.enemyDefending = enemyMove === 'defend'

  b.log.unshift(`Round ${b.round}: You used an item, enemy chose ${enemyMove.toUpperCase()}.`)
  applyBattleItem(selectedBattleItem.value)

  if (enemyMove === 'attack') {
    const effectiveDef = Math.round(p.stats.defense * defMult.value)
    const dmg = calcDamage(b.enemy.stats.attack, effectiveDef, b.playerDefending)
    p.hpCurrent = Math.max(0, p.hpCurrent - dmg)
    b.log.unshift(`${b.enemy.name} hit you for ${dmg}.`)
    flashPlayerHit()
  } else {
    b.log.unshift(`${b.enemy.name} defended.`)
  }

  if (p.hpCurrent <= 0) {
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
  runOpponents.value = []
  playerHit.value = false
  enemyHit.value = false
  playerAttacking.value = false
  enemyAttacking.value = false
  isAnimating.value = false
  clearAnimTimeout()
  resetBattleBuffs()
}

/** Pre-load opponents as soon as player exists */
watchEffect(() => {
  if (import.meta.client && player.value && !opponentsLoaded.value) {
    loadOpponentsFromDb()
  }
})
</script>

<template>
  <section class="card">
    <div class="topRow">
      <h1>Gauntlet</h1>
      <div class="muted small">API: (same-origin /api)</div>
    </div>

    <p class="muted">
      Choose an animal and fight 3 boss battles. Each turn: Attack, Defend, or Use Item.
    </p>

    <!-- If no player, don't blank out -->
    <div v-if="!player" class="panel" style="margin-top: 12px;">
      <b>No player found.</b>
      <div class="muted small" style="margin-top:6px;">
        Go to the Stable and create/select a player first.
      </div>

      <div style="margin-top: 12px;">
        <button class="btn primary" type="button" @click="navigateTo('/stable')">
          Go to Stable
        </button>
      </div>
    </div>

    <!-- DB warning -->
    <div v-else-if="opponentsLoadErr" class="panel warn" style="margin-top: 12px;">
      <b>DB Warning:</b> {{ opponentsLoadErr }}
      <div class="muted small">You can still play — enemies will be generated locally.</div>
    </div>

    <!-- Choosing screen -->
    <div v-if="player && choosing" class="panel" style="margin-top: 12px;">
      <h3>Choose Your Animal</h3>

      <div class="row">
        <button
          v-for="a in player.animals ?? []"
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

      <div style="margin-top: 12px; display:flex; gap:10px; flex-wrap:wrap;">
        <button class="btn primary" :disabled="!chosenId" @click="startGauntlet" type="button">
          Join the Fight
        </button>
        <button class="btn" type="button" @click="navigateTo('/stable')">
          Back to Stable
        </button>
      </div>

      <div v-if="(player.animals?.length ?? 0) === 0" class="muted small" style="margin-top: 10px;">
        You don’t have any animals yet — go to the Stable to add one.
      </div>
    </div>

    <!-- Battle screen -->
    <div v-else-if="battle && playerAnimal" class="battleLayout">
      <BattlePanel
        :battle="battle"
        :playerAnimal="playerAnimal"
        :atkMult="atkMult"
        :defMult="defMult"
        :playerHit="playerHit"
        :enemyHit="enemyHit"
        :playerAttacking="playerAttacking"
        :enemyAttacking="enemyAttacking"
        :isAnimating="isAnimating"
        :runGold="runGold"
        :selectedBattleItem="selectedBattleItem"
        :usableBattleItems="usableBattleItems"
        :player="player"
        @attack="stepTurn('attack')"
        @defend="stepTurn('defend')"
        @useItem="stepUseItem"
        @reset="resetRun"
        @update:selectedBattleItem="(val) => (selectedBattleItem = val)"
      />

      <div class="panel logPanel">
        <h3>Battle Log</h3>
        <div class="log">
          <div v-for="(line, i) in battle.log" :key="i" class="logLine">{{ line }}</div>
        </div>
      </div>
    </div>

    <!-- Stage Win overlay -->
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

.topRow{
  display:flex;
  justify-content:space-between;
  align-items:flex-end;
  gap: 12px;
  flex-wrap: wrap;
}

.muted { color: rgba(255, 255, 255, 0.70); }
.small { font-size: 12px; }

.panel {
  padding: 14px;
  border-radius: 16px;
  border: 1px solid rgba(255,255,255,.12);
  background: rgba(255,255,255,.04);
}
.panel.warn{
  border-color: rgba(255, 200, 70, .25);
  background: rgba(255, 200, 70, .06);
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

.battleLayout{
  display: grid;
  gap: 14px;
  margin-top: 14px;
}

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

/* Overlay modal */
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

@media (max-width: 420px){
  .overlayCard{ width: 96vw; padding: 14px; }
  .overlayImg{ width: 120px; height: 120px; }
  .overlayHeadline{ font-size: 22px; }
  .overlayActions{ gap: 8px; }
  .overlayActions .btn{ flex: 1 1 140px; max-width: 100%; }
}

@keyframes bob{
  0%, 100%{ transform: translateY(0); }
  50%{ transform: translateY(-6px); }
}
</style>
