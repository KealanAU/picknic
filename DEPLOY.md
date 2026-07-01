# Deploy Picknic

End-to-end steps to stand up the API on Azure. The code and Terraform are ready;
these are the account/secret/deploy actions that can't live in the repo.

## 0. Prerequisites
- Azure CLI (`az login`) with a subscription
- Terraform >= 1.6, Docker
- A Stripe account (optional — the app runs without it)

## 1. Provision infrastructure (phase 1)
```bash
cd infra/terraform
cp terraform.tfvars.example terraform.tfvars   # fill in postgres_admin_password, web_origin
terraform init
terraform apply        # enable_blob_events stays false on the first apply
```
This creates the resource group, ACR, Postgres, Blob Storage, Key Vault (Data
Protection), Log Analytics, and the Container App. It generates and wires the
guest signing key, DB connection string, storage identity + RBAC, and the Event
Grid webhook secret automatically.

Grab outputs:
```bash
terraform output acr_login_server      # push target
terraform output api_url               # public API URL
```

## 2. Build & push the API image
```bash
ACR=$(terraform -chdir=infra/terraform output -raw acr_login_server)
az acr login --name "${ACR%%.*}"
docker build -t "$ACR/picknic-api:latest" ./api
docker push "$ACR/picknic-api:latest"
```
Re-run `terraform apply` (or restart the container app revision) so it pulls the
new image. Confirm `GET <api_url>/api/health` returns `{"status":"ok"}`.

## 3. Enable blob-created events (phase 2)
Only after the app is live and reachable (Event Grid validates the endpoint when
the subscription is created):
```bash
# in terraform.tfvars
enable_blob_events = true
terraform apply
```
Now photos register from the authoritative `Microsoft.Storage.BlobCreated` event
(`POST /api/uploads/events`), not just the client callback.

## 4. Stripe (optional)
1. Set `stripe_secret_key` in `terraform.tfvars` (or the `Stripe__SecretKey`
   secret) and apply.
2. **Webhook:** Dashboard → Developers → Webhooks → add endpoint
   `https://<api_url>/api/checkout/webhook`, event `checkout.session.completed`.
   Copy the signing secret (`whsec_...`) into the `Stripe__WebhookSecret` env/secret.
3. **Prices (optional):** create Products/Prices in the Dashboard and set
   `Stripe__Prices__premium` / `Stripe__Prices__pro` to the `price_...` IDs.
   Without them, checkout uses the built-in inline pricing.
4. **Apple Pay:** works automatically on the hosted Checkout page — just enable
   Apple Pay under Dashboard → Settings → Payment methods and serve over HTTPS.
   No Apple Developer account or domain file needed for hosted Checkout.

### Local Stripe testing
```bash
stripe listen --forward-to localhost:5145/api/checkout/webhook   # prints a whsec_ dev secret
stripe trigger checkout.session.completed
```

## 5. Email (optional)
Set `Email__ConnectionString` + `Email__FromAddress` (Azure Communication
Services) to send invites, reveal notifications, and host account emails. Without
them the app logs emails instead of sending.
