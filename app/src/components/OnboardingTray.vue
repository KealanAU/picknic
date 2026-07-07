<script setup lang="ts">
import { ref } from 'vue';
import { VyTray } from '@vyui/kit';
import ChooseView from './onboarding/ChooseView.vue';
import GuestCodeView from './onboarding/GuestCodeView.vue';
import GuestNameView from './onboarding/GuestNameView.vue';
import HostView from './onboarding/HostView.vue';

type ResolvedGuestEvent = {
  code: string;
  eventName: string;
  secret?: string;
};

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
    :open="true"
    :dismissible="false"
    :handle="false"
    default-view="choose"
    keyboard-aware
    :ui="{
      content: 'z-[1001] pk-onboarding-tray-surface pk-onboarding-tray-content',
      morph: 'pk-onboarding-tray-surface pk-onboarding-tray-scroll-shell',
      viewport: 'pk-onboarding-tray-surface pk-onboarding-tray-scroll-shell',
      body: 'pk-onboarding-tray-surface pk-onboarding-tray-scroll-body',
      footer: 'pk-onboarding-tray-surface',
    }"
  >
    <template #default="nav">
      <scroll-view scroll-orientation="vertical" :enable-scroll="true" class="pk-onboarding-scroll-view">
        <view class="pk-onboarding-scroll-content">
          <ChooseView @guest="nav.setView('guest-code')" @host="nav.setView('host')" />
          <HostView @back="nav.goBack()" />
          <GuestCodeView @back="nav.goBack()" @resolved="resolveGuestEvent($event, nav.setView)" />
          <GuestNameView
            :code="resolvedGuestEvent.code"
            :event-name="resolvedGuestEvent.eventName"
            :secret="resolvedGuestEvent.secret"
            @back="nav.goBack()"
          />
        </view>
      </scroll-view>
    </template>
  </VyTray>
</template>
