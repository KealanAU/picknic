<script setup lang="ts">
import { VyButton } from '@vyui/kit';
import type { EventQr, HostEvent } from '../../api/hostEvents';
import { t } from '../../theme/tokens';

const props = defineProps<{
  event: HostEvent;
  qr?: EventQr | null;
  busy?: boolean;
  copied?: boolean;
}>();

defineEmits<{
  refresh: [];
  copy: [];
}>();
</script>

<template>
  <view :style="{ display: 'flex', flexDirection: 'column', gap: '12px' }">
    <view>
      <text :style="{ fontFamily: t.font.body, fontSize: '12px', letterSpacing: t.tracking, textTransform: 'uppercase', color: t.color.muted }">
        Room code
      </text>
      <text :style="{ fontFamily: t.font.display, fontSize: '48px', fontWeight: '300', lineHeight: '1', letterSpacing: t.tracking, color: t.color.blue }">
        {{ event.code }}
      </text>
    </view>

    <view
      v-if="qr?.qrPng"
      :style="{
        width: '176px',
        height: '176px',
        alignSelf: 'center',
        padding: '10px',
        borderWidth: '1px',
        borderStyle: 'solid',
        borderColor: t.color.line,
        backgroundColor: '#ffffff',
      }"
    >
      <image :src="qr.qrPng" mode="aspectFit" :style="{ width: '100%', height: '100%' }" />
    </view>

    <view :style="{ display: 'flex', flexDirection: 'column', gap: '5px' }">
      <text :style="{ fontFamily: t.font.body, fontSize: '12px', letterSpacing: t.tracking, textTransform: 'uppercase', color: t.color.muted }">
        Share link
      </text>
      <text :style="{ fontFamily: t.font.body, fontSize: '13px', lineHeight: '1.35', letterSpacing: t.tracking, color: t.color.ink }">
        {{ qr?.joinUrl || 'Generate the share link after creating the party.' }}
      </text>
    </view>

    <view :style="{ display: 'flex', flexDirection: 'column', gap: '8px' }">
      <VyButton size="lg" block :loading="busy" @click="$emit('refresh')">
        {{ qr ? 'Refresh QR' : 'Generate link + QR' }}
      </VyButton>
      <VyButton v-if="qr?.joinUrl" variant="soft" size="lg" block @click="$emit('copy')">
        {{ copied ? 'Copied' : 'Copy link' }}
      </VyButton>
    </view>
  </view>
</template>
