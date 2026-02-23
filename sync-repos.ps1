#!/usr/bin/env pwsh
# Sync to GitHub only
# Usage: .\sync-repos.ps1 "Your commit message"
param(
    [Parameter(Mandatory=$true)]
    [string]$CommitMessage
)

Write-Host "🔄 Syncing changes to GitHub..." -ForegroundColor Cyan
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

# Determine current branch and push to GitHub remote
Write-Host "🚀 Pushing to GitHub (remote: 'github')..." -ForegroundColor Yellow
$branch = git rev-parse --abbrev-ref HEAD
if ($LASTEXITCODE -ne 0) { $branch = 'master' }
git push github $branch

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to push to GitHub" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Pushed to GitHub!" -ForegroundColor Green
Write-Host ""
Write-Host "🎉 All done! Changes committed and pushed to GitHub." -ForegroundColor Green
Write-Host ""
Write-Host "GitHub: https://github.com/markyoxall/Shadow" -ForegroundColor Cyan
