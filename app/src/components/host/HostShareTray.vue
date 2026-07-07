<script setup lang="ts">
import { computed } from 'vue';
import { VyTray } from '@vyui/kit';
import type { EventQr, HostEvent } from '../../api/hostEvents';
import { t } from '../../theme/tokens';
import HostShareAction from './HostShareAction.vue';

const props = defineProps<{
  event: HostEvent;
  open?: boolean;
  qr: EventQr;
  copied?: boolean;
}>();

const emit = defineEmits<{
  'update:open': [value: boolean];
  copy: [];
  shareImage: [imageUrl: string];
  sharePlatform: [platform: SharePlatform];
}>();

type SharePlatform = 'messages' | 'whatsapp' | 'facebook' | 'messenger' | 'x' | 'snapchat' | 'threads';

const platforms = [
  { platform: 'messages', label: 'Messages', icon: 'lucide:message-circle', color: '#34c759' },
  { platform: 'whatsapp', label: 'WhatsApp', icon: 'brand:whatsapp', color: '#25d366' },
  { platform: 'facebook', label: 'Facebook', icon: 'brand:facebook', color: '#1877f2' },
  { platform: 'messenger', label: 'Messenger', icon: 'brand:messenger', color: '#0084ff' },
  { platform: 'x', label: 'X', icon: 'brand:x', color: '#000000' },
  { platform: 'snapchat', label: 'Snapchat', icon: 'brand:snapchat', color: '#fffc00' },
  { platform: 'threads', label: 'Threads', icon: 'brand:threads', color: '#000000' },
] as const;

function close() {
  emit('update:open', false);
}

function escapeXml(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

const inviteImage = computed(() => {
  if (!props.qr.qrPng) return '';

  const name = escapeXml(props.event.name);
  const code = escapeXml(props.event.code);
  const qr = escapeXml(props.qr.qrPng);
  const svg = `
<svg xmlns="http://www.w3.org/2000/svg" width="1080" height="1350" viewBox="0 0 1080 1350">
  <rect width="1080" height="1350" fill="#fff8f1"/>
  <rect x="80" y="80" width="920" height="1190" fill="#ffffff" stroke="#000000" stroke-width="6"/>
  <text x="540" y="210" text-anchor="middle" font-family="Arial, sans-serif" font-size="54" font-weight="700" fill="#000000">${name}</text>
  <text x="540" y="300" text-anchor="middle" font-family="Arial, sans-serif" font-size="36" fill="#3d4148">Join the Picknic room</text>
  <text x="540" y="470" text-anchor="middle" font-family="Arial, sans-serif" font-size="142" font-weight="700" letter-spacing="8" fill="#006eff">${code}</text>
  <rect x="315" y="570" width="450" height="450" fill="#ffffff" stroke="#e2e8f0" stroke-width="4"/>
  <image href="${qr}" x="345" y="600" width="390" height="390" preserveAspectRatio="xMidYMid meet"/>
  <text x="540" y="1110" text-anchor="middle" font-family="Arial, sans-serif" font-size="30" fill="#000000">Scan the QR or use the room code</text>
  <text x="540" y="1172" text-anchor="middle" font-family="Arial, sans-serif" font-size="24" fill="#3d4148">The invite link is included with this share.</text>
</svg>`;

  return `data:image/svg+xml;charset=utf-8,${encodeURIComponent(svg.trim())}`;
});
</script>

<template>
  <VyTray
    :open="open"
    variant="floating"
    overlay
    dismissible
    handle
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
        <view :style="{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '6px' }">
          <text :style="{ fontFamily: t.font.body, fontSize: '12px', letterSpacing: t.tracking, textTransform: 'uppercase', color: t.color.muted }">
            Party code
          </text>
          <text :style="{ fontFamily: t.font.display, fontSize: '46px', fontWeight: '300', lineHeight: '1', letterSpacing: '0.04em', color: t.color.blue }">
            {{ event.code }}
          </text>
          <view
            :style="{
              marginTop: '6px',
              padding: '10px',
              borderWidth: '1px',
              borderStyle: 'solid',
              borderColor: t.color.line,
              borderRadius: t.radius.card,
              backgroundColor: '#ffffff',
            }"
          >
            <image :src="qr.qrPng" mode="aspectFit" :style="{ width: '190px', height: '190px' }" />
          </view>
        </view>

        <scroll-view scroll-orientation="horizontal" :enable-scroll="true" :style="{ width: '100%', height: '82px' }">
          <!-- Spacing via per-tile margins, not flex gap: vue-lynx mounts v-for fragment
               anchors as real zero-width nodes, so gap would double up around them. -->
          <view :style="{ display: 'flex', flexDirection: 'row', alignItems: 'flex-start', padding: '2px 2px 6px' }">
            <HostShareAction
              v-if="inviteImage"
              icon="lucide:send"
              label="Share"
              color="#ffffff"
              :background="t.color.blue"
              @press="close(); emit('shareImage', inviteImage)"
            />
            <HostShareAction
              icon="lucide:link"
              :label="copied ? 'Copied' : 'Copy link'"
              @press="emit('copy')"
            />
            <HostShareAction
              v-for="option in platforms"
              :key="option.platform"
              :icon="option.icon"
              :label="option.label"
              :color="option.color"
              @press="close(); emit('sharePlatform', option.platform)"
            />
          </view>
        </scroll-view>
      </view>
    </template>
  </VyTray>
</template>
