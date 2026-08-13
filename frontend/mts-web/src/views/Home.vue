<template>
    <div class="home-view">
        <div class="home-panel">
            <ToggleGroup
                v-model="tab"
                class="home-panel-tabs"
                orientation="horizontal"
                aria-label="Danh mục trang chủ"
                stretch
                :hide-overflow="false"
                :options="[
                    { label: 'Trang chủ', value: 'home' },
                    { label: 'Tải game', value: 'download' },
                    { label: 'Nạp tiền', value: 'purchase' },
                    { label: 'Giftcode', value: 'giftcode' },
                ]"
            />
            <img :src="homePanelImage" alt="" class="home-panel-image" />
            <div class="home-panel-container">
                <component :is="currentTabComponent" />
            </div>
        </div>
    </div>
    <NotifyStack />
</template>
<script setup lang="ts">
import { provide, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {getDownloadUrl} from '../api/axios'
import homePanelImage from '../assets/images/home-panel.png'
import NotifyStack from '../components/NotifyStack.vue'
import ToggleGroup from '../components/ToggleGroup.vue'
import DownloadTab from './tabs/DownloadTab.vue'
import GiftCodeTab from './tabs/GiftCodeTab.vue'
import HomeTab from './tabs/HomeTab.vue'
import PaymentTab from './tabs/PaymentTab.vue'
const route = useRoute()
const router = useRouter()
const notify = (event: string) => {
    switch (event) {
        case 'payClick':
            payClick()
            break
        case 'downloadClick':
            downloadClick()
            break
        case 'giftcodeClick':
            giftCodeClick()
            break
        case 'logoutClick':
            logoutClick()
            break
    }
}
provide('notify', notify)
const payClick = () => {
    tab.value = 'purchase'
}
const downloadClick = () => {
    window.open(getDownloadUrl(), '_blank')
}
const giftCodeClick = () => {
    tab.value = 'giftcode'
}
const logoutClick = () => {
    localStorage.removeItem('accessToken')
    router.push('/auth')
}
const validTabs = [
    'home',
    'download',
    'purchase',
    'giftcode',
] as const

type Tab = typeof validTabs[number]

const tab = computed<Tab>({
    get() {
        const value = route.query.tab

        if (typeof value === 'string' && validTabs.includes(value as Tab)) {
            return value as Tab
        }

        return 'home'
    },

    set(value) {
        router.replace({
            query: {
                tab: value,
            },
        })
    },
})

const currentTabComponent = computed(() => {
    switch (tab.value) {
        case 'home':
            return HomeTab

        case 'download':
            return DownloadTab

        case 'purchase':
            return PaymentTab

        case 'giftcode':
            return GiftCodeTab

        default:
            return HomeTab
    }
})
</script>
<style scoped>
.home-view {
    width: 100%;
    min-width: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    padding-inline: clamp(10px, 3vw, 28px);
    margin-bottom: clamp(24px, 12vh, 124px);
}

.home-panel {
    position: relative;
    width: min(100%, 1180px);
}

.home-panel-tabs {
    position: absolute;
    top: 10.75%;
    left: 50%;
    z-index: 2;
    width: min(78%, 760px);
    transform: translateX(-50%);
    gap: clamp(1px, 0.28cqw, 5px);
    padding: clamp(3px, 0.7cqw, 8px) clamp(8px, 1.35cqw, 16px);
    border: 1px solid rgba(240, 203, 119, 0.38);
    border-radius: 999px;
    background:
        linear-gradient(90deg, rgba(14, 51, 53, 0.28), rgba(126, 83, 42, 0.2), rgba(14, 51, 53, 0.28)),
        rgba(30, 18, 13, 0.2);
    box-shadow:
        inset 0 1px 0 rgba(255, 240, 175, 0.22),
        inset 0 -8px 14px rgba(21, 8, 4, 0.18),
        0 10px 20px rgba(0, 0, 0, 0.22);
    backdrop-filter: blur(1.5px);
}

.home-panel-tabs::before {
    content: '';
    position: absolute;
    inset: 50% 5% auto;
    height: 1px;
    pointer-events: none;
    background: linear-gradient(90deg, transparent, rgba(255, 233, 161, 0.45), transparent);
    opacity: 0.72;
}

.home-panel-tabs :deep(.toggle-tab) {
    filter:
        drop-shadow(0 4px 5px rgba(24, 11, 5, 0.28))
        drop-shadow(0 0 6px rgba(255, 223, 140, 0.08));
    transition:
        color 140ms ease,
        filter 160ms ease,
        transform 160ms ease;
}

.home-panel-tabs :deep(.toggle-tab--selected) {
    z-index: 1;
    transform: translateY(-4%);
    filter:
        drop-shadow(0 7px 8px rgba(24, 11, 5, 0.32))
        drop-shadow(0 0 10px rgba(239, 207, 125, 0.24));
}

.home-panel-tabs :deep(.toggle-tab__label) {
    max-width: 68%;
    font-size: clamp(9px, 2.7cqw, 20px);
    text-shadow:
        0 2px 2px rgba(48, 23, 10, 0.86),
        0 0 8px rgba(255, 228, 142, 0.2);
}

.home-panel-tabs :deep(.toggle-tab--selected .toggle-tab__label) {
    text-shadow:
        0 2px 2px rgba(33, 15, 6, 0.88),
        0 0 10px rgba(255, 236, 156, 0.34);
}

.home-panel-image {
    width: 100%;
    height: auto;
    display: block;
}

.home-panel-container {
    position: absolute;
    top: 22%;
    left: 15%;
    right: 15%;
    bottom: 21%;
}

@media (max-width: 720px) {
    .home-view {
        padding-inline: clamp(6px, 2.5vw, 14px);
        margin-bottom: clamp(18px, 8vh, 64px);
    }

    .home-panel-tabs {
        top: 10.25%;
        width: min(84%, 520px);
        padding-inline: clamp(5px, 1.6cqw, 10px);
        box-shadow:
            inset 0 1px 0 rgba(255, 240, 175, 0.18),
            0 6px 12px rgba(0, 0, 0, 0.18);
    }

    .home-panel-tabs :deep(.toggle-tab--selected) {
        transform: translateY(-2%);
    }

    .home-panel-tabs :deep(.toggle-tab__label) {
        max-width: 72%;
        font-size: clamp(8px, 2.45cqw, 14px);
    }
}
</style>
