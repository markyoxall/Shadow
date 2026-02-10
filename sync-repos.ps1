# Sync to Both Repositories (Azure DevOps + GitHub)
# Usage: .\sync-repos.ps1 "Your commit message"

param(
    [Parameter(Mandatory=$true)]
    [string]$CommitMessage
)

Write-Host "🔄 Syncing changes to both repositories..." -ForegroundColor Cyan
Write-Host ""

# Check if there are changes to commit
$status = git status --porcelain
if ([string]::IsNullOrWhiteSpace($status)) {
    Write-Host "✅ No changes to commit. Working tree is clean." -ForegroundColor Green
    exit 0
}

# Add all changes
Write-Host "📦 Adding all changes..." -ForegroundColor Yellow
git add .

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to add changes" -ForegroundColor Red
    exit 1
}

# Commit changes
Write-Host "💾 Committing: '$CommitMessage'" -ForegroundColor Yellow
git commit -m $CommitMessage

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to commit changes" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✅ Committed successfully!" -ForegroundColor Green
Write-Host ""

# Push to Azure DevOps (origin)
Write-Host "🚀 Pushing to Azure DevOps (origin)..." -ForegroundColor Yellow
git push origin master

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to push to Azure DevOps" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Pushed to Azure DevOps!" -ForegroundColor Green
Write-Host ""

# Push to GitHub
Write-Host "🚀 Pushing to GitHub..." -ForegroundColor Yellow
git push github master

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to push to GitHub" -ForegroundColor Red
    Write-Host "⚠️  Changes were committed and pushed to Azure DevOps, but GitHub sync failed." -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Pushed to GitHub!" -ForegroundColor Green
Write-Host ""
Write-Host "🎉 All done! Changes synced to both repositories." -ForegroundColor Green
Write-Host ""
Write-Host "Azure DevOps: https://dev.azure.com/markyoxall65/Shadowland/_git/Shadow" -ForegroundColor Cyan
Write-Host "GitHub:       https://github.com/markyoxall/Shadow" -ForegroundColor Cyan
