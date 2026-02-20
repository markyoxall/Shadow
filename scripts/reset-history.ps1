# WARNING: This script rewrites git history and force-pushes to the remote. RUN ONLY IF YOU UNDERSTAND THE EFFECTS.
# Edit the variables below to match your repo/remote/branch names before running.

param(
    [string]$RemoteName = 'github',
    [string]$TargetBranch = 'master',
    [string]$NewBranch = 'cleaned'
)

Write-Host "This will create a new orphan branch '$NewBranch', commit the current working tree (respecting .gitignore), and force-push it to remote '$RemoteName' as branch '$TargetBranch'."
Write-Host "Make sure you have backups and that all collaborators are informed."

$confirm = Read-Host "Type 'YES' to continue"
if ($confirm -ne 'YES') { Write-Host "Aborted."; exit }

# Ensure working tree is clean
git status --porcelain
if ($LASTEXITCODE -ne 0) { Write-Host "git status failed"; exit }

# Create orphan branch
git checkout --orphan $NewBranch
# Remove all files from index and working tree
git reset --hard

# Add files according to .gitignore
git add --all
git commit -m "Initial commit — scrubbed history"

# Force push to remote
git push --force $RemoteName $NewBranch:$TargetBranch

Write-Host "Force-pushed cleaned branch to $RemoteName/$TargetBranch."
Write-Host "Now update the default branch in GitHub (or run 'gh repo edit --default-branch $TargetBranch' if you have gh CLI)."
Write-Host "After confirming default branch switch on GitHub, you can delete other remote branches if desired."

Write-Host "IMPORTANT: rotate any secrets that may have been exposed in the previous history (SendGrid keys, storage keys)."
