<script setup lang="ts">
import { computed, ref } from 'vue';
import { useGuest } from '../../composables/useGuest';
import { backButtonStyle, errStyle, headerStyle, primaryActionStyle, subStyle, titleStyle } from './styles';

const props = defineProps<{
  code: string;
  eventName: string;
  secret?: string;
}>();

defineEmits<{
  back: [];
}>();

const { join, error: guestError } = useGuest();

const guestName = ref('');
const joinBusy = ref(false);

const canJoin = computed(() => guestName.value.trim().length > 0 && !joinBusy.value);

async function submitJoin() {
  if (!canJoin.value) return;
  joinBusy.value = true;
  try {
    await join(props.code, guestName.value, props.secret);
    // Success flips isJoined, which closes the tray.
  } catch {
    // guestError set by join
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
      @click="$emit('back')"
    >
      Back
    </VyButton>
    <view :style="headerStyle">
      <text :style="titleStyle">You're joining {{ eventName }}</text>
      <text :style="subStyle">What should we call you on the roll?</text>
    </view>

    <VyForm class="flex flex-col items-stretch w-full gap-2">
      <VyFormField label="Your name">
        <VyInput v-model="guestName" size="xl" autocomplete="name" placeholder="e.g. Alex" />
      </VyFormField>
    </VyForm>

    <text v-if="guestError" :style="errStyle">{{ guestError }}</text>

    <VyButton
      color="primary"
      size="xl"
      block
      leading-icon="lucide:camera"
      :loading="joinBusy"
      :disabled="!canJoin"
      :style="primaryActionStyle"
      @click="submitJoin"
    >
      Join &amp; start snapping
    </VyButton>
  </VyTrayView>
</template>
