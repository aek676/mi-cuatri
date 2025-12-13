# Copyright (c) HashiCorp, Inc.
# SPDX-License-Identifier: MPL-2.0

terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.113"
    }
    random = {
      source  = "hashicorp/random"
      version = "3.4.3"
    }
  }
  required_version = ">= 1.1.0"
}

provider "azurerm" {
  features {}
}

resource "random_pet" "suffix" {}

resource "azurerm_resource_group" "web" {
  name     = "web-${random_pet.suffix.id}-rg"
  location = "swedencentral"
}

resource "azurerm_virtual_network" "web" {
  name                = "web-${random_pet.suffix.id}-vnet"
  address_space       = ["10.0.0.0/16"]
  location            = azurerm_resource_group.web.location
  resource_group_name = azurerm_resource_group.web.name
}

resource "azurerm_subnet" "web" {
  name                 = "web-subnet"
  resource_group_name  = azurerm_resource_group.web.name
  virtual_network_name = azurerm_virtual_network.web.name
  address_prefixes     = ["10.0.1.0/24"]
}

resource "azurerm_network_security_group" "web" {
  name                = "web-${random_pet.suffix.id}-nsg"
  location            = azurerm_resource_group.web.location
  resource_group_name = azurerm_resource_group.web.name

  security_rule {
    name                       = "allow-http-8080"
    priority                   = 100
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "8080"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }

  // Optional management access if you need to SSH in later.
  security_rule {
    name                       = "allow-ssh"
    priority                   = 110
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "22"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }
}

resource "azurerm_public_ip" "web" {
  name                = "web-${random_pet.suffix.id}-pip"
  location            = azurerm_resource_group.web.location
  resource_group_name = azurerm_resource_group.web.name
  allocation_method   = "Static"
  sku                 = "Standard"
}

resource "azurerm_network_interface" "web" {
  name                = "web-${random_pet.suffix.id}-nic"
  location            = azurerm_resource_group.web.location
  resource_group_name = azurerm_resource_group.web.name

  ip_configuration {
    name                          = "web-${random_pet.suffix.id}-ipcfg"
    subnet_id                     = azurerm_subnet.web.id
    private_ip_address_allocation = "Dynamic"
    public_ip_address_id          = azurerm_public_ip.web.id
  }
}

resource "azurerm_network_interface_security_group_association" "web" {
  network_interface_id      = azurerm_network_interface.web.id
  network_security_group_id = azurerm_network_security_group.web.id
}

resource "random_password" "admin" {
  length  = 20
  special = true
}

resource "azurerm_linux_virtual_machine" "web" {
  name                = "web-${random_pet.suffix.id}"
  resource_group_name = azurerm_resource_group.web.name
  location            = azurerm_resource_group.web.location
  size                = "Standard_B1s"
  admin_username      = "azureuser"
  admin_password      = random_password.admin.result

  network_interface_ids = [azurerm_network_interface.web.id]

  disable_password_authentication = false

  os_disk {
    caching              = "ReadWrite"
    storage_account_type = "Standard_LRS"
  }

  source_image_reference {
    publisher = "Canonical"
    offer     = "0001-com-ubuntu-server-focal"
    sku       = "20_04-lts"
    version   = "latest"
  }

  custom_data = base64encode(<<-EOF
                #!/bin/bash
                apt-get update
                apt-get install -y apache2
                sed -i -e 's/80/8080/' /etc/apache2/ports.conf
                echo "Hello World" > /var/www/html/index.html
                systemctl restart apache2
                EOF
  )
}
output "web-address" {
  value = "${azurerm_public_ip.web.ip_address}:8080"
}
