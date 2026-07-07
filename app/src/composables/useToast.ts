// App-wide toast queue: screens raise entries here and the single <AppToaster>
// in App.vue renders them as a sonner-style stack. Module-level state so the
// queue survives screen swaps and every caller shares one pile.
import { readonly, ref } from 'vue';

export interface ToastItem {
  id: number;
  title?: string;
  description: string;
}

// Oldest entries drop off past this so repeated failures stay a shallow pile.
const MAX_TOASTS = 4;

const toasts = ref<ToastItem[]>([]);
let nextId = 1;

function toastError(description: string, title?: string) {
  toasts.value = [...toasts.value, { id: nextId++, description, title }].slice(-MAX_TOASTS);
}

function dismiss(id: number) {
  toasts.value = toasts.value.filter((item) => item.id !== id);
}

export function useToast() {
  return { toasts: readonly(toasts), toastError, dismiss };
}
