<script setup lang="ts">
import type { CSSProperties } from 'vue'
import buttonImage from '../assets/sprites/sprite_08.png'

withDefaults(
  defineProps<{
    type?: 'button' | 'submit' | 'reset'
    disabled?: boolean
  }>(),
  {
    type: 'button',
    disabled: false,
  },
)

defineEmits<{
  click: [event: MouseEvent]
}>()

const buttonStyle = {
  '--button-image': `url(${buttonImage})`,
} as CSSProperties
</script>

<template>
  <button
    class="image-button"
    :type="type"
    :disabled="disabled"
    :style="buttonStyle"
    @click="$emit('click', $event)"
  >
    <span class="image-button__text">
      <slot />
    </span>
  </button>
</template>

<style scoped>
.image-button {
  width: min(100%, clamp(160px, 20vw, 180px));
  aspect-ratio: 238 / 74;
  display: inline-grid;
  place-items: center;
  border: 0;
  background-color: transparent;
  background-image: var(--button-image);
  background-position: center;
  background-repeat: no-repeat;
  background-size: 100% 100%;
  cursor: pointer;
  color: #fff7bd;
  font-family: var(--font-cloud-shui-zhu);
  filter: drop-shadow(0 6px 10px rgba(0, 0, 0, 0.28));
  transition:
    filter 160ms ease,
    transform 160ms ease;
}

.image-button:hover:not(:disabled) {
  filter:
    drop-shadow(0 8px 12px rgba(0, 0, 0, 0.32))
    drop-shadow(0 0 8px rgba(255, 217, 88, 0.45));
  transform: translateY(-1px);
}

.image-button:active:not(:disabled) {
  transform: translateY(0) scale(0.98);
}

.image-button:focus-visible {
  outline: 2px solid #fff0a6;
  outline-offset: 3px;
}

.image-button:disabled {
  cursor: not-allowed;
  filter: grayscale(0.7) opacity(0.62);
}

.image-button__text {
  max-width: 78%;
  overflow: hidden;
  text-align: center;
  white-space: nowrap;
  text-overflow: ellipsis;
  font-size: clamp(16px, 1.7vw, 24px);
  line-height: 1;
  letter-spacing: 0;
}

</style>
