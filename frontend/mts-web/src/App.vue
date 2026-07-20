<template>
  <div class="app-background">
    <Header />
    <MainLogo />
    <main class="content">
      <router-view />
    </main>
    <Footer />
  </div>
</template>


<script setup lang="ts">
import Footer from './components/Footer.vue'
import Header from './components/Header.vue'
import MainLogo from './components/MainLogo.vue'
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
</style>
