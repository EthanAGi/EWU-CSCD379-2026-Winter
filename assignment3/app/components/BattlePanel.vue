<script setup lang="ts">
import type { Animal, BattleMove, BattleState, ItemKind } from '../types/game'

export interface BattlePanelProps {
  battle: BattleState
  playerAnimal: Animal
  atkMult: number
  defMult: number
  playerHit: boolean
  enemyHit: boolean
  playerAttacking: boolean
  enemyAttacking: boolean
  isAnimating: boolean
  runGold: number
  selectedBattleItem: ItemKind | ''
  usableBattleItems: Array<{ kind: ItemKind; name: string; desc: string }>
  player: any
}

withDefaults(defineProps<BattlePanelProps>(), {})

const emit = defineEmits<{
  attack: []
  defend: []
  useItem: []
  reset: []
  'update:selectedBattleItem': [value: ItemKind | '']
}>()

const spriteUrl = (kind: string) => `/sprites/${kind}.png`

function hpPercent(current: number, max: number): number {
  if (max <= 0) return 0
  return Math.max(0, Math.min(100, (current / max) * 100))
}

function handleItemSelect(event: Event) {
  const value = (event.target as HTMLSelectElement).value as ItemKind | ''
  emit('update:selectedBattleItem', value)
}
</script>

<template>
  <div class="panel battlePanel">
    <!-- Header -->
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

        <button class="btn" @click="emit('reset')" type="button">Reset</button>
      </div>
    </div>

    <!-- Arena -->
    <div class="arena">
      <!-- Player (LEFT) -->
      <div class="fighter left">
        <div class="label muted small">You</div>

        <!-- Lunge wrapper -->
        <div class="lungeWrap" :class="{ lungeRight: playerAttacking }">
          <div class="spriteBlock" :class="{ hit: playerHit }">
            <img
              class="spriteImg"
              :src="spriteUrl(playerAnimal.kind)"
              :alt="playerAnimal.name"
              draggable="false"
            />
          </div>
        </div>

        <div class="hpBar">
          <div
            class="hpFill"
            :style="{ width: hpPercent(playerAnimal.hpCurrent, playerAnimal.stats.hpMax) + '%' }"
          />
        </div>

        <div class="hpText muted small">
          {{ playerAnimal.name }} • {{ playerAnimal.hpCurrent }}/{{ playerAnimal.stats.hpMax }}
        </div>
      </div>

      <div class="vs">VS</div>

      <!-- Enemy (RIGHT) -->
      <div class="fighter right">
        <div class="label muted small">Enemy</div>

        <!-- Lunge wrapper -->
        <div class="lungeWrap" :class="{ lungeLeft: enemyAttacking }">
          <div class="spriteBlock enemy" :class="{ hit: enemyHit }">
            <img
              class="spriteImg flip"
              :src="spriteUrl(battle.enemy.kind)"
              :alt="battle.enemy.name"
              draggable="false"
            />
          </div>
        </div>

        <div class="hpBar">
          <div
            class="hpFill"
            :style="{ width: hpPercent(battle.enemy.hpCurrent, battle.enemy.stats.hpMax) + '%' }"
          />
        </div>

        <div class="hpText muted small">
          {{ battle.enemy.name }} • {{ battle.enemy.hpCurrent }}/{{ battle.enemy.stats.hpMax }}
        </div>
      </div>
    </div>

    <!-- Controls -->
    <div class="actions" v-if="!battle.ended">
      <button class="btn primary" @click="emit('attack')" type="button" :disabled="isAnimating">
        Attack
      </button>

      <button class="btn" @click="emit('defend')" type="button" :disabled="isAnimating">
        Defend
      </button>

      <!-- Use Item -->
      <div class="itemControls">
        <select
          class="select"
          :value="selectedBattleItem"
          @change="handleItemSelect"
          :disabled="isAnimating || usableBattleItems.length === 0"
        >
          <option value="" disabled>
            {{ usableBattleItems.length === 0 ? 'No battle items' : 'Choose battle item' }}
          </option>

          <option v-for="it in usableBattleItems" :key="it.kind" :value="it.kind">
            {{ it.name }} (x{{ player?.inventory?.[it.kind] ?? 0 }})
          </option>
        </select>

        <button
          class="btn"
          @click="emit('useItem')"
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
</template>

<style scoped>
.muted { color: rgba(255, 255, 255, 0.70); }
.small { font-size: 12px; }

.panel {
  padding: 14px;
  border-radius: 16px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(255, 255, 255, 0.04);
}

.battlePanel { width: 100%; }

.battleHeader {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  flex-wrap: wrap;
}

.rightHeader {
  display: flex;
  gap: 10px;
  align-items: center;
  flex-wrap: wrap;
  max-width: 100%;
}

.pill {
  padding: 8px 12px;
  border-radius: 999px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(255, 255, 255, 0.06);
}

/* Arena */
.arena {
  margin-top: 12px;
  border-radius: 16px;
  border: 1px solid rgba(255, 255, 255, 0.10);
  background:
    radial-gradient(700px 320px at 25% 10%, rgba(124, 92, 255, 0.16), transparent 55%),
    radial-gradient(700px 320px at 80% 35%, rgba(53, 214, 197, 0.12), transparent 55%),
    rgba(0, 0, 0, 0.14);
  padding: 14px;

  display: grid;
  grid-template-columns: 1fr auto 1fr;
  align-items: center;
  gap: 12px;
  overflow: hidden;
  min-height: 240px;
}

.fighter {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.fighter.left { align-items: flex-start; justify-self: start; }
.fighter.right { align-items: flex-end; justify-self: end; }

.label { font-size: 12px; }

.vs {
  justify-self: center;
  font-weight: 900;
  letter-spacing: 1px;
  opacity: 0.85;
  padding: 6px 10px;
  border-radius: 999px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(255, 255, 255, 0.06);
}

/* Lunge animation */
.lungeWrap {
  display: inline-block;
  will-change: transform;
}

.lungeWrap.lungeRight { animation: lungeRight 260ms ease-in-out; }
.lungeWrap.lungeLeft { animation: lungeLeft 260ms ease-in-out; }

@keyframes lungeRight {
  0% { transform: translateX(0); }
  45% { transform: translateX(46px); }
  100% { transform: translateX(0); }
}
@keyframes lungeLeft {
  0% { transform: translateX(0); }
  45% { transform: translateX(-46px); }
  100% { transform: translateX(0); }
}

/* Sprite */
.spriteBlock {
  width: 140px;
  height: 140px;
  display: grid;
  place-items: center;
  border-radius: 18px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(255, 255, 255, 0.05);
  box-shadow: 0 12px 30px rgba(0, 0, 0, 0.25);
  transition: transform 120ms ease;
  position: relative;
  overflow: hidden;
  max-width: 100%;
}

.spriteImg {
  width: 110px;
  height: 110px;
  object-fit: contain;
  user-select: none;
  -webkit-user-drag: none;
  animation: bob 1.8s ease-in-out infinite;
  max-width: 100%;
}

@keyframes bob {
  0%, 100% { transform: translateY(0px); }
  50% { transform: translateY(-6px); }
}

.flip { transform: scaleX(-1); }

/* Hit flash */
.spriteBlock.hit { transform: translateX(-6px); }
.spriteBlock.enemy.hit { transform: translateX(6px); }

.spriteBlock.hit::after,
.spriteBlock.enemy.hit::after {
  content: '';
  position: absolute;
  inset: 0;
  background: rgba(255, 40, 60, 0.35);
  mix-blend-mode: screen;
  animation: hitflash 170ms ease-out;
}

@keyframes hitflash {
  0% { opacity: 0.9; }
  100% { opacity: 0; }
}

/* HP Bar */
.hpBar {
  width: 180px;
  height: 12px;
  border-radius: 999px;
  border: 1px solid rgba(255, 255, 255, 0.14);
  background: rgba(0, 0, 0, 0.22);
  overflow: hidden;
  max-width: 100%;
}

.hpFill {
  height: 100%;
  width: 100%;
  border-radius: 999px;
  background: linear-gradient(90deg, rgba(53, 214, 197, 0.95), rgba(124, 92, 255, 0.95));
  transition: width 180ms ease;
}

.hpText {
  width: 180px;
  text-align: center;
  max-width: 100%;
  overflow-wrap: anywhere;
}

/* Actions */
.actions {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
  margin-top: 12px;
  align-items: center;
}

.btn {
  border-radius: 14px;
  padding: 10px 14px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(255, 255, 255, 0.06);
  color: rgba(255, 255, 255, 0.92);
  cursor: pointer;
  text-decoration: none;
  max-width: 100%;
}

.btn.primary {
  border: none;
  background: linear-gradient(90deg, #7c5cff, #35d6c5);
  color: #0b1020;
  font-weight: 900;
}

.btn:disabled { opacity: 0.5; cursor: not-allowed; }

.itemControls {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
  align-items: center;
}

.select {
  padding: 10px 12px;
  border-radius: 14px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(0, 0, 0, 0.18);
  color: rgba(255, 255, 255, 0.92);
}

.itemHint { margin-top: 10px; }

/* Mobile adjustments */
@media (max-width: 560px) {
  .arena {
    grid-template-columns: 1fr;
    grid-template-rows: auto auto auto;
    justify-items: center;
    min-height: 420px;
    padding: 12px;
  }

  .fighter.left,
  .fighter.right {
    align-items: center;
    justify-self: center;
  }

  .spriteBlock {
    width: 118px;
    height: 118px;
  }

  .spriteImg {
    width: 92px;
    height: 92px;
  }

  .hpBar,
  .hpText {
    width: min(220px, 88vw);
  }

  .actions { justify-content: center; }

  @keyframes lungeRight {
    0% { transform: translateX(0); }
    45% { transform: translateX(26px); }
    100% { transform: translateX(0); }
  }
  @keyframes lungeLeft {
    0% { transform: translateX(0); }
    45% { transform: translateX(-26px); }
    100% { transform: translateX(0); }
  }
}
</style>
