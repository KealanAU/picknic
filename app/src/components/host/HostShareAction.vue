<script setup lang="ts">
import { VyIcon } from '@vyui/kit';
import { t } from '../../theme/tokens';

defineProps<{
  icon: string;
  label: string;
  color?: string;
  background?: string;
}>();

defineEmits<{
  press: [];
}>();
</script>

<template>
  <view
    :style="{ width: '64px', flexShrink: 0, display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '6px', marginRight: '8px' }"
    @tap="$emit('press')"
  >
    <view
      :style="{
        width: '56px',
        height: '56px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        borderRadius: t.radius.pill,
        backgroundColor: background || '#ffffff',
        borderWidth: background ? '0' : '1px',
        borderStyle: 'solid',
        borderColor: t.color.line,
        color: color || t.color.ink,
      }"
    >
      <VyIcon :name="icon" :size="26" :color="color || t.color.ink" />
    </view>
    <text :style="{ width: '64px', textAlign: 'center', fontFamily: t.font.body, fontSize: '12px', lineHeight: '1.1', letterSpacing: t.trackingSmall, color: t.color.ink }">
      {{ label }}
    </text>
  </view>
</template>
