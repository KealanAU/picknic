variable "project" {
  type        = string
  default     = "picknic"
  description = "Short project slug used in resource names."
}

variable "environment" {
  type        = string
  default     = "dev"
  description = "Deployment environment (dev, staging, prod)."
}

variable "enable_blob_events" {
  type        = bool
  default     = false
  description = "Create the Event Grid subscription to the API's blob-created webhook. Enable only after the container app is deployed and reachable — Event Grid validates the endpoint when the subscription is created."
}

variable "location" {
  type        = string
  default     = "uksouth"
  description = "Azure region."
}

variable "api_image_tag" {
  type        = string
  default     = "latest"
  description = "Tag of the picknic-api image to deploy from ACR."
}

variable "stripe_secret_key" {
  type        = string
  default     = ""
  sensitive   = true
  description = "Stripe secret key. Leave empty to run without payments."
}

variable "postgres_admin_username" {
  type        = string
  default     = "picknicadmin"
  description = "Administrator login for the PostgreSQL flexible server."
}

variable "postgres_admin_password" {
  type        = string
  sensitive   = true
  description = "Administrator password for the PostgreSQL flexible server."
}

variable "postgres_sku_name" {
  type        = string
  default     = "B_Standard_B1ms"
  description = "SKU for the PostgreSQL flexible server (e.g. B_Standard_B1ms, GP_Standard_D2s_v3)."
}

variable "postgres_storage_mb" {
  type        = number
  default     = 32768
  description = "Storage in MB for the PostgreSQL flexible server."
}

variable "web_origin" {
  type        = string
  default     = "https://localhost:3000"
  description = "Public web app origin (used for Web__BaseUrl and CORS allowed origins)."
}
