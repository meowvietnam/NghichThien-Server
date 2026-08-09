<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import Notify from './Notify.vue'

export interface NotifyStackItem {
  id?: string | number
  title?: string
  message?: string
  lifetime?: number
  closable?: boolean
}

interface InternalNotify extends NotifyStackItem {
  id: string | number
}

const props = withDefaults(
  defineProps<{
    items?: NotifyStackItem[]
    limit?: number
    newestOnTop?: boolean
    lifetime?: number
    closable?: boolean
  }>(),
  {
    items: () => [],
    limit: 5,
    newestOnTop: true,
    lifetime: 3600,
    closable: true,
  },
)

const emit = defineEmits<{
  close: [item: InternalNotify]
  expired: [item: InternalNotify]
}>()

const notifications = ref<InternalNotify[]>([])
let nextId = 0

const createId = () => `notify-${Date.now()}-${nextId++}`

const normalizeItem = (item: NotifyStackItem): InternalNotify => ({
  ...item,
  id: item.id ?? createId(),
})

const trimToLimit = () => {
  if (notifications.value.length <= props.limit) {
    return
  }

  notifications.value = props.newestOnTop
    ? notifications.value.slice(0, props.limit)
    : notifications.value.slice(-props.limit)
}

const push = (item: NotifyStackItem) => {
  const notification = normalizeItem(item)

  notifications.value = props.newestOnTop
    ? [notification, ...notifications.value]
    : [...notifications.value, notification]

  trimToLimit()
  return notification.id
}

const remove = (id: string | number) => {
  notifications.value = notifications.value.filter((item) => item.id !== id)
}

const clear = () => {
  notifications.value = []
}

const getLifetime = (item: InternalNotify) => item.lifetime ?? props.lifetime
const isClosable = (item: InternalNotify) => item.closable ?? props.closable

const orderedNotifications = computed(() => notifications.value)

watch(
  () => props.items,
  (items) => {
    notifications.value = items.map(normalizeItem)
    trimToLimit()
  },
  { immediate: true },
)

defineExpose({
  push,
  remove,
  clear,
})
</script>

<template>
  <section class="notify-stack" aria-live="polite" aria-label="Thông báo">
    <Notify
      v-for="item in orderedNotifications"
      :key="item.id"
      :fixed="false"
      :title="item.title"
      :message="item.message"
      :closable="isClosable(item)"
      :lifetime="getLifetime(item)"
      @close="emit('close', item)"
      @expired="emit('expired', item)"
      @closed="remove(item.id)"
    />
  </section>
</template>

<style scoped>
.notify-stack {
  position: fixed;
  top: max(16px, env(safe-area-inset-top));
  right: max(16px, env(safe-area-inset-right));
  z-index: 50;
  width: min(calc(100vw - 32px), clamp(320px, 42vw, 560px));
  display: grid;
  gap: clamp(6px, 1vw, 10px);
  pointer-events: none;
}

.notify-stack :deep(.notify-panel) {
  width: 100%;
  pointer-events: auto;
}

@media (max-width: 640px) {
  .notify-stack {
    top: max(12px, env(safe-area-inset-top));
    right: 12px;
    width: min(calc(100vw - 24px), 420px);
    gap: 6px;
  }
}
</style>
