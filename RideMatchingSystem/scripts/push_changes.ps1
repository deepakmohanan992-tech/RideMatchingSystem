<#
PowerShell helper to stage, commit, and push local changes.

Usage:
  .\scripts\push_changes.ps1                    # uses default message, origin, master
  .\scripts\push_changes.ps1 -Message "fix: ..." -Remote origin -Branch main

The script will:
  - verify git is available
  - show git status
  - stage all changes and commit (if there are changes)
  - pull --rebase from the remote branch
  - push to the remote branch

This is intended to be run from the repository root (D:\RideMatchingSystem).
#>

param(
	[string]$Message = "chore: remove dev and temp files",
	[string]$Remote = "origin",
	[string]$Branch = "master",
	[switch]$Force
)

function Abort($msg, $code = 1){
	Write-Error $msg
	exit $code
}

# Ensure git is available
if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
	Abort "git is not installed or not in PATH. Install Git for Windows and retry."
}

Write-Host "Git version: $(git --version)"

# Ensure we're in a git repository
$root = (git rev-parse --show-toplevel) 2>$null
if (-not $root) { Abort "Current folder is not inside a git repository." }

Write-Host "Repository root: $root"

# Show status
Write-Host "\nCurrent git status:" -ForegroundColor Cyan
git status --short

$changes = git status --porcelain
if ($changes) {
	Write-Host "\nStaging all changes..." -ForegroundColor Cyan
	git add -A

	# Try commit
	try {
		git commit -m "$Message"
	} catch {
		Write-Warning "Commit failed or nothing to commit: $_"
	}
} else {
	Write-Host "No local changes to commit." -ForegroundColor Yellow
}

# Update from remote using rebase to avoid unnecessary merge commits
Write-Host "\nPulling remote changes (rebase) from $Remote/$Branch..." -ForegroundColor Cyan
try {
	git pull --rebase $Remote $Branch
} catch {
	Write-Error "git pull --rebase failed. Resolve conflicts manually or run 'git rebase --abort'."
	exit 2
}

Write-Host "\nPushing to $Remote/$Branch..." -ForegroundColor Cyan
if ($Force) {
	git push --force $Remote $Branch
} else {
	git push $Remote $Branch
}

if ($LASTEXITCODE -eq 0) {
	Write-Host "Push completed successfully." -ForegroundColor Green
	exit 0
} else {
	Write-Error "Push failed. Exit code: $LASTEXITCODE"
	exit $LASTEXITCODE
}
