<script setup lang="ts">
import loadingEmblem from '../assets/images/loading-emblem.png'

withDefaults(
  defineProps<{
    visible?: boolean
    message?: string
  }>(),
  {
    visible: false,
    message: 'Đang đăng nhập...',
  },
)
</script>

<template>
  <Transition name="loading-overlay">
    <div
      v-if="visible"
      class="loading-overlay"
      role="status"
      aria-live="polite"
      aria-busy="true"
    >
      <div class="loading-overlay__seal">
        <span class="loading-overlay__orbit loading-overlay__orbit--outer"></span>
        <span class="loading-overlay__orbit loading-overlay__orbit--inner"></span>
        <img class="loading-overlay__emblem" :src="loadingEmblem" alt="" />
      </div>
      <div class="loading-overlay__text">
        {{ message }}
      </div>
    </div>
  </Transition>
</template>

<style scoped>
.loading-overlay {
  position: fixed;
  inset: 0;
  z-index: 90;
  display: grid;
  place-items: center;
  align-content: center;
  gap: clamp(12px, 2vw, 20px);
  padding: 24px;
  background:
    radial-gradient(circle at center, rgba(30, 49, 45, 0.66), rgba(7, 8, 12, 0.86) 58%),
    rgba(4, 5, 8, 0.76);
  backdrop-filter: blur(5px);
}

.loading-overlay__seal {
  position: relative;
  width: clamp(116px, 18vw, 196px);
  aspect-ratio: 1;
  display: grid;
  place-items: center;
}

.loading-overlay__emblem {
  width: 100%;
  height: 100%;
  object-fit: contain;
  border-radius: 50%;
  filter:
    drop-shadow(0 0 18px rgba(255, 205, 80, 0.34))
    drop-shadow(0 10px 20px rgba(0, 0, 0, 0.42));
  animation: loading-pulse 1700ms ease-in-out infinite;
}

.loading-overlay__orbit {
  position: absolute;
  inset: -8%;
  border-radius: 50%;
  border: 1px solid rgba(255, 223, 125, 0.58);
  border-inline-color: rgba(52, 210, 183, 0.2);
  box-shadow:
    0 0 16px rgba(255, 213, 105, 0.22),
    inset 0 0 14px rgba(70, 222, 195, 0.14);
  animation: loading-spin 1800ms linear infinite;
}

.loading-overlay__orbit--inner {
  inset: 12%;
  border-color: rgba(80, 220, 194, 0.42);
  border-block-color: rgba(255, 219, 112, 0.66);
  animation-duration: 1200ms;
  animation-direction: reverse;
}

.loading-overlay__text {
  color: #ffeaa7;
  font-family: var(--font-cloud-shui-zhu);
  font-size: clamp(18px, 2.2vw, 30px);
  line-height: 1;
  text-align: center;
  text-shadow:
    0 2px 4px rgba(47, 19, 5, 0.76),
    0 0 14px rgba(255, 218, 95, 0.28);
}

.loading-overlay-enter-active,
.loading-overlay-leave-active {
  transition: opacity 180ms ease;
}

.loading-overlay-enter-from,
.loading-overlay-leave-to {
  opacity: 0;
}

@keyframes loading-spin {
  to {
    transform: rotate(360deg);
  }
}

@keyframes loading-pulse {
  0%,
  100% {
    transform: scale(0.98);
  }

  50% {
    transform: scale(1.03);
  }
}
</style>
