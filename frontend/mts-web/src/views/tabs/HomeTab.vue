<template>
    <div class="banner-account">
        <div class="account-title">
            <img :src="titlePlateImage" alt="" class="account-title-image" />
            <span class="account-title-text">Tài khoản</span>
        </div>
        <Account :name="charData.name"
         :money-vip="charData.moneyVip" 
        />
        <NotifyStack ref="notifyStack" />
    </div>
</template>
<script setup lang="ts">
import Account from '../../components/Account.vue'
import { onMounted, ref, reactive } from 'vue'
import {api} from '../../api/axios'
import NotifyStack from '../../components/NotifyStack.vue'
import titlePlateImage from '../../assets/sprites/sprite_05.png'
const notifyStack = ref<InstanceType<typeof NotifyStack> | null>(null)
const charData = reactive({
    name: 'Player',
    moneyVip: 0
})
onMounted(async () => {
    const res = (await api.get('/auth/me')).data;
    if (!res.success) {
        notifyStack.value?.push({
            title: 'Thông báo',
            message: res.message,
        })
        return;
    }
    const data = res.data;
    charData.name = data.name;
    charData.moneyVip = data.moneyVip;
})
</script>
<style scoped>
.banner-account {

    position: relative;
    top:5%;
    width: 100%;
    height: 100%;
    display: flex;
    align-items: center;
    justify-content: center;
    container-type: size;
}

.account-title {
    position: absolute;
    top: -5%;
    left: 50%;
    z-index: 2;
    width: min(26%, clamp(150px, 30cqw, 272px));
    aspect-ratio: 272 / 90;
    display: grid;
    place-items: center;
    transform: translateX(-50%);
    pointer-events: none;
    filter:
        drop-shadow(0 5px 8px rgba(42, 20, 8, 0.26))
        drop-shadow(0 0 7px rgba(238, 207, 124, 0.2));
}

.account-title-image {
    position: absolute;
    inset: 0;
    width: 100%;
    height: 100%;
    display: block;
    object-fit: fill;
}

.account-title-text {
    position: relative;
    z-index: 1;
    max-width: 68%;
    overflow: hidden;
    padding-top: clamp(1px, 0.35cqh, 3px);
    color: #ffe7a6;
    font-family: var(--font-cloud-shui-zhu);
    font-size: clamp(13px, 3.4cqw, 30px);
    font-weight: 400;
    line-height: 1;
    text-align: center;
    white-space: nowrap;
    text-overflow: ellipsis;
    -webkit-text-stroke: clamp(0.3px, 0.08cqw, 0.8px) rgba(93, 40, 12, 0.72);
    paint-order: stroke fill;
    text-shadow:
        0 1px 0 rgba(255, 250, 214, 0.5),
        0 2px 3px rgba(45, 18, 6, 0.82),
        0 0 10px rgba(255, 222, 126, 0.35);
}

@media (max-width: 720px) {
    .account-title {
        top: 0;
        width: min(52%, 190px);
    }

    .account-title-text {
        font-size: clamp(11px, 4.2cqw, 20px);
    }
}
</style>
