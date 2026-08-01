resource "random_password" "db" {
  length = 32

  # O Npgsql aceita, mas '@', '/' e '"' quebram connection strings montadas
  # por concatenação e URIs postgres://.
  special          = true
  override_special = "!#%*-_=+"
}

# Parameter Store SecureString em vez do Secrets Manager: mesma criptografia
# via KMS, mas o tier padrão é gratuito (o Secrets Manager cobra US$0,40 por
# segredo por mês).
resource "aws_ssm_parameter" "db_password" {
  name        = "/${var.project_name}/${var.environment}/db/password"
  description = "Senha master do RDS, gerada pelo Terraform"
  type        = "SecureString"
  value       = random_password.db.result

  tags = { Name = "${var.project_name}-db-password" }
}

resource "aws_db_instance" "postgres" {
  identifier     = "${var.project_name}-db"
  engine         = "postgres"
  engine_version = var.db_engine_version
  instance_class = var.db_instance_class

  db_name  = var.db_name
  username = var.db_username
  password = random_password.db.result
  port     = 5432

  allocated_storage = var.db_allocated_storage
  storage_type      = "gp2" # o free tier cobre gp2, não gp3
  storage_encrypted = true

  # Autoscaling desligado: com ele um bug de escrita em loop viraria fatura.
  max_allocated_storage = 0

  db_subnet_group_name   = aws_db_subnet_group.main.name
  vpc_security_group_ids = [aws_security_group.db.id]
  publicly_accessible    = false
  multi_az               = false # free tier é single-AZ

  # O plano gratuito novo da AWS (contas a partir de jul/2025) limita a
  # retenção de backup. 7 dias é rejeitado com FreeTierRestrictionError.
  # Em conta paga, 7 a 14 dias é o razoável.
  backup_retention_period = var.db_backup_retention_days
  backup_window           = "06:00-07:00" # 03:00 em Brasília
  maintenance_window      = "sun:07:30-sun:08:30"

  auto_minor_version_upgrade = true
  apply_immediately          = true

  # Performance Insights não é gratuito nas classes t4g.
  performance_insights_enabled = false

  # Projeto de portfólio: permite destruir sem snapshot preso na conta.
  # Em ambiente real, inverta os dois.
  skip_final_snapshot = true
  deletion_protection = false

  tags = { Name = "${var.project_name}-db" }
}
