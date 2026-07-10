<script setup lang="ts">
import { VyButton, VyTray } from '@vyui/kit';
import type { AccountInfo } from '../../api/auth';
import type { HostEvent, HostGuest } from '../../api/hostEvents';
import { t } from '../../theme/tokens';
import HostEventSettings from './HostEventSettings.vue';
import HostGuestList from './HostGuestList.vue';

defineProps<{
  open?: boolean;
  event?: HostEvent;
  user: AccountInfo | null;
  guests?: HostGuest[];
  busy?: boolean;
}>();

const emit = defineEmits<{
  'update:open': [value: boolean];
  save: [payload: { name: string; partyStart: string; partyEnd: string }];
  refreshGuests: [];
  removeGuest: [guest: HostGuest];
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
      content: 'z-[1001] pk-tray-radius pk-onboarding-tray-surface pk-onboarding-tray-content',
      morph: 'pk-onboarding-tray-surface pk-onboarding-tray-scroll-shell',
      viewport: 'pk-onboarding-tray-surface pk-onboarding-tray-scroll-shell',
      body: 'px-4 pb-5 pk-onboarding-tray-surface pk-onboarding-tray-scroll-body',
      footer: 'pk-onboarding-tray-surface',
    }"
    @update:open="emit('update:open', $event)"
  >
    <template #default>
      <scroll-view scroll-orientation="vertical" :enable-scroll="true" class="pk-onboarding-scroll-view">
      <!-- Margin spacing instead of gap: the v-if guest section leaves a fragment
           anchor that container gap would treat as a child. -->
      <view class="pk-onboarding-scroll-content" :style="{ display: 'flex', flexDirection: 'column' }">
        <text :style="{ fontFamily: t.font.display, fontSize: '26px', fontWeight: '300', lineHeight: '1.05', letterSpacing: t.tracking, color: t.color.ink }">
          {{ event ? 'Party settings' : 'Make a party' }}
        </text>

        <view :style="{ marginTop: '8px' }">
          <HostEventSettings :event="event" :busy="busy" @save="emit('save', $event)" />
        </view>

        <view v-if="event" :style="{ marginTop: '14px' }">
          <view :style="{ height: '1px', backgroundColor: t.color.line }" />
          <view :style="{ marginTop: '14px' }">
            <HostGuestList
              :guests="guests ?? []"
              :busy="busy"
              @refresh="emit('refreshGuests')"
              @remove="emit('removeGuest', $event)"
            />
          </view>
        </view>

        <view :style="{ marginTop: '14px', height: '1px', backgroundColor: t.color.line }" />

        <view :style="{ marginTop: '14px', display: 'flex', flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: '12px' }">
          <text :style="{ flex: 1, fontFamily: t.font.body, fontSize: '13px', letterSpacing: t.trackingSmall, color: t.color.muted }">
            {{ user?.email }}
          </text>
          <VyButton size="sm" variant="ghost" @tap="emit('logout')">
            Log out
          </VyButton>
        </view>
      </view>
      </scroll-view>
    </template>
  </VyTray>
</template>
