# StarterTemplate origin

- Source repository: https://github.com/odhiamboally/base-template
- Source revision: b25fc820202a7d5d3686624fbd8f80ce9800b834
- Export date: 2026-10-06 (Africa/Nairobi)
- Destination repository: https://github.com/odhiamboally/starter-template (private)
- Product: StarterTemplate; technical namespace prefix: ST

The source revision includes merged baseline PR #79, local isolation PR #80, and Azure deployment gate PR #81. Their recorded CI checks passed; PR #79 has no submitted GitHub reviews. This is a selected merged baseline, not a claim of independent human review or runtime certification.

The initial tree was exported with git archive from the source revision, without source Git history, untracked or ignored files, build output, local user-secrets, or workstation data. Host UserSecretsId values were regenerated with the existing rename script. Source email identity, embedded development passwords, sandbox payment credentials and deployment naming were removed or replaced. Azure configuration/OIDC credentials and GitHub repository secrets/variables are not copied.

BaseTemplate remains the Banking/HR reference. StarterTemplate initially retains those modules while IAM is decoupled; business removal and fresh provider-specific migrations are subsequent phases. Never run these inherited migrations against deployed BaseTemplate databases.

No hosts, infrastructure, migrations or deployments were started during extraction. Local credentials must be configured independently. Azure provisioning and deployment require AZURE_DEPLOYMENT_ENABLED=true in this repository and newly configured OIDC/environment bindings.
