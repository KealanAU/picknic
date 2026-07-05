<script setup lang="ts">
// A hairline section rule with an optional uppercase label — the editorial
// "Metadata Label" pattern. Replaces the earlier hand-drawn wobble with an Ash
// (#e2e8f0) rule and a Voltage Blue grotesk label, per the flat Drive Capital
// system. Name kept for API compatibility with existing call sites.
import { t } from '../theme/tokens';

withDefaults(defineProps<{ label?: string; color?: string }>(), {
  color: '#e2e8f0' /* Ash */,
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
