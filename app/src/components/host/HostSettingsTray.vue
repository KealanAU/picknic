<script setup lang="ts">
import { VyButton, VyTray } from '@vyui/kit';
import type { AccountInfo } from '../../api/auth';
import type { HostEvent } from '../../api/hostEvents';
import { t } from '../../theme/tokens';
import HostEventSettings from './HostEventSettings.vue';

defineProps<{
  open?: boolean;
  event?: HostEvent;
  user: AccountInfo | null;
  busy?: boolean;
}>();

const emit = defineEmits<{
  'update:open': [value: boolean];
  save: [payload: { name: string; partyDate: string }];
  logout: [];
}>();
</script>

<template>
  <VyTray
    :open="open"
    variant="floating"
    overlay
    dismissible
    handle
    keyboard-aware
    :ui="{
      content: 'z-[1001] pk-onboarding-tray-surface',
      morph: 'pk-onboarding-tray-surface',
      viewport: 'pk-onboarding-tray-surface',
      body: 'px-4 pb-5 pk-onboarding-tray-surface',
      footer: 'pk-onboarding-tray-surface',
    }"
    @update:open="emit('update:open', $event)"
  >
    <template #default>
      <view :style="{ display: 'flex', flexDirection: 'column', gap: '14px' }">
        <view :style="{ alignSelf: 'center', width: '42px', height: '5px', borderRadius: t.radius.pill, backgroundColor: '#cbd3de' }" />

        <text :style="{ fontFamily: t.font.display, fontSize: '26px', fontWeight: '300', lineHeight: '1.05', letterSpacing: t.tracking, color: t.color.ink }">
          {{ event ? 'Party settings' : 'Make a party' }}
        </text>

        <HostEventSettings :event="event" :busy="busy" @save="emit('save', $event)" />

        <view :style="{ height: '1px', backgroundColor: t.color.line }" />

        <view :style="{ display: 'flex', flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: '12px' }">
          <text :style="{ flex: 1, fontFamily: t.font.body, fontSize: '13px', letterSpacing: t.tracking, color: t.color.muted }">
            {{ user?.email }}
          </text>
          <VyButton size="sm" variant="ghost" @tap="emit('logout')">
            Log out
          </VyButton>
        </view>
      </view>
    </template>
  </VyTray>
</template>
