<script setup lang="ts">
import { computed, ref } from 'vue';
import { VyButton, VyForm, VyFormField, VyInput, VyTrayView } from '@vyui/kit';
import { useGuest } from '../../composables/useGuest';
import { useToast } from '../../composables/useToast';
import { backButtonStyle, headerStyle, primaryActionStyle, subStyle, titleStyle } from './styles';

const props = defineProps<{
  code: string;
  eventName: string;
  secret?: string;
}>();

defineEmits<{
  back: [];
}>();

const { join, error: guestError } = useGuest();
const { toastError } = useToast();

const guestName = ref('');
const joinBusy = ref(false);

const canJoin = computed(() => guestName.value.trim().length > 0 && !joinBusy.value);

async function submitJoin() {
  if (!canJoin.value) return;
  joinBusy.value = true;
  try {
    await join(props.code, guestName.value, props.secret); // success flips isJoined, closing the tray
  } catch {
    toastError(guestError.value ?? 'Something went wrong. Try again.');
    return;
  } finally {
    joinBusy.value = false;
  }
}
</script>

<template>
  <VyTrayView id="guest-name">
    <VyButton
      variant="ghost"
      size="sm"
      leading-icon="lucide:arrow-left"
      :style="backButtonStyle"
      label="Back"
      @tap="$emit('back')"
    />
    <view :style="headerStyle">
      <text :style="titleStyle">You're joining {{ eventName }}</text>
      <text :style="subStyle">What should we call you on the roll?</text>
    </view>

    <VyForm class="flex flex-col items-stretch w-full">
      <VyFormField name="guestName" label="Your name">
        <VyInput v-model="guestName" size="xl" autocomplete="name" placeholder="e.g. Alex" />
      </VyFormField>
    </VyForm>

    <VyButton
      color="primary"
      size="xl"
      block
      leading-icon="lucide:camera"
      :loading="joinBusy"
      :disabled="!canJoin"
      :style="primaryActionStyle"
      label="Join & start snapping"
      @tap="submitJoin"
    />
  </VyTrayView>
</template>
