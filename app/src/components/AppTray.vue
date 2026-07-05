<script setup lang="ts">
import { VyIcon, VyTray } from '@vyui/kit';

defineProps<{
  open?: boolean;
  modelValue?: boolean;
  defaultOpen?: boolean;
  variant?: 'floating' | 'flush';
  defaultView?: string;
  view?: string;
  overlay?: boolean;
  dismissible?: boolean;
  handle?: boolean;
  duration?: number;
  keyboardAware?: boolean;
}>();

defineEmits<{
  (event: 'update:open', value: boolean): void;
  (event: 'update:modelValue', value: boolean): void;
  (event: 'update:view', value: string): void;
}>();
</script>

<template>
  <VyTray
    :open="open"
    :model-value="modelValue"
    :default-open="defaultOpen"
    :variant="variant"
    :default-view="defaultView"
    :view="view"
    :overlay="overlay"
    :dismissible="dismissible"
    :handle="handle"
    :duration="duration"
    :keyboard-aware="keyboardAware"
    :ui="{ body: 'px-0 pt-0 pb-4' }"
    @update:open="$emit('update:open', $event)"
    @update:model-value="$emit('update:modelValue', $event)"
    @update:view="$emit('update:view', $event)"
  >
    <template #trigger="slotProps">
      <slot name="trigger" v-bind="slotProps" />
    </template>

    <template #default="slotProps">
      <view class="flex flex-col">
        <view class="flex min-h-12 flex-row items-center px-2 py-2">
          <view
            v-if="slotProps.canGoBack"
            class="flex size-10 items-center justify-center rounded-full"
            @tap="slotProps.goBack"
          >
            <VyIcon name="lucide:chevron-left" class="size-6 text-neutral-900" />
          </view>
          <view v-else class="size-10" />

          <view class="min-w-0 flex-1 px-1">
            <slot name="header" v-bind="slotProps" />
          </view>
        </view>

        <view class="px-4">
          <slot v-bind="slotProps" />
        </view>
      </view>
    </template>

    <template #footer="slotProps">
      <slot name="footer" v-bind="slotProps" />
    </template>
  </VyTray>
</template>
