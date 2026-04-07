#!/usr/bin/env bash

set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
root_dir="$(cd "${script_dir}/.." && pwd)"

if [[ -n "${MAPBOX_DOWNLOADS_TOKEN:-}" && -z "${ORG_GRADLE_PROJECT_MAPBOX_DOWNLOADS_TOKEN:-}" ]]; then
  export ORG_GRADLE_PROJECT_MAPBOX_DOWNLOADS_TOKEN="${MAPBOX_DOWNLOADS_TOKEN}"
fi

if [[ -z "${ORG_GRADLE_PROJECT_MAPBOX_DOWNLOADS_TOKEN:-}" ]] && ! grep -q '^[[:space:]]*MAPBOX_DOWNLOADS_TOKEN=' "${HOME}/.gradle/gradle.properties" 2>/dev/null; then
  echo "Missing Mapbox downloads token. Set MAPBOX_DOWNLOADS_TOKEN, ORG_GRADLE_PROJECT_MAPBOX_DOWNLOADS_TOKEN, or ~/.gradle/gradle.properties." >&2
  exit 1
fi

echo "Packing Android bindings..."
dotnet pack "${root_dir}/android/bindings.g.sln" -c Debug -p:PackageOutputPath="${root_dir}/android/nugets"

echo "Packing iOS bindings..."
dotnet pack "${root_dir}/ios/mapbox-ios.sln" -c Debug -p:PackageOutputPath="${root_dir}/ios/nugets"

echo "Clearing NuGet caches so the harnesses consume the freshly packed local packages..."
dotnet nuget locals all --clear

echo "Building Android harness..."
dotnet build "${script_dir}/MapboxBindings.AndroidHarness/MapboxBindings.AndroidHarness.csproj" -f net10.0-android

echo "Building iOS harness..."
if [[ -n "${MAPBOX_ACCESS_TOKEN:-}" ]]; then
  dotnet build "${script_dir}/MapboxBindings.iOSHarness/MapboxBindings.iOSHarness.csproj" -f net10.0-ios -p:RuntimeIdentifier=iossimulator-arm64 -p:CodesignKey= -p:CodesignProvision= "-p:MapboxAccessToken=${MAPBOX_ACCESS_TOKEN}"
else
  dotnet build "${script_dir}/MapboxBindings.iOSHarness/MapboxBindings.iOSHarness.csproj" -f net10.0-ios -p:RuntimeIdentifier=iossimulator-arm64 -p:CodesignKey= -p:CodesignProvision=
fi

echo "Building MAUI Android harness..."
dotnet build "${script_dir}/MapboxBindings.MauiHarness/MapboxBindings.MauiHarness.csproj" -f net10.0-android

echo "Building MAUI iOS harness..."
if [[ -n "${MAPBOX_ACCESS_TOKEN:-}" ]]; then
  dotnet build "${script_dir}/MapboxBindings.MauiHarness/MapboxBindings.MauiHarness.csproj" -f net10.0-ios -p:RuntimeIdentifier=iossimulator-arm64 -p:CodesignKey= -p:CodesignProvision= "-p:MapboxAccessToken=${MAPBOX_ACCESS_TOKEN}"
else
  dotnet build "${script_dir}/MapboxBindings.MauiHarness/MapboxBindings.MauiHarness.csproj" -f net10.0-ios -p:RuntimeIdentifier=iossimulator-arm64 -p:CodesignKey= -p:CodesignProvision=
fi

echo "Validation complete."
