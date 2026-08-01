# Busca a AMI mais recente do Amazon Linux 2023 pelo Parameter Store público
# da AWS, em vez de fixar um ami-xxxx que envelhece e muda por região.
data "aws_ssm_parameter" "al2023" {
  name = "/aws/service/ami-amazon-linux-latest/al2023-ami-kernel-6.1-x86_64"
}

resource "aws_instance" "app" {
  ami           = data.aws_ssm_parameter.al2023.value
  instance_type = var.instance_type

  subnet_id              = aws_subnet.public.id
  vpc_security_group_ids = [aws_security_group.app.id]
  iam_instance_profile   = aws_iam_instance_profile.app.name

  # IMDSv2 obrigatório: bloqueia o roubo de credenciais da instance role
  # por SSRF, que é o vetor clássico contra o metadata endpoint.
  metadata_options {
    http_endpoint               = "enabled"
    http_tokens                 = "required"
    http_put_response_hop_limit = 2 # os containers precisam de 1 salto extra
  }

  root_block_device {
    volume_type           = "gp3"
    volume_size           = var.root_volume_size
    encrypted             = true
    delete_on_termination = true
  }

  user_data_replace_on_change = true
  user_data = templatefile("${path.module}/user_data.sh.tftpl", {
    aws_region           = var.aws_region
    container_image      = var.container_image
    db_host              = aws_db_instance.postgres.address
    db_name              = var.db_name
    db_username          = var.db_username
    db_password_ssm_path = aws_ssm_parameter.db_password.name
  })

  tags = { Name = "${var.project_name}-app" }

  depends_on = [aws_db_instance.postgres]
}

# IP fixo, para o endereço da demo não mudar a cada stop/start da instância.
# Desde fev/2024 todo IPv4 público é cobrado (~US$3,60/mês), inclusive
# anexado — o free tier cobre 750h/mês.
resource "aws_eip" "app" {
  domain   = "vpc"
  instance = aws_instance.app.id

  tags = { Name = "${var.project_name}-eip" }

  depends_on = [aws_internet_gateway.main]
}
