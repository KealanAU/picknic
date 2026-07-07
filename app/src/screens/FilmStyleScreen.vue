<script setup lang="ts">
// Pick a film stock + instant-print frame and preview it live. The developed
// film look is applied server-side at reveal; here we only preview the frame
// around the raw capture. Also the reference screen for the brand token style.
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
  () => stocks.value.find((option) => option.id === stock.value)?.displayName ?? 'Film',
);

async function takePhoto() {
  const photo = await capture({ quality: 0.9 });
  if (photo) previewUri.value = toDataUri(photo);
}

function chipStyle(active: boolean) {
  return {
    // Row/column spacing lives here, not as container gap: the v-for fragment
    // anchors would collect phantom gaps on native.
    marginTop: '8px',
    marginRight: '8px',
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

</script>

<template>
  <view :style="{ width: '100%', minHeight: '100vh', backgroundColor: t.color.paper }">
    <!-- Margin spacing instead of gap: the v-if error texts and v-for chips
         leave fragment anchors that container gap would treat as children. -->
    <view :style="{ display: 'flex', flexDirection: 'column', alignItems: 'center', padding: '29px' }">
      <text :style="{ fontFamily: t.font.display, fontSize: '38px', fontWeight: '400', lineHeight: '1', letterSpacing: t.tracking, color: t.color.blue, textAlign: 'center' }">
        Choose your roll
      </text>

      <PaperCard :padding="14" :style="{ marginTop: '29px' }">
        <InstaxCard :src="previewUri" :caption="stockName" :print="print" :width="260" />
      </PaperCard>

      <StickerButton :disabled="!available" :style="{ marginTop: '29px' }" @tap="takePhoto">
        {{ busy ? 'Snapping…' : available ? '📸 Take a photo' : 'Camera unavailable' }}
      </StickerButton>
      <text v-if="cameraError" :style="{ marginTop: '29px', fontFamily: t.font.body, fontSize: '16px', letterSpacing: t.tracking, color: t.color.danger }">
        {{ cameraError }}
      </text>

      <DoodleDivider label="the frame" :style="{ marginTop: '29px' }" />

      <!-- Chips carry their own 8px top margin, so the section offsets are 21px. -->
      <view :style="{ width: '100%', marginTop: '21px' }">
        <view :style="{ display: 'flex', flexDirection: 'row', flexWrap: 'wrap' }">
          <view
            v-for="printOption in prints"
            :key="printOption.id"
            :style="chipStyle(printOption.id === print)"
            @tap="print = printOption.id"
          >
            <text :style="chipTextStyle(printOption.id === print)">{{ printOption.displayName }}</text>
          </view>
        </view>
      </view>

      <DoodleDivider label="the stock" :style="{ marginTop: '29px' }" />

      <view :style="{ width: '100%', marginTop: '21px' }">
        <text v-if="loading" :style="{ marginTop: '8px', fontFamily: t.font.body, fontSize: '16px', letterSpacing: t.tracking, color: t.color.muted }">
          Loading…
        </text>
        <text v-if="error" :style="{ marginTop: '8px', fontFamily: t.font.body, fontSize: '16px', letterSpacing: t.tracking, color: t.color.danger }">
          {{ error }}
        </text>
        <view :style="{ display: 'flex', flexDirection: 'row', flexWrap: 'wrap' }">
          <view
            v-for="stockOption in stocks"
            :key="stockOption.id"
            :style="chipStyle(stockOption.id === stock)"
            @tap="stock = stockOption.id"
          >
            <text :style="chipTextStyle(stockOption.id === stock)">{{ stockOption.displayName }}</text>
          </view>
        </view>
      </view>
    </view>
  </view>
</template>
