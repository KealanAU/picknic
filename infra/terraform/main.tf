terraform {
  required_version = ">= 1.6"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
  }
}

provider "azurerm" {
  features {}
}

data "azurerm_client_config" "current" {}

locals {
  name = "${var.project}-${var.environment}"
  tags = {
    project     = var.project
    environment = var.environment
    managed_by  = "terraform"
  }
}

# Random suffix to keep globally-unique names (Key Vault, Postgres) collision-free.
resource "random_string" "suffix" {
  length  = 6
  upper   = false
  special = false
  numeric = true
}

# Strong random secret used to sign guest JWTs.
resource "random_password" "guest_signing_key" {
  length  = 48
  special = false
}

# Shared secret Event Grid presents (?code=…) when calling the blob-created webhook.
resource "random_password" "eventgrid_secret" {
  length  = 32
  special = false
}

resource "azurerm_resource_group" "main" {
  name     = "rg-${local.name}"
  location = var.location
  tags     = local.tags
}

# ---- Container registry for the API image ----
resource "azurerm_container_registry" "acr" {
  name                = replace("acr${var.project}${var.environment}", "-", "")
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku                 = "Basic"
  admin_enabled       = true
  tags                = local.tags
}

# ---- Blob storage for guest photos ----
resource "azurerm_storage_account" "photos" {
  name                     = replace("st${var.project}${var.environment}", "-", "")
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  tags                     = local.tags
}

resource "azurerm_storage_container" "photos" {
  name                  = "photos"
  storage_account_id    = azurerm_storage_account.photos.id
  container_access_type = "private"
}

# Holds the ASP.NET Core Data Protection key ring.
resource "azurerm_storage_container" "dataprotection_keys" {
  name                  = "dataprotection-keys"
  storage_account_id    = azurerm_storage_account.photos.id
  container_access_type = "private"
}

# ---- Key Vault for protecting the Data Protection key ring ----
resource "azurerm_key_vault" "main" {
  name                       = substr(replace("kv${var.project}${var.environment}${random_string.suffix.result}", "-", ""), 0, 24)
  resource_group_name        = azurerm_resource_group.main.name
  location                   = azurerm_resource_group.main.location
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  rbac_authorization_enabled = true
  tags                       = local.tags
}

# Let the Terraform deployer create/manage keys in the vault.
resource "azurerm_role_assignment" "deployer_kv_crypto_officer" {
  scope                = azurerm_key_vault.main.id
  role_definition_name = "Key Vault Crypto Officer"
  principal_id         = data.azurerm_client_config.current.object_id
}

resource "azurerm_key_vault_key" "dataprotection" {
  name         = "dataprotection-key"
  key_vault_id = azurerm_key_vault.main.id
  key_type     = "RSA"
  key_size     = 2048
  key_opts     = ["wrapKey", "unwrapKey"]

  depends_on = [azurerm_role_assignment.deployer_kv_crypto_officer]
}

# ---- PostgreSQL flexible server for app data ----
resource "azurerm_postgresql_flexible_server" "main" {
  name                          = "psql-${local.name}-${random_string.suffix.result}"
  resource_group_name           = azurerm_resource_group.main.name
  location                      = azurerm_resource_group.main.location
  version                       = "16"
  administrator_login           = var.postgres_admin_username
  administrator_password        = var.postgres_admin_password
  sku_name                      = var.postgres_sku_name
  storage_mb                    = var.postgres_storage_mb
  public_network_access_enabled = true
  zone                          = "1"
  tags                          = local.tags
}

resource "azurerm_postgresql_flexible_server_database" "picknic" {
  name      = "picknic"
  server_id = azurerm_postgresql_flexible_server.main.id
  collation = "en_US.utf8"
  charset   = "UTF8"
}

# Allow other Azure services (e.g. the Container App) to reach the server.
resource "azurerm_postgresql_flexible_server_firewall_rule" "azure" {
  name             = "allow-azure-services"
  server_id        = azurerm_postgresql_flexible_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

locals {
  postgres_connection_string = join(";", [
    "Host=${azurerm_postgresql_flexible_server.main.fqdn}",
    "Database=${azurerm_postgresql_flexible_server_database.picknic.name}",
    "Username=${var.postgres_admin_username}",
    "Password=${var.postgres_admin_password}",
    "SslMode=Require",
  ])
}

# ---- Container App environment + API ----
resource "azurerm_log_analytics_workspace" "main" {
  name                = "log-${local.name}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku                 = "PerGB2018"
  retention_in_days   = 30
  tags                = local.tags
}

resource "azurerm_container_app_environment" "main" {
  name                       = "cae-${local.name}"
  resource_group_name        = azurerm_resource_group.main.name
  location                   = azurerm_resource_group.main.location
  log_analytics_workspace_id = azurerm_log_analytics_workspace.main.id
  tags                       = local.tags
}

resource "azurerm_container_app" "api" {
  name                         = "ca-${local.name}-api"
  resource_group_name          = azurerm_resource_group.main.name
  container_app_environment_id = azurerm_container_app_environment.main.id
  revision_mode                = "Single"
  tags                         = local.tags

  identity {
    type = "SystemAssigned"
  }

  registry {
    server               = azurerm_container_registry.acr.login_server
    username             = azurerm_container_registry.acr.admin_username
    password_secret_name = "acr-password"
  }

  secret {
    name  = "acr-password"
    value = azurerm_container_registry.acr.admin_password
  }

  secret {
    name  = "stripe-secret-key"
    value = var.stripe_secret_key
  }

  secret {
    name  = "db-connection-string"
    value = local.postgres_connection_string
  }

  secret {
    name  = "guest-signing-key"
    value = random_password.guest_signing_key.result
  }

  secret {
    name  = "eventgrid-secret"
    value = random_password.eventgrid_secret.result
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  template {
    min_replicas = 1
    max_replicas = 3

    container {
      name   = "api"
      image  = "${azurerm_container_registry.acr.login_server}/picknic-api:${var.api_image_tag}"
      cpu    = 0.5
      memory = "1Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }
      env {
        name        = "Stripe__SecretKey"
        secret_name = "stripe-secret-key"
      }
      env {
        name        = "ConnectionStrings__Default"
        secret_name = "db-connection-string"
      }
      env {
        name  = "Storage__AccountUrl"
        value = azurerm_storage_account.photos.primary_blob_endpoint
      }
      env {
        name        = "Auth__Guest__SigningKey"
        secret_name = "guest-signing-key"
      }
      env {
        name  = "Web__BaseUrl"
        value = var.web_origin
      }
      env {
        name  = "Cors__AllowedOrigins"
        value = var.web_origin
      }
      env {
        name  = "DataProtection__BlobUri"
        value = "${azurerm_storage_account.photos.primary_blob_endpoint}${azurerm_storage_container.dataprotection_keys.name}/keys.xml"
      }
      env {
        name  = "DataProtection__KeyVaultKeyId"
        value = azurerm_key_vault_key.dataprotection.versionless_id
      }
      env {
        name        = "EventGrid__Secret"
        secret_name = "eventgrid-secret"
      }
    }
  }
}

# ---- Event Grid: register photos only once their blob has actually landed ----
resource "azurerm_eventgrid_system_topic" "storage" {
  name                   = "egst-${local.name}"
  resource_group_name    = azurerm_resource_group.main.name
  location               = azurerm_resource_group.main.location
  source_arm_resource_id = azurerm_storage_account.photos.id
  topic_type             = "Microsoft.Storage.StorageAccounts"
  tags                   = local.tags
}

# The subscription validates the webhook at creation, so it can only be created
# after the container app is deployed and reachable — hence the toggle.
resource "azurerm_eventgrid_system_topic_event_subscription" "blob_created" {
  count               = var.enable_blob_events ? 1 : 0
  name                = "photos-blob-created"
  system_topic        = azurerm_eventgrid_system_topic.storage.name
  resource_group_name = azurerm_resource_group.main.name

  included_event_types = ["Microsoft.Storage.BlobCreated"]

  subject_filter {
    subject_begins_with = "/blobServices/default/containers/${azurerm_storage_container.photos.name}/blobs/"
  }

  webhook_endpoint {
    url = "https://${azurerm_container_app.api.ingress[0].fqdn}/api/uploads/events?code=${random_password.eventgrid_secret.result}"
  }
}

# ---- Identity-based access for the container app ----
resource "azurerm_role_assignment" "app_storage_blob" {
  scope                = azurerm_storage_account.photos.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_container_app.api.identity[0].principal_id
}

resource "azurerm_role_assignment" "app_kv_crypto_user" {
  scope                = azurerm_key_vault.main.id
  role_definition_name = "Key Vault Crypto User"
  principal_id         = azurerm_container_app.api.identity[0].principal_id
}
