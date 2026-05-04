#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
configuration="${1:-Release}"
products_dir="${NUGET_PRODUCTS_DIR:-${script_dir}/products}"
android_packages_dir="${script_dir}/android/nugets"
ios_packages_dir="${script_dir}/ios/nugets"

if [[ -n "${MAPBOX_DOWNLOADS_TOKEN:-}" && -z "${ORG_GRADLE_PROJECT_MAPBOX_DOWNLOADS_TOKEN:-}" ]]; then
  export ORG_GRADLE_PROJECT_MAPBOX_DOWNLOADS_TOKEN="${MAPBOX_DOWNLOADS_TOKEN}"
fi

if [[ -z "${ORG_GRADLE_PROJECT_MAPBOX_DOWNLOADS_TOKEN:-}" ]] &&
  ! grep -q '^[[:space:]]*MAPBOX_DOWNLOADS_TOKEN=' "${HOME}/.gradle/gradle.properties" 2>/dev/null; then
  echo "Missing Mapbox downloads token. Set MAPBOX_DOWNLOADS_TOKEN, ORG_GRADLE_PROJECT_MAPBOX_DOWNLOADS_TOKEN, or ~/.gradle/gradle.properties." >&2
  exit 1
fi

mkdir -p "${products_dir}" "${android_packages_dir}" "${ios_packages_dir}"

echo "Cleaning package output folders..."
find "${products_dir}" -maxdepth 1 -type f \( -name '*.nupkg' -o -name '*.snupkg' \) -delete
find "${android_packages_dir}" -maxdepth 1 -type f \( -name '*.nupkg' -o -name '*.snupkg' \) -delete
find "${ios_packages_dir}" -maxdepth 1 -type f \( -name '*.nupkg' -o -name '*.snupkg' \) -delete

echo "Packing Android bindings (${configuration})..."
dotnet pack "${script_dir}/android/bindings.g.sln" \
  -c "${configuration}" \
  -p:PackageOutputPath="${android_packages_dir}" \
  --nologo

echo "Packing iOS bindings (${configuration})..."
dotnet pack "${script_dir}/ios/mapbox-ios.sln" \
  -c "${configuration}" \
  -p:PackageOutputPath="${ios_packages_dir}" \
  --nologo

shopt -s nullglob
packages=(
  "${android_packages_dir}"/*.nupkg
  "${ios_packages_dir}"/*.nupkg
)

if (( ${#packages[@]} == 0 )); then
  echo "No NuGet packages were produced." >&2
  exit 1
fi

for package in "${packages[@]}"; do
  cp "${package}" "${products_dir}/"
done

echo
echo "Prepared ${#packages[@]} package(s) in ${products_dir}:"
for package in "${products_dir}"/*.nupkg; do
  echo "  ${package}"
done
