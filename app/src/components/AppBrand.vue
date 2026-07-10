<script setup lang="ts">
import { t } from '../theme/tokens';
</script>

<template>
  <view :style="{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '11px' }">
    <text
      :style="{
        fontFamily: t.font.display,
        fontSize: '62px',
        fontWeight: '400',
        lineHeight: '1.2',
        paddingTop: '8px',
        letterSpacing: t.tracking,
        color: t.color.blue,
      }"
    >
      Picknic
    </text>
    <text
      :style="{
        fontFamily: t.font.body,
        fontSize: '14px',
        fontWeight: '300',
        letterSpacing: t.trackingSmall,
        textTransform: 'uppercase',
        color: t.color.blue,
      }"
    >
      One roll · revealed at the end
    </text>
  </view>
</template>
