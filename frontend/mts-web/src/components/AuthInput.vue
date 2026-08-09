<script setup lang="ts">
import { computed, useId } from 'vue'

defineOptions({
  inheritAttrs: false,
})

const model = defineModel<string | number>({
  default: '',
})

const props = withDefaults(
  defineProps<{
    id?: string
    label?: string
    type?: 'text' | 'email' | 'password' | 'number' | 'tel'
    name?: string
    placeholder?: string
    autocomplete?: string
    disabled?: boolean
    readonly?: boolean
    required?: boolean
    error?: string
    hint?: string
  }>(),
  {
    type: 'text',
    disabled: false,
    readonly: false,
    required: false,
  },
)

const autoId = useId()
const inputId = computed(() => props.id || props.name || autoId)
const message = computed(() => props.error || props.hint)
const messageId = computed(() => (message.value ? `${inputId.value}-message` : undefined))
</script>

<template>
  <label class="auth-input" :for="inputId">
    <span v-if="label" class="auth-input__label">
      {{ label }}
    </span>

    <span
      class="auth-input__shell"
      :class="{
        'auth-input__shell--error': error,
        'auth-input__shell--disabled': disabled,
      }"
    >
      <span v-if="$slots.prefix" class="auth-input__prefix">
        <slot name="prefix" />
      </span>

      <input
        :id="inputId"
        v-model="model"
        class="auth-input__control"
        :class="{ 'auth-input__control--password': type === 'password' }"
        :type="type"
        :name="name"
        :placeholder="placeholder"
        :autocomplete="autocomplete"
        :disabled="disabled"
        :readonly="readonly"
        :required="required"
        :aria-invalid="Boolean(error)"
        :aria-describedby="messageId"
        v-bind="$attrs"
      />

      <span v-if="$slots.suffix" class="auth-input__suffix">
        <slot name="suffix" />
      </span>
    </span>

    <span
      v-if="message"
      :id="messageId"
      class="auth-input__message"
      :class="{ 'auth-input__message--error': error }"
    >
      {{ message }}
    </span>
  </label>
</template>

<style scoped>
.auth-input {
  width: min(100%, 360px);
  display: grid;
  gap: 6px;
  color: #f9e9b8;
  font-family: var(--font-alegreya);
}

.auth-input__label {
  padding-inline: 10px;
  color: #424242;
  font-size: clamp(19px, 1.3vw, 26px);
  line-height: 1;
}

.auth-input__shell {
  min-height: clamp(42px, 5vw, 52px);
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 5px 14px;
  border: 1px solid rgba(226, 190, 108, 0.9);
  border-radius: 6px;
  background:
    linear-gradient(180deg, rgba(243, 238, 216, 0.88), rgba(232, 204, 169, 0.88)),
    rgba(152, 152, 148, 0.92);
  box-shadow:
    0 3px 10px rgba(149, 145, 145, 0.2),
    inset 0 0 0 1px rgba(255, 255, 255, 0.4),
    inset 0 -8px 14px rgba(102, 64, 28, 0.12);
  transition:
    border-color 160ms ease,
    box-shadow 160ms ease,
    filter 160ms ease;
}

.auth-input__shell:focus-within {
  border-color: #fff0a6;
  box-shadow:
    0 0 0 2px rgba(106, 45, 29, 0.28),
    0 0 18px rgba(255, 228, 130, 0.42),
    inset 0 0 0 1px rgba(255, 255, 255, 0.55);
}

.auth-input__shell--error {
  border-color: #c94b4b;
  box-shadow:
    0 0 0 2px rgba(201, 75, 75, 0.18),
    inset 0 0 0 1px rgba(255, 255, 255, 0.32);
}

.auth-input__shell--disabled {
  filter: grayscale(0.5);
  opacity: 0.65;
}

.auth-input__control {
  width: 100%;
  min-width: 0;
  border: 0;
  outline: 0;
  appearance: none;
  -webkit-appearance: none;
  background: transparent;
  color: #382514;
  font-size: clamp(16px, 1.5vw, 22px);
  line-height: 1.2;
  letter-spacing: 0;
}

.auth-input__control--password {
  letter-spacing: 0.08em;
  -webkit-text-security: disc;
}

.auth-input__control::placeholder {
  color: rgba(74, 52, 34, 0.58);
}

.auth-input__control:disabled {
  cursor: not-allowed;
}

.auth-input__prefix,
.auth-input__suffix {
  flex: 0 0 auto;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #6b4720;
}

.auth-input__message {
  min-height: 1em;
  padding-inline: 10px;
  color: rgba(72, 72, 72, 0.82);
  font-size: clamp(12px, 1vw, 15px);
  line-height: 1.2;
}

.auth-input__message--error {
  color: #f3421e;
  font-size: clamp(14px, 1.2vw, 18px);
}

</style>
