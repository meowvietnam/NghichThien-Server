

<template>
  <section class="auth-view">
    <div class="form-container" :style="formFrameStyle">
      <div class="form-title">
        <img :src="titleImage" alt="" class="title-image" />
        <div class="title-text">
          Đăng nhập
        </div>
      </div>

      <form class="form-content" @submit.prevent="handleLogin">
        <AuthInput
          v-model="username"
          label="Tài khoản"
          type="text"
          placeholder="Nhập tài khoản"
          autocomplete="username"
          :disabled="isLoading"
        />
        <AuthInput
          v-model="password"
          label="Mật khẩu"
          type="password"
          placeholder="Nhập mật khẩu"
          autocomplete="current-password"
          :disabled="isLoading"
        />
        <Button type="submit" :disabled="isLoading">
          {{ isLoading ? 'Đang nhập' : 'Đăng nhập' }}
        </Button>
        <img :src="orImage" alt="" class="or-image" />
        <GoogleSignInButton @loading-change="setLoading" />
      </form>
    </div>
  </section>
  <NotifyStack ref="notifyStack" />
  <LoadingOverlay :visible="isLoading" />
</template>
<script setup lang="ts">
import type { CSSProperties } from 'vue'
import { ref } from 'vue'
import api from '../api/axios'
import formImage from '../assets/sprites/sprite_01.png'
import titleImage from '../assets/sprites/sprite_05.png'
import orImage from '../assets/sprites/sprite_37.png'
import GoogleSignInButton from '../components/GoogleSignInButton.vue'
import AuthInput from '../components/AuthInput.vue'
import Button from '../components/Button.vue'
import LoadingOverlay from '../components/LoadingOverlay.vue'
import NotifyStack from '../components/NotifyStack.vue'

const formFrameStyle = {
  '--form-frame': `url(${formImage})`,
} as CSSProperties

const username = ref('')
const password = ref('')
const isLoading = ref(false)
const notifyStack = ref<InstanceType<typeof NotifyStack> | null>(null)

const delay = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms))

const setLoading = (loading: boolean) => {
  isLoading.value = loading
}

const handleLogin = async () => {
  if (isLoading.value) {
    return
  }

  if (!username.value.trim() || !password.value) {
    notifyStack.value?.push({
      title: 'Thông báo',
      message: 'Vui lòng nhập đầy đủ tài khoản và mật khẩu.',
    })
    return
  }

  isLoading.value = true

  try {
    const res = (await api.post('/auth/login', {
      username: username.value.trim(),
      password: password.value,
    })).data

    if (!res.success) {
      notifyStack.value?.push({
        title: 'Thông báo',
        message: res.message,
      })
      return
    }

    const data = res.data
    notifyStack.value?.push({
      title: 'Thông báo',
      message: data.message,
    })
    localStorage.setItem('accessToken', data.token)
    await delay(900)
    window.location.href = '/'
  } catch (error) {
    console.error(error)
    notifyStack.value?.push({
      title: 'Thông báo',
      message: 'Không thể đăng nhập, vui lòng thử lại.',
    })
  } finally {
    isLoading.value = false
  }
}
</script>
<style scoped>
.auth-view {
  flex: 1 1 auto;
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: clamp(24px, 6vw, 72px) clamp(14px, 4vw, 48px);
}

.form-container {
  --form-border-x: clamp(34px, 5.2vw, 58px);
  --form-border-y: clamp(28px, 4vw, 44px);

  position: relative;
  width: min(100%, 823px);
  aspect-ratio: 823 / 290;
  min-height: clamp(400px, 36vw, 540px);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: clamp(28px, 5vw, 64px) clamp(28px, 8vw, 108px);
  background: transparent;
  border: solid transparent;
  border-width: var(--form-border-y) var(--form-border-x);
  border-image-source: var(--form-frame);
  border-image-slice: 42 54 fill;
  border-image-width: var(--form-border-y) var(--form-border-x);
  border-image-repeat: stretch;
}

.form-content {
  width: min(100%, clamp(250px, 38vw, 360px));
  display: grid;
  gap: clamp(8px, 1.2vw, 12px);
  justify-items: center;
}
.form-title {
  position: absolute;
  top: 0;
  left: 50%;
  z-index: 2;
  width: clamp(150px, 30%, 272px);
  aspect-ratio: 272 / 90;
  display: grid;
  place-items: center;
  transform: translate(-50%, -100%);
  pointer-events: none;
}

.title-image {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  object-fit: contain;
}

.title-text {
  position: absolute;
  z-index: 1;
  max-width: 72%;
  overflow-wrap: anywhere;
  text-align: center;
  white-space: nowrap;
  font-size: clamp(18px, 2.1vw, 32px);
  line-height: 1;
  font-family: var(--font-cloud-shui-zhu);
  color: #e3e01a;
  text-shadow:
    0 2px 3px rgba(99, 98, 98, 0.7),
    0 0 10px rgba(255, 255, 255, 0.34);
}
.or-image {
  width: clamp(160px, 28vw, 300px);
  height: auto;
}

</style>
