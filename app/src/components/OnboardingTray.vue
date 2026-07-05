<script setup lang="ts">
import { computed, ref, watchEffect } from 'vue';
import { VyTray } from '@vyui/kit';
import { useAuth } from '../composables/useAuth';
import { useGuest } from '../composables/useGuest';
import ChooseView from './onboarding/ChooseView.vue';
import GuestCodeView from './onboarding/GuestCodeView.vue';
import GuestNameView from './onboarding/GuestNameView.vue';
import HostView from './onboarding/HostView.vue';

type ResolvedGuestEvent = {
  code: string;
  eventName: string;
  secret?: string;
};

const { status: authStatus, isAuthenticated } = useAuth();
const { isJoined, isReady: guestReady } = useGuest();

// Latch once both singletons finish their initial restore, so a later
// login/join "loading" phase never momentarily collapses the tray.
const booted = ref(false);
watchEffect(() => {
  const authSettled = authStatus.value === 'authenticated' || authStatus.value === 'unauthenticated';
  if (!booted.value && authSettled && guestReady.value) booted.value = true;
});
const showTray = computed(() => booted.value && !isAuthenticated.value && !isJoined.value);

const resolvedGuestEvent = ref<ResolvedGuestEvent>({
  code: '',
  eventName: '',
});

function resolveGuestEvent(payload: ResolvedGuestEvent, setView: (id: string) => void) {
  resolvedGuestEvent.value = payload;
  setView('guest-name');
}
</script>

<template>
  <VyTray
    :open="showTray"
    :dismissible="false"
    :handle="false"
    default-view="choose"
    keyboard-aware
    :ui="{
      content: 'z-[1001] !bg-cream !border-cream',
      morph: '!bg-cream',
      viewport: '!bg-cream',
      body: '!bg-cream',
    }"
  >
    <template #default="nav">
      <ChooseView @guest="nav.setView('guest-code')" @host="nav.setView('host')" />
      <HostView @back="nav.goBack()" />
      <GuestCodeView @back="nav.goBack()" @resolved="resolveGuestEvent($event, nav.setView)" />
      <GuestNameView
        :code="resolvedGuestEvent.code"
        :event-name="resolvedGuestEvent.eventName"
        :secret="resolvedGuestEvent.secret"
        @back="nav.goBack()"
      />
    </template>
  </VyTray>
</template>
