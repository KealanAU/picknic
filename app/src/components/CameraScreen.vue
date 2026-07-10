<script lang="ts">
// Module-level on purpose: show the intro once per app run, not on every
// remount (the guest FAB reopens this screen).
let introSeen = false;
</script>

<script setup lang="ts">
// Full-screen capture flow shared by host and guests: shutter -> framed
// preview -> caption -> upload through the guest-token pipeline. The native
// module presents the system camera; this screen is the before/after chrome.
// Hosts pass a camera-pass token, guests their session token.
import { computed, ref } from 'vue';
import { VyButton, VyIcon, VyInput, VyTray } from '@vyui/kit';
import { isApiError } from '../api/http';
import { completeUpload, createUpload, putBlob } from '../api/photos';
import { useCamera } from '../composables/useCamera';
import { useToast } from '../composables/useToast';
import { cameraInstallStatus, toDataUri, type CapturedPhoto } from '../native/camera';
import { t } from '../theme/tokens';
import InstaxCard from './InstaxCard.vue';
import { subStyle, titleStyle } from './onboarding/styles';

const props = defineProps<{
  eventId: string;
  token: string;
  title?: string;
}>();

const emit = defineEmits<{
  close: [];
  added: [];
}>();

const camera = useCamera();
const { toastError } = useToast();

// Shown when the camera is unavailable so on-device debugging (Lynx Go,
// Explorer) explains itself without DevTool; e.g. "native-module-missing".
const installCode = cameraInstallStatus().code;

const photo = ref<CapturedPhoto | null>(null);
const closePressed = ref(false);
const shutterPressed = ref(false);
const caption = ref('');
const uploadStage = ref<'idle' | 'requesting' | 'uploading' | 'finalizing'>('idle');
const addedCount = ref(0);
const justAdded = ref(false);

const previewUri = computed(() => (photo.value ? toDataUri(photo.value) : undefined));
const uploading = computed(() => uploadStage.value !== 'idle');
const shutterReady = computed(() => camera.available && !camera.busy.value);

function friendly(e: unknown): string {
  return isApiError(e) ? e.message : 'Something went wrong. Try again.';
}

// Intro tray instead of dropping people straight into the camera; its CTA
// launches the first capture. Swiping it away just leaves the shutter.
const introOpen = ref(!introSeen);

function introDone(launch: boolean) {
  introSeen = true;
  introOpen.value = false;
  if (launch) void snap();
}

async function snap() {
  if (!shutterReady.value) return;
  justAdded.value = false;
  const shot = await camera.capture();
  if (shot) {
    photo.value = shot;
    caption.value = '';
  } else if (camera.error.value) {
    // capture() returns null on cancel too; only real failures set error.
    toastError(camera.error.value);
  }
}

function retake() {
  if (uploading.value) return;
  photo.value = null;
  caption.value = '';
}

async function addToRoll() {
  const shot = photo.value;
  if (!shot || uploading.value) return;
  try {
    uploadStage.value = 'requesting';
    const target = await createUpload(props.eventId, props.token);
    uploadStage.value = 'uploading';
    await putBlob(target.uploadUrl, shot.bytes, shot.mime);
    uploadStage.value = 'finalizing';
    await completeUpload(props.eventId, target.blobPath, caption.value.trim() || null, props.token);
    addedCount.value += 1;
    justAdded.value = true;
    photo.value = null;
    caption.value = '';
    emit('added');
  } catch (e) {
    toastError(friendly(e));
  } finally {
    uploadStage.value = 'idle';
  }
}
</script>

<template>
  <!-- ponytail: no exit animation; v-if unmount is a hard cut -->
  <view
    class="camera-screen-enter"
    :style="{
      position: 'fixed',
      top: '0',
      left: '0',
      right: '0',
      bottom: '0',
      zIndex: 1050,
      backgroundColor: t.color.cream,
      display: 'flex',
      flexDirection: 'column',
    }"
  >
    <view class="safe-top" :style="{ display: 'flex', flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', paddingLeft: '20px', paddingRight: '20px', paddingBottom: '20px' }">
      <view
        :style="{
          width: '40px',
          height: '40px',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          borderRadius: t.radius.pill,
          backgroundColor: '#ffffff',
          borderWidth: '1px',
          borderStyle: 'solid',
          borderColor: t.color.line,
          opacity: uploading ? 0.4 : 1,
          transform: closePressed ? 'scale(0.92)' : 'scale(1)',
        }"
        @tap="!uploading && emit('close')"
        @touchstart="closePressed = true"
        @touchend="closePressed = false"
        @touchcancel="closePressed = false"
      >
        <VyIcon name="lucide:x" :style="{ width: '20px', height: '20px', color: t.color.ink }" />
      </view>
      <text :style="{ fontFamily: t.font.body, fontSize: '13px', letterSpacing: t.trackingSmall, color: t.color.muted }">
        {{ addedCount ? `${addedCount} on the roll` : title }}
      </text>
    </view>

    <!-- Margin spacing throughout: children are conditional and vue-lynx
         renders v-if anchors as real nodes, so container gap would double up. -->
    <view :style="{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', padding: '0 20px' }">
      <InstaxCard :src="previewUri" :caption="photo ? caption || ' ' : undefined" :width="300" develop />

      <VyInput
        v-if="photo"
        v-model="caption"
        size="lg"
        autocomplete="off"
        placeholder="Write on the frame…"
        :disabled="uploading"
        :style="{ width: '300px', marginTop: '14px' }"
      />

      <text
        v-if="justAdded && !photo"
        :style="{ marginTop: '14px', fontFamily: t.font.body, fontSize: '14px', letterSpacing: t.trackingSmall, color: t.color.blue }"
      >
        On the roll ✓
      </text>
    </view>

    <view :style="{ height: '150px', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', padding: '0 20px' }">
      <view
        v-if="!camera.available"
        :style="{ display: 'flex', flexDirection: 'column', alignItems: 'center' }"
      >
        <text :style="{ fontFamily: t.font.body, fontSize: '14px', letterSpacing: t.trackingSmall, color: t.color.muted }">
          Camera isn't available on this device.
        </text>
        <text :style="{ marginTop: '6px', fontFamily: t.font.body, fontSize: '11px', letterSpacing: t.trackingSmall, color: t.color.muted }">
          {{ installCode }}
        </text>
      </view>

      <view
        v-else-if="!photo"
        :style="{
          width: '76px',
          height: '76px',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          borderRadius: t.radius.pill,
          borderWidth: '3px',
          borderStyle: 'solid',
          borderColor: t.color.ink,
          opacity: shutterReady ? 1 : 0.4,
          transform: shutterPressed && shutterReady ? 'scale(0.9)' : 'scale(1)',
        }"
        @tap="snap"
        @touchstart="shutterPressed = true"
        @touchend="shutterPressed = false"
        @touchcancel="shutterPressed = false"
      >
        <view
          :style="{
            width: '60px',
            height: '60px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            borderRadius: t.radius.pill,
            backgroundColor: t.color.blue,
          }"
        >
          <VyIcon name="lucide:camera" :style="{ width: '28px', height: '28px', color: '#ffffff' }" />
        </view>
      </view>

      <view v-else :style="{ width: '100%', display: 'flex', flexDirection: 'row', alignItems: 'center', gap: '12px' }">
        <VyButton size="lg" variant="ghost" :disabled="uploading" @tap="retake">
          Retake
        </VyButton>
        <VyButton color="primary" size="lg" :loading="uploading" :style="{ flex: 1 }" @tap="addToRoll">
          Add to the roll
        </VyButton>
      </view>
    </view>

    <VyTray
      :open="introOpen"
      variant="floating"
      overlay
      dismissible
      handle
      :ui="{
        content: 'z-[1051] pk-tray-radius pk-onboarding-tray-surface',
        morph: 'pk-onboarding-tray-surface',
        viewport: 'pk-onboarding-tray-surface',
        body: 'px-4 pb-5 pk-onboarding-tray-surface',
        footer: 'pk-onboarding-tray-surface',
      }"
      @update:open="!$event && introDone(false)"
    >
      <template #default>
        <view :style="{ display: 'flex', flexDirection: 'column', gap: '4px', marginBottom: '16px' }">
          <text :style="titleStyle">Say cheese</text>
          <text :style="subStyle">
            Photos you snap land on the party's roll. The roll stays hidden until the party wraps — no peeking until then.
          </text>
        </view>
        <VyButton color="primary" size="xl" block leading-icon="lucide:camera" @tap="introDone(true)">
          Open the camera
        </VyButton>
      </template>
    </VyTray>
  </view>
</template>

<style>
.camera-screen-enter {
  animation: camera-screen-rise 200ms ease-out;
}
@keyframes camera-screen-rise {
  from {
    opacity: 0;
    transform: translateY(24px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>
