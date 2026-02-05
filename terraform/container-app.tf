resource "azurerm_log_analytics_workspace" "micuatrilaw" {
  name                = "micuatri-law"
  location            = azurerm_resource_group.mi-cuatri.location
  resource_group_name = azurerm_resource_group.mi-cuatri.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

resource "azurerm_container_app_environment" "micuatrienv" {
  name                       = "micuatri-environment"
  location                   = azurerm_resource_group.mi-cuatri.location
  resource_group_name        = azurerm_resource_group.mi-cuatri.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.micuatrilaw.id
}

resource "azurerm_container_app" "micuatribackend" {
  name                         = "micuatri-backend"
  container_app_environment_id = azurerm_container_app_environment.micuatrienv.id
  resource_group_name          = azurerm_resource_group.mi-cuatri.name
  revision_mode                = "Single"

  template {
    container {
      name   = "micuatri-backend"
      image  = "gcr.io/google-samples/hello-app:1.0"
      cpu    = 0.5
      memory = "1.0Gi"

      env {
        name  = "ConnectionStrings__MongoDB"
        value = var.mongodb_connection_string
      }

      env {
        name  = "Google__ClientId"
        value = var.google_client_id
      }
      env {
        name  = "Google__ClientSecret"
        value = var.google_client_secret
      }
      env {
        name  = "Google__RedirectUri"
        value = var.google_redirect_uri
      }
    }


  }

  ingress {
    allow_insecure_connections = false
    external_enabled           = false
    target_port                = 8080
    transport                  = "http"

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  lifecycle {
    ignore_changes = [
      template[0].container[0].image,
      template[0].container[0].env,
      secret
    ]
  }
}

resource "azurerm_container_app" "micuatriapp" {
  name                         = "micuatri-frontend"
  container_app_environment_id = azurerm_container_app_environment.micuatrienv.id
  resource_group_name          = azurerm_resource_group.mi-cuatri.name
  revision_mode                = "Single"

  template {
    container {
      name   = "micuatri-frontend"
      image  = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "INTERNAL_API_BASE_URL"
        value = "https://${azurerm_container_app.micuatribackend.ingress[0].fqdn}"
      }

      env {
        name  = "HOST"
        value = "0.0.0.0"
      }

      env {
        name  = "PORT"
        value = "4321"
      }
    }
  }

  ingress {
    allow_insecure_connections = false
    external_enabled           = true
    target_port                = 4321

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  lifecycle {
    ignore_changes = [
      template[0].container[0].image,
      template[0].container[0].env
    ]
  }
}


