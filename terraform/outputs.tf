output "container_app_url" {
  description = "The URL of the Azure Container App"
  value       = "https://${azurerm_container_app.micuatriapp.ingress[0].fqdn}"
}
