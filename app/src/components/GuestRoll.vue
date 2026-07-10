<script setup lang="ts">
// Guest main screen — guests land straight in the camera; closing it shows a
// slim home with the header plus a session line underneath. Party name/date
// come from the public event lookup; the screen works if that fails.
import { onMounted, ref } from 'vue';
import { VyButton, VyIcon } from '@vyui/kit';
import { getEvent } from '../api/events';
import type { GuestSession } from '../api/guest';
import { t } from '../theme/tokens';
import CameraScreen from './CameraScreen.vue';
import PartyHeader from './PartyHeader.vue';

const props = defineProps<{
  session: GuestSession | null;
}>();

defineEmits<{
  leave: [];
}>();

const partyName = ref('');
const partyDate = ref('');
const cameraOpen = ref(true);
const fabPressed = ref(false);

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
    <view class="safe-top" :style="{ width: '100%', display: 'flex', flexDirection: 'column', paddingLeft: '20px', paddingRight: '20px', paddingBottom: '120px' }">
      <PartyHeader kicker="You're on the roll" :title="partyName || 'Your party'" :date="partyDate" />

      <text
        v-if="session"
        :style="{ marginTop: '4px', fontFamily: t.font.body, fontSize: '13px', letterSpacing: t.trackingSmall, color: t.color.muted }"
      >
        Snapping as {{ session.name }} · Party code {{ session.code }}
      </text>

      <view :style="{ display: 'flex', flexDirection: 'row', justifyContent: 'flex-end', marginTop: '10px' }">
        <VyButton size="sm" variant="ghost" label="Leave the roll" @tap="$emit('leave')" />
      </view>
    </view>

    <view
      v-if="session && !cameraOpen"
      :style="{
        position: 'fixed',
        bottom: '28px',
        left: '50%',
        transform: fabPressed ? 'translateX(-50%) scale(0.92)' : 'translateX(-50%)',
        zIndex: 900,
        width: '68px',
        height: '68px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        borderRadius: t.radius.pill,
        backgroundColor: t.color.blue,
        borderWidth: '3px',
        borderStyle: 'solid',
        borderColor: '#ffffff',
        boxShadow: t.shadow.lift,
      }"
      @tap="cameraOpen = true"
      @touchstart="fabPressed = true"
      @touchend="fabPressed = false"
      @touchcancel="fabPressed = false"
    >
      <VyIcon name="lucide:camera" :style="{ width: '30px', height: '30px', color: '#ffffff' }" />
    </view>

    <CameraScreen
      v-if="cameraOpen && session"
      :event-id="session.eventId"
      :token="session.token"
      :title="partyName || 'Your party'"
      @close="cameraOpen = false"
    />
  </view>
</template>
