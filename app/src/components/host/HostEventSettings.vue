<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { VyButton, VyForm, VyFormField, VyInput } from '@vyui/kit';
import type { HostEvent } from '../../api/hostEvents';
import DatePicker from '../DatePicker.vue';

const props = defineProps<{
  event?: HostEvent;
  busy?: boolean;
}>();

const emit = defineEmits<{
  save: [payload: { name: string; partyDate: string }];
}>();

const name = ref('');
const partyDate = ref(new Date().toISOString().slice(0, 10));

watch(
  () => props.event,
  (event) => {
    if (!event) return;
    name.value = event.name;
    partyDate.value = event.uploadOpensAt.slice(0, 10);
  },
  { immediate: true },
);

const canSave = computed(() => !!name.value.trim() && /^\d{4}-\d{2}-\d{2}$/.test(partyDate.value) && !props.busy);

function save() {
  if (!canSave.value) return;
  emit('save', { name: name.value.trim(), partyDate: partyDate.value });
}
</script>

<template>
  <view :style="{ display: 'flex', flexDirection: 'column', gap: '10px' }">
    <VyForm class="flex flex-col items-stretch w-full gap-2">
      <VyFormField label="Party name">
        <VyInput v-model="name" size="lg" autocomplete="off" placeholder="e.g. Sarah + Max" />
      </VyFormField>
      <VyFormField label="Party date">
        <DatePicker v-model="partyDate" :disabled="busy" />
      </VyFormField>
    </VyForm>

    <VyButton color="primary" size="lg" block :loading="busy" :disabled="!canSave" @tap="save">
      {{ event ? 'Save' : 'Create party' }}
    </VyButton>
  </view>
</template>
