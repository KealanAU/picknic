<script setup lang="ts">
// Party-code display with share actions — the guest screen no longer shows
// the code big, so only the host roll uses this.
import { VyButton } from '@vyui/kit';
import { t } from '../theme/tokens';

defineProps<{
  code: string;
  busy?: boolean;
  showShare?: boolean;
  shareReady?: boolean;
}>();

defineEmits<{
  refresh: [];
  share: [];
}>();
</script>

<template>
  <!-- Margin spacing instead of gap: the v-if/v-else button leaves a fragment
       anchor that container gap would treat as a child. -->
  <view :style="{ display: 'flex', flexDirection: 'column', alignItems: 'center' }">
    <view
      :style="{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '6px' }"
      @tap="shareReady && $emit('share')"
    >
      <text :style="{ fontFamily: t.font.body, fontSize: '12px', letterSpacing: t.trackingSmall, textTransform: 'uppercase', color: t.color.muted }">
        Party code
      </text>
      <text :style="{ fontFamily: t.font.display, fontSize: '46px', fontWeight: '300', lineHeight: '1', letterSpacing: '0.04em', color: t.color.blue }">
        {{ code }}
      </text>
    </view>

    <template v-if="showShare">
      <VyButton v-if="!shareReady" size="lg" block :loading="busy" :style="{ marginTop: '12px' }" label="Get invite link" @tap="$emit('refresh')" />
      <VyButton v-else color="primary" size="lg" block :style="{ marginTop: '12px' }" label="Share invite" @tap="$emit('share')" />
    </template>
  </view>
</template>
