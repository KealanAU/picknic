<script setup lang="ts">
// A native instant-print frame: renders a photo inside a Polaroid/Instax-style
// paper card. Geometry mirrors the server-side PrintFramer so the in-app preview
// matches the developed JPEG. Works with any image URL (the reveal SAS url, or a
// data: URI from a fresh capture).
import { computed } from 'vue';

const props = withDefaults(
  defineProps<{
    src?: string;
    caption?: string;
    /** Print style id: none | polaroid | instax_mini | instax_square | instax_wide */
    print?: string;
    /** Card width in px. Borders scale from this. */
    width?: number;
  }>(),
  { print: 'polaroid', width: 300 },
);

interface Layout {
  aspect: number; // photo window w/h
  side: number;
  top: number;
  bottom: number; // fractions of card width
}

const LAYOUTS: Record<string, Layout> = {
  none: { aspect: 1, side: 0, top: 0, bottom: 0 },
  polaroid: { aspect: 1, side: 0.05, top: 0.05, bottom: 0.2 },
  instax_mini: { aspect: 46 / 62, side: 0.07, top: 0.07, bottom: 0.2 },
  instax_square: { aspect: 1, side: 0.07, top: 0.07, bottom: 0.2 },
  instax_wide: { aspect: 99 / 62, side: 0.05, top: 0.05, bottom: 0.16 },
};

const layout = computed(() => LAYOUTS[props.print] ?? LAYOUTS.polaroid);
const framed = computed(() => props.print !== 'none');

const side = computed(() => Math.round(props.width * layout.value.side));
const top = computed(() => Math.round(props.width * layout.value.top));
const bottom = computed(() => Math.round(props.width * layout.value.bottom));
const windowW = computed(() => props.width - 2 * side.value);
const windowH = computed(() => Math.round(windowW.value / layout.value.aspect));

const cardStyle = computed(() => ({
  width: `${props.width}px`,
  backgroundColor: framed.value ? '#fff8f1' : 'transparent',
  paddingLeft: `${side.value}px`,
  paddingRight: `${side.value}px`,
  paddingTop: `${top.value}px`,
  paddingBottom: `${bottom.value}px`,
  borderRadius: '0px',
  borderWidth: framed.value ? '1px' : '0px',
  borderStyle: 'solid' as const,
  borderColor: '#e2e8f0',
  display: 'flex',
  flexDirection: 'column' as const,
  alignItems: 'center' as const,
}));

const windowStyle = computed(() => ({
  width: `${windowW.value}px`,
  height: `${windowH.value}px`,
  backgroundColor: '#e2e8f0',
  overflow: 'hidden' as const,
  display: 'flex',
  alignItems: 'center' as const,
  justifyContent: 'center' as const,
}));

const captionStyle = computed(() => ({
  marginTop: `${Math.round(props.width * 0.06)}px`,
  fontSize: `${Math.max(12, Math.round(props.width * 0.055))}px`,
  fontFamily: "'Founders Grotesk', Inter, ui-sans-serif, system-ui, sans-serif",
  letterSpacing: '-0.02em',
  color: '#000000',
  textAlign: 'center' as const,
}));
</script>

<template>
  <view :style="cardStyle">
    <view :style="windowStyle">
      <image v-if="src" :src="src" mode="aspectFill" :style="{ width: '100%', height: '100%' }" />
      <text v-else :style="{ fontSize: '13px', letterSpacing: '-0.02em', color: '#6b7480' }">No photo yet</text>
    </view>
    <text v-if="framed && caption" :style="captionStyle">{{ caption }}</text>
  </view>
</template>
