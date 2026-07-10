<script setup lang="ts">
import { VyButton, VyTrayView } from '@vyui/kit';
import { subStyle, titleStyle } from './styles';

defineEmits<{
  guest: [];
  host: [];
}>();
</script>

<template>
  <VyTrayView id="choose">
    <view :style="{ display: 'flex', flexDirection: 'column', gap: '4px', marginBottom: '14px' }">
      <text :style="titleStyle">Welcome to Picknic</text>
      <text :style="subStyle">Snapping at a party, or throwing one?</text>
    </view>
    <view :style="{ display: 'flex', flexDirection: 'column', gap: '10px' }">
      <VyButton color="primary" size="xl" block leading-icon="lucide:ticket" label="Join a party" @tap="$emit('guest')" />
      <VyButton variant="outline" size="xl" block leading-icon="lucide:calendar-plus" label="Host a party" @tap="$emit('host')" />
    </view>
  </VyTrayView>
</template>
