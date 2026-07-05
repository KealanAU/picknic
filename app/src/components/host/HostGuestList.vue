<script setup lang="ts">
import { ref } from 'vue';
import { VyButton, VySwipeAction } from '@vyui/kit';
import type { HostGuest } from '../../api/hostEvents';
import { t } from '../../theme/tokens';

defineProps<{
  guests: HostGuest[];
  busy?: boolean;
}>();

const emit = defineEmits<{
  refresh: [];
  remove: [guest: HostGuest];
}>();

const pendingRemoveId = ref<string | null>(null);

function remove(guest: HostGuest) {
  if (pendingRemoveId.value === guest.id) {
    pendingRemoveId.value = null;
    emit('remove', guest);
    return;
  }
  pendingRemoveId.value = guest.id;
}

function joinedLabel(value: string) {
  return new Date(value).toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
}
</script>

<template>
  <view :style="{ display: 'flex', flexDirection: 'column', gap: '12px' }">
    <view :style="{ display: 'flex', flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: '12px' }">
      <view>
        <text :style="{ fontFamily: t.font.body, fontSize: '12px', letterSpacing: t.tracking, textTransform: 'uppercase', color: t.color.muted }">
          Party guests
        </text>
        <text :style="{ fontFamily: t.font.display, fontSize: '28px', fontWeight: '300', lineHeight: '1', letterSpacing: t.tracking, color: t.color.ink }">
          {{ guests.filter((guest) => !guest.removed).length }} joined
        </text>
      </view>
      <VyButton size="sm" variant="ghost" :loading="busy" @click="$emit('refresh')">
        Refresh
      </VyButton>
    </view>

    <text
      v-if="!guests.length"
      :style="{ fontFamily: t.font.body, fontSize: '14px', letterSpacing: t.tracking, color: t.color.muted }"
    >
      Guests will appear here after they join with the room code or QR.
    </text>

    <view v-for="guest in guests" :key="guest.id" :style="{ opacity: guest.removed ? 0.45 : 1 }">
      <VySwipeAction
        :action-width="112"
        :row-width="336"
        :disabled="guest.removed || busy"
        side="right"
        @commit="remove(guest)"
      >
        <view
          :style="{
            display: 'flex',
            flexDirection: 'row',
            alignItems: 'center',
            justifyContent: 'space-between',
            gap: '12px',
            padding: '12px',
            borderWidth: '1px',
            borderStyle: 'solid',
            borderColor: t.color.line,
          }"
        >
          <view :style="{ display: 'flex', flexDirection: 'column', gap: '3px' }">
            <text :style="{ fontFamily: t.font.body, fontSize: '16px', letterSpacing: t.tracking, color: t.color.ink }">
              {{ guest.displayName }}
            </text>
            <text :style="{ fontFamily: t.font.body, fontSize: '12px', letterSpacing: t.tracking, color: t.color.muted }">
              {{ guest.email || `Joined ${joinedLabel(guest.joinedAt)}` }}
            </text>
          </view>
          <text :style="{ fontFamily: t.font.body, fontSize: '12px', letterSpacing: t.tracking, color: t.color.muted }">
            {{ guest.removed ? 'Removed' : `${guest.photos} photos` }}
          </text>
        </view>

        <template #actions>
          <view
            :style="{
              width: '112px',
              height: '100%',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              backgroundColor: t.color.danger,
            }"
            @tap="remove(guest)"
          >
            <text :style="{ fontFamily: t.font.body, fontSize: '13px', letterSpacing: t.tracking, textTransform: 'uppercase', color: '#ffffff' }">
              {{ pendingRemoveId === guest.id ? 'Confirm' : 'Remove' }}
            </text>
          </view>
        </template>
      </VySwipeAction>
    </view>
  </view>
</template>
