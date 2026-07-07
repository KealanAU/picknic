<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { VyIcon } from '@vyui/kit';
import { dateFromIso, isoDate } from '../lib/dates';
import { t } from '../theme/tokens';

const props = withDefaults(
  defineProps<{
    start: string;
    end: string;
    disabled?: boolean;
    min?: string;
    max?: string;
  }>(),
  { disabled: false },
);

const emit = defineEmits<{
  'update:start': [value: string];
  'update:end': [value: string];
}>();

type CalendarDay = {
  date: Date;
  value: string;
  day: number;
  inMonth: boolean;
  disabled: boolean;
};

const open = ref(false);
const visibleMonth = ref(startOfMonth(dateFromIso(props.start) ?? new Date()));

const weekDays = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
const fullDateFormatter = new Intl.DateTimeFormat(undefined, {
  weekday: 'long',
  month: 'short',
  day: 'numeric',
  year: 'numeric',
});
const shortDateFormatter = new Intl.DateTimeFormat(undefined, {
  weekday: 'short',
  month: 'short',
  day: 'numeric',
});
const monthFormatter = new Intl.DateTimeFormat(undefined, { month: 'long', year: 'numeric' });

function startOfMonth(date: Date) {
  return new Date(date.getFullYear(), date.getMonth(), 1);
}

function addMonths(date: Date, months: number) {
  return new Date(date.getFullYear(), date.getMonth() + months, 1);
}

function sameMonth(left: Date, right: Date) {
  return left.getFullYear() === right.getFullYear() && left.getMonth() === right.getMonth();
}

const rangeLabel = computed(() => {
  const startDate = dateFromIso(props.start);
  if (!startDate) return 'Pick your days';
  if (props.start === props.end) return fullDateFormatter.format(startDate);
  const endDate = dateFromIso(props.end);
  if (!endDate) return `${shortDateFormatter.format(startDate)} – pick the last day`;
  return `${shortDateFormatter.format(startDate)} – ${shortDateFormatter.format(endDate)}`;
});

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
      date,
      value,
      day: date.getDate(),
      inMonth: sameMonth(date, monthStart),
      disabled: beforeMin || afterMax,
    };
  });
}

// Computed so the 42-day grid is only built once the calendar panel actually renders;
// building it eagerly made opening the settings tray janky.
const monthLabel = computed(() => monthFormatter.format(visibleMonth.value));
const calendarDays = computed(() => buildCalendarDays(visibleMonth.value));

watch(
  () => props.start,
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

  const pickingEnd = props.start && !props.end;
  if (!pickingEnd) {
    emit('update:start', day.value);
    emit('update:end', '');
    return;
  }

  if (day.value < props.start) {
    // Tapped before the first day: treat it as the new start and keep picking.
    emit('update:start', day.value);
    emit('update:end', props.start);
  } else {
    emit('update:end', day.value);
  }
  open.value = false;
}

function dayClass(day: CalendarDay) {
  let cls = 'pk-dp-day';
  if (!day.inMonth) cls += ' pk-dp-day--out';
  if (day.disabled) cls += ' pk-dp-day--disabled';
  if (day.value === props.start || day.value === props.end) cls += ' pk-dp-day--selected';
  else if (props.start && props.end && day.value > props.start && day.value < props.end) cls += ' pk-dp-day--between';
  return cls;
}

function dayTextClass(day: CalendarDay) {
  return day.value === props.start || day.value === props.end
    ? 'pk-dp-day-text pk-dp-day-text--selected'
    : 'pk-dp-day-text';
}
</script>

<template>
  <view :class="disabled ? 'pk-dp-root pk-dp-root--disabled' : 'pk-dp-root'">
    <view :class="open ? 'pk-dp-trigger pk-dp-trigger--open' : 'pk-dp-trigger'" @tap="toggle">
      <view class="pk-dp-trigger-label">
        <text class="pk-dp-kicker">Party days</text>
        <text class="pk-dp-value">{{ rangeLabel }}</text>
      </view>
      <view class="pk-dp-icon-slot">
        <VyIcon name="streamline-freehand:calendar-grid" :style="{ color: open ? t.color.ink : t.color.blue }" />
      </view>
    </view>

    <view v-if="open" class="pk-dp-panel">
      <view class="pk-dp-panel-head">
        <view class="pk-dp-nav" @tap="moveMonth(-1)">
          <VyIcon name="lucide:chevron-left" :style="{ color: t.color.ink }" />
        </view>
        <text class="pk-dp-month">{{ monthLabel }}</text>
        <view class="pk-dp-nav" @tap="moveMonth(1)">
          <VyIcon name="lucide:chevron-right" :style="{ color: t.color.ink }" />
        </view>
      </view>

      <view class="pk-dp-weekdays">
        <view v-for="weekDay in weekDays" :key="weekDay" class="pk-dp-weekday">
          <text class="pk-dp-weekday-text">{{ weekDay }}</text>
        </view>
      </view>

      <!-- Cells are keyed by grid position, not date, so month navigation patches
           text/classes on the existing 42 nodes instead of recreating the grid. -->
      <view class="pk-dp-grid">
        <view v-for="(day, index) in calendarDays" :key="index" :class="dayClass(day)" @tap="select(day)">
          <text :class="dayTextClass(day)">{{ day.day }}</text>
        </view>
      </view>
    </view>
  </view>
</template>
