# Não há regra de entrada na porta 22. O acesso ao shell é pelo SSM Session
# Manager, que sai da instância para a AWS — nada de chave privada circulando
# nem de porta de SSH exposta na internet.

resource "aws_security_group" "app" {
  name        = "${var.project_name}-app-sg"
  description = "Trafego HTTP de entrada para a API"
  vpc_id      = aws_vpc.main.id

  tags = { Name = "${var.project_name}-app-sg" }
}

resource "aws_vpc_security_group_ingress_rule" "app_http" {
  security_group_id = aws_security_group.app.id
  description       = "HTTP publico"
  cidr_ipv4         = var.allowed_http_cidr
  from_port         = 80
  to_port           = 80
  ip_protocol       = "tcp"
}

resource "aws_vpc_security_group_ingress_rule" "app_https" {
  security_group_id = aws_security_group.app.id
  description       = "HTTPS, reservado para quando houver dominio"
  cidr_ipv4         = var.allowed_http_cidr
  from_port         = 443
  to_port           = 443
  ip_protocol       = "tcp"
}

# Saída liberada: a instância precisa alcançar o GHCR (pull da imagem),
# o ViaCEP, os endpoints do SSM e os repositórios de pacotes.
resource "aws_vpc_security_group_egress_rule" "app_all" {
  security_group_id = aws_security_group.app.id
  description       = "Saida liberada"
  cidr_ipv4         = "0.0.0.0/0"
  ip_protocol       = "-1"
}

# --- Banco --------------------------------------------------------------

resource "aws_security_group" "db" {
  name        = "${var.project_name}-db-sg"
  description = "Postgres acessivel apenas pela EC2 da aplicacao"
  vpc_id      = aws_vpc.main.id

  tags = { Name = "${var.project_name}-db-sg" }
}

# Referencia o security group da app, não um CIDR: se a instância for
# recriada com outro IP, a regra continua valendo.
resource "aws_vpc_security_group_ingress_rule" "db_from_app" {
  security_group_id            = aws_security_group.db.id
  description                  = "Postgres a partir da aplicacao"
  referenced_security_group_id = aws_security_group.app.id
  from_port                    = 5432
  to_port                      = 5432
  ip_protocol                  = "tcp"
}

# Sem regra de egress de propósito: o RDS não inicia conexões.
