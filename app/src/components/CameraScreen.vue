<script setup lang="ts">
// Full-screen capture flow shared by host and guests: shutter -> framed
// preview -> caption -> upload through the guest-token pipeline. The native
// module presents the system camera; this screen is the before/after chrome.
// Hosts pass a camera-pass token, guests their session token.
import { computed, ref } from 'vue';
import { VyButton, VyIcon, VyInput } from '@vyui/kit';
import { isApiError } from '../api/http';
import { completeUpload, createUpload, putBlob } from '../api/photos';
import { useCamera } from '../composables/useCamera';
import { toDataUri, type CapturedPhoto } from '../native/camera';
import { t } from '../theme/tokens';
import InstaxCard from './InstaxCard.vue';

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

const photo = ref<CapturedPhoto | null>(null);
const caption = ref('');
const uploadStage = ref<'idle' | 'requesting' | 'uploading' | 'finalizing'>('idle');
const uploadError = ref<string | null>(null);
const addedCount = ref(0);
const justAdded = ref(false);

const previewUri = computed(() => (photo.value ? toDataUri(photo.value) : undefined));
const uploading = computed(() => uploadStage.value !== 'idle');
const shutterReady = computed(() => camera.available && !camera.busy.value);
const errorText = computed(() => uploadError.value ?? camera.error.value);

function friendly(e: unknown): string {
  return isApiError(e) ? e.message : 'Something went wrong. Try again.';
}

async function snap() {
  if (!shutterReady.value) return;
  justAdded.value = false;
  uploadError.value = null;
  const shot = await camera.capture();
  if (shot) {
    photo.value = shot;
    caption.value = '';
  }
}

function retake() {
  if (uploading.value) return;
  photo.value = null;
  caption.value = '';
  uploadError.value = null;
}

async function addToRoll() {
  const shot = photo.value;
  if (!shot || uploading.value) return;
  uploadError.value = null;
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
    uploadError.value = friendly(e);
  } finally {
    uploadStage.value = 'idle';
  }
}
</script>

<template>
  <view
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
    <view :style="{ display: 'flex', flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', padding: '20px' }">
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
        }"
        @tap="!uploading && emit('close')"
      >
        <VyIcon name="lucide:x" :style="{ width: '20px', height: '20px', color: t.color.ink }" />
      </view>
      <text :style="{ fontFamily: t.font.body, fontSize: '13px', letterSpacing: t.tracking, color: t.color.muted }">
        {{ addedCount ? `${addedCount} on the roll` : title }}
      </text>
    </view>

    <!-- Margin spacing throughout: children are conditional and vue-lynx
         renders v-if anchors as real nodes, so container gap would double up. -->
    <view :style="{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', padding: '0 20px' }">
      <InstaxCard :src="previewUri" :caption="photo ? caption || ' ' : undefined" :width="300" />

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
        :style="{ marginTop: '14px', fontFamily: t.font.body, fontSize: '14px', letterSpacing: t.tracking, color: t.color.blue }"
      >
        On the roll ✓
      </text>

      <text
        v-if="errorText"
        :style="{ marginTop: '14px', fontFamily: t.font.body, fontSize: '14px', letterSpacing: t.tracking, color: t.color.danger, textAlign: 'center' }"
      >
        {{ errorText }}
      </text>
    </view>

    <view :style="{ height: '150px', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', padding: '0 20px' }">
      <text
        v-if="!camera.available"
        :style="{ fontFamily: t.font.body, fontSize: '14px', letterSpacing: t.tracking, color: t.color.muted }"
      >
        Camera isn't available on this device.
      </text>

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
        }"
        @tap="snap"
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
  </view>
</template>
