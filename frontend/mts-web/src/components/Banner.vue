<template>
    <div class="banner">
        <img :src="bannerImage" alt="" class="banner-image" />
        <div class="button-group">
            <Button
                class="banner-button">
                Nạp tiền
            </Button>
            <Button
                class="banner-button">
                Giftcode
            </Button>
        </div>
    </div>
</template>
<script setup lang="ts">
import bannerImage from '../assets/images/banner.png'
import Button from './Button.vue'
</script>
<style scoped>
.banner {
    position: relative;
}
.banner-image {
    display: block;
    width: 100%;
    height: auto;
}
.button-group {
    position: absolute;
    display: flex;
    gap: clamp(12px, 1.2vw, 24px);
    width: 50%;
    top: 35%;
    left: 50%;
    transform: translate(-50%, -50%);
}
.banner-button {
    font-size: clamp(6px, 1.2vw, 16px);
}
</style>