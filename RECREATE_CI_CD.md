Recreating CI/CD for Shadow (Azure Pipelines and GitHub Actions)

This document describes how the repository's CI/CD is configured and provides step-by-step instructions to recreate the pipeline and deployments from scratch.

Overview
- Projects target: .NET 10
- Blazor WebAssembly client + BFF + Azure Functions
- Branch -> environment mapping used in pipeline:
  - `test` / `develop` -> Test
  - `staging` -> Staging
  - `master` / `main` -> Production

Prerequisites
- .NET 10 SDK
- Azure CLI
- Azure subscription with rights to create resources
- GitHub repo access
- Azure DevOps account if using Azure Pipelines

Common concepts
- Build produces artifacts (BFF, Blazor client, Function ZIP). The Function ZIP is deployed using an Azure deployment task.
- The repo contains:
  - `azure-pipelines.yml` (Azure DevOps pipeline)
  - `.github/workflows/ci-cd.yml` (GitHub Actions workflow, optional)
  - `sync-repos.ps1` (helper script; pushes to GitHub)

Option A — Azure Pipelines (recommended if using Azure DevOps)

1) Create Azure resources

```bash
az login
az group create -n <RESOURCE_GROUP> -l UKWest
az storage account create -n <STORAGE_ACCOUNT> -g <RESOURCE_GROUP> -l UKWest --sku Standard_LRS
az functionapp create -g <RESOURCE_GROUP> -n funkygibbon-test --storage-account <STORAGE_ACCOUNT> --runtime dotnet-isolated --functions-version 4
az functionapp create -g <RESOURCE_GROUP> -n funkygibbon-staging --storage-account <STORAGE_ACCOUNT> --runtime dotnet-isolated --functions-version 4
az functionapp create -g <RESOURCE_GROUP> -n funkygibbon-func --storage-account <STORAGE_ACCOUNT> --runtime dotnet-isolated --functions-version 4
```

2) Create a service principal for the pipeline

```bash
az ad sp create-for-rbac --name "shadow-pipeline-sp" --role contributor --scopes /subscriptions/<SUBSCRIPTION_ID>/resourceGroups/<RESOURCE_GROUP> --sdk-auth
```

Save the JSON output. You'll use it to create service connections in Azure DevOps.

3) Create service connections in Azure DevOps
- Project Settings -> Service connections -> New -> Azure Resource Manager -> Service principal (manual)
- Paste the JSON credentials and give names that match pipeline variables, e.g.:
  - `Azure-Shadow-Test`
  - `Azure-Shadow-Staging`
  - `Azure-Shadow-Production`
- Grant access to pipelines (Grant access permission to all pipelines) to avoid manual approvals.

4) Create the pipeline in Azure DevOps
- Azure DevOps -> Pipelines -> New pipeline -> Select GitHub -> Authorize -> choose `markyoxall/Shadow`
- Configure using the existing YAML and select `/azure-pipelines.yml`.
- Optionally set pipeline variables in the UI (function app names, service connection names).

5) Authorize resources on first run
- If a run is blocked with "needs permission to access a resource", open the run and click the authorization banner or go to Project Settings -> Service connections and grant access.
- Approve any environment checks under Pipelines -> Environments if configured.

6) Verify
- Commit to `test` -> Test deploy
- Commit to `staging` -> Staging deploy
- Commit to `master`/`main` -> Production deploy

Option B — GitHub Actions (alternative)

1) Create Azure resources and service principal as in Option A.
2) Add GitHub repository secrets (Settings -> Secrets -> Actions):
- `AZURE_CREDENTIALS` (the service principal JSON)
- `AZURE_RESOURCE_GROUP`
- `FUNCTIONAPP_NAME_TEST`
- `FUNCTIONAPP_NAME_STAGING`
- `FUNCTIONAPP_NAME_PROD`

3) Use the existing workflow `.github/workflows/ci-cd.yml` in this repo.
- To make Actions automatically deploy to `test` on push, update the `on:` section to include push for the `test` branch (or leave as `workflow_dispatch` for manual runs).

4) Verify by committing to the corresponding branch.

Tips & Troubleshooting
- Avoid duplicate pipelines: keep either Azure Pipelines or GitHub Actions as the canonical deploy mechanism, or scope their triggers to different branches.
- If environment deploys are blocked, grant service connection permission and/or add pipeline approvals for the environment.
- If deploy tasks fail, recreate the service principal and update the service connection credentials.
- Use `curl` to test the function endpoint after deploy:

```bash
curl https://<function-app-name>.azurewebsites.net/api/walkthrough
```

If you want I can also:
- Add scripts to create the resource group and Function Apps automatically
- Add a helper to generate the service principal and the required JSON for GitHub/DevOps
Tell me which automation you want and I will add it to the repo.

Note: A PowerShell helper has been added at `scripts/create-azure-resources.ps1`. It:

- Creates the resource group and storage account (if missing)
- Creates three Function Apps (`test`, `staging`, `prod`) using the .NET 10 isolated worker
- Optionally creates Application Insights instances
- Creates a service principal and writes the SDK auth JSON to a local file

Run it locally in PowerShell (example):

```
.
\scripts\create-azure-resources.ps1 -ResourceGroup shadow-rg -StorageAccount shadowstorage123 -FunctionAppTest funkygibbon-test -FunctionAppStaging funkygibbon-staging -FunctionAppProd funkygibbon-func
```

After running: copy the generated JSON into GitHub Secrets (`AZURE_CREDENTIALS`) or use it to create Azure DevOps service connections.
