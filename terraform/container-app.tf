resource "azurerm_log_analytics_workspace" "micuatrilaw" {
  name                = "micuatri-law"
  resource_group_name = azurerm_resource_group.mi-cuatri.name
  location            = azurerm_resource_group.mi-cuatri.location
}

resource "azurerm_container_app_environment" "micuatrienv" {
  name                       = "micuatri-environment"
  location                   = azurerm_resource_group.mi-cuatri.location
  resource_group_name        = azurerm_resource_group.mi-cuatri.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.micuatrilaw.id
}

resource "azurerm_container_app" "micuatriapp" {
  name                         = "micuatri-app"
  container_app_environment_id = azurerm_container_app_environment.micuatrienv.id
  resource_group_name          = azurerm_resource_group.mi-cuatri.name
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
  }

  registry {
    server   = azurerm_container_registry.acr.login_server
  }

  template {
    container {
      name   = "mi-instancia"
      image  = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
      cpu    = 0.25
      memory = "0.5Gi"
    }
  }
}

resource "azurerm_role_assignment" "acr_pull" {
  scope                = azurerm_container_registry.acr.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_container_app.micuatriapp.identity[0].principal_id
}



