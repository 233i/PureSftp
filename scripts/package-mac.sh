#!/usr/bin/env bash
set -euo pipefail

APP_NAME="PureSftp"
BUNDLE_ID="com.puresftp.app"
CONFIGURATION="Release"
VERSION="${VERSION:-1.0.0}"
SIGN_IDENTITY="${SIGN_IDENTITY:--}"
ENTITLEMENTS_FILE=""
CREATE_DMG=1
SIGN_APP=1
CLEAN=1
RID=""

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT_FILE="$PROJECT_DIR/PureSFTP.csproj"
ARTIFACTS_DIR="$PROJECT_DIR/artifacts"
INFO_PLIST_TEMPLATE="$PROJECT_DIR/Platforms/macOS/Info.plist"
DEFAULT_ENTITLEMENTS_FILE="$PROJECT_DIR/Platforms/macOS/Entitlements.plist"
ICON_FILE="$PROJECT_DIR/Assets/puresftp.icns"

usage() {
    cat <<EOF
Usage: scripts/package-mac.sh [options]

Options:
  --rid <rid>             Runtime identifier: osx-arm64 or osx-x64.
                          Defaults to current Mac architecture.
  --version <version>     Bundle version. Default: ${VERSION}
  --configuration <name>  Build configuration. Default: Release
  --sign-identity <id>    codesign identity. Default: - (ad-hoc local signing)
  --entitlements <path>   Entitlements plist for Developer ID signing.
                          Default: Platforms/macOS/Entitlements.plist
  --no-sign               Skip codesign.
  --no-dmg                Only create .app, skip .dmg.
  --no-clean              Keep previous output folders before packaging.
  -h, --help              Show this help.

Examples:
  scripts/package-mac.sh
  scripts/package-mac.sh --rid osx-arm64 --version 1.0.0
  SIGN_IDENTITY="Developer ID Application: Your Name (TEAMID)" scripts/package-mac.sh --rid osx-arm64
EOF
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --rid)
            RID="${2:-}"
            shift 2
            ;;
        --version)
            VERSION="${2:-}"
            shift 2
            ;;
        --configuration)
            CONFIGURATION="${2:-}"
            shift 2
            ;;
        --sign-identity)
            SIGN_IDENTITY="${2:-}"
            shift 2
            ;;
        --entitlements)
            ENTITLEMENTS_FILE="${2:-}"
            shift 2
            ;;
        --no-sign)
            SIGN_APP=0
            shift
            ;;
        --no-dmg)
            CREATE_DMG=0
            shift
            ;;
        --no-clean)
            CLEAN=0
            shift
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            echo "Unknown option: $1" >&2
            usage
            exit 1
            ;;
    esac
done

if [[ -z "$RID" ]]; then
    case "$(uname -m)" in
        arm64)
            RID="osx-arm64"
            ;;
        x86_64)
            RID="osx-x64"
            ;;
        *)
            echo "Cannot infer macOS RID for architecture: $(uname -m)" >&2
            echo "Pass --rid osx-arm64 or --rid osx-x64." >&2
            exit 1
            ;;
    esac
fi

if [[ "$RID" != "osx-arm64" && "$RID" != "osx-x64" ]]; then
    echo "Unsupported RID: $RID. Use osx-arm64 or osx-x64." >&2
    exit 1
fi

if [[ -z "$ENTITLEMENTS_FILE" ]]; then
    ENTITLEMENTS_FILE="$DEFAULT_ENTITLEMENTS_FILE"
fi

for required_file in "$PROJECT_FILE" "$INFO_PLIST_TEMPLATE" "$ICON_FILE"; do
    if [[ ! -f "$required_file" ]]; then
        echo "Required file not found: $required_file" >&2
        exit 1
    fi
done

PUBLISH_DIR="$ARTIFACTS_DIR/publish/$RID"
PACKAGE_DIR="$ARTIFACTS_DIR/macos/$RID"
APP_BUNDLE="$PACKAGE_DIR/$APP_NAME.app"
DMG_STAGING_DIR="$PACKAGE_DIR/dmg"
CONTENTS_DIR="$APP_BUNDLE/Contents"
MACOS_DIR="$CONTENTS_DIR/MacOS"
RESOURCES_DIR="$CONTENTS_DIR/Resources"
DMG_FILE="$ARTIFACTS_DIR/$APP_NAME-$VERSION-$RID.dmg"

echo "Packaging $APP_NAME $VERSION for $RID..."

if [[ "$CLEAN" -eq 1 ]]; then
    rm -rf "$PUBLISH_DIR" "$PACKAGE_DIR" "$DMG_FILE"
fi

dotnet publish "$PROJECT_FILE" \
    -c "$CONFIGURATION" \
    -r "$RID" \
    --self-contained true \
    -p:AssemblyName="$APP_NAME" \
    -p:Product="$APP_NAME" \
    -p:Version="$VERSION" \
    -p:FileVersion="$VERSION" \
    -p:InformationalVersion="$VERSION" \
    -o "$PUBLISH_DIR"

mkdir -p "$MACOS_DIR" "$RESOURCES_DIR"
cp "$INFO_PLIST_TEMPLATE" "$CONTENTS_DIR/Info.plist"
cp "$ICON_FILE" "$RESOURCES_DIR/puresftp.icns"
cp -R "$PUBLISH_DIR"/. "$MACOS_DIR"/
chmod +x "$MACOS_DIR/$APP_NAME"

/usr/libexec/PlistBuddy -c "Set :CFBundleName $APP_NAME" "$CONTENTS_DIR/Info.plist"
/usr/libexec/PlistBuddy -c "Set :CFBundleDisplayName $APP_NAME" "$CONTENTS_DIR/Info.plist"
/usr/libexec/PlistBuddy -c "Set :CFBundleExecutable $APP_NAME" "$CONTENTS_DIR/Info.plist"
/usr/libexec/PlistBuddy -c "Set :CFBundleIdentifier $BUNDLE_ID" "$CONTENTS_DIR/Info.plist"
/usr/libexec/PlistBuddy -c "Set :CFBundleShortVersionString $VERSION" "$CONTENTS_DIR/Info.plist"
/usr/libexec/PlistBuddy -c "Set :CFBundleVersion $VERSION" "$CONTENTS_DIR/Info.plist"

if [[ "$SIGN_APP" -eq 1 ]]; then
    echo "Signing app with identity: $SIGN_IDENTITY"
    if [[ "$SIGN_IDENTITY" == "-" ]]; then
        # Local ad-hoc signing should not enable hardened runtime. If it does,
        # macOS library validation can block bundled .NET runtime dylibs.
        codesign --force --deep --sign "$SIGN_IDENTITY" "$APP_BUNDLE"
    else
        if [[ ! -f "$ENTITLEMENTS_FILE" ]]; then
            echo "Entitlements file not found: $ENTITLEMENTS_FILE" >&2
            exit 1
        fi

        codesign \
            --force \
            --deep \
            --options runtime \
            --timestamp \
            --entitlements "$ENTITLEMENTS_FILE" \
            --sign "$SIGN_IDENTITY" \
            "$APP_BUNDLE"
    fi
fi

if [[ "$CREATE_DMG" -eq 1 ]]; then
    rm -rf "$DMG_STAGING_DIR"
    mkdir -p "$DMG_STAGING_DIR"
    cp -R "$APP_BUNDLE" "$DMG_STAGING_DIR/"
    ln -s /Applications "$DMG_STAGING_DIR/Applications"

    hdiutil create \
        -volname "$APP_NAME" \
        -srcfolder "$DMG_STAGING_DIR" \
        -ov \
        -format UDZO \
        "$DMG_FILE"
    echo "Created DMG: $DMG_FILE"
fi

echo "Created app bundle: $APP_BUNDLE"
