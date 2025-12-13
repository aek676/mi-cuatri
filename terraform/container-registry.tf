resource "azurerm_container_registry" "micuatriacr" {
  name                = "micuatriacr"
  resource_group_name = azurerm_resource_group.mi-cuatri.name
  location            = azurerm_resource_group.mi-cuatri.location
  sku                 = "Basic"
  admin_enabled       = false
}
