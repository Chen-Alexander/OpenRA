[CmdletBinding()]
param(
    [string]$Remote = 'upstream',
    [string]$Branch = 'solar',
    [switch]$Rebase
)

$ErrorActionPreference = 'Stop'

git fetch $Remote
if ($LASTEXITCODE -ne 0) {
    throw "git fetch $Remote failed"
}

git switch $Branch
if ($LASTEXITCODE -ne 0) {
    throw "git switch $Branch failed"
}

if ($Rebase) {
    git rebase "$Remote/bleed"
}
else {
    git merge --no-ff "$Remote/bleed" -m "Merge $Remote/bleed into $Branch"
}

if ($LASTEXITCODE -ne 0) {
    Write-Host ''
    Write-Host 'Upstream merge produced conflicts.' -ForegroundColor Yellow
    Write-Host 'Resolve them, then run: git add <files>; git commit' -ForegroundColor Yellow
    Write-Host 'Rerere is enabled, so the same conflicts will be resolved automatically next time.' -ForegroundColor Yellow
    exit 1
}

Write-Host "Synced $Branch with $Remote/bleed." -ForegroundColor Green
