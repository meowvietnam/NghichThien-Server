<template>
    <div>
        <GoogleLogin
            :button-config="buttonConfig"
            :callback="callback"
            :error="handleError"
        />
    </div>
</template>

<script setup lang="ts">
import { GoogleLogin } from 'vue3-google-login'

const buttonConfig = {
    type: 'standard',
    theme: 'outline_dark',
    size: 'large',
    shape: 'pill',
    logo_alignment: 'left',
    locale: 'vi',
} as const

const callback = (response: unknown) => {
    console.log(response)
}

const handleError = (error: unknown) => {
    console.error(error)
}
</script>

<style scoped>
</style>