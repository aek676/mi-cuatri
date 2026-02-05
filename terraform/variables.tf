variable "aca_name" {
  default     = "micuatri-app"
  type        = string
  description = "Name for Azure Container App"
}

variable "location" {
  default     = "swedencentral"
  type        = string
  description = "Location of Azure resources"
}

variable "mongodb_connection_string" {
  type      = string
  sensitive = true
}

variable "google_client_id" {
  type      = string
  sensitive = true
}

variable "google_client_secret" {
  type      = string
  sensitive = true
}

variable "google_redirect_uri" {
  type = string
}
