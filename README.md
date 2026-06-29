# 🧺 Picknic

> Shared event camera. Guests snap photos through the app during a wedding or
> party — everything pools into one roll that "develops" and is revealed at the
> end of the event.

**Stack:** .NET 10 / ASP.NET Core · Azure · Vue 3 · Lynx (mobile)

`dotnet` · `aspnet-core` · `csharp` · `azure` · `vue`

## Architecture

| Folder | What | Tech |
| ------ | ---- | ---- |
| [`api/`](./api) | REST API, auth, event + photo management | **.NET 10 / ASP.NET Core** |
| [`web/`](./web) | Guest-facing PWA (QR join, capture, reveal) | Vue 3 + Vite (PWA) |
| [`app/`](./app) | Native mobile app | Lynx via [`vue-lynx`](https://www.npmjs.com/package/vue-lynx) + [`@vyui`](https://www.npmjs.com/package/@vyui/kit) |

Photo blobs live in **Azure Blob Storage**; metadata in the API's database.

## Getting started

### API (.NET)
```bash
cd api
dotnet run
```

### Web (Vue PWA)
```bash
cd web
pnpm install
pnpm dev
```

### App (Lynx mobile)
```bash
cd app
pnpm install
pnpm dev   # scan with the Lynx Explorer app
```

## License

[AGPL-3.0](./LICENSE) — the code is open, but running a hosted/closed
competing service requires open-sourcing your changes. The official Picknic
service and name are operated by the maintainer.
