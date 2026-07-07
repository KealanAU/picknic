<script setup lang="ts">
import { computed, ref } from 'vue';
import { VyButton, VySwipeAction } from '@vyui/kit';
import type { HostGuest } from '../../api/hostEvents';
import { t } from '../../theme/tokens';

const props = defineProps<{
  guests: HostGuest[];
  busy?: boolean;
}>();

const emit = defineEmits<{
  refresh: [];
  remove: [guest: HostGuest];
}>();

const pendingRemoveId = ref<string | null>(null);

const joinedCount = computed(() => props.guests.filter((guest) => !guest.removed).length);

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
  <view :style="{ display: 'flex', flexDirection: 'column', gap: '4px' }">
    <view :style="{ display: 'flex', flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: '12px' }">
      <text :style="{ fontFamily: t.font.body, fontSize: '12px', letterSpacing: t.tracking, textTransform: 'uppercase', color: t.color.muted }">
        Guests · {{ joinedCount }}
      </text>
      <VyButton size="sm" variant="ghost" :loading="busy" @tap="$emit('refresh')">
        Refresh
      </VyButton>
    </view>

    <text
      v-if="!guests.length"
      :style="{ fontFamily: t.font.body, fontSize: '14px', letterSpacing: t.tracking, color: t.color.muted, paddingTop: '4px', paddingBottom: '8px' }"
    >
      No guests yet — share the invite.
    </text>

    <view v-for="(guest, index) in guests" :key="guest.id">
      <view v-if="index" :style="{ height: '1px', backgroundColor: t.color.line }" />
      <view :style="{ opacity: guest.removed ? 0.45 : 1 }">
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
              paddingTop: '11px',
              paddingBottom: '11px',
            }"
          >
            <view :style="{ display: 'flex', flexDirection: 'column', gap: '2px' }">
              <text :style="{ fontFamily: t.font.body, fontSize: '15px', letterSpacing: t.tracking, color: t.color.ink }">
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
  </view>
</template>
