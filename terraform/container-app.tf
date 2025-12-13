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

resource "azurerm_user_assigned_identity" "aca_identity" {
  name                = "identity-aca-access"
  location            = azurerm_resource_group.mi-cuatri.location
  resource_group_name = azurerm_resource_group.mi-cuatri.name
}

resource "azurerm_container_app" "micuatriapp" {
  name                         = "micuatri-app"
  container_app_environment_id = azurerm_container_app_environment.micuatrienv.id
  resource_group_name          = azurerm_resource_group.mi-cuatri.name
  revision_mode                = "Single"

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.aca_identity.id]
  }

  template {
    container {
      name   = "mi-instancia"
      image  = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
      cpu    = 0.25
      memory = "0.5Gi"
    }
  }

  ingress {
    allow_insecure_connections = false
    external_enabled           = true
    target_port                = 80
    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }
}
