<template>
    <section class="giftcode-tab">
        <form class="giftcode-card" @submit.prevent="claimGiftCode">
            <div class="giftcode-copy">
                <p class="giftcode-kicker">Quà tặng tu luyện</p>
                <h2 class="giftcode-title">Giftcode</h2>
            </div>

            <label class="giftcode-field">
                <span class="giftcode-label">Nhập mã</span>
                <span class="giftcode-input-shell" :class="{ 'giftcode-input-shell--error': errorMessage }">
                    <input
                        v-model="code"
                        class="giftcode-input"
                        type="text"
                        name="giftcode"
                        autocomplete="one-time-code"
                        autocapitalize="characters"
                        spellcheck="false"
                        placeholder="VD: MTS2026"
                        :disabled="isClaiming"
                    />
                </span>
            </label>

            <p
                v-if="statusMessage"
                class="giftcode-message"
                :class="{ 'giftcode-message--error': errorMessage }"
            >
                {{ statusMessage }}
            </p>

            <button class="giftcode-button" type="submit" :disabled="isClaiming">
                {{ isClaiming ? 'Đang nhận' : 'Nhận quà' }}
            </button>
        </form>

        <NotifyStack ref="notifyStack" />
    </section>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'

import api from '../../api/axios'
import NotifyStack from '../../components/NotifyStack.vue'

interface ApiResponse<T> {
    success: boolean
    data?: T
    message?: string
}

interface GiftCodeClaimData {
    message?: string
    Message?: string
}

const code = ref('')
const isClaiming = ref(false)
const successMessage = ref('')
const errorMessage = ref('')
const notifyStack = ref<InstanceType<typeof NotifyStack> | null>(null)

const statusMessage = computed(() => errorMessage.value || successMessage.value)

const pushNotify = (title: string, message: string) => {
    notifyStack.value?.push({
        title,
        message,
    })
}

const getErrorMessage = (error: unknown, fallback: string) => {
    if (error instanceof Error && error.message) {
        return error.message
    }

    return fallback
}

const claimGiftCode = async () => {
    const trimmedCode = code.value.trim()
    successMessage.value = ''
    errorMessage.value = ''

    if (!trimmedCode) {
        errorMessage.value = 'Vui lòng nhập giftcode.'
        pushNotify('Giftcode', errorMessage.value)
        return
    }

    isClaiming.value = true

    try {
        const res = (await api.post<ApiResponse<GiftCodeClaimData>>('/giftcode/claim', {
            code: trimmedCode,
        })).data

        if (!res.success) {
            throw new Error(res.message || 'Không nhận được giftcode.')
        }

        successMessage.value = res.data?.message || res.data?.Message || 'Nhận giftcode thành công!'
        code.value = ''
        pushNotify('Giftcode', successMessage.value)
    } catch (error) {
        errorMessage.value = getErrorMessage(error, 'Không nhận được giftcode.')
        pushNotify('Giftcode', errorMessage.value)
    } finally {
        isClaiming.value = false
    }
}
</script>

<style scoped>
.giftcode-tab {
    width: 100%;
    height: 100%;
    min-height: 0;
    display: flex;
    align-items: center;
    justify-content: safe center;
    padding:
        clamp(12px, 3vw, 34px)
        clamp(12px, 3vw, 34px)
        calc(clamp(12px, 3vw, 34px) + env(safe-area-inset-bottom));
    container-type: size;
    overflow-x: hidden;
    overflow-y: auto;
    scroll-padding-bottom: calc(24px + env(safe-area-inset-bottom));
    scrollbar-width: thin;
    scrollbar-color: rgba(140, 91, 37, 0.46) transparent;
}

.giftcode-tab::-webkit-scrollbar {
    width: 6px;
}

.giftcode-tab::-webkit-scrollbar-track {
    background: transparent;
}

.giftcode-tab::-webkit-scrollbar-thumb {
    border-radius: 999px;
    background: rgba(140, 91, 37, 0.46);
}

.giftcode-card {
    width: min(100%, 520px);
    max-height: 100%;
    display: grid;
    justify-items: center;
    gap: clamp(10px, 2cqh, 18px);
    padding: clamp(14px, 3.2cqw, 30px);
    overflow-y: auto;
    overscroll-behavior: contain;
    border: 1px solid rgba(148, 94, 37, 0.28);
    border-radius: 8px;
    background:
        linear-gradient(180deg, rgba(255, 250, 225, 0.74), rgba(223, 238, 219, 0.5)),
        rgba(255, 246, 216, 0.42);
    box-shadow:
        inset 0 1px 0 rgba(255, 255, 255, 0.42),
        inset 0 -10px 18px rgba(113, 77, 33, 0.08),
        0 8px 18px rgba(83, 50, 18, 0.16);
    text-align: center;
}

.giftcode-copy {
    display: grid;
    gap: clamp(2px, 0.45cqh, 5px);
}

.giftcode-kicker {
    color: #8b5925;
    font-family: var(--font-alegreya);
    font-size: clamp(11px, 1.55cqw, 17px);
    font-weight: 700;
    line-height: 1;
}

.giftcode-title {
    color: #704116;
    font-family: var(--font-cloud-shui-zhu);
    font-size: clamp(28px, 5.2cqw, 56px);
    font-weight: 400;
    line-height: 1;
    letter-spacing: 0;
    text-shadow:
        0 1px 0 rgba(255, 250, 218, 0.8),
        0 3px 6px rgba(95, 52, 17, 0.22);
}

.giftcode-field {
    width: min(100%, 380px);
    display: grid;
    gap: clamp(5px, 0.9cqh, 8px);
    text-align: left;
}

.giftcode-label {
    padding-inline: clamp(8px, 1.2cqw, 12px);
    color: #684119;
    font-family: var(--font-cloud-shui-zhu);
    font-size: clamp(14px, 2cqw, 20px);
    line-height: 1;
    text-shadow: 0 1px 0 rgba(255, 249, 218, 0.72);
}

.giftcode-input-shell {
    min-height: clamp(44px, 6.4cqh, 56px);
    display: flex;
    align-items: center;
    padding: 5px clamp(12px, 1.8cqw, 18px);
    border: 1px solid rgba(164, 106, 42, 0.34);
    border-radius: 7px;
    background:
        linear-gradient(180deg, rgba(255, 253, 237, 0.94), rgba(239, 218, 176, 0.78)),
        rgba(255, 248, 224, 0.86);
    box-shadow:
        inset 0 1px 0 rgba(255, 255, 255, 0.58),
        inset 0 -6px 11px rgba(118, 73, 29, 0.12),
        0 3px 8px rgba(91, 53, 18, 0.12);
    transition:
        border-color 150ms ease,
        box-shadow 150ms ease,
        filter 150ms ease;
}

.giftcode-input-shell:focus-within {
    border-color: #fff0a6;
    box-shadow:
        0 0 0 2px rgba(120, 68, 23, 0.18),
        0 0 16px rgba(255, 221, 118, 0.28),
        inset 0 1px 0 rgba(255, 255, 255, 0.68);
}

.giftcode-input-shell--error {
    border-color: rgba(183, 65, 44, 0.72);
}

.giftcode-input {
    width: 100%;
    min-width: 0;
    border: 0;
    outline: 0;
    appearance: none;
    -webkit-appearance: none;
    background: transparent;
    color: #3f2811;
    font-family: var(--font-alegreya);
    font-size: clamp(18px, 2.4cqw, 26px);
    font-weight: 800;
    line-height: 1.2;
    letter-spacing: 0.04em;
    text-align: center;
}

.giftcode-input::placeholder {
    color: rgba(75, 51, 28, 0.48);
    font-weight: 600;
}

.giftcode-input:disabled {
    cursor: wait;
}

.giftcode-message {
    min-height: 1.2em;
    max-width: min(100%, 420px);
    overflow-wrap: anywhere;
    color: #2f7062;
    font-family: var(--font-alegreya);
    font-size: clamp(12px, 1.55cqw, 16px);
    font-weight: 700;
    line-height: 1.25;
}

.giftcode-message--error {
    color: #9b321f;
}

.giftcode-button {
    min-width: clamp(128px, 18cqw, 180px);
    padding: clamp(8px, 1.2cqw, 12px) clamp(18px, 2.4cqw, 26px);
    border: 1px solid rgba(104, 61, 20, 0.28);
    border-radius: 7px;
    background:
        linear-gradient(180deg, #fff2b7, #d28b31),
        #d28b31;
    box-shadow:
        inset 0 1px 0 rgba(255, 255, 255, 0.42),
        0 5px 10px rgba(80, 42, 14, 0.22);
    color: #5f2e0a;
    cursor: pointer;
    font-family: var(--font-cloud-shui-zhu);
    font-size: clamp(15px, 2.1cqw, 22px);
    line-height: 1;
    white-space: nowrap;
    text-shadow: 0 1px 0 rgba(255, 246, 195, 0.52);
    -webkit-tap-highlight-color: transparent;
    touch-action: manipulation;
    transition:
        filter 150ms ease,
        transform 150ms ease,
        opacity 150ms ease;
}

.giftcode-button:hover:not(:disabled) {
    filter:
        drop-shadow(0 5px 8px rgba(82, 44, 15, 0.24))
        drop-shadow(0 0 7px rgba(255, 217, 105, 0.22));
    transform: translateY(-1px);
}

.giftcode-button:active:not(:disabled) {
    transform: translateY(0) scale(0.98);
}

.giftcode-button:focus-visible {
    outline: 2px solid #fff0a6;
    outline-offset: 3px;
}

.giftcode-button:disabled {
    cursor: wait;
    opacity: 0.68;
    filter: grayscale(0.18);
}

@media (max-width: 720px) {
    .giftcode-tab {
        align-items: flex-start;
        padding:
            clamp(8px, 2.8vw, 16px)
            clamp(8px, 2.8vw, 16px)
            calc(clamp(18px, 5vw, 28px) + env(safe-area-inset-bottom));
    }

    .giftcode-card {
        width: min(100%, 360px);
        max-height: none;
        gap: clamp(8px, 1.8cqh, 14px);
        padding: clamp(12px, 4cqw, 18px);
    }

    .giftcode-kicker {
        font-size: clamp(9px, 2.4cqw, 12px);
    }

    .giftcode-title {
        font-size: clamp(22px, 7cqw, 32px);
    }

    .giftcode-label {
        font-size: clamp(12px, 3.4cqw, 15px);
    }

    .giftcode-input-shell {
        min-height: clamp(38px, 10cqw, 46px);
    }

    .giftcode-input {
        font-size: clamp(16px, 4.5cqw, 19px);
    }

    .giftcode-message {
        font-size: clamp(11px, 3cqw, 13px);
    }

    .giftcode-button {
        min-width: clamp(110px, 36cqw, 140px);
        padding: 8px 16px;
        font-size: clamp(13px, 4cqw, 16px);
    }
}

@media (hover: none) {
    .giftcode-button:hover:not(:disabled) {
        filter: none;
        transform: none;
    }
}
</style>
