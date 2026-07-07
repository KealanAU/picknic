<script setup lang="ts">
// Guest main screen — same shell as the host roll (PartyHeader + PartyCodeCard),
// minus the settings cog and share actions. Party name/date come from the
// public event lookup; the screen still works if that fails.
import { onMounted, ref } from 'vue';
import { VyButton, VyCard } from '@vyui/kit';
import { getEvent } from '../api/events';
import type { GuestSession } from '../api/guest';
import { t } from '../theme/tokens';
import PartyCodeCard from './PartyCodeCard.vue';
import PartyHeader from './PartyHeader.vue';

const props = defineProps<{
  session: GuestSession | null;
}>();

defineEmits<{
  leave: [];
}>();

const partyName = ref('');
const partyDate = ref('');

onMounted(async () => {
  if (!props.session) return;
  try {
    const ev = await getEvent(props.session.code);
    partyName.value = ev.name;
    partyDate.value = new Date(ev.uploadClosesAt).toLocaleDateString(undefined, {
      weekday: 'long',
      month: 'short',
      day: 'numeric',
    });
  } catch {
    // Header falls back to the generic title.
  }
});
</script>

<template>
  <view :style="{ width: '100%', height: '100%' }">
    <view :style="{ width: '100%', display: 'flex', flexDirection: 'column', padding: '24px 20px 40px' }">
      <PartyHeader kicker="You're on the roll" :title="partyName || 'Your party'" :date="partyDate" />

      <VyCard :style="{ width: '100%', marginTop: '14px' }">
        <PartyCodeCard :code="session?.code ?? ''" />
      </VyCard>

      <view :style="{ display: 'flex', flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: '12px', marginTop: '14px' }">
        <text :style="{ flex: 1, fontFamily: t.font.body, fontSize: '13px', letterSpacing: t.tracking, color: t.color.muted }">
          Snapping as {{ session?.name }}
        </text>
        <VyButton size="sm" variant="ghost" @tap="$emit('leave')">
          Leave the roll
        </VyButton>
      </view>
    </view>
  </view>
</template>
