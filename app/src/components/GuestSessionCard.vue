<script setup lang="ts">
import { VyButton, VyCard } from '@vyui/kit';
import type { GuestSession } from '../api/guest';
import { t } from '../theme/tokens';

defineProps<{
  session: GuestSession | null;
}>();

defineEmits<{
  leave: [];
}>();
</script>

<template>
  <VyCard :style="{ width: '100%' }">
    <text
      :style="{ fontFamily: t.font.display, fontSize: '28px', fontWeight: '400', letterSpacing: t.tracking, color: t.color.ink }"
    >
      You're on the roll
    </text>
    <text
      :style="{ fontFamily: t.font.body, fontSize: '16px', letterSpacing: t.tracking, color: t.color.muted, marginTop: '7px' }"
    >
      {{ session?.name }} · room {{ session?.code }}
    </text>
    <VyButton variant="soft" block :style="{ marginTop: '20px' }" @tap="$emit('leave')">
      Leave roll
    </VyButton>
  </VyCard>
</template>
