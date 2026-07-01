# 🧺 Picknic

> Shared event camera. Guests snap photos through the app during a wedding or
> party — everything pools into one roll that "develops" and is revealed at the
> end of the event.

**Stack:** .NET 10 / ASP.NET Core · Azure · Vue 3 · Lynx (mobile) · Stripe · Terraform · Docker

`dotnet` · `aspnet-core` · `csharp` · `azure` · `vue` · `terraform` · `stripe`

## Architecture

| Folder | What | Tech |
| ------ | ---- | ---- |
| [`api/`](./api) | REST API, events, photos, payments | **.NET 10 / ASP.NET Core** + Stripe |
| [`web/`](./web) | Guest-facing PWA + SSR for SEO | Nuxt 4 + [`@nuxt/ui`](https://ui.nuxt.com) (PWA) |
| [`app/`](./app) | Native mobile app | Lynx via [`vue-lynx`](https://www.npmjs.com/package/vue-lynx) + [`@vyui`](https://www.npmjs.com/package/@vyui/kit) |
| [`infra/`](./infra/terraform) | Azure infra as code | Terraform (Container Apps, ACR, Blob Storage) |

Photo blobs live in **Azure Blob Storage**; metadata in the API's database.
Payments are **optional** — set `Stripe__SecretKey` to enable event upgrades.

## Getting started

### API (.NET)
Needs a Postgres instance. Spin one up with Docker, then run the API:
```bash
docker run -d --name picknic-db -p 5432:5432 \
  -e POSTGRES_DB=picknic -e POSTGRES_USER=picknic -e POSTGRES_PASSWORD=picknic \
  postgres:17

cd api
dotnet run
```
The connection string lives in `appsettings.Development.json` (`ConnectionStrings:Default`); override it with `ConnectionStrings__Default` in production.

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

### Everything via Docker
```bash
docker compose up --build   # api on :5000, web (SSR) on :3000
```

## Payments (Stripe)

Optional. Without a key the `/api/checkout` endpoint returns `501` and the app
runs fine. To enable:

```bash
export STRIPE_SECRET_KEY=sk_test_...   # docker compose picks this up
# or set Stripe:SecretKey in api/appsettings.Development.json
```

## Infrastructure (Terraform → Azure)

```bash
cd infra/terraform
cp terraform.tfvars.example terraform.tfvars   # fill in values
terraform init
terraform apply
```

Provisions a resource group, Container Registry, Blob Storage, a PostgreSQL
flexible server, Log Analytics, and a Container App running the API. Set
`postgres_admin_password` in `terraform.tfvars`; the connection string is
injected into the Container App as `ConnectionStrings__Default`.

## License

[AGPL-3.0](./LICENSE) — the code is open, but running a hosted/closed
competing service requires open-sourcing your changes. The official Picknic
service and name are operated by the maintainer.
