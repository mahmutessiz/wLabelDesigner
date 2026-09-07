# Build and package wLabelDesigner

This guide covers local builds, framework-dependent publishing, self-contained publishing, and creation of the Windows installer with Inno Setup.

## Quick summary

- Use the self-contained publish command to create a standalone Windows x64 executable. The target computer does not need a separately installed .NET runtime.
- Use the installer command to run the Release tests, create the self-contained executable, and package it with Inno Setup.
- Replace `1.0.0` with the release version being created.
- Outputs are written below `artifacts\publish\win-x64` and `artifacts\installer`.

Create only the self-contained executable:

```powershell
dotnet publish .\wLabelDesigner.csproj --configuration Release -p:PublishProfile=win-x64-self-contained -p:Version=1.0.0
```

Create the complete installer after installing Inno Setup:

```powershell
.\Installer\Build-Installer.ps1 -Version 1.0.0
```

## Prerequisites

- Windows 10 or Windows 11 x64.
- .NET 10 SDK.
- [Inno Setup 7](https://jrsoftware.org/isdl.php) when building the installer. Inno Setup 6.3 or newer is also compatible. Its command-line compiler is `ISCC.exe`.

Run all commands from the repository root in PowerShell. Close wLabelDesigner before rebuilding or publishing because Windows can lock the running executable.

## Restore, build, and test

```powershell
dotnet restore .\wLabelDesigner.csproj; dotnet build .\wLabelDesigner.csproj --configuration Release --no-restore; dotnet test .\wLabelDesigner.Tests\wLabelDesigner.Tests.csproj --configuration Release --no-restore
```

The regular Release build is written below `bin\Release\net10.0-windows`. It is not the recommended folder to distribute.

## Framework-dependent publish

A framework-dependent build is smaller, but the target computer must have the .NET 10 Desktop Runtime installed.

```powershell
dotnet publish .\wLabelDesigner.csproj --configuration Release --no-self-contained --output .\artifacts\publish\framework-dependent
```

Distribute the entire `artifacts\publish\framework-dependent` directory, not only the executable.

## Self-contained single-file publish

The recommended release is a Windows x64, self-contained, single-file executable. It carries the .NET runtime, so users do not need to install the .NET Desktop Runtime separately.

Use the checked-in publish profile:

```powershell
dotnet publish .\wLabelDesigner.csproj --configuration Release -p:PublishProfile=win-x64-self-contained -p:Version=1.0.0
```

Output:

```text
artifacts\publish\win-x64\wLabelDesigner.exe
```

The equivalent command without the profile is:

```powershell
dotnet publish .\wLabelDesigner.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:PublishTrimmed=false -p:DebugType=None -p:DebugSymbols=false -p:Version=1.0.0 --output .\artifacts\publish\win-x64
```

Trimming is deliberately disabled because WPF and UI libraries can rely on reflection. The executable may extract bundled native libraries into the current user's temporary `.net` directory when it runs.

## Build the Inno Setup installer

Install Inno Setup 7 from its official website. The repository contains `Installer\wLabelDesigner.iss`, which installs per user under `%LOCALAPPDATA%\Programs\wLabelDesigner` and therefore does not require administrator privileges.

The simplest release command runs tests, creates the self-contained publish output, locates `ISCC.exe`, and compiles the installer:

```powershell
.\Installer\Build-Installer.ps1 -Version 1.0.0
```

Use `-SkipTests` only if the same source revision has already passed the Release test suite:

```powershell
.\Installer\Build-Installer.ps1 -Version 1.0.0 -SkipTests
```

To invoke Inno Setup manually after publishing:

```powershell
& "$env:ProgramFiles\Inno Setup 7\ISCC.exe" "/DMyAppVersion=1.0.0" ".\Installer\wLabelDesigner.iss"
```

Installer output:

```text
artifacts\installer\wLabelDesigner-1.0.0-win-x64-setup.exe
```

The installer includes the GPL license, Start menu shortcut, optional desktop shortcut, English and Turkish installer languages, and an uninstall entry.

## Release verification

The app's **Help → Check for updates…** command compares its assembly version with the latest stable release at `mahmutessiz/wLabelDesigner` on GitHub. Publish releases with numeric tags such as `v1.2.0` and build the matching installer with `-Version 1.2.0` (or publish with `-p:Version=1.2.0`). Two to four numeric version components are supported, with an optional `v` prefix. Drafts and prereleases are excluded. Checks run on demand with a 15-second HTTP timeout.

Attach the installer as `wLabelDesigner-1.2.0-win-x64-setup.exe` with the version matching the release tag. **Download Update** requires this asset and its GitHub-provided SHA-256 digest; it does not run portable executables or unverified assets. Downloads stream to a unique file in the user's temporary `wLabelDesigner/Updates` directory with progress, cancellation, and a 15-minute timeout. The app checks the byte count and SHA-256 before starting setup, and deletes incomplete downloads. Completed installers remain in the temporary directory for retry or normal Windows temporary-file cleanup.

Before launching setup, the app uses its existing save/discard/cancel prompt for unsaved work. Cancelling preserves the current session and downloaded installer. Once setup starts successfully, the app closes so the installer can replace its files. Users follow the normal setup wizard and can launch the updated app from its final page. Installer download and command behavior are tested with simulated services; release verification should include the full upgrade on a Windows test installation.


Before distributing a release:

1. Run the Release test suite without failures or warnings.
2. Install on a clean Windows x64 test account without a .NET Desktop Runtime and launch the application.
3. Create, save, reopen, export, and print a sample label.
4. Uninstall from Windows Settings and confirm the application files are removed.
5. Code-sign the application and installer with a trusted certificate for public distribution. The repository does not contain signing credentials and produces unsigned binaries by default.
