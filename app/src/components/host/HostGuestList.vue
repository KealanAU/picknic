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

type GuestFilter = 'all' | 'joined' | 'removed';

const pendingRemoveId = ref<string | null>(null);
const filter = ref<GuestFilter>('all');

const joinedCount = computed(() => props.guests.filter((guest) => !guest.removed).length);
const removedCount = computed(() => props.guests.length - joinedCount.value);

const filterOptions = computed(
  () =>
    [
      { id: 'all', label: `All · ${props.guests.length}` },
      { id: 'joined', label: `Joined · ${joinedCount.value}` },
      { id: 'removed', label: `Removed · ${removedCount.value}` },
    ] as const,
);

const visibleGuests = computed(() => {
  if (filter.value === 'joined') return props.guests.filter((guest) => !guest.removed);
  if (filter.value === 'removed') return props.guests.filter((guest) => guest.removed);
  return props.guests;
});

const emptyLabel = computed(() => {
  if (!props.guests.length) return 'No guests yet — share the invite.';
  return filter.value === 'removed' ? 'No removed guests.' : 'No joined guests.';
});

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

function chipStyle(active: boolean) {
  return {
    paddingLeft: '12px',
    paddingRight: '12px',
    paddingTop: '6px',
    paddingBottom: '6px',
    marginRight: '6px',
    borderRadius: t.radius.pill,
    borderWidth: '1.5px',
    borderStyle: 'solid' as const,
    borderColor: active ? t.color.blue : t.color.line,
    backgroundColor: active ? t.color.claySoft : 'transparent',
  } as const;
}

function chipTextStyle(active: boolean) {
  return {
    fontFamily: t.font.body,
    fontSize: '12px',
    letterSpacing: t.trackingSmall,
    color: active ? t.color.blue : t.color.muted,
  } as const;
}
</script>

<template>
  <!-- Margin spacing instead of gap: v-if/v-for children leave fragment
       anchors that container gap would treat as children. -->
  <view :style="{ display: 'flex', flexDirection: 'column' }">
    <view :style="{ display: 'flex', flexDirection: 'row', alignItems: 'center', gap: '12px', marginBottom: '4px' }">
      <text :style="{ fontFamily: t.font.body, fontSize: '12px', letterSpacing: t.trackingSmall, textTransform: 'uppercase', color: t.color.muted }">
        Guests · {{ joinedCount }}
      </text>
      <VyButton size="sm" variant="ghost" :loading="busy" @tap="$emit('refresh')">
        Refresh
      </VyButton>
    </view>

    <view v-if="guests.length" :style="{ display: 'flex', flexDirection: 'row', flexWrap: 'wrap', marginBottom: '8px' }">
      <view
        v-for="option in filterOptions"
        :key="option.id"
        :style="chipStyle(filter === option.id)"
        @tap="filter = option.id"
      >
        <text :style="chipTextStyle(filter === option.id)">{{ option.label }}</text>
      </view>
    </view>

    <text
      v-if="!visibleGuests.length"
      :style="{ fontFamily: t.font.body, fontSize: '14px', letterSpacing: t.trackingSmall, color: t.color.muted, paddingTop: '4px', paddingBottom: '8px' }"
    >
      {{ emptyLabel }}
    </text>

    <view v-for="(guest, index) in visibleGuests" :key="guest.id">
      <view v-if="index" :style="{ height: '1px', backgroundColor: t.color.line }" />
      <view :style="{ opacity: guest.removed ? 0.45 : 1 }">
        <VySwipeAction
          :action-width="112"
          :row-width="336"
          :disabled="guest.removed || busy || guest.isHost"
          side="right"
          @commit="remove(guest)"
        >
          <!-- Explicit row height: SwipeAction rows don't auto-size from their
               content on Lynx, so without it every row collapses and overlaps. -->
          <view
            :style="{
              height: '56px',
              display: 'flex',
              flexDirection: 'row',
              alignItems: 'center',
              justifyContent: 'space-between',
              gap: '12px',
            }"
          >
            <view :style="{ display: 'flex', flexDirection: 'column', gap: '2px' }">
              <text :style="{ fontFamily: t.font.body, fontSize: '15px', letterSpacing: t.tracking, color: t.color.ink }">
                {{ guest.displayName }}
              </text>
              <text :style="{ fontFamily: t.font.body, fontSize: '12px', letterSpacing: t.trackingSmall, color: t.color.muted }">
                {{ guest.isHost ? 'That’s you' : guest.email || `Joined ${joinedLabel(guest.joinedAt)}` }}
              </text>
            </view>
            <text :style="{ fontFamily: t.font.body, fontSize: '12px', letterSpacing: t.trackingSmall, color: t.color.muted }">
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
              <text :style="{ fontFamily: t.font.body, fontSize: '13px', letterSpacing: t.trackingSmall, textTransform: 'uppercase', color: '#ffffff' }">
                {{ pendingRemoveId === guest.id ? 'Confirm' : 'Remove' }}
              </text>
            </view>
          </template>
        </VySwipeAction>
      </view>
    </view>
  </view>
</template>
