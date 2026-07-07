<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { VyIcon } from '@vyui/kit';
import { t } from '../theme/tokens';

const props = withDefaults(
  defineProps<{
    modelValue: string;
    disabled?: boolean;
    min?: string;
    max?: string;
  }>(),
  { disabled: false },
);

const emit = defineEmits<{
  'update:modelValue': [value: string];
}>();

type CalendarDay = {
  key: string;
  date: Date;
  value: string;
  day: number;
  inMonth: boolean;
  disabled: boolean;
};

const open = ref(false);
const visibleMonth = ref(startOfMonth(dateFromIso(props.modelValue) ?? new Date()));
const calendarDays = ref<CalendarDay[]>([]);
const monthLabel = ref('');

const weekDays = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
const fullDateFormatter = new Intl.DateTimeFormat(undefined, {
  weekday: 'long',
  month: 'short',
  day: 'numeric',
  year: 'numeric',
});
const monthFormatter = new Intl.DateTimeFormat(undefined, { month: 'long', year: 'numeric' });

function isoDate(date: Date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function dateFromIso(value?: string) {
  if (!value) return null;
  const [year, month, day] = value.split('-').map(Number);
  if (!year || !month || !day) return null;

  const date = new Date(year, month - 1, day);
  if (date.getFullYear() !== year || date.getMonth() !== month - 1 || date.getDate() !== day) return null;
  return date;
}

function startOfMonth(date: Date) {
  return new Date(date.getFullYear(), date.getMonth(), 1);
}

function addMonths(date: Date, months: number) {
  return new Date(date.getFullYear(), date.getMonth() + months, 1);
}

function sameMonth(left: Date, right: Date) {
  return left.getFullYear() === right.getFullYear() && left.getMonth() === right.getMonth();
}

function formatDate(value: string) {
  const date = dateFromIso(value);
  if (!date) return value || 'Pick a date';
  return fullDateFormatter.format(date);
}

const minDate = computed(() => dateFromIso(props.min));
const maxDate = computed(() => dateFromIso(props.max));

function buildCalendarDays(monthStart: Date) {
  const firstGridDate = new Date(monthStart);
  firstGridDate.setDate(monthStart.getDate() - monthStart.getDay());

  return Array.from({ length: 42 }, (_, index): CalendarDay => {
    const date = new Date(firstGridDate);
    date.setDate(firstGridDate.getDate() + index);
    const value = isoDate(date);
    const beforeMin = minDate.value ? date < minDate.value : false;
    const afterMax = maxDate.value ? date > maxDate.value : false;

    return {
      key: value,
      date,
      value,
      day: date.getDate(),
      inMonth: sameMonth(date, monthStart),
      disabled: beforeMin || afterMax,
    };
  });
}

function preloadVisibleMonth() {
  monthLabel.value = monthFormatter.format(visibleMonth.value);
  calendarDays.value = buildCalendarDays(visibleMonth.value);
}

watch([visibleMonth, () => props.min, () => props.max], preloadVisibleMonth, { immediate: true });

watch(
  () => props.modelValue,
  (value) => {
    const date = dateFromIso(value);
    if (date) visibleMonth.value = startOfMonth(date);
  },
);

function toggle() {
  if (props.disabled) return;
  open.value = !open.value;
}

function moveMonth(months: number) {
  if (props.disabled) return;
  visibleMonth.value = addMonths(visibleMonth.value, months);
}

function select(day: CalendarDay) {
  if (props.disabled || day.disabled) return;
  emit('update:modelValue', day.value);
  visibleMonth.value = startOfMonth(day.date);
  open.value = false;
}

function dayStyle(day: CalendarDay) {
  const selected = day.value === props.modelValue;

  return {
    width: '14.2857%',
    height: '42px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: '1px',
    borderStyle: 'solid',
    borderColor: selected ? t.color.blue : 'transparent',
    backgroundColor: selected ? t.color.blue : day.inMonth ? '#ffffff' : t.color.paper,
    opacity: day.disabled ? 0.35 : day.inMonth ? 1 : 0.42,
  };
}

function dayTextStyle(day: CalendarDay) {
  return {
    fontFamily: t.font.body,
    fontSize: '15px',
    letterSpacing: t.tracking,
    color: day.value === props.modelValue ? '#ffffff' : t.color.ink,
  };
}
</script>

<template>
  <view :style="{ display: 'flex', flexDirection: 'column', gap: '6px', opacity: disabled ? 0.55 : 1 }">
    <view
      :style="{
        minHeight: '52px',
        display: 'flex',
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'space-between',
        gap: '12px',
        paddingTop: '10px',
        paddingRight: '12px',
        paddingBottom: '10px',
        paddingLeft: '12px',
        borderWidth: '1px',
        borderStyle: 'solid',
        borderColor: open ? t.color.blue : t.color.line,
        backgroundColor: '#ffffff',
      }"
      @tap="toggle"
    >
      <view :style="{ display: 'flex', flexDirection: 'column', gap: '2px' }">
        <text :style="{ fontFamily: t.font.body, fontSize: '12px', letterSpacing: t.tracking, textTransform: 'uppercase', color: t.color.muted }">
          Selected date
        </text>
        <text :style="{ fontFamily: t.font.body, fontSize: '16px', letterSpacing: t.tracking, color: t.color.ink }">
          {{ formatDate(modelValue) }}
        </text>
      </view>
      <view :style="{ width: '40px', height: '40px', display: 'flex', alignItems: 'center', justifyContent: 'center' }">
        <VyIcon name="streamline-freehand:calendar-grid" :style="{ color: open ? t.color.ink : t.color.blue }" />
      </view>
    </view>

    <view
      :style="{
        height: open ? '330px' : '0px',
        opacity: open ? 1 : 0,
        overflow: 'hidden',
        pointerEvents: open ? 'auto' : 'none',
        display: 'flex',
        flexDirection: 'column',
        borderWidth: open ? '1px' : '0px',
        borderStyle: 'solid',
        borderColor: t.color.line,
        backgroundColor: '#ffffff',
      }"
    >
      <view
        :style="{
          minHeight: '48px',
          display: 'flex',
          flexDirection: 'row',
          alignItems: 'center',
          justifyContent: 'space-between',
          paddingTop: '8px',
          paddingRight: '8px',
          paddingBottom: '8px',
          paddingLeft: '8px',
          borderBottomWidth: '1px',
          borderBottomStyle: 'solid',
          borderBottomColor: t.color.line,
        }"
      >
        <view :style="{ width: '40px', height: '36px', display: 'flex', alignItems: 'center', justifyContent: 'center' }" @tap="moveMonth(-1)">
          <VyIcon name="lucide:chevron-left" :style="{ color: t.color.ink }" />
        </view>
        <text :style="{ fontFamily: t.font.body, fontSize: '16px', letterSpacing: t.tracking, color: t.color.ink }">
          {{ monthLabel }}
        </text>
        <view :style="{ width: '40px', height: '36px', display: 'flex', alignItems: 'center', justifyContent: 'center' }" @tap="moveMonth(1)">
          <VyIcon name="lucide:chevron-right" :style="{ color: t.color.ink }" />
        </view>
      </view>

      <view :style="{ display: 'flex', flexDirection: 'row', paddingTop: '8px', paddingRight: '8px', paddingLeft: '8px' }">
        <view
          v-for="weekDay in weekDays"
          :key="weekDay"
          :style="{ width: '14.2857%', display: 'flex', alignItems: 'center', justifyContent: 'center', paddingBottom: '8px' }"
        >
          <text :style="{ fontFamily: t.font.body, fontSize: '11px', letterSpacing: t.tracking, textTransform: 'uppercase', color: t.color.muted }">
            {{ weekDay }}
          </text>
        </view>
      </view>

      <view :style="{ display: 'flex', flexDirection: 'row', flexWrap: 'wrap', paddingRight: '8px', paddingBottom: '8px', paddingLeft: '8px' }">
        <view v-for="day in calendarDays" :key="day.key" :style="dayStyle(day)" @tap="select(day)">
          <text :style="dayTextStyle(day)">{{ day.day }}</text>
        </view>
      </view>
    </view>
  </view>
</template>
