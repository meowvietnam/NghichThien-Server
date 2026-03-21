<script setup>
import { RouterView } from 'vue-router'
import AppHeader from './components/layout/AppHeader.vue'
import AppFooter from './components/layout/AppFooter.vue'
import { onMounted } from 'vue';
const env = import.meta.env;
onMounted(() => {
  const apiUrl = env.VITE_API_BASE_URL;
  console.log(apiUrl)
})
</script>

<template>
  <div class="app-layout">
    <AppHeader />
    
    <main class="main-content">
      <RouterView />
    </main>

    <AppFooter />
  </div>
</template>

<style scoped>
.app-layout {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}

.main-content {
  flex: 1;
  width: 100%;
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem;
  animation: fadeIn 0.4s ease;
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}

@media screen and (max-width: 768px) {
  .main-content {
    padding: 1rem;
  }
}
</style>
