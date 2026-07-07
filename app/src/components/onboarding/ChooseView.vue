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
      <text :style="subStyle">One roll, revealed at the end.</text>
    </view>
    <view :style="{ display: 'flex', flexDirection: 'column', gap: '10px' }">
      <VyButton color="primary" size="xl" block leading-icon="lucide:ticket" @tap="$emit('guest')">
        Join an event
      </VyButton>
      <VyButton variant="outline" size="xl" block leading-icon="lucide:calendar-plus" @tap="$emit('host')">
        Host an event
      </VyButton>
    </view>
  </VyTrayView>
</template>
