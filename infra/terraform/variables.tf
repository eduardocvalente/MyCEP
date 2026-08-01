variable "project_name" {
  description = "Prefixo aplicado ao nome de todos os recursos."
  type        = string
  default     = "mycep"
}

variable "environment" {
  description = "Ambiente lógico, usado nas tags."
  type        = string
  default     = "prod"
}

variable "aws_region" {
  description = "Região da AWS. sa-east-1 reduz a latência até o ViaCEP, mas é mais cara que us-east-1 depois do free tier."
  type        = string
  default     = "sa-east-1"
}

variable "vpc_cidr" {
  description = "Bloco CIDR da VPC."
  type        = string
  default     = "10.0.0.0/16"
}

variable "instance_type" {
  description = <<-EOT
    Tipo da EC2. Em sa-east-1 o free tier cobre t2.micro (750h/mês);
    t3.micro tem CPU melhor mas confirme a elegibilidade em
    Billing > Free Tier antes de trocar.
  EOT
  type        = string
  default     = "t3.micro"
}

variable "root_volume_size" {
  description = "Tamanho do disco da EC2 em GB. O free tier cobre 30GB de EBS no total."
  type        = number
  default     = 20
}

variable "db_instance_class" {
  description = "Classe do RDS. db.t4g.micro é elegível ao free tier e mais barata que db.t3.micro."
  type        = string
  default     = "db.t4g.micro"
}

variable "db_engine_version" {
  description = "Versão maior do PostgreSQL. A AWS resolve a menor mais recente."
  type        = string
  default     = "16"
}

variable "db_allocated_storage" {
  description = "Armazenamento do RDS em GB. O free tier cobre 20GB."
  type        = number
  default     = 20
}

variable "db_backup_retention_days" {
  description = <<-EOT
    Dias de retenção dos backups automáticos. O plano gratuito da AWS
    (contas criadas a partir de jul/2025) rejeita valores altos com
    FreeTierRestrictionError; use 1. Em conta paga, 7 é o usual.
    0 desativa backup automático e o point-in-time recovery junto.
  EOT
  type        = number
  default     = 1
}

variable "db_name" {
  description = "Nome do banco criado na instância."
  type        = string
  default     = "mycep"
}

variable "db_username" {
  description = "Usuário master do RDS. A senha é gerada pelo Terraform e guardada no SSM Parameter Store."
  type        = string
  default     = "mycep_app"
}

variable "container_image" {
  description = "Imagem publicada no GHCR, ex.: ghcr.io/<usuario>/mycep-api:latest"
  type        = string
}

variable "allowed_http_cidr" {
  description = "De onde a API aceita tráfego HTTP. Deixe aberto para a demo do portfólio ou restrinja ao seu IP."
  type        = string
  default     = "0.0.0.0/0"
}

variable "github_repository" {
  description = "Repositório no formato owner/repo, autorizado a assumir a role de deploy via OIDC. Deixe vazio para não criar a integração."
  type        = string
  default     = ""
}
