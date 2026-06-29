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
