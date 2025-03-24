if ((Get-Command git).Length -eq 0) {
    throw "This script requires git be in your path to update the repositories."
}

if ((Get-Command msbuild).Length -gt 0) {
    msbuild /t:clean santedb-server-ext.sln
}
else {
    Write-Output "msbuild not found. Skipping clean action"
}

Write-Output "Pulling Submodules"
git submodule foreach git pull
Write-Output "Pulling root"
git pull