<script setup lang="ts">
// Primary action: an outlined Voltage Blue pill. `variant` is inert — both render
// outlined (filled buttons are disallowed in this system), kept for compat.
import { computed, ref } from 'vue';
import { t } from '../theme/tokens';

const props = withDefaults(
  defineProps<{
    variant?: 'solid' | 'soft';
    disabled?: boolean;
    block?: boolean;
  }>(),
  { variant: 'solid', disabled: false, block: false },
);

const emit = defineEmits<{ (e: 'tap'): void }>();

const pressed = ref(false);

const rootStyle = computed(() => ({
  display: 'flex',
  flexDirection: 'row' as const,
  alignItems: 'center',
  justifyContent: 'center',
  alignSelf: props.block ? 'stretch' : 'flex-start',
  paddingLeft: '43px',
  paddingRight: '43px',
  paddingTop: '14px',
  paddingBottom: '14px',
  backgroundColor: 'transparent',
  borderRadius: t.radius.pill,
  borderWidth: '1.5px',
  borderStyle: 'solid' as const,
  borderColor: t.color.blue,
  opacity: props.disabled ? 0.4 : pressed.value ? 0.6 : 1,
}));

const textStyle = computed(() => ({
  fontFamily: t.font.body,
  fontSize: '16px',
  fontWeight: '300' as const,
  letterSpacing: t.tracking,
  textTransform: 'uppercase' as const,
  color: t.color.blue,
}));

function onTap() {
  if (!props.disabled) emit('tap');
}
</script>

<template>
  <view
    :style="rootStyle"
    @tap="onTap"
    @touchstart="pressed = true"
    @touchend="pressed = false"
    @touchcancel="pressed = false"
  >
    <text :style="textStyle"><slot /></text>
  </view>
</template>
