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

variable "mongo_connection_string" {
  type        = string
  sensitive   = true
  description = "Connection string for MongoDB Atlas"
}
