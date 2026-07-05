// Loads the film-stock and instant-print catalogues from the API and holds the
// current selection. Pure selection state — a screen decides what to do with it
// (preview, or pass {stock, print} to developPhoto).
import { onMounted, readonly, ref } from 'vue';
import {
  listFilmStocks,
  listPrintStyles,
  type FilmStockInfo,
  type PrintStyleInfo,
} from '../api/photos';

export function useFilmStyles(defaultStock = 'portra400', defaultPrint = 'none') {
  const stocks = ref<FilmStockInfo[]>([]);
  const prints = ref<PrintStyleInfo[]>([]);
  const stock = ref(defaultStock);
  const print = ref(defaultPrint);
  const loading = ref(false);
  const error = ref<string | null>(null);

  async function load(): Promise<void> {
    loading.value = true;
    error.value = null;
    try {
      const [s, p] = await Promise.all([listFilmStocks(), listPrintStyles()]);
      stocks.value = s;
      prints.value = p;
      // Fall back to the first available option if our default isn't offered.
      if (s.length && !s.some((x) => x.id === stock.value)) stock.value = s[0].id;
      if (p.length && !p.some((x) => x.id === print.value)) print.value = p[0].id;
    } catch (e) {
      error.value = e instanceof Error ? e.message : String(e);
    } finally {
      loading.value = false;
    }
  }

  onMounted(load);

  return {
    stocks: readonly(stocks),
    prints: readonly(prints),
    stock,
    print,
    loading: readonly(loading),
    error: readonly(error),
    reload: load,
  };
}
