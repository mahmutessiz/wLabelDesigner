[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+(?:\.\d+)?$')]
    [string]$Version = '1.0.0',

    [switch]$SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot 'wLabelDesigner.csproj'
$testProjectPath = Join-Path $repositoryRoot 'wLabelDesigner.Tests\wLabelDesigner.Tests.csproj'
$installerScriptPath = Join-Path $PSScriptRoot 'wLabelDesigner.iss'
$artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts'))
$publishDirectory = [System.IO.Path]::GetFullPath((Join-Path $artifactsRoot 'publish\win-x64'))

if (-not $publishDirectory.StartsWith(
        $artifactsRoot + [System.IO.Path]::DirectorySeparatorChar,
        [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to clean a publish directory outside the artifacts folder: $publishDirectory"
}

Push-Location $repositoryRoot
try {
    if (-not $SkipTests) {
        & dotnet test $testProjectPath --configuration Release
        if ($LASTEXITCODE -ne 0) {
            throw "Tests failed with exit code $LASTEXITCODE."
        }
    }

    if (Test-Path -LiteralPath $publishDirectory) {
        Remove-Item -LiteralPath $publishDirectory -Recurse -Force
    }

    & dotnet publish $projectPath `
        --configuration Release `
        -p:PublishProfile=win-x64-self-contained `
        -p:Version=$Version
    if ($LASTEXITCODE -ne 0) {
        throw "Publishing failed with exit code $LASTEXITCODE."
    }

    $isccCommand = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
    $isccPath = if ($null -ne $isccCommand) { $isccCommand.Source } else { $null }
    if (-not $isccPath) {
        $isccCandidates = @(
            (Join-Path $env:ProgramFiles 'Inno Setup 7\ISCC.exe'),
            (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 7\ISCC.exe'),
            (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'),
            (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe')
        )
        $isccPath = $isccCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    }

    if (-not $isccPath) {
        throw 'ISCC.exe was not found. Install Inno Setup 7 (or 6.3+) or add its installation directory to PATH.'
    }

    & $isccPath "/DMyAppVersion=$Version" $installerScriptPath
    if ($LASTEXITCODE -ne 0) {
        throw "Inno Setup compilation failed with exit code $LASTEXITCODE."
    }

    $installerPath = Join-Path $artifactsRoot "installer\wLabelDesigner-$Version-win-x64-setup.exe"
    Write-Host "Installer created: $installerPath"
}
finally {
    Pop-Location
}
