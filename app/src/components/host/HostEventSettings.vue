<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { VyButton, VyForm, VyFormField, VyInput } from '@vyui/kit';
import type { HostEvent } from '../../api/hostEvents';
import { t } from '../../theme/tokens';

const props = defineProps<{
  event?: HostEvent;
  busy?: boolean;
}>();

const emit = defineEmits<{
  save: [payload: { name: string; partyDate: string }];
}>();

const name = ref('Local Test Roll');
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
    <view :style="{ display: 'flex', flexDirection: 'column', gap: '3px' }">
      <text :style="{ fontFamily: t.font.body, fontSize: '12px', letterSpacing: t.tracking, textTransform: 'uppercase', color: t.color.muted }">
        Party settings
      </text>
      <text :style="{ fontFamily: t.font.display, fontSize: '28px', fontWeight: '300', lineHeight: '1', letterSpacing: t.tracking, color: t.color.ink }">
        {{ event ? 'Edit party' : 'Make a party' }}
      </text>
    </view>

    <VyForm class="flex flex-col items-stretch w-full gap-2">
      <VyFormField label="Party name">
        <VyInput v-model="name" size="lg" autocomplete="off" placeholder="e.g. Sarah + Max" />
      </VyFormField>
      <VyFormField label="Party date" hint="YYYY-MM-DD">
        <VyInput v-model="partyDate" size="lg" autocomplete="off" placeholder="2026-07-05" />
      </VyFormField>
    </VyForm>

    <VyButton color="primary" size="lg" block :loading="busy" :disabled="!canSave" @click="save">
      {{ event ? 'Save settings' : 'Create party' }}
    </VyButton>
  </view>
</template>
