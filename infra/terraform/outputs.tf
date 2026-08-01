output "api_url" {
  description = "Endereço público da API."
  value       = "http://${aws_eip.app.public_ip}"
}

output "health_check_url" {
  description = "Use para confirmar que a aplicação subiu e enxerga o banco."
  value       = "http://${aws_eip.app.public_ip}/health"
}

output "example_request" {
  description = "Consulta de exemplo."
  value       = "curl http://${aws_eip.app.public_ip}/api/ZipCodeLookup/01001000"
}

output "instance_id" {
  description = "ID da EC2."
  value       = aws_instance.app.id
}

output "ssm_session_command" {
  description = "Abre um shell na instância sem SSH."
  value       = "aws ssm start-session --target ${aws_instance.app.id} --region ${var.aws_region}"
}

output "db_endpoint" {
  description = "Endpoint do RDS (acessível apenas de dentro da VPC)."
  value       = aws_db_instance.postgres.address
}

output "db_password_ssm_path" {
  description = "Caminho no Parameter Store onde a senha do banco foi guardada."
  value       = aws_ssm_parameter.db_password.name
}

output "github_deploy_role_arn" {
  description = "ARN a configurar no secret AWS_DEPLOY_ROLE_ARN do repositório."
  value       = local.github_enabled ? aws_iam_role.github_deploy[0].arn : "(nao configurado: defina github_repository)"
}
