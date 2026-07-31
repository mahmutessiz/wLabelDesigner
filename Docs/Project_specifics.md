# Project Specification: FckBarTender (Custom WPF Thermal Label Designer)

## Overview
Build a lightweight, zero-bloat C# WPF desktop application for designing and printing thermal labels (specifically optimized for 203 DPI/300 DPI thermal printers like Xprinter). The software serves as a fast, free replacement for BarTender.

---

## Technical Stack & Target
* **Target Framework:** .NET 8.0 (Windows Desktop SDK)
* **Architecture:** WPF with MVVM (`CommunityToolkit.Mvvm`)
* **Target OS:** Windows 10/11 (x64)
* **Primary File Extension:** `.fckbartndr` (JSON-formatted template file)

---

## Required NuGet Packages
Please install and reference the following NuGet packages:
```xml
<ItemGroup>
  <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.*"/>
  <PackageReference Include="BarcodeLib" Version="3.1.*"/>
  <PackageReference Include="QRCoder" Version="1.4.*"/>
  <PackageReference Include="CsvHelper" Version="31.0.*"/>
</ItemGroup>