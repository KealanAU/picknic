<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { VyButton, VyCard } from '@vyui/kit';
import type { AccountInfo } from '../../api/auth';
import {
  createHostEvent,
  getEventQr,
  listGuests,
  listHostEvents,
  removeGuest,
  updateHostEvent,
  type EventQr,
  type HostEvent,
  type HostGuest,
} from '../../api/hostEvents';
import { isApiError } from '../../api/http';
import { t } from '../../theme/tokens';
import HostEventSettings from './HostEventSettings.vue';
import HostGuestList from './HostGuestList.vue';
import HostSharePanel from './HostSharePanel.vue';

const props = defineProps<{
  user: AccountInfo | null;
}>();

const emit = defineEmits<{
  logout: [];
}>();

const hostEvents = ref<HostEvent[]>([]);
const guests = ref<HostGuest[]>([]);
const qr = ref<EventQr | null>(null);
const error = ref<string | null>(null);
const busy = ref(false);
const copied = ref(false);

const latestEvent = computed(() => hostEvents.value[0]);

function messageFor(e: unknown): string {
  return isApiError(e) ? e.message : 'Request failed';
}

function windowFromPartyDate(partyDate: string) {
  const opens = new Date(`${partyDate}T00:00:00`);
  const closes = new Date(`${partyDate}T23:59:00`);
  const reveal = new Date(closes);
  reveal.setHours(reveal.getHours() + 1);

  return {
    uploadOpensAt: opens.toISOString(),
    uploadClosesAt: closes.toISOString(),
    revealAt: reveal.toISOString(),
  };
}

async function loadEvents() {
  hostEvents.value = await listHostEvents();
}

async function loadPartyDetails() {
  if (!latestEvent.value) {
    guests.value = [];
    qr.value = null;
    return;
  }
  const eventId = latestEvent.value.id;
  const loadedGuests = await listGuests(eventId);
  guests.value = loadedGuests;
  try {
    qr.value = await getEventQr(eventId);
  } catch (e) {
    qr.value = null;
    if (isApiError(e) && e.status === 409) {
      error.value = e.message;
      return;
    }
    throw e;
  }
}

async function refreshAll() {
  busy.value = true;
  error.value = null;
  try {
    await loadEvents();
    await loadPartyDetails();
  } catch (e) {
    error.value = messageFor(e);
  } finally {
    busy.value = false;
  }
}

async function saveSettings(payload: { name: string; partyDate: string }) {
  busy.value = true;
  error.value = null;
  try {
    const body = {
      name: payload.name,
      ...windowFromPartyDate(payload.partyDate),
    };
    if (latestEvent.value) await updateHostEvent(latestEvent.value.id, body);
    else await createHostEvent(body);
    await refreshAll();
  } catch (e) {
    error.value = messageFor(e);
  } finally {
    busy.value = false;
  }
}

async function refreshShare() {
  if (!latestEvent.value) return;
  busy.value = true;
  error.value = null;
  try {
    qr.value = await getEventQr(latestEvent.value.id);
  } catch (e) {
    if (isApiError(e) && e.status === 409) qr.value = null;
    error.value = messageFor(e);
  } finally {
    busy.value = false;
  }
}

async function refreshGuests() {
  if (!latestEvent.value) return;
  busy.value = true;
  error.value = null;
  try {
    guests.value = await listGuests(latestEvent.value.id);
  } catch (e) {
    error.value = messageFor(e);
  } finally {
    busy.value = false;
  }
}

async function removePartyGuest(guest: HostGuest) {
  if (!latestEvent.value) return;
  busy.value = true;
  error.value = null;
  try {
    await removeGuest(latestEvent.value.id, guest.id);
    await refreshGuests();
  } catch (e) {
    error.value = messageFor(e);
  } finally {
    busy.value = false;
  }
}

async function copyShareLink() {
  if (!qr.value?.joinUrl) return;
  copied.value = false;
  const clipboard = globalThis.navigator?.clipboard;
  if (!clipboard) return;
  await clipboard.writeText(qr.value.joinUrl);
  copied.value = true;
}

watch(latestEvent, () => {
  void loadPartyDetails();
});

onMounted(() => {
  void refreshAll();
});
</script>

<template>
  <scroll-view :style="{ width: '100%', height: '100%' }">
    <view :style="{ width: '100%', display: 'flex', flexDirection: 'column', gap: '16px', paddingBottom: '32px' }">
      <view :style="{ display: 'flex', flexDirection: 'row', alignItems: 'flex-start', justifyContent: 'space-between', gap: '14px' }">
        <view :style="{ flex: 1, display: 'flex', flexDirection: 'column', gap: '4px' }">
          <text :style="{ fontFamily: t.font.body, fontSize: '12px', letterSpacing: t.tracking, textTransform: 'uppercase', color: t.color.muted }">
            Host roll
          </text>
          <text :style="{ fontFamily: t.font.display, fontSize: '34px', fontWeight: '300', lineHeight: '1', letterSpacing: t.tracking, color: t.color.ink }">
            {{ latestEvent?.name || 'New party' }}
          </text>
          <text :style="{ fontFamily: t.font.body, fontSize: '13px', letterSpacing: t.tracking, color: t.color.muted }">
            {{ props.user?.email }}
          </text>
        </view>
        <VyButton size="sm" variant="ghost" @click="emit('logout')">
          Log out
        </VyButton>
      </view>

      <text v-if="error" :style="{ fontFamily: t.font.body, fontSize: '14px', letterSpacing: t.tracking, color: t.color.danger }">
        {{ error }}
      </text>

      <VyCard :style="{ width: '100%' }">
        <HostEventSettings :event="latestEvent" :busy="busy" @save="saveSettings" />
      </VyCard>

      <VyCard v-if="latestEvent" :style="{ width: '100%' }">
        <HostSharePanel :event="latestEvent" :qr="qr" :busy="busy" :copied="copied" @refresh="refreshShare" @copy="copyShareLink" />
      </VyCard>

      <VyCard v-if="latestEvent" :style="{ width: '100%' }">
        <HostGuestList :guests="guests" :busy="busy" @refresh="refreshGuests" @remove="removePartyGuest" />
      </VyCard>
    </view>
  </scroll-view>
</template>
