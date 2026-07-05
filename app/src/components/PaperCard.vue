<script setup lang="ts">
// A flat cream panel: square corners, an Ash hairline edge, no shadow and no
// tilt — separation comes from the border and whitespace alone. The base
// surface of the Drive Capital "Summer Drive" look. The `tilt` / `tape` props
// are retained for API compatibility but are intentionally inert: the editorial
// system is strictly flat and un-decorated.
import { computed } from 'vue';
import { t } from '../theme/tokens';

const props = withDefaults(
  defineProps<{
    /** Retained for API compatibility; no-op in the flat editorial system. */
    tilt?: number;
    /** Retained for API compatibility; no-op in the flat editorial system. */
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
