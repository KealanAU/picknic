<script setup lang="ts">
// Flat cream panel with an Ash hairline edge. tilt/tape are inert, kept only so
// existing call sites still type-check.
import { computed } from 'vue';
import { t } from '../theme/tokens';

const props = withDefaults(
  defineProps<{
    tilt?: number;
    tape?: boolean;
    padding?: number;
  }>(),
  { tilt: 0, tape: false, padding: 14 },
);

const cardStyle = computed(() => ({
  display: 'flex',
  flexDirection: 'column' as const,
  backgroundColor: t.color.card,
  borderRadius: t.radius.card,
  padding: `${props.padding}px`,
  borderWidth: '1px',
  borderStyle: 'solid' as const,
  borderColor: t.color.line,
}));
</script>

<template>
  <view :style="cardStyle">
    <slot />
  </view>
</template>
