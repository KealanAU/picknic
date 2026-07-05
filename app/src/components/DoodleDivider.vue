<script setup lang="ts">
// Hairline section rule with an optional uppercase label. Name kept for compat.
import { t } from '../theme/tokens';

withDefaults(defineProps<{ label?: string; color?: string }>(), {
  color: '#e2e8f0',
});
</script>

<template>
  <view :style="{ display: 'flex', flexDirection: 'column', alignItems: 'center', width: '100%', gap: '11px' }">
    <text
      v-if="label"
      :style="{
        fontFamily: t.font.body,
        fontSize: '14px',
        fontWeight: '300',
        letterSpacing: t.tracking,
        textTransform: 'uppercase',
        color: t.color.blue,
      }"
    >
      {{ label }}
    </text>
    <view :style="{ width: '100%', height: '1px', backgroundColor: color }" />
  </view>
</template>
