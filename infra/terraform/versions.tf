terraform {
  required_version = ">= 1.6"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.60"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }

  # O state guarda a senha do banco em texto plano. Enquanto for local,
  # nunca versione o terraform.tfstate (já coberto pelo .gitignore).
  # Para mover ao S3, crie o bucket com versionamento e descomente:
  #
  # backend "s3" {
  #   bucket       = "mycep-tfstate-<sufixo-unico>"
  #   key          = "prod/terraform.tfstate"
  #   region       = "sa-east-1"
  #   encrypt      = true
  #   use_lockfile = true
  # }
}

provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Project     = var.project_name
      Environment = var.environment
      ManagedBy   = "terraform"
    }
  }
}
