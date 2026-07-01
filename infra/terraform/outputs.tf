output "api_url" {
  value       = "https://${azurerm_container_app.api.ingress[0].fqdn}"
  description = "Public URL of the API container app."
}

output "acr_login_server" {
  value       = azurerm_container_registry.acr.login_server
  description = "Push images here: docker push <login_server>/picknic-api:<tag>."
}

output "photos_storage_account" {
  value       = azurerm_storage_account.photos.name
  description = "Blob storage account holding guest photos."
}

output "postgres_fqdn" {
  value       = azurerm_postgresql_flexible_server.main.fqdn
  description = "Hostname of the PostgreSQL flexible server."
}

output "key_vault_uri" {
  value       = azurerm_key_vault.main.vault_uri
  description = "Base URI of the Key Vault protecting the Data Protection key ring."
}
