output "frontend_url" {
  value = "https://${azurerm_container_app.micuatriapp.ingress[0].fqdn}"
}

output "backend_internal_url" {
  value = "https://${azurerm_container_app.micuatribackend.ingress[0].fqdn}"
}
