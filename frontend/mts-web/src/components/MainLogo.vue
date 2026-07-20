<template>
    <img class="main-logo" :src="mainLogo" alt="Nghịch Thiên - Sky Breaker" />
</template>

<script setup lang="ts">
import mainLogo from '../assets/images/main-logo.png'
</script>

<style scoped>
.main-logo {
    display: block;
    width: clamp(150px, 28vw, 400px);
    margin-left: clamp(10px, 5vw, 20px);
    margin-top: clamp(5px, 5vw, 20px);
}
</style>
