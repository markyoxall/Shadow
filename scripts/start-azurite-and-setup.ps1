<#
Start Azurite (if not running) and create the `walkthrough` container for local testing.

Usage:
  .\scripts\start-azurite-and-setup.ps1 [-Port 10000] [-Container walkthrough] [-NoWait]

Notes:
 - Requires Node-installed Azurite (command `azurite`) or `npx` available.
 - Requires Azure CLI (`az`) to create/list blobs (used to create the container).
 - This script starts Azurite in the background and waits until the blob endpoint is responsive.
 - It then creates the container using the well-known Azurite development connection string.
 - Run this once before starting your projects in Visual Studio.
#>

param(
    [int]$Port = 10000,
    [string]$Container = 'walkthrough',
    [switch]$NoWait
)

function Test-PortOpen {
    param(
        [string]$hostname,
        [int]$portNumber
    )
    try {
        $conn = Test-NetConnection -ComputerName $hostname -Port $portNumber -WarningAction SilentlyContinue
        return $conn.TcpTestSucceeded
    }
    catch {
        return $false
    }
}

# Determine azurite command
$azuriteCmd = Get-Command azurite -ErrorAction SilentlyContinue
$useNpx = $false
if (-not $azuriteCmd) {
    if (Get-Command npx -ErrorAction SilentlyContinue) {
        $useNpx = $true
        Write-Host "'azurite' not found on PATH; will try 'npx azurite'"
    }
    else {
        Write-Error "Neither 'azurite' nor 'npx' were found on PATH. Install Azurite (npm i -g azurite) or ensure npx is available."; exit 1
    }
}
else {
    Write-Host "Found azurite at: $($azuriteCmd.Path)"
}

# If port already open, assume Azurite running
$hostName = '127.0.0.1'
if (Test-PortOpen -hostname $hostName -portNumber $Port) {
    Write-Host "Azurite appears to be running on port $Port"
}
else {
    # Start Azurite in background
    # Include --skipApiVersionCheck to avoid Azure CLI using newer API versions causing rejection
    $azuriteArgs = "--silent --skipApiVersionCheck --location ./azurite --debug ./azurite/debug.log --blobPort $Port"
    if ($useNpx) {
        $psi = New-Object System.Diagnostics.ProcessStartInfo
        $psi.FileName = 'npx'
        $psi.Arguments = "azurite $azuriteArgs"
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true
        $psi.UseShellExecute = $false
        $psi.CreateNoWindow = $true
        $p = [System.Diagnostics.Process]::Start($psi)
        Write-Host "Started azurite via npx (PID $($p.Id))"
    }
    else {
        # Determine the actual azurite command path and handle PowerShell script shims installed by npm
        $azPath = $azuriteCmd.Path
        try {
            if ($azPath -and $azPath.ToLower().EndsWith('.ps1')) {
                # Run the ps1 script using PowerShell executable to avoid "not a valid Win32 application" errors
                $pwsh = Get-Command powershell -ErrorAction SilentlyContinue
                if (-not $pwsh) { $pwsh = Get-Command pwsh -ErrorAction SilentlyContinue }
                if ($pwsh) {
                    $pwshPath = $pwsh.Path
                    $arg = "-NoProfile -ExecutionPolicy Bypass -File `"$azPath`" $azuriteArgs"
                    $p = Start-Process -FilePath $pwshPath -ArgumentList $arg -NoNewWindow -PassThru
                    Write-Host "Started azurite (PowerShell script) via $($pwshPath) (PID $($p.Id))"
                }
                else {
                    Write-Warning "PowerShell executable not found to run azurite ps1 script. Attempting to invoke azurite directly."
                    $p = Start-Process -FilePath 'azurite' -ArgumentList $azuriteArgs -NoNewWindow -PassThru
                    Write-Host "Started azurite (PID $($p.Id))"
                }
            }
            else {
                $p = Start-Process -FilePath $azPath -ArgumentList $azuriteArgs -NoNewWindow -PassThru
                Write-Host "Started azurite (PID $($p.Id))"
            }
        }
        catch {
            Write-Warning "Failed to start azurite using detected path '$azPath': $_. Attempting generic start-Process call."
            $p = Start-Process -FilePath 'azurite' -ArgumentList $azuriteArgs -NoNewWindow -PassThru
            Write-Host "Started azurite (PID $($p.Id))"
        }
    }

    if (-not $NoWait) {
        Write-Host "Waiting for Azurite blob endpoint to become available on port $Port..."
        $max = 30; $i = 0
        while ($i -lt $max) {
            Start-Sleep -Seconds 1
            if (Test-PortOpen -hostname $hostName -portNumber $Port) { Write-Host "Azurite is ready"; break }
            $i++
        }
        if ($i -ge $max) { Write-Error "Timed out waiting for Azurite to start on port $Port"; exit 1 }
    }
}

# Ensure az CLI is available
if (-not (Get-Command az -ErrorAction SilentlyContinue)) { Write-Error "Azure CLI 'az' not found on PATH. Install Azure CLI to create container."; exit 1 }

# Well-known Azurite dev connection string (respect configured ports)
$blobEndpoint = "http://127.0.0.1:$Port/devstoreaccount1"
$queuePort = $Port + 1
$tablePort = $Port + 2
$queueEndpoint = "http://127.0.0.1:$queuePort/devstoreaccount1"
$tableEndpoint = "http://127.0.0.1:$tablePort/devstoreaccount1"
$azuriteConn = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPL/SAMPLEDEVSTOREKEY==;BlobEndpoint=$blobEndpoint;QueueEndpoint=$queueEndpoint;TableEndpoint=$tableEndpoint;"

Write-Host "Creating container '$Container' in local Azurite instance..."
az storage container create --name $Container --connection-string $azuriteConn | Out-Null
if ($LASTEXITCODE -ne 0) { Write-Error "Failed to create container (az returned non-zero)."; exit 1 }

Write-Host "Container '$Container' is ready. You can now run your projects in Visual Studio and they will use Azurite for blob storage if configured to use development storage."
Write-Host "Note: local.settings.json should have AzureWebJobsStorage set to 'UseDevelopmentStorage=true' for the Functions host to use Azurite."

Write-Host "To stop Azurite, find the process and terminate it (e.g., Stop-Process -Id <pid>) or close the terminal that started it." 
