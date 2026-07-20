import { createApp } from 'vue'
import './style.css'
import App from './App.vue'
import router from './router'
import vue3GoogleLogin from 'vue3-google-login'
createApp(App)
.use(vue3GoogleLogin, {
    clientId: '616494410116-bf6kl6dulrv1n0bm3f616ehkmq1mnkbh.apps.googleusercontent.com'
})
.use(router)
.mount('#app')
