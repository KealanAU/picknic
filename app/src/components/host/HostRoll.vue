<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { VyButton, VyCard, VyIcon } from '@vyui/kit';
import type { AccountInfo } from '../../api/auth';
import {
  createHostEvent,
  getCameraPass,
  getEventQr,
  listGuests,
  listHostEvents,
  removeGuest,
  updateHostEvent,
  type CameraPass,
  type EventQr,
  type HostEvent,
  type HostGuest,
} from '../../api/hostEvents';
import { isApiError, platformFetch } from '../../api/http';
import { t } from '../../theme/tokens';
import CameraScreen from '../CameraScreen.vue';
import PartyCodeCard from '../PartyCodeCard.vue';
import PartyHeader from '../PartyHeader.vue';
import HostSettingsTray from './HostSettingsTray.vue';
import HostShareTray from './HostShareTray.vue';

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
const shareTrayOpen = ref(false);
const settingsTrayOpen = ref(false);
const cameraOpen = ref(false);
const cameraPass = ref<CameraPass | null>(null);

const latestEvent = computed(() => hostEvents.value[0]);

const dateLabel = computed(() => {
  if (!latestEvent.value) return 'No party yet';
  const opens = new Date(latestEvent.value.uploadOpensAt);
  const closes = new Date(latestEvent.value.uploadClosesAt);
  const singleDay =
    opens.getFullYear() === closes.getFullYear() &&
    opens.getMonth() === closes.getMonth() &&
    opens.getDate() === closes.getDate();
  if (singleDay) {
    return opens.toLocaleDateString(undefined, { weekday: 'long', month: 'short', day: 'numeric' });
  }
  const short: Intl.DateTimeFormatOptions = { weekday: 'short', month: 'short', day: 'numeric' };
  return `${opens.toLocaleDateString(undefined, short)} – ${closes.toLocaleDateString(undefined, short)}`;
});

function messageFor(e: unknown): string {
  return isApiError(e) ? e.message : 'Something went wrong. Try again.';
}

function windowFromPartyDates(partyStart: string, partyEnd: string) {
  const opens = new Date(`${partyStart}T00:00:00`);
  const closes = new Date(`${partyEnd}T23:59:00`);
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

async function saveSettings(payload: { name: string; partyStart: string; partyEnd: string }) {
  busy.value = true;
  error.value = null;
  try {
    const body = {
      name: payload.name,
      ...windowFromPartyDates(payload.partyStart, payload.partyEnd),
    };
    if (latestEvent.value) await updateHostEvent(latestEvent.value.id, body);
    else await createHostEvent(body);
    await refreshAll();
    settingsTrayOpen.value = false;
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
  error.value = null;
  try {
    await copyText(qr.value.joinUrl);
    copied.value = true;
  } catch {
    error.value = "Couldn't copy the link on this device. Try sharing it instead.";
  }
}

async function copyText(value: string): Promise<void> {
  const clipboard = globalThis.navigator?.clipboard;
  if (clipboard) {
    await clipboard.writeText(value);
    return;
  }

  if (typeof document === 'undefined') throw new Error('Clipboard unavailable');

  const textarea = document.createElement('textarea');
  textarea.value = value;
  textarea.setAttribute('readonly', 'true');
  textarea.style.position = 'fixed';
  textarea.style.left = '-9999px';
  document.body.appendChild(textarea);
  textarea.select();
  const ok = document.execCommand('copy');
  document.body.removeChild(textarea);
  if (!ok) throw new Error('Copy command failed');
}

function shareText() {
  if (!latestEvent.value || !qr.value?.joinUrl) return '';
  return `Join ${latestEvent.value.name} on Picknic — party code ${latestEvent.value.code}. ${qr.value.joinUrl}`;
}

function openExternal(url: string): void {
  const opener = (globalThis as typeof globalThis & { open?: (url?: string, target?: string, features?: string) => unknown }).open;
  if (opener) {
    opener(url, '_blank', 'noopener,noreferrer');
    return;
  }

  if (typeof window !== 'undefined') window.location.href = url;
}

async function shareInviteImage(imageUrl: string) {
  if (!qr.value?.joinUrl) return;
  error.value = null;
  const text = shareText();
  const nav = globalThis.navigator;

  try {
    const blob = await (await platformFetch(imageUrl)).blob();
    const file = new File([blob], 'picknic-invite.svg', { type: 'image/svg+xml' });
    if (nav?.canShare?.({ files: [file] }) && nav.share) {
      await nav.share({
        title: latestEvent.value?.name ?? 'Picknic invite',
        text,
        url: qr.value.joinUrl,
        files: [file],
      });
      return;
    }
    if (nav?.share) {
      await nav.share({
        title: latestEvent.value?.name ?? 'Picknic invite',
        text,
        url: qr.value.joinUrl,
      });
      return;
    }
    openExternal(imageUrl);
  } catch {
    error.value = "Sharing isn't available on this device. Copy the link instead.";
  }
}

type SharePlatform = 'messages' | 'whatsapp' | 'facebook' | 'messenger' | 'x' | 'snapchat' | 'threads';

async function sharePlatform(platform: SharePlatform) {
  if (!qr.value?.joinUrl) return;
  const url = qr.value.joinUrl;
  const text = shareText();

  if (platform === 'snapchat') {
    try {
      await copyText(text);
      copied.value = true;
    } catch {
      // Snapchat does not expose a reliable web prefill target; keep the link copied.
    }
    openExternal('https://www.snapchat.com/');
    return;
  }

  const targets = {
    messages: `sms:&body=${encodeURIComponent(text)}`,
    whatsapp: `https://wa.me/?text=${encodeURIComponent(text)}`,
    facebook: `https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(url)}`,
    messenger: `fb-messenger://share?link=${encodeURIComponent(url)}`,
    x: `https://twitter.com/intent/tweet?text=${encodeURIComponent(text)}`,
    threads: `https://www.threads.net/intent/post?text=${encodeURIComponent(text)}`,
  };
  openExternal(targets[platform]);
}

function openShareTray() {
  if (qr.value?.joinUrl) shareTrayOpen.value = true;
}

// The pass is idempotent server-side, so re-fetching on every open keeps the
// token fresh without any persistence.
async function openCamera() {
  if (!latestEvent.value || busy.value) return;
  busy.value = true;
  error.value = null;
  try {
    cameraPass.value = await getCameraPass(latestEvent.value.id);
    cameraOpen.value = true;
  } catch (e) {
    error.value = messageFor(e);
  } finally {
    busy.value = false;
  }
}

function closeCamera() {
  cameraOpen.value = false;
  void refreshGuests();
}

watch(latestEvent, () => {
  void loadPartyDetails();
});

onMounted(() => {
  void refreshAll();
});
</script>

<template>
  <view :style="{ width: '100%', height: '100%' }">
    <scroll-view scroll-orientation="vertical" :enable-scroll="true" :style="{ width: '100%', height: '100%' }">
      <!-- Children are conditional, so space them with margins: vue-lynx renders
           v-if/v-for anchors as real nodes and container gap would double up. -->
      <view :style="{ width: '100%', display: 'flex', flexDirection: 'column', padding: '24px 20px 120px' }">
        <PartyHeader
          kicker="Your party"
          :title="latestEvent?.name || 'New party'"
          :date="dateLabel"
          settings
          @settings="settingsTrayOpen = true"
        />

        <text v-if="error" :style="{ marginTop: '14px', fontFamily: t.font.body, fontSize: '14px', letterSpacing: t.tracking, color: t.color.danger }">
          {{ error }}
        </text>

        <VyCard v-if="!latestEvent" :style="{ width: '100%', marginTop: '14px' }">
          <view :style="{ display: 'flex', flexDirection: 'column' }">
            <text :style="{ fontFamily: t.font.body, fontSize: '14px', letterSpacing: t.tracking, color: t.color.muted }">
              Name it, date it, get your party code.
            </text>
            <VyButton color="primary" size="lg" block :style="{ marginTop: '12px' }" @tap="settingsTrayOpen = true">
              Make the party
            </VyButton>
          </view>
        </VyCard>

        <VyCard v-if="latestEvent" :style="{ width: '100%', marginTop: '14px' }">
          <PartyCodeCard
            :code="latestEvent.code"
            :busy="busy"
            show-share
            :share-ready="!!qr?.joinUrl"
            @refresh="refreshShare"
            @share="openShareTray"
          />
        </VyCard>
      </view>
    </scroll-view>

    <view
      v-if="latestEvent && !cameraOpen"
      :style="{
        position: 'fixed',
        bottom: '28px',
        left: '50%',
        transform: 'translateX(-50%)',
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
        opacity: busy ? 0.6 : 1,
      }"
      @tap="openCamera"
    >
      <VyIcon name="lucide:camera" :style="{ width: '30px', height: '30px', color: '#ffffff' }" />
    </view>

    <CameraScreen
      v-if="cameraOpen && latestEvent && cameraPass"
      :event-id="latestEvent.id"
      :token="cameraPass.token"
      :title="latestEvent.name"
      @close="closeCamera"
      @added="refreshGuests"
    />

    <HostSettingsTray
      v-model:open="settingsTrayOpen"
      :event="latestEvent"
      :user="props.user"
      :guests="guests"
      :busy="busy"
      @save="saveSettings"
      @refresh-guests="refreshGuests"
      @remove-guest="removePartyGuest"
      @logout="emit('logout')"
    />

    <HostShareTray
      v-if="latestEvent && qr?.joinUrl"
      v-model:open="shareTrayOpen"
      :event="latestEvent"
      :qr="qr"
      :copied="copied"
      @copy="copyShareLink"
      @share-image="shareInviteImage"
      @share-platform="sharePlatform"
    />
  </view>
</template>
