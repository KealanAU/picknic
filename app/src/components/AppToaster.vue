<script setup lang="ts">
// Sonner-style error stack pinned to the top of the screen: toasts collapse
// into an overlapping pile, tap fans them out, swipe flings one away. Entries
// come from useToast(); the viewport renders through OverlayRoot in App.vue.
import { ToastProvider, ToastViewport } from '@vyui/core';
import { VyIcon, VyToast } from '@vyui/kit';
import { useToast } from '../composables/useToast';

const { toasts, dismiss } = useToast();

// Hoisted so the repeated toasts share one object instead of shipping a fresh
// literal across the thread boundary per node.
const toastUi = {
  title: 'pk-toast-title',
  description: 'pk-toast-desc',
  progress: 'pk-toast-progress',
  close: 'pk-toast-close',
};
</script>

<template>
  <ToastProvider :duration="4500">
    <!-- zIndex clears the camera screen (1050) and trays (1001) so errors
         raised from inside either still surface on top. top: 76px keeps the
         stack below the close X / settings cog row (~20-64px), and the v-if
         unmounts the viewport entirely when empty so nothing can eat taps.
         Tapping a toast expands the stack, which pauses auto-dismiss — the
         per-toast close X guarantees a way out of that state. -->
    <ToastViewport v-if="toasts.length" position="top-center" :style="{ top: '76px', zIndex: 1200 }">
      <VyToast
        v-for="item in toasts"
        :key="item.id"
        :title="item.title"
        :description="item.description"
        stacked
        stack-from="top"
        swipe
        progress
        :close="{ size: 'sm' }"
        close-icon="lucide:x"
        class="pk-toast"
        :ui="toastUi"
        @update:open="(open: boolean) => open || dismiss(item.id)"
      >
        <template #leading>
          <VyIcon name="streamline-freehand:alerts-warning-triangle" class="pk-toast-icon" />
        </template>
      </VyToast>
    </ToastViewport>
  </ToastProvider>
</template>
