<script setup lang="ts">
import { computed } from 'vue';
import { t } from '../theme/tokens';

const props = withDefaults(
  defineProps<{
    modelValue: boolean;
    size?: 'sm' | 'md';
  }>(),
  { size: 'md' },
);

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
}>();

const boxSize = computed(() => (props.size === 'sm' ? '17px' : '18px'));
const checkFontSize = computed(() => (props.size === 'sm' ? '12px' : '13px'));
const rowGap = computed(() => (props.size === 'sm' ? '9px' : '10px'));
const rowHeight = computed(() => (props.size === 'sm' ? '26px' : '28px'));

function toggle() {
  emit('update:modelValue', !props.modelValue);
}
</script>

<template>
  <view
    :style="{ display: 'flex', flexDirection: 'row', alignItems: 'center', gap: rowGap, minHeight: rowHeight }"
    @tap="toggle"
  >
    <view
      :style="{
        width: boxSize,
        height: boxSize,
        border: `1px solid ${modelValue ? t.color.blue : t.color.line}`,
        background: modelValue ? t.color.blue : 'transparent',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
      }"
    >
      <text v-if="modelValue" :style="{ color: '#fff', fontSize: checkFontSize, lineHeight: boxSize }">✓</text>
    </view>
    <text :style="{ fontFamily: t.font.body, fontSize: '14px', letterSpacing: t.tracking, color: t.color.inkSoft }">
      Remember me
    </text>
  </view>
</template>
