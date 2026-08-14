<template>
    <div>
        <GoogleLogin :button-config="buttonConfig" :callback="callback" :error="handleError" />
        <NotifyStack ref="notifyStack" />
    </div>
</template>

<script setup lang="ts">
import { GoogleLogin } from 'vue3-google-login'
import NotifyStack from './NotifyStack.vue'
import { ref } from 'vue'
import {api} from '../api/axios'
const notifyStack = ref<InstanceType<typeof NotifyStack> | null>(null)
const emit = defineEmits<{
  'loading-change': [loading: boolean]
}>()
const buttonConfig = {
    type: 'standard',
    theme: 'outline_dark',
    size: 'large',
    shape: 'pill',
    logo_alignment: 'left',
    locale: 'vi',
} as const
const delay = (ms: number) =>
  new Promise(resolve => setTimeout(resolve, ms));
const callback = async (response: { credential: string }) => {
    emit('loading-change', true)
    const idToken = response.credential

    try {
      const res = (await api.post('/auth/login-with-google', { idToken })).data
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
    await delay(2000)
    window.location.href = '/'
    } catch (error) {
      console.error(error)
      notifyStack.value?.push({
        title: 'Thông báo',
        message: 'Không thể đăng nhập Google, vui lòng thử lại.',
      })
    } finally {
      emit('loading-change', false)
    }
}

const handleError = (error: unknown) => {
    console.error(error)
}
</script>

<style scoped></style>
