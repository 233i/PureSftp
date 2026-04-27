# PureSftp

PureSftp is a lightweight cross-platform SFTP desktop client built with Avalonia.

It focuses on simple remote file browsing, saved connections, upload/download actions, and a clean bilingual interface.

## Features

- Cross-platform Avalonia desktop UI
- Saved SFTP connections backed by SQLite
- Chinese and English language switching
- Remote directory browsing with parent navigation
- Upload local files to the current remote directory
- Download remote files or folders to local storage
- Create and delete remote items from the file context menu
- macOS native menu integration
- Lightweight toast notifications
- macOS packaging script with `.app` and drag-to-Applications `.dmg`
- Windows packaging script with portable `.zip`

## Tech Stack

- .NET 10
- Avalonia 12
- SSH.NET
- Microsoft.Data.Sqlite
- CommunityToolkit.Mvvm

## Requirements

- .NET SDK 10.0 or later
- macOS, Windows, or Linux for development
- macOS packaging requires macOS command line tools such as `hdiutil` and `codesign`

## Run

```bash
dotnet run --project PureSFTP.csproj
```

## Build

```bash
dotnet build PureSFTP.csproj
```

## macOS Package

Create a local macOS `.app` and `.dmg`:

```bash
scripts/package-mac.sh --rid osx-arm64 --version 1.0.0
```

For Intel Macs:

```bash
scripts/package-mac.sh --rid osx-x64 --version 1.0.0
```

The generated files are written to:

```text
artifacts/
```

Local builds use ad-hoc signing. Public distribution outside your own machine should use a Developer ID certificate and Apple notarization.

## Windows Package

Create a portable Windows package:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/package-windows.ps1 -Rid win-x64 -Version 1.0.0
```

For Windows ARM64:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/package-windows.ps1 -Rid win-arm64 -Version 1.0.0
```

The generated `.zip` is written to:

```text
artifacts/
```

For a guided installer, use the generated package folder with an installer tool such as Inno Setup or WiX.

## Data Storage

PureSftp stores local app data in the user application data directory:

```text
PureSFTP/puresftp.db
```

Saved connection passwords are stored locally in SQLite. Do not share your local database file.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
