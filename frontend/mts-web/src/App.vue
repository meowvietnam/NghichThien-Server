<template>
  <div class="app-background">
    <Header />
    <MainLogo />
    <main class="content">
      <router-view />
    </main>
    <Footer />
    <div v-if="isDownloadButtonVisible" class="button-download">
      <Button @click="download">
        Tải game
      </Button>
    </div>
    <a href="https://zalo.me/g/n876cseg8jpcqjon2yq1" target="_blank" rel="noopener noreferrer" class="zalo-floating-button">
      <img src="./assets/images/zalo-icon.svg" alt="Zalo" />
    </a>
  </div>
</template>


<script setup lang="ts">
import { provide, ref } from 'vue'
import Button from './components/Button.vue';
import Footer from './components/Footer.vue'
import Header from './components/Header.vue'
import MainLogo from './components/MainLogo.vue'
import { getDownloadUrl } from './api/axios'
const isDownloadButtonVisible = ref(true)

const setDownloadButtonVisible = (isVisible: boolean) => {
  isDownloadButtonVisible.value = isVisible
}

provide('setDownloadButtonVisible', setDownloadButtonVisible)

const download = () => {
  window.open(getDownloadUrl(), '_blank')
}

</script>

<style scoped>
.app-background {
  width: 100%;
  min-height: 100dvh;
  display: flex;
  flex-direction: column;
  overflow-x: hidden;
  background-color: #090a10;
  background-image: url('./assets/images/background.png');
  background-position: center top;
  background-repeat: no-repeat;
  background-size: cover;
  border-radius: 16px;
}
.button-download {
  position: fixed;
  top: 16px;
  right: 16px;
  z-index: 1000;
  min-width: clamp(120px, 20vw, 200px);
}
.content {
  flex: 1 1 auto;
  width: 100%;
  display: flex;
}

@supports not (min-height: 100dvh) {
  .app-background {
    min-height: 100svh;
  }
}

@media (max-width: 768px) {
  .app-background {
    background-position: center top;
  }
}

.zalo-floating-button {
  position: fixed;
  bottom: 24px;
  right: 24px;
  z-index: 1000;
  width: 56px;
  height: 56px;
  border-radius: 50%;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  transition: transform 0.2s ease-in-out;
  display: flex;
  align-items: center;
  justify-content: center;
}
.zalo-floating-button:hover {
  transform: scale(1.1);
}
.zalo-floating-button img {
  width: 100%;
  height: 100%;
  object-fit: contain;
}
</style>
