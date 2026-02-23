<#
Test Azurite blob storage script

Usage:
  .\scripts\test-azurite-blob.ps1 [-FunctionUrl <url>] [-DownloadPath <path>]

What it does:
 - Uses the Azurite development connection string to ensure the container exists
 - Lists blobs in the `walkthrough` container
 - If blobs exist, downloads the most recent blob to the specified download path
 - Optionally, posts a test payload to your function URL to cause a new blob to be created, then re-lists and downloads

Notes:
 - Requires `az` CLI (Azure CLI) available on PATH for storage commands.
 - Azurite must be running locally (default blob endpoint http://127.0.0.1:10000).
#>

param(
    [string]$FunctionUrl = '',
    [string]$DownloadPath = '.\downloaded_blob.txt'
)

# Azurite dev connection string (standard)
$azuriteConn = 'DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPL...DevStoreAccountKey...;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;'
$containerName = 'walkthrough'

function Check-AzCli {
    if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
        Write-Error "Azure CLI ('az') not found on PATH. Install Azure CLI to use this script or use Azure Storage Explorer to inspect Azurite blobs."
        exit 1
    }
}

Check-AzCli

Write-Host "Using Azurite connection string (dev)"
Write-Host "Ensuring container '$containerName' exists..."
az storage container create --name $containerName --connection-string $azuriteConn | Out-Null
if ($LASTEXITCODE -ne 0) { Write-Error "Failed to ensure container exists (az returned non-zero)."; exit 1 }

# Optionally trigger the function to create a new blob
if ($FunctionUrl -ne '') {
    Write-Host "Posting test payload to function at: $FunctionUrl"
    try {
        $response = Invoke-RestMethod -Uri $FunctionUrl -Method Post -Body "test from test-azurite-blob.ps1" -ContentType 'text/plain' -TimeoutSec 10
        Write-Host "Function response:" (ConvertTo-Json $response -Depth 5)
    }
    catch {
        Write-Warning "Failed to POST to function: $_"
    }
    Start-Sleep -Seconds 1
}

Write-Host "Listing blobs in container '$containerName'..."
$blobsJson = az storage blob list --container-name $containerName --connection-string $azuriteConn --output json | Out-String
if ($LASTEXITCODE -ne 0) { Write-Error "Failed to list blobs (az returned non-zero)."; exit 1 }

$blobs = @()
try {
    $blobs = $blobsJson | ConvertFrom-Json
}
catch {
    Write-Warning "No blobs or failed to parse list output."
}

if ($null -eq $blobs -or $blobs.Count -eq 0) {
    Write-Host "No blobs found in container '$containerName'."
    exit 0
}

Write-Host "Found $($blobs.Count) blob(s):"
$index = 0
foreach ($b in $blobs) {
    $index++
    Write-Host "[$index] $($b.name)  -  $($b.properties.contentLength) bytes"
}

# Download the most recent blob (by lastModified)
$latest = $blobs | Sort-Object { $_.properties.lastModified } -Descending | Select-Object -First 1
$blobName = $latest.name
Write-Host "Downloading latest blob '$blobName' to '$DownloadPath'..."
az storage blob download --container-name $containerName --name $blobName --file $DownloadPath --connection-string $azuriteConn | Out-Null
if ($LASTEXITCODE -ne 0) { Write-Error "Failed to download blob (az returned non-zero)."; exit 1 }
Write-Host "Downloaded to: $DownloadPath"

# Show first lines of the file
try {
    Write-Host "----- blob preview -----"
    Get-Content -Path $DownloadPath -TotalCount 20 | ForEach-Object { Write-Host $_ }
    Write-Host "------------------------"
}
catch {
    Write-Warning "Failed to read downloaded file: $_"
}
