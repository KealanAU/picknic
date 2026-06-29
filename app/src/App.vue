<script setup lang="ts">
import { ref } from 'vue';

const code = ref('');
const joining = ref(false);

function join() {
  if (!code.value) return;
  joining.value = true;
}
</script>

<template>
  <view :style="{ width: '100%', height: '100%', backgroundColor: '#fdf6ec' }">
    <view
      :style="{
        flex: 1,
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '24px',
        gap: '24px',
      }"
    >
      <view :style="{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '6px' }">
        <text :style="{ fontSize: '40px' }">🧺</text>
        <text :style="{ fontSize: '28px', fontWeight: '700', color: '#2b2118' }">Picknic</text>
        <text :style="{ fontSize: '14px', color: '#8a7a66' }">One roll. Revealed at the end.</text>
      </view>

      <VyCard :style="{ width: '100%' }">
        <VyFormField label="Event code">
          <VyInput v-model="code" placeholder="e.g. SARAH-MAX" leading-icon="i-lucide-ticket" />
        </VyFormField>

        <VyButton
          color="primary"
          block
          leading-icon="i-lucide-camera"
          :loading="joining"
          :disabled="!code"
          @click="join"
        >
          Join &amp; start snapping
        </VyButton>
      </VyCard>
    </view>
  </view>
</template>
