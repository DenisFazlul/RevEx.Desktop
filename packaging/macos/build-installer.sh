#!/bin/bash
set -euo pipefail

version="${1:?Usage: build-installer.sh VERSION osx-x64|osx-arm64}"
runtime="${2:?A macOS runtime identifier is required}"
if [[ ! "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo 'Version must have the format 1.2.3' >&2
    exit 1
fi
case "$runtime" in
    osx-x64) architecture=x86_64 ;;
    osx-arm64) architecture=arm64 ;;
    *) echo 'Only osx-x64 and osx-arm64 are supported' >&2; exit 1 ;;
esac

repo_root="$(cd "$(dirname "$0")/../.." && pwd)"
publish_dir="$repo_root/artifacts/publish"
app_bundle="$repo_root/artifacts/macos/RevEx.app"
output_dir="$repo_root/artifacts/installer"

test -f "$publish_dir/RevEx.Desktop"
lipo -verify_arch "$architecture" "$publish_dir/RevEx.Desktop"
# Refuse to mix a previous bundle with the new publish output.
test ! -e "$app_bundle"
mkdir -p "$app_bundle/Contents/MacOS" "$app_bundle/Contents/Resources" "$output_dir"
ditto "$publish_dir" "$app_bundle/Contents/MacOS"
cp "$repo_root/packaging/macos/Info.plist" "$app_bundle/Contents/Info.plist"
/usr/libexec/PlistBuddy -c "Set :CFBundleShortVersionString $version" "$app_bundle/Contents/Info.plist"
/usr/libexec/PlistBuddy -c "Set :CFBundleVersion $version" "$app_bundle/Contents/Info.plist"
plutil -lint "$app_bundle/Contents/Info.plist"
chmod +x "$app_bundle/Contents/MacOS/RevEx.Desktop"

# Ad-hoc signing supplies bundle integrity; it is not a Developer ID signature.
codesign --force --deep --sign - "$app_bundle"
codesign --verify --deep --strict "$app_bundle"
productbuild --component "$app_bundle" /Applications \
    --identifier com.revex.desktop.installer --version "$version" \
    "$output_dir/RevEx-Setup-$version-$runtime.pkg"
pkgutil --payload-files "$output_dir/RevEx-Setup-$version-$runtime.pkg"
