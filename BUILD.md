# Building & Publishing <PackageName>

## Prerequisites

- .NET 10 SDK
- Access to the target Azure DevOps NuGet feed
- A `NuGet.Config` with credentials for the feed (for example, a local path under your workspace)

---

## Version bump

Before every publish, increment `<Version>` in the csproj (Azure DevOps rejects re-pushing the same version):

```
<PackageProjectFolder>/<PackageProjectName>.csproj
```

```xml
<Version>0.0.0</Version>   <!-- bump this -->
```

---

## Option A — Shell script (recommended)

Run from the repo root of this workspace:

```bash
bash pack_and_push.sh
```

The script will:
1. Prompt for the path to `NuGet.Config` (press Enter to accept the default).
2. Build in Release configuration.
3. Pack — produces `nupkg/<PackageName>.<version>.nupkg`.
4. Push the newest `.nupkg` to the configured feed.

---

## Option B — Manual steps

All commands run from the repo root of this workspace.

### 1. Build

```powershell
dotnet build "<PackageProjectFolder>/<PackageProjectName>.csproj" `
  -c Release /p:WarningLevel=0
```

### 2. Pack

```powershell
dotnet pack "<PackageProjectFolder>/<PackageProjectName>.csproj" `
  -c Release --no-build /p:WarningLevel=0 `
  -o "nupkg"
```

Output: `nupkg/<PackageName>.<version>.nupkg`

### 3. Push

```powershell
dotnet nuget push "nupkg/<PackageName>.<version>.nupkg" `
  --source "<FeedName>" `
  --api-key az `
  --configfile "<PathToNuGetConfig>"
```

---

## Option C — Build specific TFM only (faster, no pack)

Useful during development to generate ref assemblies for `ProjectReference` use:

```powershell
dotnet build "<PackageProjectFolder>/<PackageProjectName>.csproj" `
  -f net10.0-android /p:WarningLevel=0

dotnet build "<PackageProjectFolder>/<PackageProjectName>.csproj" `
  -f net10.0 /p:WarningLevel=0
```

---
