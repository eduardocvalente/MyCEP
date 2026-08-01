data "aws_iam_policy_document" "ec2_assume_role" {
  statement {
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["ec2.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "app" {
  name               = "${var.project_name}-ec2-role"
  assume_role_policy = data.aws_iam_policy_document.ec2_assume_role.json
}

# Habilita o Session Manager: é o que substitui o SSH.
resource "aws_iam_role_policy_attachment" "ssm_core" {
  role       = aws_iam_role.app.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonSSMManagedInstanceCore"
}

# Leitura restrita ao parâmetro da senha — não a todo o Parameter Store.
data "aws_iam_policy_document" "read_db_password" {
  statement {
    actions   = ["ssm:GetParameter", "ssm:GetParameters"]
    resources = [aws_ssm_parameter.db_password.arn]
  }

  # A chave gerenciada alias/aws/ssm não pode ser referenciada por ARN de
  # alias numa policy de identidade. A condição ViaService restringe o
  # Decrypt a chamadas originadas do SSM.
  statement {
    actions   = ["kms:Decrypt"]
    resources = ["*"]

    condition {
      test     = "StringEquals"
      variable = "kms:ViaService"
      values   = ["ssm.${var.aws_region}.amazonaws.com"]
    }
  }
}

resource "aws_iam_role_policy" "read_db_password" {
  name   = "${var.project_name}-read-db-password"
  role   = aws_iam_role.app.id
  policy = data.aws_iam_policy_document.read_db_password.json
}

resource "aws_iam_instance_profile" "app" {
  name = "${var.project_name}-ec2-profile"
  role = aws_iam_role.app.name
}
