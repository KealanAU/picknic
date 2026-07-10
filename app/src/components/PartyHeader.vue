<script setup lang="ts">
// Shared screen header for host and guest roll views; only hosts get the
// settings cog.
import { VyIcon } from '@vyui/kit';
import { t } from '../theme/tokens';

withDefaults(
  defineProps<{
    kicker: string;
    title: string;
    date?: string;
    settings?: boolean;
  }>(),
  { date: '', settings: false },
);

defineEmits<{
  settings: [];
}>();
</script>

<template>
  <!-- Margin spacing instead of gap: the v-if date/settings children leave
       fragment anchors that container gap would treat as children. -->
  <view :style="{ display: 'flex', flexDirection: 'row', alignItems: 'flex-start', justifyContent: 'space-between' }">
    <view :style="{ flex: 1, display: 'flex', flexDirection: 'column' }">
      <text :style="{ fontFamily: t.font.body, fontSize: '12px', letterSpacing: t.trackingSmall, textTransform: 'uppercase', color: t.color.muted }">
        {{ kicker }}
      </text>
      <text :style="{ marginTop: '4px', fontFamily: t.font.display, fontSize: '34px', fontWeight: '300', lineHeight: '1', letterSpacing: t.tracking, color: t.color.ink }">
        {{ title }}
      </text>
      <text v-if="date" :style="{ marginTop: '4px', fontFamily: t.font.body, fontSize: '13px', letterSpacing: t.trackingSmall, color: t.color.muted }">
        {{ date }}
      </text>
    </view>
    <view
      v-if="settings"
      :style="{
        marginLeft: '14px',
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
      <VyIcon name="streamline-freehand:settings-cog" :style="{ width: '20px', height: '20px', color: t.color.ink }" />
    </view>
  </view>
</template>
