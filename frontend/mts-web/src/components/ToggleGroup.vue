<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import Toggle from './Toggle.vue'
import type { ToggleValue } from './Toggle.vue'

export interface ToggleOption {
  label: string
  value: ToggleValue
  disabled?: boolean
}

type ToggleOrientation = 'horizontal' | 'vertical' | 'verticle'

const model = defineModel<ToggleValue | null>({
  default: null,
})

const props = withDefaults(
  defineProps<{
    options: ToggleOption[]
    disabled?: boolean
    ariaLabel?: string
    stretch?: boolean
    orientation?: ToggleOrientation
    wrap?: boolean
    hideOverflow?: boolean
  }>(),
  {
    disabled: false,
    ariaLabel: 'Lựa chọn',
    stretch: false,
    orientation: 'horizontal',
    wrap: false,
    hideOverflow: true,
  },
)

const emit = defineEmits<{
  change: [value: ToggleValue, option: ToggleOption]
}>()

const enabledOptions = computed(() => props.options.filter((option) => !option.disabled))
const normalizedOrientation = computed(() =>
  props.orientation === 'vertical' || props.orientation === 'verticle' ? 'vertical' : 'horizontal',
)
const groupEl = ref<HTMLElement | null>(null)
const maxVisibleCount = ref(props.options.length)
let resizeObserver: ResizeObserver | undefined
let animationFrame = 0

const displayedOptions = computed(() => props.options.slice(0, maxVisibleCount.value))

const resetVisibleCount = () => {
  maxVisibleCount.value = props.options.length
}

const measureOverflow = async () => {
  if (!props.hideOverflow || props.wrap || props.stretch) {
    resetVisibleCount()
    return
  }

  resetVisibleCount()
  await nextTick()

  const element = groupEl.value
  const firstToggle = element?.querySelector<HTMLElement>('.toggle-tab')

  if (!element || !firstToggle) {
    return
  }

  const styles = window.getComputedStyle(element)
  const gap = Number.parseFloat(styles.columnGap || styles.gap || '0') || 0
  const availableSize =
    normalizedOrientation.value === 'vertical' ? element.clientHeight : element.clientWidth
  const itemSize =
    normalizedOrientation.value === 'vertical'
      ? firstToggle.offsetHeight
      : firstToggle.offsetWidth

  if (!availableSize || !itemSize) {
    return
  }

  const nextCount = Math.max(0, Math.floor((availableSize + gap) / (itemSize + gap)))
  maxVisibleCount.value = Math.min(props.options.length, nextCount)
}

const scheduleMeasure = () => {
  if (animationFrame) {
    cancelAnimationFrame(animationFrame)
  }

  animationFrame = requestAnimationFrame(() => {
    void measureOverflow()
  })
}

const selectOption = (option: ToggleOption) => {
  if (props.disabled || option.disabled) {
    return
  }

  model.value = option.value
  emit('change', option.value, option)
}

const moveSelection = (step: number) => {
  if (!enabledOptions.value.length) {
    return
  }

  const visibleEnabledOptions = displayedOptions.value.filter((option) => !option.disabled)

  if (!visibleEnabledOptions.length) {
    return
  }

  const currentIndex = visibleEnabledOptions.findIndex((option) => option.value === model.value)
  const nextIndex =
    currentIndex === -1
      ? 0
      : (currentIndex + step + visibleEnabledOptions.length) % visibleEnabledOptions.length

  selectOption(visibleEnabledOptions[nextIndex])
}

const handleKeydown = (event: KeyboardEvent) => {
  const nextKeys =
    normalizedOrientation.value === 'vertical' ? ['ArrowDown'] : ['ArrowRight']
  const previousKeys =
    normalizedOrientation.value === 'vertical' ? ['ArrowUp'] : ['ArrowLeft']

  if (nextKeys.includes(event.key)) {
    event.preventDefault()
    moveSelection(1)
  }

  if (previousKeys.includes(event.key)) {
    event.preventDefault()
    moveSelection(-1)
  }
}

watch(
  () => [props.options, props.orientation, props.wrap, props.stretch, props.hideOverflow],
  () => {
    scheduleMeasure()
  },
  { deep: true },
)

onMounted(() => {
  resizeObserver = new ResizeObserver(scheduleMeasure)

  if (groupEl.value) {
    resizeObserver.observe(groupEl.value)
  }

  scheduleMeasure()
})

onBeforeUnmount(() => {
  resizeObserver?.disconnect()

  if (animationFrame) {
    cancelAnimationFrame(animationFrame)
  }
})
</script>

<template>
  <div
    ref="groupEl"
    class="toggle-group"
    :class="[
      `toggle-group--${normalizedOrientation}`,
      {
        'toggle-group--stretch': stretch,
        'toggle-group--wrap': wrap,
      },
    ]"
    role="radiogroup"
    :aria-label="ariaLabel"
    :aria-orientation="normalizedOrientation"
    @keydown="handleKeydown"
  >
    <Toggle
      v-for="option in displayedOptions"
      :key="option.value"
      mode="radio"
      :value="option.value"
      :selected="model === option.value"
      :disabled="disabled || option.disabled"
      @select="selectOption(option)"
    >
      {{ option.label }}
    </Toggle>
  </div>
</template>

<style scoped>
.toggle-group {
  width: 100%;
  container-type: inline-size;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: clamp(2px, 0.5cqw, 8px);
}

.toggle-group--horizontal {
  flex-direction: row;
  flex-wrap: nowrap;
}

.toggle-group--vertical {
  flex-direction: column;
  flex-wrap: nowrap;
}

.toggle-group--wrap {
  flex-wrap: wrap;
}

.toggle-group--horizontal :deep(.toggle-tab) {
  flex: 0 0 auto;
  width: clamp(108px, 24cqw, 168px);
}

.toggle-group--stretch :deep(.toggle-tab) {
  flex: 1 1 0;
  max-width: none;
}

.toggle-group--vertical.toggle-group--stretch :deep(.toggle-tab) {
  flex: 0 0 auto;
  width: 100%;
  max-width: 100%;
}

@media (max-width: 640px) {
  .toggle-group--horizontal :deep(.toggle-tab) {
    width: clamp(94px, 36cqw, 168px);
  }

  .toggle-group--stretch :deep(.toggle-tab) {
    flex-basis: 0;
    max-width: none;
  }

  .toggle-group--vertical.toggle-group--stretch :deep(.toggle-tab) {
    width: 100%;
    max-width: 100%;
  }
}
</style>
