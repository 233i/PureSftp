# Release Guide

This document records the current packaging flow for PureSftp.

## macOS

Build an Apple Silicon package:

```bash
scripts/package-mac.sh --rid osx-arm64 --version 1.0.0
```

Build an Intel package:

```bash
scripts/package-mac.sh --rid osx-x64 --version 1.0.0
```

Outputs:

```text
artifacts/macos/<rid>/PureSftp.app
artifacts/PureSftp-<version>-<rid>.dmg
```

The DMG contains:

```text
PureSftp.app
Applications -> /Applications
```

For local testing, the script uses ad-hoc signing. For public distribution, sign with Developer ID:

```bash
SIGN_IDENTITY="Developer ID Application: Your Name (TEAMID)" \
scripts/package-mac.sh --rid osx-arm64 --version 1.0.0
```

Public macOS distribution should also be notarized:

```bash
xcrun notarytool submit artifacts/PureSftp-1.0.0-osx-arm64.dmg \
  --apple-id "you@example.com" \
  --team-id "TEAMID" \
  --password "app-specific-password" \
  --wait

xcrun stapler staple artifacts/PureSftp-1.0.0-osx-arm64.dmg
```

## Windows

Publish a self-contained Windows build:

```bash
dotnet publish PureSFTP.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:EnableCompressionInSingleFile=true \
  -o artifacts/publish/win-x64
```

For a simple release, zip `artifacts/publish/win-x64`.

For a traditional installer, use Inno Setup or another installer builder.

## Verification

Before publishing a release:

```bash
dotnet build PureSFTP.csproj -c Release
```

On macOS, verify the app bundle:

```bash
codesign --verify --deep --strict --verbose=2 artifacts/macos/osx-arm64/PureSftp.app
```

For public Developer ID builds:

```bash
spctl --assess --type execute --verbose=4 artifacts/macos/osx-arm64/PureSftp.app
```
