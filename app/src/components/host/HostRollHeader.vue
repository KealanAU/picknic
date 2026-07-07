<script setup lang="ts">
import { computed } from 'vue';
import { VyIcon } from '@vyui/kit';
import type { HostEvent } from '../../api/hostEvents';
import { t } from '../../theme/tokens';

const props = defineProps<{
  event?: HostEvent;
}>();

defineEmits<{
  settings: [];
}>();

const dateLabel = computed(() => {
  if (!props.event) return 'No party yet';
  return new Date(props.event.uploadOpensAt).toLocaleDateString(undefined, {
    weekday: 'long',
    month: 'short',
    day: 'numeric',
  });
});
</script>

<template>
  <view :style="{ display: 'flex', flexDirection: 'row', alignItems: 'flex-start', justifyContent: 'space-between', gap: '14px' }">
    <view :style="{ flex: 1, display: 'flex', flexDirection: 'column', gap: '4px' }">
      <text :style="{ fontFamily: t.font.body, fontSize: '12px', letterSpacing: t.tracking, textTransform: 'uppercase', color: t.color.muted }">
        Host roll
      </text>
      <text :style="{ fontFamily: t.font.display, fontSize: '34px', fontWeight: '300', lineHeight: '1', letterSpacing: t.tracking, color: t.color.ink }">
        {{ event?.name || 'New party' }}
      </text>
      <text :style="{ fontFamily: t.font.body, fontSize: '13px', letterSpacing: t.tracking, color: t.color.muted }">
        {{ dateLabel }}
      </text>
    </view>
    <view
      :style="{
        width: '40px',
        height: '40px',
        flexShrink: 0,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        borderRadius: t.radius.pill,
        backgroundColor: '#ffffff',
        borderWidth: '1px',
        borderStyle: 'solid',
        borderColor: t.color.line,
      }"
      @tap="$emit('settings')"
    >
      <VyIcon name="lucide:settings-2" :style="{ width: '20px', height: '20px', color: t.color.ink }" />
    </view>
  </view>
</template>
