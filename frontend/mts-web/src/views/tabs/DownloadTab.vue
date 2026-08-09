<template>
    <section class="download-tab">
        <div class="download-content">
            <div class="download-copy">
                <p class="download-kicker">Phiên bản Android</p>
                <h2 class="download-title">Tải game</h2>
            </div>

            <button class="google-play-button" type="button" @click="handleDownload">
                <span class="google-play-button__icon-frame">
                    <img :src="googlePlayIcon" alt="" class="google-play-button__icon" />
                </span>
                <span class="google-play-button__text">
                    <span class="google-play-button__eyebrow">Tải trên</span>
                    <span class="google-play-button__label">Google Play</span>
                </span>
            </button>
        </div>
    </section>
</template>

<script setup lang="ts">
import { inject } from 'vue'
import googlePlayIcon from '../../assets/icons/google-play.png'

const notify = inject<(event: string) => void>('notify')

const handleDownload = () => {
    notify?.('downloadClick')
}
</script>

<style scoped>
.download-tab {
    width: 100%;
    height: 100%;
    min-height: 0;
    display: grid;
    place-items: center;
    padding: clamp(10px, 3.6vw, 34px);
}

.download-content {
    width: min(72%, 520px);
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: clamp(10px, 2.4vw, 22px);
    text-align: center;
}

.download-copy {
    display: grid;
    gap: clamp(2px, 0.7vw, 8px);
}

.download-kicker {
    color: #8f5a26;
    font-family: var(--font-alegreya);
    font-size: clamp(11px, 1.55vw, 18px);
    line-height: 1;
}

.download-title {
    color: #704116;
    font-family: var(--font-cloud-shui-zhu);
    font-size: clamp(24px, 5.2vw, 56px);
    font-weight: 400;
    line-height: 1;
    letter-spacing: 0;
    text-shadow:
        0 1px 0 rgba(255, 247, 211, 0.86),
        0 3px 6px rgba(94, 52, 18, 0.24);
}

.google-play-button {
    position: relative;
    isolation: isolate;
    width: min(100%, clamp(190px, 30vw, 340px));
    min-height: clamp(58px, 8.2vw, 86px);
    display: grid;
    grid-template-columns: auto 1fr;
    align-items: center;
    gap: clamp(9px, 1.4vw, 16px);
    padding: clamp(8px, 1.2vw, 12px) clamp(14px, 2vw, 22px);
    border: 1px solid rgba(218, 220, 224, 0.98);
    border-radius: 8px;
    background: #ffffff;
    box-shadow:
        0 1px 2px rgba(60, 64, 67, 0.16),
        0 6px 14px rgba(73, 41, 16, 0.14);
    color: #202124;
    cursor: pointer;
    font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
    overflow: hidden;
    -webkit-tap-highlight-color: transparent;
    touch-action: manipulation;
    transition:
        filter 160ms ease,
        transform 160ms ease,
        box-shadow 160ms ease;
}

.google-play-button::before {
    content: '';
    position: absolute;
    inset: 1px;
    z-index: -1;
    border-radius: 7px;
    background: linear-gradient(115deg, rgba(255, 255, 255, 0.42), transparent 46%);
    pointer-events: none;
}

.google-play-button:hover {
    filter:
        drop-shadow(0 8px 12px rgba(52, 30, 12, 0.18))
        drop-shadow(0 0 8px rgba(255, 255, 255, 0.28));
    transform: translateY(-1px);
    box-shadow:
        0 2px 4px rgba(60, 64, 67, 0.18),
        0 8px 18px rgba(73, 41, 16, 0.18);
}

.google-play-button:active {
    transform: translateY(0) scale(0.985);
}

.google-play-button:focus-visible {
    outline: 2px solid #fff0a6;
    outline-offset: 3px;
}

.google-play-button__icon-frame {
    width: clamp(34px, 5.2vw, 54px);
    aspect-ratio: 1;
    display: grid;
    place-items: center;
    border-radius: 8px;
    background: transparent;
}

.google-play-button__icon {
    width: 72%;
    height: 72%;
    display: block;
    object-fit: contain;
}

.google-play-button__text {
    min-width: 0;
    display: grid;
    gap: clamp(1px, 0.25vw, 3px);
    justify-items: start;
    text-align: left;
}

.google-play-button__eyebrow {
    color: #5f6368;
    font-size: clamp(11px, 1.45vw, 16px);
    font-weight: 400;
    line-height: 1;
}

.google-play-button__label {
    max-width: 100%;
    overflow: hidden;
    color: #202124;
    font-size: clamp(18px, 2.8vw, 30px);
    font-weight: 500;
    line-height: 1;
    white-space: nowrap;
    text-overflow: ellipsis;
}

@media (max-width: 720px) {
    .download-tab {
        padding: clamp(8px, 2.8vw, 16px);
    }

    .download-content {
        width: min(84%, 360px);
        gap: clamp(8px, 2vw, 14px);
    }

    .download-kicker {
        font-size: clamp(9px, 2.2vw, 12px);
    }

    .download-title {
        font-size: clamp(18px, 6vw, 30px);
    }

    .google-play-button {
        width: min(100%, 260px);
        min-height: clamp(46px, 12vw, 58px);
        gap: 9px;
        padding: 7px 12px;
        box-shadow:
            0 1px 2px rgba(60, 64, 67, 0.14),
            0 4px 10px rgba(73, 41, 16, 0.14);
    }

    .google-play-button__icon-frame {
        width: clamp(30px, 8vw, 38px);
    }

    .google-play-button__eyebrow {
        font-size: clamp(9px, 2.5vw, 11px);
    }

    .google-play-button__label {
        font-size: clamp(15px, 4.8vw, 20px);
    }
}

@media (hover: none) {
    .google-play-button:hover {
        filter: none;
        transform: none;
    }

    .google-play-button:active {
        transform: scale(0.985);
    }
}
</style>
