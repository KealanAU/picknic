<script setup lang="ts">
import { computed, ref } from 'vue';
import { VyButton, VyForm, VyFormField, VyInput, VyTrayView } from '@vyui/kit';
import { useGuest } from '../../composables/useGuest';
import { backButtonStyle, errStyle, headerStyle, primaryActionStyle, subStyle, titleStyle } from './styles';

const emit = defineEmits<{
  back: [];
  resolved: [payload: { code: string; eventName: string; secret?: string }];
}>();

const { lookup, error: guestError } = useGuest();

const code = ref('');
// Set only from a scanned QR deep link (future); a typed room code has no secret.
const secret = ref<string | undefined>();
const codeBusy = ref(false);

const canContinueCode = computed(() => code.value.trim().length > 0 && !codeBusy.value);

async function continueCode() {
  if (!canContinueCode.value) return;
  codeBusy.value = true;
  try {
    const ev = await lookup(code.value);
    emit('resolved', { code: code.value, eventName: ev.name, secret: secret.value });
  } catch {
    // guestError set by lookup
  } finally {
    codeBusy.value = false;
  }
}
</script>

<template>
  <VyTrayView id="guest-code">
    <VyButton
      variant="ghost"
      size="sm"
      leading-icon="lucide:arrow-left"
      :style="backButtonStyle"
      @tap="$emit('back')"
    >
      Back
    </VyButton>
    <view :style="headerStyle">
      <text :style="titleStyle">Join an event</text>
      <text :style="subStyle">Enter the room code from the host, or scan their QR.</text>
    </view>

    <VyForm class="flex flex-col items-stretch w-full gap-2">
      <VyFormField label="Room code">
        <VyInput
          v-model="code"
          size="xl"
          autocapitalize="characters"
          placeholder="e.g. K7QF2P"
          leading-icon="lucide:ticket"
        />
      </VyFormField>
    </VyForm>

    <text v-if="guestError" :style="errStyle">{{ guestError }}</text>

    <VyButton
      color="primary"
      size="xl"
      block
      :loading="codeBusy"
      :disabled="!canContinueCode"
      :style="primaryActionStyle"
      @tap="continueCode"
    >
      Continue
    </VyButton>
  </VyTrayView>
</template>
