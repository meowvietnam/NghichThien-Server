import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'Home',
      component: () => import('../views/Home.vue')
    },
    {
      path: '/auth',
      name: 'Auth',
      component: () => import('../views/Auth.vue')
    }
  ]
})

export default router