<#
.SYNOPSIS
Creates Azure resources required by the Shadow portfolio pipeline and prints a service principal JSON for CI usage.

.DESCRIPTION
This helper creates a resource group, storage account and three Azure Function Apps (test, staging, production)
and creates a service principal (SDK auth JSON) which you can paste into GitHub Secrets (`AZURE_CREDENTIALS`) or
use when creating an Azure DevOps service connection.

The script is intentionally interactive and idempotent where possible. It does not change pipelines or push
credentials to any remote - it only prints where to store the produced JSON.

.PARAMETER ResourceGroup
Name of the Azure resource group to create/use.

.PARAMETER Location
Azure region (default: UKWest).

.PARAMETER StorageAccount
Storage account name to use/create (must be globally unique, lower-case, 3-24 chars). If omitted a name will be generated.

.PARAMETER FunctionAppTest
Function App name for Test environment.

.PARAMETER FunctionAppStaging
Function App name for Staging environment.

.PARAMETER FunctionAppProd
Function App name for Production environment.

.PARAMETER SubscriptionId
(Optional) Azure subscription id to target. If omitted the script will use the current subscription from `az account show`.

.PARAMETER CreateAppInsights
Switch to create Application Insights for each Function App.

.EXAMPLE
PS> .\scripts\create-azure-resources.ps1 -ResourceGroup shadow-rg -StorageAccount shadowstorage123 -FunctionAppTest funkygibbon-test -FunctionAppStaging funkygibbon-staging -FunctionAppProd funkygibbon-func -SubscriptionId xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
#>
[CmdletBinding(SupportsShouldProcess=$true)]
param(
    [Parameter(Mandatory=$true)] [string]$ResourceGroup,
    [Parameter(Mandatory=$false)] [string]$Location = 'UKWest',
    [Parameter(Mandatory=$false)] [string]$StorageAccount = '',
    [Parameter(Mandatory=$true)] [string]$FunctionAppTest,
    [Parameter(Mandatory=$true)] [string]$FunctionAppStaging,
    [Parameter(Mandatory=$true)] [string]$FunctionAppProd,
    [Parameter(Mandatory=$false)] [string]$SubscriptionId = '',
    [switch]$CreateAppInsights
)

function Ensure-AzCli {
    if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
        Write-Error "Azure CLI (az) not found in PATH. Install from https://aka.ms/azcli and retry."
        exit 1
    }
}

function Get-SubscriptionId {
    param([string]$SubscriptionId)
    if ($SubscriptionId) { return $SubscriptionId }
    $current = az account show --query id -o tsv 2>$null
    if (-not $current) {
        Write-Host "No subscription selected. Running 'az login' now..."
        az login | Out-Null
        $current = az account show --query id -o tsv
        if (-not $current) { Write-Error "Failed to get subscription id."; exit 1 }
    }
    return $current
}

function Ensure-StorageAccountName {
    param([string]$name)
    if ($name -and $name.Length -ge 3 -and $name.Length -le 24 -and $name -cmatch '^[a-z0-9]+$') { return $name }
    # generate unique name
    $suffix = -join ((48..57) + (97..102) | Get-Random -Count 6 | ForEach-Object {[char]$_})
    $generated = "shadowstorage$suffix"
    return $generated.Substring(0, [Math]::Min(24, $generated.Length))
}

# Start
Ensure-AzCli

Write-Host "Starting Azure resource creation script for Shadow project" -ForegroundColor Cyan

$subId = Get-SubscriptionId -SubscriptionId $SubscriptionId
Write-Host "Using subscription: $subId"

# Create resource group
Write-Host "Creating resource group '$ResourceGroup' in location '$Location' (if it doesn't exist)" -ForegroundColor Yellow
az group create --name $ResourceGroup --location $Location | Out-Null
if ($LASTEXITCODE -ne 0) { Write-Error "Failed to create or check resource group."; exit 1 }

# Storage account
$saName = Ensure-StorageAccountName -name $StorageAccount
Write-Host "Using storage account name: $saName" -ForegroundColor Yellow
$sa = az storage account show -n $saName -g $ResourceGroup --query name -o tsv 2>$null
if (-not $sa) {
    Write-Host "Creating storage account $saName..." -ForegroundColor Yellow
    az storage account create -n $saName -g $ResourceGroup -l $Location --sku Standard_LRS | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to create storage account."; exit 1 }
} else {
    Write-Host "Storage account already exists: $saName" -ForegroundColor Green
}

# Optionally create App Insights
if ($CreateAppInsights) {
    foreach ($aiName in @("${FunctionAppTest}-ai","${FunctionAppStaging}-ai","${FunctionAppProd}-ai")) {
        $exists = az monitor app-insights component show -g $ResourceGroup -a $aiName --query appId -o tsv 2>$null
        if (-not $exists) {
            Write-Host "Creating Application Insights: $aiName" -ForegroundColor Yellow
            az monitor app-insights component create -g $ResourceGroup -a $aiName -l $Location --application-type web | Out-Null
            if ($LASTEXITCODE -ne 0) { Write-Warning "Failed to create App Insights $aiName" }
        } else { Write-Host "App Insights exists: $aiName" -ForegroundColor Green }
    }
}

# Create Function Apps
function Create-FunctionApp($name) {
    $exists = az functionapp show -n $name -g $ResourceGroup --query name -o tsv 2>$null
    if ($exists) {
        Write-Host "Function App already exists: $name" -ForegroundColor Green
        return
    }
    Write-Host "Creating Function App: $name" -ForegroundColor Yellow
    az functionapp create -g $ResourceGroup -n $name --storage-account $saName --consumption-plan-location $Location --runtime dotnet-isolated --functions-version 4 --os-type Windows | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to create Function App: $name"; exit 1 }
}

Create-FunctionApp -name $FunctionAppTest
Create-FunctionApp -name $FunctionAppStaging
Create-FunctionApp -name $FunctionAppProd

# Create service principal
$spName = "shadow-pipeline-sp-$((Get-Random) % 10000)"
Write-Host "Creating service principal: $spName" -ForegroundColor Yellow
$spJson = az ad sp create-for-rbac --name $spName --role contributor --scopes /subscriptions/$subId/resourceGroups/$ResourceGroup --sdk-auth 2>$null
if ($LASTEXITCODE -ne 0 -or -not $spJson) { Write-Error "Failed to create service principal."; exit 1 }

# Output service principal JSON to file
$outFile = "servicePrincipal-$($spName).json"
$spJson | Out-File -FilePath $outFile -Encoding utf8

Write-Host "Service principal JSON written to: $outFile" -ForegroundColor Green
Write-Host "Copy the contents of this file into your GitHub secret 'AZURE_CREDENTIALS' or use it when creating Azure DevOps service connections." -ForegroundColor Cyan

# Display summary
Write-Host "\nSummary:" -ForegroundColor Cyan
Write-Host "Resource Group: $ResourceGroup"
Write-Host "Location: $Location"
Write-Host "Storage Account: $saName"
Write-Host "Function Apps: $FunctionAppTest, $FunctionAppStaging, $FunctionAppProd"
Write-Host "Service Principal file: $outFile"

Write-Host "\nNext steps:" -ForegroundColor Yellow
Write-Host "- Add the service principal JSON to GitHub Secrets as AZURE_CREDENTIALS" -ForegroundColor White
Write-Host "- In Azure DevOps create service connections using the same JSON and name them to match pipeline variables (Azure-Shadow-Test/Staging/Production) or update the pipeline variables to match your connection names." -ForegroundColor White
Write-Host "- Run the pipeline by committing to the respective branch (test/staging/master)" -ForegroundColor White

Write-Host "Done." -ForegroundColor Green
