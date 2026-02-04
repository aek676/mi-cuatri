resource "azurerm_log_analytics_workspace" "micuatrilaw" {
  name                = "micuatri-law"
  resource_group_name = azurerm_resource_group.mi-cuatri.name
  location            = azurerm_resource_group.mi-cuatri.location
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

  secret {
    name  = "mongo-conn"
    value = var.mongo_connection_string
  }

  template {
    container {
      name   = "mi-cuatri-backend"
      image  = "aek676/mi-cuatri-backend:latest"
      cpu    = 0.5
      memory = "1.0Gi"

      env {
        name        = "ConnectionStrings__MongoDb"
        secret_name = "mongo-conn"
      }
    }
  }

  ingress {
    allow_insecure_connections = false
    external_enabled           = false # <--- IMPORTANTE: False = Solo interno
    target_port                = 8080  # Puerto donde escucha tu .NET (revisa tu Dockerfile)
    transport                  = "http"
    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  lifecycle {
    ignore_changes = [
      template[0].container[0].image
    ]
  }
}

# --- 3. FRONTEND (Astro) ---

resource "azurerm_container_app" "micuatriapp" {
  name                         = "micuatri-frontend" # Le cambié el nombre para ser más explícito
  container_app_environment_id = azurerm_container_app_environment.micuatrienv.id
  resource_group_name          = azurerm_resource_group.mi-cuatri.name
  revision_mode                = "Single"

  template {
    container {
      name   = "mi-cuatri-frontend"
      image  = "aek676/mi-cuatri-frontend:latest"
      cpu    = 0.25
      memory = "0.5Gi"

      # AQUÍ ESTÁ LA MAGIA: Conectamos el frontend con el backend
      env {
        name = "INTERNAL_API_BASE_URL"
        # Terraform lee la URL del backend que acabamos de crear y la inyecta aquí.
        # "https://" es necesario porque Container Apps usa TLS internamente.
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
    external_enabled           = true # <--- True = Público
    target_port                = 4321
    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  lifecycle {
    ignore_changes = [
      template[0].container[0].image
    ]
  }
}

# Output para que sepas dónde entrar
output "frontend_url" {
  value = azurerm_container_app.micuatriapp.ingress[0].fqdn
}
