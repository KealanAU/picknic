<script setup lang="ts">
// The film lab: pick a film stock + instant-print frame, and preview it live in
// the app. Ties together useFilmStyles (catalogue + selection), useCamera
// (capture), and the InstaxCard frame. The developed film look itself is applied
// server-side at reveal — here we preview the frame around the raw capture.
//
// Reference screen for the Drive Capital "Summer Drive" look: brand tokens (no
// hardcoded hex), serif display headlines against a grotesk body, and the flat
// PaperCard / outlined StickerButton / hairline DoodleDivider signature
// components. Copy this pattern to the other screens.
import { computed, ref } from 'vue';
import { useFilmStyles } from '../composables/useFilmStyles';
import { useCamera } from '../composables/useCamera';
import { toDataUri } from '../native/camera';
import InstaxCard from '../components/InstaxCard.vue';
import PaperCard from '../components/PaperCard.vue';
import StickerButton from '../components/StickerButton.vue';
import DoodleDivider from '../components/DoodleDivider.vue';
import { t } from '../theme/tokens';

const { stocks, prints, stock, print, loading, error } = useFilmStyles();
const { available, busy, error: cameraError, capture } = useCamera();

const previewUri = ref<string | undefined>(undefined);

const stockName = computed(
  () => stocks.value.find((s) => s.id === stock.value)?.displayName ?? 'Film',
);

async function takePhoto() {
  const photo = await capture({ quality: 0.9 });
  if (photo) previewUri.value = toDataUri(photo);
}

function chipStyle(active: boolean) {
  return {
    paddingLeft: '20px',
    paddingRight: '20px',
    paddingTop: '11px',
    paddingBottom: '11px',
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
    fontSize: '16px',
    fontWeight: '300' as const,
    letterSpacing: t.tracking,
    color: active ? t.color.blue : t.color.muted,
  } as const;
}

const labelStyle = {
  fontFamily: t.font.body,
  fontSize: '14px',
  fontWeight: '300' as const,
  letterSpacing: t.tracking,
  textTransform: 'uppercase' as const,
  color: t.color.blue,
} as const;
</script>

<template>
  <scroll-view :style="{ width: '100%', height: '100%', backgroundColor: t.color.paper }">
    <view :style="{ display: 'flex', flexDirection: 'column', alignItems: 'center', padding: '29px', gap: '29px' }">
      <text :style="{ fontFamily: t.font.display, fontSize: '38px', fontWeight: '400', lineHeight: '1', letterSpacing: t.tracking, color: t.color.blue, textAlign: 'center' }">
        Choose your roll
      </text>

      <!-- Live print preview -->
      <PaperCard :padding="14">
        <InstaxCard :src="previewUri" :caption="stockName" :print="print" :width="260" />
      </PaperCard>

      <StickerButton :disabled="!available" @tap="takePhoto">
        {{ busy ? 'Snapping…' : available ? '📸 Take a photo' : 'Camera unavailable' }}
      </StickerButton>
      <text v-if="cameraError" :style="{ fontFamily: t.font.body, fontSize: '16px', letterSpacing: t.tracking, color: t.color.danger }">
        {{ cameraError }}
      </text>

      <DoodleDivider label="the frame" />

      <!-- Print style -->
      <view :style="{ width: '100%', gap: '10px' }">
        <text :style="labelStyle">Print frame</text>
        <view :style="{ display: 'flex', flexDirection: 'row', flexWrap: 'wrap', gap: '8px' }">
          <view
            v-for="p in prints"
            :key="p.id"
            :style="chipStyle(p.id === print)"
            @tap="print = p.id"
          >
            <text :style="chipTextStyle(p.id === print)">{{ p.displayName }}</text>
          </view>
        </view>
      </view>

      <DoodleDivider label="the stock" />

      <!-- Film stock -->
      <view :style="{ width: '100%', gap: '10px' }">
        <text :style="labelStyle">Film stock</text>
        <text v-if="loading" :style="{ fontFamily: t.font.body, fontSize: '16px', letterSpacing: t.tracking, color: t.color.muted }">
          Loading…
        </text>
        <text v-if="error" :style="{ fontFamily: t.font.body, fontSize: '16px', letterSpacing: t.tracking, color: t.color.danger }">
          {{ error }}
        </text>
        <view :style="{ display: 'flex', flexDirection: 'row', flexWrap: 'wrap', gap: '8px' }">
          <view
            v-for="s in stocks"
            :key="s.id"
            :style="chipStyle(s.id === stock)"
            @tap="stock = s.id"
          >
            <text :style="chipTextStyle(s.id === stock)">{{ s.displayName }}</text>
          </view>
        </view>
      </view>
    </view>
  </scroll-view>
</template>
