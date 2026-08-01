# Integração opcional: só é criada se var.github_repository for preenchida.
#
# Com OIDC o GitHub Actions troca um token de identidade por credenciais
# temporárias da AWS. Não existe access key nem secret guardado no
# repositório — nada para vazar ou rotacionar.

locals {
  github_enabled = var.github_repository != ""
}

resource "aws_iam_openid_connect_provider" "github" {
  count = local.github_enabled ? 1 : 0

  url            = "https://token.actions.githubusercontent.com"
  client_id_list = ["sts.amazonaws.com"]
  thumbprint_list = [
    "6938fd4d98bab03faadb97b34396831e3780aea1",
    "1c58a3a8518e8759bf075b76b750d4f2df264fcd",
  ]

  tags = { Name = "${var.project_name}-github-oidc" }
}

data "aws_iam_policy_document" "github_assume_role" {
  count = local.github_enabled ? 1 : 0

  statement {
    actions = ["sts:AssumeRoleWithWebIdentity"]

    principals {
      type        = "Federated"
      identifiers = [aws_iam_openid_connect_provider.github[0].arn]
    }

    condition {
      test     = "StringEquals"
      variable = "token.actions.githubusercontent.com:aud"
      values   = ["sts.amazonaws.com"]
    }

    # Restringe ao seu repositório. Sem esta condição, qualquer repositório
    # do GitHub poderia assumir a role.
    condition {
      test     = "StringLike"
      variable = "token.actions.githubusercontent.com:sub"
      values   = ["repo:${var.github_repository}:*"]
    }
  }
}

resource "aws_iam_role" "github_deploy" {
  count = local.github_enabled ? 1 : 0

  name               = "${var.project_name}-github-deploy"
  assume_role_policy = data.aws_iam_policy_document.github_assume_role[0].json
}

# O pipeline não precisa de acesso amplo: só manda a instância rodar
# o deploy.sh e lê o resultado.
data "aws_iam_policy_document" "github_deploy" {
  count = local.github_enabled ? 1 : 0

  statement {
    actions   = ["ssm:SendCommand"]
    resources = [aws_instance.app.arn]
  }

  statement {
    actions   = ["ssm:SendCommand"]
    resources = ["arn:aws:ssm:${var.aws_region}::document/AWS-RunShellScript"]
  }

  statement {
    actions   = ["ssm:GetCommandInvocation", "ssm:ListCommandInvocations"]
    resources = ["*"]
  }
}

resource "aws_iam_role_policy" "github_deploy" {
  count = local.github_enabled ? 1 : 0

  name   = "${var.project_name}-github-deploy"
  role   = aws_iam_role.github_deploy[0].id
  policy = data.aws_iam_policy_document.github_deploy[0].json
}
