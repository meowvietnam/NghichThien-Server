<template>
    <div class="account-view">
        <div class="account-panel">
            <div class="account-info">
                <div class="account-row account-name">
                    <span class="account-label">Nhân vật</span>
                    <span class="account-value">{{ name }}</span>
                </div>
                <div class="account-row money-vip">
                    <span class="account-label">Tiên ngọc</span>
                    <span class="money-vip-value">
                        <img :src="tienNgocIcon" alt="" class="money-vip-icon" />
                        <span>{{ FormatUtil.number(moneyVip) }}</span>
                    </span>
                </div>

                <Button class="button" @click="payClick">
                    Nạp tiền
                </Button>
                <Button class="button" @click="giftcodeClick">
                    Giftcode
                </Button>
                <Button class="button" @click="logoutClick">
                    Đăng xuất
                </Button>
            </div>
        </div>
    </div>
</template>
<script setup lang="ts">
import Button from './Button.vue';
import { FormatUtil} from '../utils/utils.ts'
import { inject } from 'vue'
import tienNgocIcon from '../assets/icons/tien_ngoc.png'

const notify = inject<(event: string,params: any) => void>('notify')
withDefaults(defineProps<{
    name?: string
    moneyVip?: number
}>(), {
    name: 'Player',
    moneyVip: 0,
})
const payClick = () => {
    notify?.('payClick', {});
}
const giftcodeClick = () => {
    notify?.('giftcodeClick', {});
}
const logoutClick = () => {
    notify?.('logoutClick', {});
}
</script>
<style scoped>
.account-view {
    width: 100%;
    height: 100%;
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 0;
}

.account-panel {
    width: 100%;
    height: 100%;

    background-image: url('../assets/sprites/sprite_02.png');
    background-repeat: no-repeat;
    background-position: center;
    background-size: contain;

    display: flex;
    justify-content: center;
    align-items: flex-end;

    box-sizing: border-box;
    padding-bottom: 8%;
}

.account-info {
    width: min(44%, 360px);

    display: flex;
    flex-direction: column;
    align-items: center;

    gap: clamp(4px, 0.7vw, 10px);

    font-size: clamp(9px, 1.2vw, 16px);
    line-height: 1.3;
    text-align: center;
}

.account-row {
    width: 100%;
    min-height: clamp(26px, 3.7vw, 42px);
    display: grid;
    grid-template-columns: auto minmax(0, 1fr);
    align-items: center;
    gap: clamp(6px, 0.9vw, 12px);
    padding: clamp(5px, 0.7vw, 9px) clamp(10px, 1.4vw, 18px);
    border: 1px solid rgba(255, 226, 153, 0.26);
    border-radius: clamp(5px, 0.5vw, 8px);
    background:
        linear-gradient(90deg, rgba(20, 62, 61, 0.38), rgba(40, 22, 12, 0.34)),
        rgba(0, 0, 0, 0.28);
    box-shadow:
        inset 0 1px 0 rgba(255, 244, 197, 0.12),
        0 2px 6px rgba(33, 15, 5, 0.2);
}

.account-label {
    color: #ffe6a8;
    font-family: var(--font-cloud-shui-zhu);
    font-size: clamp(10px, 1.45vw, 19px);
    line-height: 1;
    white-space: nowrap;
    -webkit-text-stroke: clamp(0.2px, 0.05vw, 0.55px) rgba(80, 31, 8, 0.74);
    paint-order: stroke fill;
    text-shadow:
        0 1px 0 rgba(255, 250, 218, 0.34),
        0 2px 3px rgba(37, 16, 5, 0.72);
}

.account-value,
.money-vip-value {
    min-width: 0;
    justify-self: end;
    color: #fff8d5;
    font-family: var(--font-alegreya);
    font-size: clamp(12px, 1.55vw, 21px);
    font-weight: 700;
    line-height: 1;
    text-align: right;
    text-shadow:
        0 1px 1px rgba(39, 16, 5, 0.7),
        0 0 7px rgba(255, 222, 120, 0.16);
}

.account-value {
    overflow: hidden;
    white-space: nowrap;
    text-overflow: ellipsis;
}

.money-vip-value {
    display: inline-flex;
    align-items: center;
    gap: clamp(4px, 0.5vw, 7px);
}

.money-vip-icon {
    flex: 0 0 auto;
    width: clamp(17px, 2.3vw, 28px);
    aspect-ratio: 1;
    display: block;
    object-fit: contain;
    filter:
        drop-shadow(0 1px 2px rgba(25, 9, 4, 0.42))
        drop-shadow(0 0 7px rgba(255, 101, 206, 0.34));
}
.button {
    width: 60%;
}

@media (max-width: 720px) {
    .account-info {
        width: min(52%, 260px);
        gap: clamp(3px, 0.8vw, 6px);
    }

    .account-row {
        min-height: clamp(22px, 5.7vw, 30px);
        gap: 5px;
        padding: 4px 8px;
    }

    .account-label {
        font-size: clamp(8px, 2.5vw, 12px);
    }

    .account-value,
    .money-vip-value {
        font-size: clamp(9px, 2.6vw, 13px);
    }

    .money-vip-icon {
        width: clamp(14px, 4vw, 19px);
    }
}
</style>
