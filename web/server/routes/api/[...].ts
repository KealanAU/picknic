// Runtime proxy: forwards /api/** to the .NET backend using the runtime
// `apiBase` (NUXT_API_BASE). Done at request time — not baked at build — so the
// same image points at localhost in dev and the API service in Docker/prod.
export default defineEventHandler((event) => {
  const { apiBase } = useRuntimeConfig();
  return proxyRequest(event, `${apiBase}${event.path}`);
});
