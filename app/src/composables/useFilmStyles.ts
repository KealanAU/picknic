// Loads the film-stock and instant-print catalogues and holds the current
// selection; a screen decides what to do with {stock, print}.
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
      const [loadedStocks, loadedPrints] = await Promise.all([listFilmStocks(), listPrintStyles()]);
      stocks.value = loadedStocks;
      prints.value = loadedPrints;
      const stockOffered = loadedStocks.some((option) => option.id === stock.value);
      const printOffered = loadedPrints.some((option) => option.id === print.value);
      if (loadedStocks.length && !stockOffered) stock.value = loadedStocks[0].id;
      if (loadedPrints.length && !printOffered) print.value = loadedPrints[0].id;
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
