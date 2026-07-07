<script setup lang="ts">
import { VyButton } from '@vyui/kit';
import type { EventQr, HostEvent } from '../../api/hostEvents';
import { t } from '../../theme/tokens';

defineProps<{
  event: HostEvent;
  qr?: EventQr | null;
  busy?: boolean;
}>();

defineEmits<{
  refresh: [];
  share: [];
}>();
</script>

<template>
  <view :style="{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '12px' }">
    <view
      :style="{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '6px' }"
      @tap="qr?.joinUrl && $emit('share')"
    >
      <text :style="{ fontFamily: t.font.body, fontSize: '12px', letterSpacing: t.tracking, textTransform: 'uppercase', color: t.color.muted }">
        Room code
      </text>
      <text :style="{ fontFamily: t.font.display, fontSize: '46px', fontWeight: '300', lineHeight: '1', letterSpacing: '0.04em', color: t.color.blue }">
        {{ event.code }}
      </text>
    </view>

    <VyButton v-if="!qr?.joinUrl" size="lg" block :loading="busy" @tap="$emit('refresh')">
      Get invite link
    </VyButton>
    <VyButton v-else color="primary" size="lg" block @tap="$emit('share')">
      Share invite
    </VyButton>
  </view>
</template>
