<template>
    <section class="payment-tab" :style="paymentStyle">
        <div class="payment-header">
            <p class="payment-kicker">Cửa hàng</p>
            <h2 class="payment-title">Nạp tiên ngọc</h2>
        </div>

        <div
            v-if="paymentResultMessage"
            class="payment-result-message"
            :class="`payment-result-message--${paymentResultType}`"
            role="status"
        >
            {{ paymentResultMessage }}
        </div>

        <div v-if="latestOrder" class="payment-order-info" role="status">
            <div class="payment-order-main">
                <span class="payment-order-status">
                    {{ formatOrderStatus(latestOrder.status) }}
                </span>
                <strong class="payment-order-code">
                    #{{ latestOrder.orderId }}
                </strong>
            </div>

            <div class="payment-order-details">
                <span>{{ latestOrder.productName }}</span>
                <span>{{ formatPrice(latestOrder.amount) }}</span>
                <span>{{ formatOrderDate(latestOrder.date) }}</span>
            </div>

            <button class="payment-order-link" type="button" @click="openPaymentLink(latestOrder.link)">
                Thanh toán
            </button>
        </div>

        <div v-if="!isLoading && !errorMessage && sortedPacks.length" class="pack-entry-panel">
            <button class="pack-open-button" type="button" @click="openPackPanel">
                Xem gói nạp
            </button>
        </div>

        <div v-if="activePromotions.length" class="promotion-strip" aria-label="Khuyến mãi">
            <article
                v-for="(promotion, index) in activePromotions"
                :key="getPromotionKey(promotion, index)"
                class="promotion-card"
            >
                <span class="promotion-label">Khuyến mãi</span>
                <div class="promotion-copy">
                    <strong class="promotion-name">{{ getPromotionName(promotion) }}</strong>
                    <span class="promotion-description">{{ getPromotionDescription(promotion) }}</span>
                </div>
                <span class="promotion-time">{{ getPromotionTimeText(promotion) }}</span>
            </article>
        </div>

        <div v-if="isLoading" class="payment-state">
            Đang tải gói nạp...
        </div>

        <div v-else-if="errorMessage" class="payment-state payment-state--error">
            <p>{{ errorMessage }}</p>
            <button class="payment-retry-button" type="button" @click="loadPacks">
                Thử lại
            </button>
        </div>

        <div v-else-if="!sortedPacks.length" class="payment-state">
            Chưa có gói nạp khả dụng.
        </div>

        <div
            v-if="isPackPanelOpen"
            class="pack-panel-backdrop"
            role="dialog"
            aria-modal="true"
            aria-labelledby="pack-panel-title"
            @click.self="closePackPanel"
        >
            <section class="pack-panel">
                <div class="pack-panel-header">
                    <div class="pack-panel-title-group">
                        <span class="pack-panel-kicker">Cửa hàng</span>
                        <h3 id="pack-panel-title" class="pack-panel-title">Gói nạp</h3>
                    </div>

                    <button class="pack-panel-close-button" type="button" @click="closePackPanel">
                        Đóng
                    </button>
                </div>

                <div class="pack-grid pack-panel-grid">
                    <article
                        v-for="product in sortedPacks"
                        :key="product.id"
                        class="pack-card"
                        :class="{ 'pack-card--featured': product.storeProductId === 'start_pack' }"
                    >
                        <div class="pack-art">
                            <span v-if="product.storeProductId === 'start_pack'" class="pack-badge">
                                Khởi đầu
                            </span>
                            <img :src="getPackImage(product)" :alt="product.name" class="pack-image" />
                        </div>

                        <div class="pack-info">
                            <h3 class="pack-name">{{ product.name }}</h3>

                            <div
                                class="pack-description"
                                :class="{ 'pack-description--expanded': isDescriptionExpanded(product.id) }"
                            >
                                <ul class="pack-rewards">
                                    <li
                                        v-for="reward in getVisibleRewardItems(product)"
                                        :key="`${product.id}-${reward}`"
                                        class="pack-reward"
                                    >
                                        {{ reward }}
                                    </li>
                                </ul>

                                <button
                                    v-if="hasHiddenRewards(product)"
                                    class="pack-description-button"
                                    type="button"
                                    :aria-expanded="isDescriptionExpanded(product.id)"
                                    :aria-label="getDescriptionToggleAriaLabel(product)"
                                    @click="toggleDescription(product.id)"
                                >
                                    {{ getDescriptionToggleLabel(product) }}
                                </button>
                            </div>

                            <div class="pack-footer">
                                <div class="pack-price-box" aria-label="Giá gói nạp">
                                    <span class="pack-price-label">Giá</span>
                                    <strong class="pack-price">{{ formatPrice(product.price) }}</strong>
                                </div>
                                <button
                                    class="pack-buy-button"
                                    :class="{ 'pack-buy-button--loading': payingId === product.id }"
                                    type="button"
                                    :disabled="payingId !== null"
                                    :aria-busy="payingId === product.id"
                                    @click="createPaymentLink(product)"
                                >
                                    {{ payingId === product.id ? 'Đang tạo' : 'Mua' }}
                                </button>
                            </div>
                        </div>
                    </article>
                </div>
            </section>
        </div>

        <LoadingOverlay :visible="isCreatingPaymentLink" message="Đang tạo link thanh toán..." />
        <NotifyStack ref="notifyStack" />
    </section>
</template>

<script setup lang="ts">
import { computed, inject, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import type { CSSProperties } from 'vue'
import { useRoute } from 'vue-router'

import {api} from '../../api/axios'
import LoadingOverlay from '../../components/LoadingOverlay.vue'
import NotifyStack from '../../components/NotifyStack.vue'
import { FormatUtil } from '../../utils/utils'
import packBackgroundImage from '../../assets/packs/pack_background_responsive.png'
import startPackImage from '../../assets/packs/start_pack.png'
import tienNgoc1Image from '../../assets/packs/tien_ngoc_1.png'
import tienNgoc2Image from '../../assets/packs/tien_ngoc_2.png'
import tienNgoc3Image from '../../assets/packs/tien_ngoc_3.png'
import tienNgoc4Image from '../../assets/packs/tien_ngoc_4.png'
import tienNgoc5Image from '../../assets/packs/tien_ngoc_5.png'
import tienNgoc6Image from '../../assets/packs/tien_ngoc_6.png'

interface ApiResponse<T> {
    success: boolean
    data?: T
    message?: string
}

interface Product {
    id: number
    storeProductId: string
    price: number
    productType: string
    actualProductType: string
    store: string
    isActive: boolean
    name: string
    description: string,
    priority: number
}
interface Promotion {
    id?: number
    Id?: number
    name?: string
    Name?: string
    description?: string
    Description?: string
    store?: string
    Store?: string
    active?: boolean
    Active?: boolean
    startDate?: string
    StartDate?: string
    endDate?: string
    EndDate?: string
}
interface ProductData {
    products?: Product[]
    Products?: Product[]
    promotions?: Promotion[]
    Promotions?: Promotion[]
}
interface PaymentOrder {
    orderId: number | string
    productId: number
    charId: number
    amount: number
    purchaseId: number | null
    link: string
    status: string | number
    date: string
    expireAt?: string
}

interface PaymentOrderInfo extends PaymentOrder {
    productName: string
}


const packImages: Record<string, string> = {
    start_pack: startPackImage,
    tien_ngoc_1: tienNgoc1Image,
    tien_ngoc_2: tienNgoc2Image,
    tien_ngoc_3: tienNgoc3Image,
    tien_ngoc_4: tienNgoc4Image,
    tien_ngoc_5: tienNgoc5Image,
    tien_ngoc_6: tienNgoc6Image,
}

const paymentStyle = {
    '--pack-card-background': `url(${packBackgroundImage})`,
} as CSSProperties

const packs = ref<Product[]>([])
const promotions = ref<Promotion[]>([])
const isLoading = ref(true)
const errorMessage = ref('')
const payingId = ref<number | null>(null)
const latestOrder = ref<PaymentOrderInfo | null>(null)
const paymentResultMessage = ref('')
const paymentResultType = ref<'success' | 'error'>('success')
const isPackPanelOpen = ref(false)
const expandedDescriptions = ref<Record<number, boolean>>({})
const notifyStack = ref<InstanceType<typeof NotifyStack> | null>(null)
const setDownloadButtonVisible = inject<(isVisible: boolean) => void>('setDownloadButtonVisible')
const route = useRoute()
let paymentResultTimer: ReturnType<typeof setTimeout> | undefined

const sortedPacks = computed(() =>
    [...packs.value],
)

const activePromotions = computed(() => promotions.value.filter(isPromotionVisible))

const isCreatingPaymentLink = computed(() => payingId.value !== null)

const getPackImage = (product: Product) => packImages[product.storeProductId] ?? startPackImage

const getRewardItems = (product: Product) =>
    product.description
        .split(',')
        .map((reward) => reward.trim())
        .filter(Boolean)

const collapsedRewardCount = () => 1

const isDescriptionExpanded = (productId: number) => Boolean(expandedDescriptions.value[productId])

const getVisibleRewardItems = (product: Product) => {
    const rewards = getRewardItems(product)

    if (isDescriptionExpanded(product.id)) {
        return rewards
    }

    return rewards.slice(0, collapsedRewardCount())
}

const getHiddenRewardCount = (product: Product) =>
    Math.max(0, getRewardItems(product).length - collapsedRewardCount())

const hasHiddenRewards = (product: Product) => getHiddenRewardCount(product) > 0

const getDescriptionToggleLabel = (product: Product) =>
    isDescriptionExpanded(product.id) ? '-' : `+${getHiddenRewardCount(product)}`

const getDescriptionToggleAriaLabel = (product: Product) =>
    isDescriptionExpanded(product.id)
        ? `Thu gọn mô tả ${product.name}`
        : `Xem thêm mô tả ${product.name}`

const toggleDescription = (productId: number) => {
    expandedDescriptions.value = {
        ...expandedDescriptions.value,
        [productId]: !expandedDescriptions.value[productId],
    }
}

const openPackPanel = () => {
    isPackPanelOpen.value = true
    setDownloadButtonVisible?.(false)
}

const closePackPanel = () => {
    isPackPanelOpen.value = false
    setDownloadButtonVisible?.(true)
}

const handleKeydown = (event: KeyboardEvent) => {
    if (event.key === 'Escape' && isPackPanelOpen.value) {
        closePackPanel()
    }
}

const getResponseProducts = (data?: ProductData) => data?.products ?? data?.Products ?? []

const getResponsePromotions = (data?: ProductData) => data?.promotions ?? data?.Promotions ?? []

const getPromotionKey = (promotion: Promotion, index: number) =>
    `${promotion.id ?? promotion.Id ?? 'promotion'}-${index}`

const getPromotionName = (promotion: Promotion) => promotion.name ?? promotion.Name ?? 'Khuyến mãi'

const getPromotionDescription = (promotion: Promotion) =>
    promotion.description ?? promotion.Description ?? 'Ưu đãi đang diễn ra.'

const getPromotionStartDate = (promotion: Promotion) => promotion.startDate ?? promotion.StartDate

const getPromotionEndDate = (promotion: Promotion) => promotion.endDate ?? promotion.EndDate

const hasTimezoneSuffix = (date: string) => /(?:z|[+-]\d{2}:?\d{2})$/i.test(date)

const normalizeServerUtcDate = (date: string) => {
    const trimmedDate = date.trim()
    const normalizedFraction = trimmedDate.replace(/(\.\d{3})\d+/, '$1')
    const normalizedOffset = normalizedFraction.replace(/([+-]\d{2})(\d{2})$/, '$1:$2')

    return hasTimezoneSuffix(normalizedOffset) ? normalizedOffset : `${normalizedOffset}Z`
}

const parseServerUtcDate = (date?: string) => {
    if (!date) {
        return null
    }

    const value = new Date(normalizeServerUtcDate(date))

    return Number.isNaN(value.getTime()) ? null : value
}

const isPromotionVisible = (promotion: Promotion) => {
    const isActive = promotion.active ?? promotion.Active ?? true

    if (!isActive) {
        return false
    }

    const now = Date.now()
    const startDate = parseServerUtcDate(getPromotionStartDate(promotion))
    const endDate = parseServerUtcDate(getPromotionEndDate(promotion))

    if (startDate && now < startDate.getTime()) {
        return false
    }

    if (endDate && now > endDate.getTime()) {
        return false
    }

    return true
}

const formatPrice = (price: number) => `${FormatUtil.number(price)} đ`

const formatPromotionDate = (date?: string) => {
    const value = parseServerUtcDate(date)

    if (!value) {
        return ''
    }

    return new Intl.DateTimeFormat('vi-VN', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
    }).format(value)
}

const getPromotionTimeText = (promotion: Promotion) => {
    const endDate = formatPromotionDate(getPromotionEndDate(promotion))

    return endDate ? `Đến ${endDate}` : 'Đang mở'
}

const formatOrderStatus = (status: string | number) => {
    switch (String(status).toLowerCase()) {
        case '0':
        case 'processing':
            return 'Đang xử lý'
        case '1':
        case 'complete':
        case 'completed':
            return 'Hoàn tất'
        case '2':
        case 'cancelled':
        case 'canceled':
            return 'Đã hủy'
        case '3':
        case 'failed':
            return 'Thất bại'
        default:
            return String(status)
    }
}

const formatOrderDate = (date: string) => {
    const value = parseServerUtcDate(date)

    if (!value) {
        return date
    }

    return new Intl.DateTimeFormat('vi-VN', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
    }).format(value)
}

const getErrorMessage = (error: unknown, fallback: string) => {
    if (error instanceof Error && error.message) {
        return error.message
    }

    return fallback
}

const pushNotify = (title: string, message: string) => {
    notifyStack.value?.push({
        title,
        message,
    })
}

const clearPaymentResultTimer = () => {
    if (!paymentResultTimer) {
        return
    }

    clearTimeout(paymentResultTimer)
    paymentResultTimer = undefined
}

const normalizeQueryValue = (value: unknown) => {
    if (Array.isArray(value)) {
        return value[0]
    }

    return typeof value === 'string' ? value : undefined
}

const showPaymentResult = (success: boolean) => {
    clearPaymentResultTimer()
    paymentResultType.value = success ? 'success' : 'error'
    paymentResultMessage.value = success
        ? 'Thanh toán thành công, hãy kiểm tra hòm thư!'
        : 'Thanh toán thất bại'

    pushNotify('Thanh toán', paymentResultMessage.value)

    paymentResultTimer = setTimeout(() => {
        paymentResultMessage.value = ''
        paymentResultTimer = undefined
    }, 15000)
}

const openPaymentLink = (checkoutUrl: string) => {
    window.location.assign(checkoutUrl)
}

const loadPacks = async () => {
    isLoading.value = true
    errorMessage.value = ''
    closePackPanel()

    try {
        const res = (await api.get<ApiResponse<ProductData>>('/pay/pack')).data

        if (!res.success) {
            throw new Error(res.message || 'Không tải được danh sách gói nạp.')
        }
        const data = res.data
        packs.value = getResponseProducts(data).filter((product) => product.isActive)
        promotions.value = getResponsePromotions(data)
    } catch (error) {
        errorMessage.value = getErrorMessage(error, 'Không tải được danh sách gói nạp.')
        pushNotify('Nạp tiền', errorMessage.value)
    } finally {
        isLoading.value = false
    }
}

const createPaymentLink = async (product: Product) => {
    if (payingId.value !== null) {
        return
    }

    payingId.value = product.id
    let shouldResetPaying = true

    try {
        const res = (await api.post<ApiResponse<PaymentOrder>>('/pay/create-link', {
            productId: product.id,
        })).data

        const order = res.data

        if (!res.success || !order?.link) {
            throw new Error(res.message || 'Không tạo được link thanh toán.')
        }

        latestOrder.value = {
            ...order,
            productName: product.name,
        }

        pushNotify(
            'Đã tạo đơn thanh toán',
            `#${order.orderId} - ${formatPrice(order.amount)} - ${formatOrderStatus(order.status)}.`,
        )
        shouldResetPaying = false
        openPaymentLink(order.link)
    } catch (error) {
        pushNotify('Thanh toán', getErrorMessage(error, 'Không tạo được link thanh toán.'))
    } finally {
        if (shouldResetPaying) {
            payingId.value = null
        }
    }
}

onMounted(() => {
    void loadPacks()
    window.addEventListener('keydown', handleKeydown)
})

watch(
    () => route.query.success,
    (successQuery) => {
        const successValue = normalizeQueryValue(successQuery)

        if (successValue === undefined) {
            return
        }

        showPaymentResult(successValue.toLowerCase() === 'true' || successValue === '1')
    },
    { immediate: true },
)

onBeforeUnmount(() => {
    clearPaymentResultTimer()
    setDownloadButtonVisible?.(true)
    window.removeEventListener('keydown', handleKeydown)
})
</script>

<style scoped>
.payment-tab {
    position: relative;
    width: 100%;
    height: 100%;
    min-height: 0;
    display: flex;
    flex-direction: column;
    gap: clamp(8px, 1.4cqh, 14px);
    padding: clamp(8px, 1.8cqw, 18px) clamp(8px, 2.2cqw, 22px) clamp(12px, 2.4cqh, 24px);
    container-type: size;
    overflow-x: hidden;
    overflow-y: auto;
    scrollbar-width: thin;
    scrollbar-color: rgba(140, 91, 37, 0.46) transparent;
}

.payment-tab::-webkit-scrollbar {
    width: 6px;
}

.payment-tab::-webkit-scrollbar-track {
    background: transparent;
}

.payment-tab::-webkit-scrollbar-thumb {
    border-radius: 999px;
    background: rgba(140, 91, 37, 0.46);
}

.payment-header {
    display: grid;
    justify-items: center;
    gap: clamp(2px, 0.4cqh, 5px);
    text-align: center;
}

.payment-kicker {
    color: #8a5624;
    font-family: var(--font-cloud-shui-zhu);
    font-size: clamp(14px, 1.95cqw, 22px);
    font-weight: 400;
    line-height: 1;
}

.payment-title {
    color: #704116;
    font-family: var(--font-cloud-shui-zhu);
    font-size: clamp(22px, 4.4cqw, 46px);
    font-weight: 400;
    line-height: 1;
    letter-spacing: 0;
    -webkit-text-stroke: clamp(0.2px, 0.06cqw, 0.7px) rgba(255, 244, 205, 0.42);
    paint-order: stroke fill;
    text-shadow:
        0 1px 0 rgba(255, 249, 217, 0.74),
        0 3px 6px rgba(91, 51, 18, 0.24);
}

.payment-result-message {
    flex: 0 0 auto;
    width: min(100%, 620px);
    align-self: center;
    padding: clamp(8px, 1.25cqw, 12px) clamp(12px, 1.8cqw, 18px);
    border: 1px solid rgba(137, 96, 40, 0.28);
    border-radius: 8px;
    background:
        linear-gradient(180deg, rgba(255, 251, 228, 0.9), rgba(235, 224, 184, 0.72)),
        rgba(255, 248, 224, 0.72);
    box-shadow:
        inset 0 1px 0 rgba(255, 255, 255, 0.48),
        0 5px 12px rgba(85, 51, 18, 0.14);
    font-family: var(--font-cloud-shui-zhu);
    font-size: clamp(15px, 2.05cqw, 22px);
    line-height: 1.15;
    text-align: center;
    text-shadow: 0 1px 0 rgba(255, 250, 220, 0.72);
}

.payment-result-message--success {
    color: #2f7062;
}

.payment-result-message--error {
    color: #9b321f;
}

.payment-order-info {
    flex: 0 0 auto;
    width: min(100%, 760px);
    align-self: center;
    display: grid;
    grid-template-columns: minmax(0, auto) minmax(0, 1fr) auto;
    align-items: center;
    gap: clamp(7px, 1.2cqw, 14px);
    padding: clamp(7px, 1.1cqw, 10px) clamp(10px, 1.6cqw, 16px);
    border: 1px solid rgba(158, 106, 42, 0.34);
    border-radius: 8px;
    background:
        linear-gradient(90deg, rgba(255, 249, 226, 0.64), rgba(220, 238, 218, 0.48)),
        rgba(255, 248, 224, 0.52);
    box-shadow:
        inset 0 1px 0 rgba(255, 255, 255, 0.36),
        0 5px 12px rgba(85, 51, 18, 0.13);
}

.payment-order-main {
    min-width: 0;
    display: grid;
    gap: 3px;
}

.payment-order-status {
    color: #2f7062;
    font-family: var(--font-cloud-shui-zhu);
    font-size: clamp(11px, 1.55cqw, 16px);
    line-height: 1;
    white-space: nowrap;
    text-shadow: 0 1px 0 rgba(255, 250, 224, 0.7);
}

.payment-order-code {
    min-width: 0;
    overflow: hidden;
    color: #784317;
    font-family: var(--font-alegreya);
    font-size: clamp(12px, 1.45cqw, 15px);
    font-weight: 800;
    line-height: 1;
    white-space: nowrap;
    text-overflow: ellipsis;
}

.payment-order-details {
    min-width: 0;
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    justify-content: center;
    gap: clamp(5px, 0.9cqw, 10px);
    color: #6f4a24;
    font-family: var(--font-alegreya);
    font-size: clamp(13px, 1.75cqw, 18px);
    font-weight: 700;
    line-height: 1.1;
    text-align: center;
}

.payment-order-details span {
    min-width: 0;
    max-width: 100%;
    overflow: hidden;
    white-space: nowrap;
    text-overflow: ellipsis;
}

.payment-order-link {
    min-width: clamp(70px, 10cqw, 104px);
    padding: clamp(6px, 0.8cqw, 8px) clamp(10px, 1.3cqw, 14px);
    border: 1px solid rgba(104, 61, 20, 0.28);
    border-radius: 6px;
    background:
        linear-gradient(180deg, #fff2b7, #d28b31),
        #d28b31;
    box-shadow:
        inset 0 1px 0 rgba(255, 255, 255, 0.42),
        0 3px 7px rgba(80, 42, 14, 0.2);
    color: #5f2e0a;
    cursor: pointer;
    font-family: var(--font-cloud-shui-zhu);
    font-size: clamp(11px, 1.45cqw, 15px);
    line-height: 1;
    text-align: center;
    text-shadow: 0 1px 0 rgba(255, 246, 195, 0.48);
    -webkit-tap-highlight-color: transparent;
    touch-action: manipulation;
    transition:
        filter 150ms ease,
        transform 150ms ease;
}

.promotion-strip {
    flex: 0 0 auto;
    width: min(100%, 780px);
    align-self: center;
    display: flex;
    gap: clamp(8px, 1.3cqw, 14px);
    overflow-x: auto;
    overflow-y: hidden;
    padding: 2px 3px 6px;
    scrollbar-width: thin;
    scrollbar-color: rgba(140, 91, 37, 0.48) transparent;
}

.promotion-strip::-webkit-scrollbar {
    height: 5px;
}

.promotion-strip::-webkit-scrollbar-track {
    background: transparent;
}

.promotion-strip::-webkit-scrollbar-thumb {
    border-radius: 999px;
    background: rgba(140, 91, 37, 0.48);
}

.promotion-card {
    position: relative;
    flex: 0 0 min(100%, clamp(255px, 43cqw, 370px));
    min-width: 0;
    display: grid;
    grid-template-columns: auto minmax(0, 1fr);
    grid-template-rows: auto auto;
    align-items: center;
    gap: clamp(4px, 0.7cqw, 7px) clamp(8px, 1.2cqw, 12px);
    padding: clamp(8px, 1.2cqw, 12px) clamp(10px, 1.6cqw, 16px);
    overflow: hidden;
    border: 1px solid rgba(152, 96, 33, 0.32);
    border-radius: 8px;
    background:
        linear-gradient(135deg, rgba(255, 249, 218, 0.94), rgba(225, 241, 218, 0.84) 56%, rgba(249, 212, 146, 0.86)),
        rgba(255, 248, 224, 0.78);
    box-shadow:
        inset 0 1px 0 rgba(255, 255, 255, 0.52),
        0 5px 12px rgba(85, 51, 18, 0.13);
}

.promotion-card::before {
    content: '';
    position: absolute;
    inset: 0;
    pointer-events: none;
    background:
        linear-gradient(90deg, rgba(255, 255, 255, 0.36), transparent 46%),
        linear-gradient(180deg, rgba(255, 244, 190, 0.34), transparent 64%);
}

.promotion-label {
    position: relative;
    grid-row: 1 / span 2;
    padding: clamp(5px, 0.7cqw, 8px) clamp(9px, 1.15cqw, 13px);
    border: 1px solid rgba(112, 65, 22, 0.22);
    border-radius: 999px;
    background:
        linear-gradient(180deg, rgba(255, 239, 176, 0.95), rgba(205, 137, 55, 0.88)),
        #d28b31;
    color: #65340d;
    font-family: var(--font-cloud-shui-zhu);
    font-size: clamp(13px, 1.75cqw, 18px);
    line-height: 1;
    white-space: nowrap;
    text-shadow: 0 1px 0 rgba(255, 247, 203, 0.58);
}

.promotion-copy {
    position: relative;
    min-width: 0;
    display: grid;
    gap: 2px;
}

.promotion-name {
    min-width: 0;
    overflow: hidden;
    color: #704116;
    font-family: var(--font-cloud-shui-zhu);
    font-size: clamp(14px, 1.95cqw, 20px);
    font-weight: 400;
    line-height: 1.05;
    white-space: nowrap;
    text-overflow: ellipsis;
    text-shadow: 0 1px 0 rgba(255, 250, 220, 0.72);
}

.promotion-description {
    min-width: 0;
    overflow: hidden;
    color: #6f4a24;
    display: -webkit-box;
    font-family: var(--font-alegreya);
    font-size: clamp(11px, 1.45cqw, 14px);
    font-weight: 700;
    line-height: 1.12;
    -webkit-box-orient: vertical;
    -webkit-line-clamp: 2;
}

.promotion-time {
    position: relative;
    justify-self: start;
    min-width: 0;
    max-width: 100%;
    overflow: hidden;
    padding: clamp(2px, 0.35cqw, 4px) clamp(6px, 0.85cqw, 9px);
    border: 1px solid rgba(52, 105, 81, 0.18);
    border-radius: 999px;
    background: rgba(230, 247, 222, 0.66);
    color: #2f7062;
    font-family: var(--font-alegreya);
    font-size: clamp(12px, 1.6cqw, 16px);
    font-weight: 800;
    line-height: 1;
    white-space: nowrap;
    text-overflow: ellipsis;
}

.payment-state {
    align-self: center;
    justify-self: center;
    width: min(100%, 420px);
    display: grid;
    justify-items: center;
    gap: clamp(8px, 1.4cqh, 14px);
    padding: clamp(12px, 2.4cqw, 24px);
    border: 1px solid rgba(151, 104, 45, 0.28);
    border-radius: 8px;
    background: rgba(255, 248, 224, 0.56);
    color: #6b3d13;
    font-family: var(--font-cloud-shui-zhu);
    font-size: clamp(14px, 2.5cqw, 22px);
    line-height: 1.25;
    text-align: center;
}

.payment-state--error {
    color: #80311f;
}

.pack-entry-panel {
    flex: 0 0 auto;
    min-height: 0;
    display: grid;
    place-items: center;
    padding: clamp(5px, 0.9cqh, 9px) clamp(8px, 1.2cqw, 14px);
}

.payment-retry-button,
.pack-open-button,
.pack-panel-close-button,
.pack-buy-button {
    border: 1px solid rgba(104, 61, 20, 0.28);
    border-radius: 6px;
    background:
        linear-gradient(180deg, #fff2b7, #d28b31),
        #d28b31;
    box-shadow:
        inset 0 1px 0 rgba(255, 255, 255, 0.42),
        0 3px 7px rgba(80, 42, 14, 0.24);
    color: #5f2e0a;
    cursor: pointer;
    font-family: var(--font-cloud-shui-zhu);
    line-height: 1;
    text-align: center;
    text-shadow: 0 1px 0 rgba(255, 246, 195, 0.48);
    -webkit-tap-highlight-color: transparent;
    touch-action: manipulation;
    transition:
        filter 150ms ease,
        transform 150ms ease,
        opacity 150ms ease;
}

.payment-retry-button {
    padding: 8px 18px;
    font-size: clamp(13px, 2.1cqw, 18px);
}

.pack-open-button {
    min-width: clamp(150px, 21cqw, 230px);
    padding: clamp(10px, 1.35cqw, 14px) clamp(18px, 2.4cqw, 28px);
    font-size: clamp(18px, 2.7cqw, 30px);
    filter:
        drop-shadow(0 6px 11px rgba(82, 44, 15, 0.2))
        drop-shadow(0 0 8px rgba(255, 217, 105, 0.18));
}

.pack-panel-backdrop {
    position: fixed;
    inset: 0;
    z-index: 60;
    display: grid;
    place-items: center;
    padding:
        clamp(10px, 2.2vw, 26px)
        clamp(10px, 2.2vw, 26px)
        calc(clamp(10px, 2.2vw, 26px) + env(safe-area-inset-bottom));
    background: rgba(45, 29, 12, 0.42);
    backdrop-filter: blur(3px);
    -webkit-backdrop-filter: blur(3px);
}

.pack-panel {
    width: min(1180px, 96vw);
    height: min(840px, 92vh);
    min-height: 0;
    display: flex;
    flex-direction: column;
    overflow: hidden;
    container-type: inline-size;
    border: 1px solid rgba(135, 86, 32, 0.34);
    border-radius: 8px;
    background:
        linear-gradient(180deg, rgba(255, 251, 229, 0.94), rgba(237, 224, 184, 0.9)),
        rgba(255, 248, 224, 0.96);
    box-shadow:
        inset 0 1px 0 rgba(255, 255, 255, 0.58),
        0 18px 40px rgba(49, 30, 12, 0.32);
}

.pack-panel-header {
    flex: 0 0 auto;
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: clamp(10px, 1.8vw, 22px);
    padding: clamp(10px, 1.7vw, 18px) clamp(14px, 2.3vw, 26px);
    border-bottom: 1px solid rgba(144, 96, 42, 0.24);
    background:
        linear-gradient(90deg, rgba(255, 246, 207, 0.78), rgba(222, 238, 214, 0.52)),
        rgba(255, 248, 224, 0.62);
}

.pack-panel-title-group {
    min-width: 0;
    display: grid;
    gap: 2px;
}

.pack-panel-kicker {
    color: #8a5624;
    font-family: var(--font-cloud-shui-zhu);
    font-size: clamp(12px, 1.45vw, 18px);
    line-height: 1;
    white-space: nowrap;
}

.pack-panel-title {
    min-width: 0;
    overflow: hidden;
    color: #704116;
    font-family: var(--font-cloud-shui-zhu);
    font-size: clamp(22px, 3.2vw, 38px);
    font-weight: 400;
    line-height: 1;
    white-space: nowrap;
    text-overflow: ellipsis;
    text-shadow:
        0 1px 0 rgba(255, 249, 217, 0.74),
        0 3px 6px rgba(91, 51, 18, 0.2);
}

.pack-panel-close-button {
    flex: 0 0 auto;
    min-width: clamp(70px, 9vw, 104px);
    padding: clamp(7px, 0.9vw, 10px) clamp(12px, 1.4vw, 18px);
    font-size: clamp(13px, 1.55vw, 18px);
}

.pack-grid {
    flex: 1 1 auto;
    min-height: 0;
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(clamp(170px, 25cqw, 250px), 1fr));
    align-content: start;
    gap: clamp(24px, 3.4cqw, 40px);
    overflow: auto;
    padding: clamp(12px, 1.8cqw, 18px) clamp(18px, 2.8cqw, 28px) clamp(18px, 2.7cqh, 28px);
    scroll-padding-bottom: clamp(40px, 9cqh, 88px);
    scrollbar-width: thin;
    scrollbar-color: rgba(140, 91, 37, 0.56) transparent;
    overscroll-behavior: contain;
}

.pack-grid::-webkit-scrollbar {
    width: 6px;
}

.pack-grid::-webkit-scrollbar-track {
    background: transparent;
}

.pack-grid::-webkit-scrollbar-thumb {
    border-radius: 999px;
    background: rgba(140, 91, 37, 0.56);
}

.pack-panel-grid {
    flex: 1 1 auto;
    grid-template-columns: repeat(auto-fit, minmax(min(100%, clamp(220px, 27cqw, 285px)), 1fr));
    gap: clamp(26px, 3cqw, 44px);
    padding:
        clamp(20px, 2.5vw, 32px)
        clamp(22px, 3vw, 38px)
        calc(clamp(52px, 8vw, 92px) + env(safe-area-inset-bottom));
    scroll-padding-bottom: calc(clamp(52px, 8vw, 92px) + env(safe-area-inset-bottom));
}

.pack-card {
    position: relative;
    isolation: isolate;
    min-width: 0;
    aspect-ratio: 2 / 3;
    display: grid;
    grid-template-rows: 38% minmax(0, 1fr);
    padding: clamp(30px, 4cqw, 44px) clamp(22px, 3.1cqw, 34px) clamp(22px, 3.1cqw, 34px);
    overflow: visible;
    filter: drop-shadow(0 7px 11px rgba(72, 43, 18, 0.22));
    transition:
        filter 170ms ease,
        transform 170ms ease;
}

.pack-card::before {
    content: '';
    position: absolute;
    inset: 0;
    z-index: -1;
    background-image: var(--pack-card-background);
    background-position: center;
    background-repeat: no-repeat;
    background-size: 100% 100%;
}

.pack-card--featured {
    filter:
        drop-shadow(0 8px 13px rgba(72, 43, 18, 0.26))
        drop-shadow(0 0 10px rgba(236, 188, 85, 0.16));
}

.pack-card:hover {
    filter:
        drop-shadow(0 10px 16px rgba(72, 43, 18, 0.28))
        drop-shadow(0 0 9px rgba(236, 188, 85, 0.16));
    transform: translateY(-2px);
}

.pack-art {
    position: relative;
    min-width: 0;
    display: grid;
    place-items: center;
    padding-top: clamp(8px, 1.2cqw, 14px);
}

.pack-image {
    width: min(62%, 106px);
    aspect-ratio: 1;
    display: block;
    object-fit: contain;
    filter:
        drop-shadow(0 5px 7px rgba(70, 37, 12, 0.22))
        drop-shadow(0 0 10px rgba(255, 216, 106, 0.16));
}

.pack-card--featured .pack-image {
    width: min(54%, 94px);
}

.pack-badge {
    position: absolute;
    top: clamp(0px, 0.5cqw, 4px);
    left: 50%;
    max-width: 78%;
    padding: clamp(2px, 0.45cqw, 4px) clamp(7px, 0.9cqw, 10px);
    overflow: hidden;
    border: 1px solid rgba(255, 236, 166, 0.48);
    border-radius: 999px;
    background: rgba(105, 57, 21, 0.62);
    color: #fff2ba;
    font-family: var(--font-cloud-shui-zhu);
    font-size: clamp(9px, 1.35cqw, 13px);
    line-height: 1;
    white-space: nowrap;
    text-overflow: ellipsis;
    text-shadow: 0 1px 2px rgba(35, 13, 4, 0.72);
    transform: translateX(-50%);
}

.pack-info {
    min-height: 0;
    display: grid;
    grid-template-rows: auto minmax(38px, 1fr) auto;
    gap: clamp(6px, 0.9cqw, 10px);
    padding: clamp(2px, 0.4cqw, 5px) clamp(5px, 0.8cqw, 9px) 0;
}

.pack-name {
    min-width: 0;
    overflow: hidden;
    color: #714012;
    font-family: var(--font-cloud-shui-zhu);
    font-size: clamp(15px, 2.2cqw, 22px);
    font-weight: 400;
    line-height: 1.05;
    text-align: center;
    white-space: nowrap;
    text-overflow: ellipsis;
    text-shadow:
        0 1px 0 rgba(255, 250, 220, 0.8),
        0 2px 3px rgba(95, 52, 17, 0.18);
}

.pack-description {
    min-width: 0;
    min-height: 0;
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto;
    align-content: start;
    align-items: center;
    gap: clamp(5px, 0.75cqw, 8px);
    padding: clamp(4px, 0.65cqw, 7px) clamp(5px, 0.75cqw, 8px);
    border: 1px solid rgba(145, 101, 47, 0.2);
    border-radius: 7px;
    background:
        linear-gradient(180deg, rgba(255, 250, 231, 0.76), rgba(245, 225, 182, 0.58)),
        rgba(255, 248, 224, 0.54);
    box-shadow:
        inset 0 1px 0 rgba(255, 255, 255, 0.45),
        inset 0 -3px 5px rgba(127, 85, 39, 0.08);
    overflow: hidden;
}

.pack-description--expanded {
    align-items: start;
    overflow: auto;
    padding-right: clamp(5px, 0.75cqw, 8px);
    scrollbar-width: thin;
    scrollbar-color: rgba(140, 91, 37, 0.4) transparent;
}

.pack-description--expanded::-webkit-scrollbar {
    width: 4px;
}

.pack-description--expanded::-webkit-scrollbar-track {
    background: transparent;
}

.pack-description--expanded::-webkit-scrollbar-thumb {
    border-radius: 999px;
    background: rgba(140, 91, 37, 0.4);
}

.pack-rewards {
    min-height: 0;
    min-width: 0;
    display: block;
    overflow: hidden;
    list-style: none;
}

.pack-reward {
    max-width: 100%;
    overflow: hidden;
    color: #76522a;
    font-family: var(--font-alegreya);
    font-size: clamp(11px, 1.55cqw, 14px);
    font-weight: 700;
    line-height: 1.1;
    text-align: left;
    white-space: nowrap;
    text-overflow: ellipsis;
}

.pack-description--expanded .pack-rewards {
    display: grid;
    gap: clamp(3px, 0.5cqw, 5px);
}

.pack-description--expanded .pack-reward {
    position: relative;
    padding-left: clamp(9px, 1.15cqw, 12px);
    white-space: normal;
    overflow-wrap: anywhere;
    line-height: 1.16;
}

.pack-description--expanded .pack-reward::before {
    content: '';
    position: absolute;
    top: 0.45em;
    left: 1px;
    width: 4px;
    aspect-ratio: 1;
    border-radius: 50%;
    background: #a46c2c;
    box-shadow: 0 0 4px rgba(255, 215, 112, 0.34);
}

.pack-description-button {
    width: clamp(23px, 3.3cqw, 31px);
    aspect-ratio: 1;
    display: grid;
    place-items: center;
    overflow: hidden;
    border: 1px solid rgba(124, 86, 38, 0.28);
    border-radius: 50%;
    background:
        linear-gradient(180deg, rgba(255, 238, 174, 0.92), rgba(205, 137, 55, 0.88)),
        #d28b31;
    box-shadow:
        inset 0 1px 0 rgba(255, 255, 255, 0.45),
        0 2px 4px rgba(80, 42, 14, 0.18);
    color: #61300b;
    cursor: pointer;
    font-family: var(--font-alegreya);
    font-size: clamp(10px, 1.35cqw, 13px);
    font-weight: 800;
    line-height: 1;
    white-space: nowrap;
    text-overflow: ellipsis;
    -webkit-tap-highlight-color: transparent;
    touch-action: manipulation;
    transition:
        background-color 140ms ease,
        color 140ms ease,
        filter 140ms ease,
        transform 140ms ease;
}

.pack-description-button:hover {
    background:
        linear-gradient(180deg, rgba(255, 246, 197, 0.96), rgba(218, 151, 64, 0.94)),
        #d28b31;
    color: #5f2e0a;
    filter: drop-shadow(0 2px 4px rgba(82, 44, 15, 0.16));
    transform: translateY(-1px);
}

.pack-description-button:active {
    transform: translateY(0) scale(0.98);
}

.pack-description-button:focus-visible {
    outline: 2px solid #fff0a6;
    outline-offset: 2px;
}

.pack-footer {
    min-width: 0;
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto;
    align-items: center;
    gap: clamp(5px, 0.8cqw, 8px);
}

.pack-price-box {
    min-width: 0;
    display: grid;
    gap: clamp(1px, 0.22cqw, 3px);
    justify-items: start;
    padding: clamp(4px, 0.6cqw, 7px) clamp(7px, 0.9cqw, 10px);
    border: 1px solid rgba(141, 85, 28, 0.2);
    border-radius: 7px;
    background:
        linear-gradient(180deg, rgba(255, 250, 222, 0.9), rgba(246, 217, 157, 0.72)),
        rgba(255, 244, 204, 0.84);
    box-shadow:
        inset 0 1px 0 rgba(255, 255, 255, 0.54),
        0 2px 5px rgba(99, 58, 21, 0.12);
}

.pack-price-label {
    color: rgba(111, 66, 22, 0.78);
    font-family: var(--font-cloud-shui-zhu);
    font-size: clamp(9px, 1.18cqw, 12px);
    line-height: 1;
    white-space: nowrap;
    text-shadow: 0 1px 0 rgba(255, 250, 221, 0.74);
}

.pack-price {
    min-width: 0;
    overflow: hidden;
    color: #9f360f;
    font-family: var(--font-alegreya);
    font-size: clamp(16px, 2.15cqw, 22px);
    font-weight: 900;
    line-height: 1;
    white-space: nowrap;
    text-overflow: ellipsis;
    text-shadow:
        0 1px 0 rgba(255, 246, 206, 0.78),
        0 1px 3px rgba(105, 54, 18, 0.18);
}

.pack-buy-button {
    min-width: clamp(58px, 8.5cqw, 82px);
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: clamp(5px, 0.7cqw, 8px);
    padding: clamp(6px, 0.85cqw, 9px) clamp(10px, 1.3cqw, 14px);
    font-size: clamp(13px, 1.7cqw, 17px);
}

.pack-buy-button--loading::before {
    content: '';
    width: clamp(11px, 1.4cqw, 15px);
    aspect-ratio: 1;
    border: 2px solid rgba(95, 46, 10, 0.28);
    border-top-color: #5f2e0a;
    border-radius: 50%;
    animation: pack-buy-spin 760ms linear infinite;
}

.payment-retry-button:hover,
.payment-order-link:hover,
.pack-open-button:hover,
.pack-panel-close-button:hover,
.pack-buy-button:hover:not(:disabled) {
    filter:
        drop-shadow(0 4px 7px rgba(82, 44, 15, 0.22))
        drop-shadow(0 0 7px rgba(255, 217, 105, 0.22));
    transform: translateY(-1px);
}

.payment-retry-button:active,
.payment-order-link:active,
.pack-open-button:active,
.pack-panel-close-button:active,
.pack-buy-button:active:not(:disabled) {
    transform: translateY(0) scale(0.98);
}

.payment-retry-button:focus-visible,
.payment-order-link:focus-visible,
.pack-open-button:focus-visible,
.pack-panel-close-button:focus-visible,
.pack-buy-button:focus-visible {
    outline: 2px solid #fff0a6;
    outline-offset: 2px;
}

.pack-buy-button:disabled {
    cursor: wait;
    opacity: 0.66;
    filter: grayscale(0.22);
}

@media (max-width: 720px) {
    .payment-tab {
        gap: clamp(5px, 1.2cqh, 10px);
        padding: clamp(6px, 1.8cqw, 12px);
    }

    .payment-kicker {
        font-size: clamp(12px, 3.1cqw, 16px);
    }

    .payment-title {
        font-size: clamp(18px, 5.4cqw, 28px);
    }

    .payment-result-message {
        padding: 7px 10px;
        font-size: clamp(12px, 3.35cqw, 15px);
    }

    .payment-order-info {
        grid-template-columns: minmax(0, 1fr) auto;
        gap: 6px 8px;
        padding: 7px 9px;
    }

    .payment-order-details {
        grid-column: 1 / -1;
        grid-row: 2;
        justify-content: flex-start;
        font-size: clamp(12px, 3.2cqw, 15px);
        text-align: left;
    }

    .payment-order-status {
        font-size: clamp(10px, 2.8cqw, 12px);
    }

    .payment-order-code {
        font-size: clamp(10px, 2.6cqw, 12px);
    }

    .payment-order-link {
        min-width: 62px;
        padding: 6px 8px;
        font-size: clamp(9px, 2.5cqw, 11px);
    }

    .pack-entry-panel {
        padding: 4px 8px;
    }

    .pack-open-button {
        min-width: clamp(132px, 46cqw, 180px);
        padding: 9px 16px;
        font-size: clamp(15px, 4.4cqw, 20px);
    }

    .pack-panel-backdrop {
        padding: 8px 8px calc(8px + env(safe-area-inset-bottom));
    }

    .pack-panel {
        width: calc(100vw - 16px);
        height: calc(100dvh - 16px - env(safe-area-inset-bottom));
        max-height: calc(100dvh - 16px - env(safe-area-inset-bottom));
    }

    .pack-panel-header {
        gap: 10px;
        padding: 10px 12px;
    }

    .pack-panel-kicker {
        font-size: clamp(10px, 3cqw, 13px);
    }

    .pack-panel-title {
        font-size: clamp(18px, 5.4cqw, 26px);
    }

    .pack-panel-close-button {
        min-width: 62px;
        padding: 6px 9px;
        font-size: clamp(11px, 3.2cqw, 14px);
    }

    .promotion-strip {
        width: 100%;
        gap: 8px;
        padding: 0 2px 4px;
    }

    .promotion-card {
        flex-basis: min(88%, 320px);
        grid-template-columns: minmax(0, 1fr);
        gap: 5px;
        padding: 8px 10px;
    }

    .promotion-label {
        grid-row: auto;
        justify-self: start;
        font-size: clamp(12px, 3cqw, 14px);
    }

    .promotion-name {
        font-size: clamp(12px, 3.3cqw, 15px);
    }

    .promotion-description {
        font-size: clamp(10px, 2.75cqw, 12px);
        -webkit-line-clamp: 1;
    }

    .promotion-time {
        font-size: clamp(11px, 3cqw, 14px);
    }

    .pack-grid {
        grid-template-columns: repeat(auto-fit, minmax(clamp(132px, 42cqw, 176px), 1fr));
        gap: clamp(24px, 7cqw, 48px);
        padding: clamp(10px, 2.7cqw, 14px) clamp(12px, 3.2cqw, 16px) clamp(14px, 3.5cqw, 20px);
    }

    .pack-panel-grid {
        grid-template-columns: repeat(auto-fit, minmax(clamp(150px, 44vw, 190px), 1fr));
        gap: clamp(20px, 6vw, 34px);
        padding:
            clamp(12px, 3.5vw, 18px)
            clamp(12px, 4vw, 18px)
            calc(clamp(96px, 22vw, 132px) + env(safe-area-inset-bottom));
        scroll-padding-bottom: calc(clamp(96px, 22vw, 132px) + env(safe-area-inset-bottom));
    }

    .pack-card {
        aspect-ratio: 2 / 3;
        grid-template-rows: 37% minmax(0, 1fr);
        padding: clamp(19px, 5.2cqw, 25px) clamp(13px, 3.8cqw, 18px) clamp(14px, 4cqw, 20px);
    }

    .pack-card::before {
        inset: 0;
    }

    .pack-image {
        width: min(58%, 70px);
    }

    .pack-card--featured .pack-image {
        width: min(50%, 62px);
    }

    .pack-badge {
        font-size: clamp(8px, 2.2cqw, 10px);
    }

    .pack-name {
        font-size: clamp(12px, 3.5cqw, 15px);
    }

    .pack-reward {
        font-size: clamp(10px, 2.85cqw, 12px);
    }

    .pack-info {
        grid-template-rows: auto minmax(34px, 1fr) auto;
        gap: clamp(5px, 1.4cqw, 8px);
        padding-inline: clamp(4px, 1.4cqw, 8px);
    }

    .pack-description-button {
        width: clamp(20px, 5.5cqw, 24px);
        font-size: clamp(9px, 2.4cqw, 10px);
    }

    .pack-price {
        font-size: clamp(14px, 3.8cqw, 17px);
    }

    .pack-price-box {
        width: 100%;
        justify-items: center;
        padding: 4px 6px;
    }

    .pack-price-label {
        font-size: clamp(8px, 2.2cqw, 10px);
    }

    .pack-buy-button {
        width: 100%;
        min-width: 0;
        padding: 6px 8px;
        font-size: clamp(11px, 3cqw, 13px);
    }

    .pack-footer {
        grid-template-columns: minmax(0, 1fr);
        gap: 5px;
    }
}

@supports not (height: 100dvh) {
    @media (max-width: 720px) {
        .pack-panel {
            height: calc(100vh - 16px - env(safe-area-inset-bottom));
            max-height: calc(100vh - 16px - env(safe-area-inset-bottom));
        }
    }
}

@media (hover: none) {
    .pack-card:hover {
        filter: drop-shadow(0 7px 11px rgba(72, 43, 18, 0.22));
        transform: none;
    }

    .payment-retry-button:hover,
    .payment-order-link:hover,
    .pack-open-button:hover,
    .pack-panel-close-button:hover,
    .pack-description-button:hover,
    .pack-buy-button:hover:not(:disabled) {
        filter: none;
        transform: none;
    }
}

@keyframes pack-buy-spin {
    to {
        transform: rotate(360deg);
    }
}
</style>
