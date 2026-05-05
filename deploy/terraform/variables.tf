variable "environment" {
  description = "Environment name (staging, production)"
  type        = string
  default     = "staging"
}

variable "location" {
  description = "Azure region"
  type        = string
  default     = "westeurope"
}

variable "kubernetes_version" {
  description = "Kubernetes version"
  type        = string
  default     = "1.28.5"
}

variable "system_node_count" {
  description = "Initial system node count"
  type        = number
  default     = 2
}

variable "system_node_max" {
  description = "Maximum system nodes"
  type        = number
  default     = 4
}

variable "system_node_size" {
  description = "VM size for system nodes"
  type        = string
  default     = "Standard_D2s_v3"
}

variable "workload_node_count" {
  description = "Initial workload node count"
  type        = number
  default     = 3
}

variable "workload_node_max" {
  description = "Maximum workload nodes"
  type        = number
  default     = 10
}

variable "workload_node_size" {
  description = "VM size for workload nodes"
  type        = string
  default     = "Standard_D4s_v3"
}

variable "tags" {
  description = "Resource tags"
  type        = map(string)
  default = {
    Project     = "Libr4"
    ManagedBy   = "Terraform"
  }
}
