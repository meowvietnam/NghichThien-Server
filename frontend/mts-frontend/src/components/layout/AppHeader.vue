<script setup>
import { ref } from 'vue'
import { RouterLink } from 'vue-router'

const isMenuOpen = ref(false)

const toggleMenu = () => {
  isMenuOpen.value = !isMenuOpen.value
}

const closeMenu = () => {
  isMenuOpen.value = false
}
</script>

<template>
  <header class="app-header">
    <div class="logo">
      <RouterLink to="/" @click="closeMenu">
        <span class="logo-text">Nghịch Thiên</span>
      </RouterLink>
    </div>
    
    <button class="mobile-toggle" @click="toggleMenu" aria-label="Toggle menu">
      <span class="bar" :class="{ 'open': isMenuOpen }"></span>
      <span class="bar" :class="{ 'open': isMenuOpen }"></span>
      <span class="bar" :class="{ 'open': isMenuOpen }"></span>
    </button>

    <nav class="app-nav" :class="{ 'nav-open': isMenuOpen }">
      <RouterLink to="/" @click="closeMenu">Trang Chủ</RouterLink>
      <RouterLink to="/about" @click="closeMenu">Giới Thiệu</RouterLink>
    </nav>
  </header>
</template>

<style scoped>
.app-header {
  background-color: #1e293b;
  padding: 1rem 2rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 2px solid #38bdf8;
  box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.3);
  position: relative;
  z-index: 100;
}

.logo-text {
  font-size: 1.5rem;
  font-weight: bold;
  color: #38bdf8;
  text-transform: uppercase;
  letter-spacing: 2px;
}

.app-nav {
  display: flex;
  gap: 1rem;
}

.app-nav a {
  color: #cbd5e1;
  font-weight: 500;
  padding: 0.5rem 1rem;
  border-radius: 4px;
  transition: all 0.3s ease;
}

.app-nav a:hover,
.app-nav a.router-link-exact-active {
  background-color: #334155;
  color: #38bdf8;
}

.mobile-toggle {
  display: none;
  flex-direction: column;
  justify-content: space-around;
  width: 2rem;
  height: 2rem;
  background: transparent;
  border: none;
  cursor: pointer;
  padding: 0;
  z-index: 101;
}

.mobile-toggle .bar {
  width: 2rem;
  height: 0.25rem;
  background-color: #38bdf8;
  border-radius: 10px;
  transition: all 0.3s linear;
  position: relative;
  transform-origin: 1px;
}

@media screen and (max-width: 768px) {
  .app-header {
    padding: 1rem;
  }

  .mobile-toggle {
    display: flex;
  }

  .logo-text {
    font-size: 1.25rem;
  }

  .app-nav {
    position: absolute;
    top: 100%;
    right: 0;
    flex-direction: column;
    background-color: #1e293b;
    width: 100%;
    padding: 1rem;
    gap: 0.5rem;
    border-bottom: 2px solid #38bdf8;
    box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.3);
    
    transition: transform 0.3s ease-in-out, opacity 0.3s ease-in-out;
    transform: translateY(-150%);
    opacity: 0;
    pointer-events: none;
  }

  .app-nav.nav-open {
    transform: translateY(0);
    opacity: 1;
    pointer-events: auto;
  }

  .app-nav a {
    text-align: center;
    padding: 1rem;
    width: 100%;
  }

  .mobile-toggle .bar.open:nth-child(1) {
    transform: rotate(45deg);
  }
  .mobile-toggle .bar.open:nth-child(2) {
    opacity: 0;
  }
  .mobile-toggle .bar.open:nth-child(3) {
    transform: rotate(-45deg);
  }
}
</style>
