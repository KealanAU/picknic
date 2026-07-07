<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { VyButton, VyForm, VyFormField, VyInput } from '@vyui/kit';
import type { HostEvent } from '../../api/hostEvents';
import { isoDate } from '../../lib/dates';
import DatePicker from '../DatePicker.vue';

const props = defineProps<{
  event?: HostEvent;
  busy?: boolean;
}>();

const emit = defineEmits<{
  save: [payload: { name: string; partyStart: string; partyEnd: string }];
}>();

const name = ref('');
const partyStart = ref(isoDate(new Date()));
const partyEnd = ref(isoDate(new Date()));

watch(
  () => props.event,
  (event) => {
    if (!event) return;
    name.value = event.name;
    partyStart.value = isoDate(new Date(event.uploadOpensAt));
    partyEnd.value = isoDate(new Date(event.uploadClosesAt));
  },
  { immediate: true },
);

const DATE_RE = /^\d{4}-\d{2}-\d{2}$/;
const canSave = computed(
  () =>
    !!name.value.trim() &&
    DATE_RE.test(partyStart.value) &&
    DATE_RE.test(partyEnd.value) &&
    partyStart.value <= partyEnd.value &&
    !props.busy,
);

function save() {
  if (!canSave.value) return;
  emit('save', { name: name.value.trim(), partyStart: partyStart.value, partyEnd: partyEnd.value });
}
</script>

<template>
  <!-- No gap here: VyForm/VyFormField have fragment roots whose anchor nodes
       become real flex items on native and collect phantom gaps; space with
       margins on the field roots instead. -->
  <view :style="{ display: 'flex', flexDirection: 'column' }">
    <VyForm class="flex flex-col items-stretch w-full">
      <VyFormField label="Party name">
        <VyInput v-model="name" size="lg" autocomplete="off" placeholder="e.g. Sarah + Max" />
      </VyFormField>
      <VyFormField class="mt-2" label="Party days">
        <DatePicker v-model:start="partyStart" v-model:end="partyEnd" :disabled="busy" />
      </VyFormField>
    </VyForm>

    <VyButton color="primary" size="lg" block :loading="busy" :disabled="!canSave" :style="{ marginTop: '10px' }" @tap="save">
      {{ event ? 'Save' : 'Create party' }}
    </VyButton>
  </view>
</template>
