<script setup lang="ts">
import type { CSSProperties } from 'vue'
import selectedImage from '../assets/images/toggle-tab-selected.png'
import unselectedImage from '../assets/images/toggle-tab-unselected.png'

export type ToggleValue = string | number

const props = withDefaults(
  defineProps<{
    value?: ToggleValue
    selected?: boolean
    disabled?: boolean
    mode?: 'button' | 'radio'
    type?: 'button' | 'submit' | 'reset'
  }>(),
  {
    selected: false,
    disabled: false,
    mode: 'button',
    type: 'button',
  },
)

const emit = defineEmits<{
  click: [event: MouseEvent]
  select: [value: ToggleValue | undefined]
}>()

const toggleStyle = {
  '--toggle-selected-image': `url(${selectedImage})`,
  '--toggle-unselected-image': `url(${unselectedImage})`,
} as CSSProperties

const handleClick = (event: MouseEvent) => {
  if (props.disabled) {
    return
  }

  emit('click', event)
  emit('select', props.value)
}
</script>

<template>
  <button
    class="toggle-tab"
    :class="{
      'toggle-tab--selected': selected,
      'toggle-tab--disabled': disabled,
    }"
    :type="type"
    :disabled="disabled"
    :style="toggleStyle"
    :role="mode === 'radio' ? 'radio' : undefined"
    :aria-checked="mode === 'radio' ? selected : undefined"
    :aria-pressed="mode === 'button' ? selected : undefined"
    @click="handleClick"
  >
    <span class="toggle-tab__label">
      <slot />
    </span>
  </button>
</template>

<style scoped>
.toggle-tab {
  position: relative;
  isolation: isolate;
  width: 100%;
  max-width: 100%;
  min-width: 0;
  aspect-ratio: 580 / 160;
  display: inline-grid;
  place-items: center;
  border: 0;
  background-color: transparent;
  cursor: pointer;
  -webkit-tap-highlight-color: transparent;
  touch-action: manipulation;
  color: #f8e7b7;
  filter: drop-shadow(0 6px 10px rgba(0, 0, 0, 0.25));
  transition:
    color 140ms ease,
    filter 160ms ease,
    transform 160ms ease;
  will-change: transform;
}

.toggle-tab::before,
.toggle-tab::after {
  content: '';
  position: absolute;
  inset: 0;
  z-index: -1;
  background-position: center;
  background-repeat: no-repeat;
  background-size: 100% 100%;
  transition: opacity 160ms ease;
  transform: translateZ(0);
}

.toggle-tab::before {
  background-image: var(--toggle-unselected-image);
  opacity: 1;
}

.toggle-tab::after {
  background-image: var(--toggle-selected-image);
  opacity: 0;
}

.toggle-tab--selected::before {
  opacity: 0;
}

.toggle-tab--selected::after {
  opacity: 1;
}

.toggle-tab:hover:not(:disabled) {
  filter:
    drop-shadow(0 8px 12px rgba(0, 0, 0, 0.3))
    drop-shadow(0 0 8px rgba(255, 221, 118, 0.28));
  transform: translateY(-1px);
}

.toggle-tab:active:not(:disabled) {
  transform: translateY(0) scale(0.985);
}

.toggle-tab:focus-visible {
  outline: 2px solid #fff0a6;
  outline-offset: 3px;
}

.toggle-tab--selected {
  color: #fff8c8;
}

.toggle-tab--disabled {
  cursor: not-allowed;
  filter: grayscale(0.65) opacity(0.62);
}

.toggle-tab__label {
  position: relative;
  z-index: 1;
  max-width: 66%;
  overflow: hidden;
  text-align: center;
  white-space: nowrap;
  text-overflow: ellipsis;
  font-family: var(--font-cloud-shui-zhu);
  font-size: clamp(10px, 3.2cqw, 25px);
  line-height: 1;
  letter-spacing: 0;
  text-shadow:
    0 2px 2px rgba(52, 25, 12, 0.76),
    0 0 8px rgba(255, 230, 129, 0.18);
}

@media (hover: none) {
  .toggle-tab {
    filter: drop-shadow(0 4px 7px rgba(0, 0, 0, 0.22));
  }

  .toggle-tab:hover:not(:disabled) {
    filter: drop-shadow(0 4px 7px rgba(0, 0, 0, 0.22));
    transform: none;
  }

  .toggle-tab:active:not(:disabled) {
    transform: scale(0.98);
  }
}

</style>
