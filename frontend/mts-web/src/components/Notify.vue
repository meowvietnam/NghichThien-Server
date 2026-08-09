<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import type { CSSProperties } from 'vue'
import notifyImage from '../assets/images/notify.png'

const props = withDefaults(
  defineProps<{
    title?: string
    message?: string
    visible?: boolean
    closable?: boolean
    lifetime?: number
    fixed?: boolean
  }>(),
  {
    title: 'Thông báo',
    message: '',
    visible: true,
    closable: false,
    lifetime: 3600,
    fixed: true,
  },
)

const emit = defineEmits<{
  close: []
  expired: []
  closed: []
}>()

const isVisible = ref(props.visible)
const isPaused = ref(false)
let timer: ReturnType<typeof setTimeout> | undefined
let startedAt = 0
let remainingMs = props.lifetime

const hasLifetime = computed(() => props.lifetime > 0)
const notifyStyle = computed(
  () =>
    ({
      '--notify-image': `url(${notifyImage})`,
      '--notify-duration': `${props.lifetime}ms`,
    }) as CSSProperties,
)

const clearAutoClose = () => {
  if (!timer) {
    return
  }

  clearTimeout(timer)
  timer = undefined
}

const scheduleAutoClose = (duration = props.lifetime) => {
  clearAutoClose()

  if (!isVisible.value || !hasLifetime.value) {
    return
  }

  remainingMs = duration
  startedAt = Date.now()
  timer = setTimeout(() => {
    isVisible.value = false
    emit('expired')
    emit('close')
  }, remainingMs)
}

const closeNotify = () => {
  clearAutoClose()
  isVisible.value = false
  emit('close')
}

const pauseLifetime = () => {
  if (!timer || !hasLifetime.value) {
    return
  }

  isPaused.value = true
  remainingMs = Math.max(0, remainingMs - (Date.now() - startedAt))
  clearAutoClose()
}

const resumeLifetime = () => {
  if (!isVisible.value || !hasLifetime.value) {
    return
  }

  isPaused.value = false
  scheduleAutoClose(remainingMs)
}

watch(
  () => props.visible,
  (visible) => {
    isVisible.value = visible
    isPaused.value = false
    remainingMs = props.lifetime

    if (visible) {
      scheduleAutoClose()
    } else {
      clearAutoClose()
    }
  },
)

watch(
  () => props.lifetime,
  () => {
    remainingMs = props.lifetime
    scheduleAutoClose()
  },
)

onMounted(() => {
  scheduleAutoClose()
})

onBeforeUnmount(() => {
  clearAutoClose()
})
</script>

<template>
  <Transition name="notify" @after-leave="emit('closed')">
    <aside
      v-if="isVisible"
      class="notify-panel"
      :class="{
        'notify-panel--fixed': fixed,
        'notify-panel--paused': isPaused,
      }"
      :style="notifyStyle"
      role="status"
      aria-live="polite"
      @mouseenter="pauseLifetime"
      @mouseleave="resumeLifetime"
      @focusin="pauseLifetime"
      @focusout="resumeLifetime"
    >
      <button
        v-if="closable"
        class="notify-panel__close"
        type="button"
        aria-label="Đóng thông báo"
        @click="closeNotify"
      >
        ×
      </button>

      <div class="notify-panel__content">
        <div v-if="title" class="notify-panel__title">
          {{ title }}
        </div>
        <div v-if="message" class="notify-panel__message">
          {{ message }}
        </div>
        <slot />
      </div>

      <span v-if="hasLifetime" class="notify-panel__lifetime" aria-hidden="true"></span>
    </aside>
  </Transition>
</template>

<style scoped>
.notify-panel {
  position: relative;
  width: min(calc(100vw - 32px), clamp(320px, 42vw, 560px));
  aspect-ratio: 1654 / 434;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: clamp(28px, 4vw, 52px) clamp(34px, 5vw, 64px);
  background-image: var(--notify-image);
  background-position: center;
  background-repeat: no-repeat;
  background-size: 100% 100%;
  filter: drop-shadow(0 12px 24px rgba(0, 0, 0, 0.36));
  pointer-events: auto;
}

.notify-panel--fixed {
  position: fixed;
  top: max(16px, env(safe-area-inset-top));
  right: max(16px, env(safe-area-inset-right));
  z-index: 50;
}

.notify-panel__lifetime {
  position: absolute;
  right: 8.5%;
  bottom: 12.5%;
  left: 8.5%;
  height: 3px;
  overflow: hidden;
  border-radius: 999px;
  background: rgba(91, 54, 23, 0.18);
}

.notify-panel__lifetime::before {
  content: '';
  display: block;
  width: 100%;
  height: 100%;
  border-radius: inherit;
  background: linear-gradient(90deg, #9c5c18, #ffd672);
  transform-origin: left center;
  animation: notify-lifetime var(--notify-duration) linear forwards;
}

.notify-panel--paused .notify-panel__lifetime::before {
  animation-play-state: paused;
}

.notify-panel__content {
  width: 100%;
  min-width: 0;
  display: grid;
  gap: 4px;
  color: #3e2a17;
  text-align: center;
}

.notify-panel__title {
  color: #8c4c15;
  font-family: var(--font-cloud-shui-zhu);
  font-size: clamp(18px, 2vw, 28px);
  line-height: 1;
  text-shadow: 0 1px 0 rgba(255, 246, 205, 0.8);
}

.notify-panel__message {
  overflow: hidden;
  display: -webkit-box;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
  color: rgba(61, 43, 28, 0.86);
  font-family: var(--font-cloud-libian), serif;
  font-size: clamp(14px, 1.3vw, 18px);
  line-height: 1.25;
}

.notify-panel__close {
  position: absolute;
  top: 15%;
  left: 5.2%;
  width: clamp(22px, 3vw, 34px);
  aspect-ratio: 1;
  display: grid;
  place-items: center;
  border: 0;
  border-radius: 50%;
  background: transparent;
  color: #ffe0a0;
  cursor: pointer;
  font-family: Arial, Helvetica, sans-serif;
  font-size: clamp(16px, 2vw, 24px);
  line-height: 1;
  text-shadow: 0 1px 2px rgba(59, 13, 9, 0.75);
}

.notify-panel__close:focus-visible {
  outline: 2px solid #fff0a6;
  outline-offset: 2px;
}

.notify-enter-active,
.notify-leave-active {
  transition:
    opacity 240ms ease,
    transform 240ms ease;
}

.notify-enter-from,
.notify-leave-to {
  opacity: 0;
  transform: translateX(calc(100% + 24px));
}

.notify-enter-to,
.notify-leave-from {
  opacity: 1;
  transform: translateX(0);
}

@keyframes notify-lifetime {
  from {
    transform: scaleX(1);
  }

  to {
    transform: scaleX(0);
  }
}

@media (max-width: 640px) {
  .notify-panel--fixed {
    top: max(12px, env(safe-area-inset-top));
    right: 12px;
  }

  .notify-panel {
    width: min(calc(100vw - 24px), 420px);
    padding: 24px 30px;
  }
}
</style>
