$items = @(
    '.git_backup*',
    'Shadow.FunkyGibbon/__blobstorage__',
    'Shadow.FunkyGibbon/__azurite_db_blob__.json',
    'Shadow.FunkyGibbon/__azurite_db_blob_extent__.json',
    'Shadow.FunkyGibbon/__azurite_db_queue__.json',
    'Shadow.FunkyGibbon/__azurite_db_queue_extent__.json',
    'Shadow.FunkyGibbon/__azurite_db_table__.json',
    '__azurite_db_blob__.json',
    '__azurite_db_blob_extent__.json'
)

foreach($p in $items) {
    if (Test-Path $p) {
        try {
            Remove-Item -Recurse -Force $p -ErrorAction Stop
            Write-Host "Removed: $p"
        }
        catch {
            Write-Host "Failed to remove: $p -> $_"
        }
    }
    else {
        Write-Host "Not found: $p"
    }
}

# Remove any remaining .git_backup* directories at repo root
Get-ChildItem -Path . -Directory -Filter '.git_backup*' -ErrorAction SilentlyContinue | ForEach-Object {
    try {
        Remove-Item -Recurse -Force $_.FullName -ErrorAction Stop
        Write-Host "Removed directory: $($_.FullName)"
    } catch {
        Write-Host "Failed to remove directory: $($_.FullName) -> $_"
    }
}

# Stage and commit removals if any
git add -A
$status = git status --porcelain
if ($status -ne '') {
    git commit -m "Remove local backup and azurite temp files"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "git commit failed"
    } else {
        Write-Host "Committed removals"
    }
} else {
    Write-Host "No changes to commit"
}
