<#
Clears local Git metadata and re-initializes a clean repository while preserving working files.
Run from the repository root in PowerShell.

This script will:
 - Move the existing .git directory to a timestamped backup (.git_backup_YYYYMMDDHHMMSS)
 - Ensure .gitignore contains entries to ignore local settings and blob/json artifacts
 - Run `git init`, stage files and create a single initial commit
 - Optionally add a remote (pass -RemoteUrl) but it will NOT push

USAGE:
  .\scripts\clear-local-git.ps1 [-RemoteUrl <git-remote-url>]

WARNING: This rewrites local git metadata. Keep backups of .git if you need history.
#>
param(
    [string]$RemoteUrl = ''
)

function Ensure-GitIgnoreEntry([string]$pattern) {
    $gitignore = Join-Path -Path (Get-Location) -ChildPath '.gitignore'
    if (-not (Test-Path $gitignore)) {
        "# Generated .gitignore`n" | Out-File -FilePath $gitignore -Encoding utf8
    }
    $content = Get-Content $gitignore -ErrorAction SilentlyContinue
    if ($content -notcontains $pattern) {
        Add-Content -Path $gitignore -Value $pattern
        Write-Host "Added ignore pattern: $pattern"
    }
}

Write-Host "This script will remove local Git metadata and create a fresh repository."
$ok = Read-Host "Type 'YES' to continue"
if ($ok -ne 'YES') { Write-Host 'Aborted.'; exit 1 }

# Backup .git if present
$gitDir = Join-Path (Get-Location) '.git'
if (Test-Path $gitDir) {
    $ts = Get-Date -Format yyyyMMddHHmmss
    $backup = ".git_backup_$ts"
    Write-Host "Moving existing .git -> $backup"
    Move-Item -Path $gitDir -Destination $backup -Force
} else {
    Write-Host "No .git directory found."
}

# Ensure .gitignore has safe entries
$patterns = @(
    "**/local.settings.json",
    "**/appsettings.local.json",
    "**/appsettings.*.local.json",
    "**/*.local.json",
    "**/*.blob.json",
    "**/*.secret",
    "**/Shadow.FunkyGibbon/local.settings.json"
)
foreach ($p in $patterns) { Ensure-GitIgnoreEntry $p }

# Initialize new git repo
Write-Host "Initializing new git repository..."
git init
if ($LASTEXITCODE -ne 0) { Write-Error "git init failed. Make sure git is installed and available on PATH."; exit 1 }

# Add all files (gitignore will prevent sensitive files being added)
Write-Host "Staging files (honoring .gitignore)..."
git add --all
if ($LASTEXITCODE -ne 0) { Write-Error "git add failed."; exit 1 }

# Commit
Write-Host "Creating initial commit..."
git commit -m "Initial commit — clean history"
if ($LASTEXITCODE -ne 0) { Write-Error "git commit failed. Check identity or staged files."; exit 1 }

# Optionally set remote
if ($RemoteUrl -ne '') {
    Write-Host "Adding remote 'origin' -> $RemoteUrl"
    git remote add origin $RemoteUrl
    if ($LASTEXITCODE -ne 0) { Write-Warning "Failed to add remote. You can add it manually later." }
}

Write-Host "Done. Local git metadata replaced. Review `.gitignore` and verify no secrets are staged.`nYou can now push to your remote with:`n  git push -u origin master`n(Replace origin/master as appropriate.)"
