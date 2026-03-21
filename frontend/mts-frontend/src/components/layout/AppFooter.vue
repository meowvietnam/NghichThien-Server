<template>
  <footer class="app-footer">
    <p>&copy; {{ new Date().getFullYear() }} Nghịch Thiên: Sky Breaker. All rights reserved.</p>
  </footer>
</template>

<style scoped>
.app-footer {
  background-color: #1e293b;
  color: #94a3b8;
  text-align: center;
  padding: 1.5rem;
  margin-top: auto;
  border-top: 1px solid #334155;
  font-size: 0.9rem;
}

@media screen and (max-width: 768px) {
  .app-footer {
    padding: 1rem;
    font-size: 0.8rem;
  }
}
</style>
