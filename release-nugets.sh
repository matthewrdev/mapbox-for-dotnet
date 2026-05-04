#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "${script_dir}"

configuration="Release"
pack=true
upload=true
assume_yes=false
source_url="${NUGET_SOURCE:-https://api.nuget.org/v3/index.json}"
products_dir="${NUGET_PRODUCTS_DIR:-${script_dir}/products}"

usage() {
  cat <<'USAGE'
Usage: ./release-nugets.sh [Release|Debug] [options]

Options:
  -c, --configuration <name>  Build configuration. Defaults to Release.
  --no-pack                  Upload packages already present in products.
  --no-upload                Pack only; do not prompt for upload.
  -y, --yes                  Upload without an interactive confirmation.
  -h, --help                 Show this help text.

Environment:
  MAPBOX_NUGET_KEY        NuGet API key used for upload.
  NUGET_SOURCE            NuGet source URL. Defaults to nuget.org.
  MAPBOX_DOWNLOADS_TOKEN  Mapbox downloads token for Android artifacts.
USAGE
}

confirm() {
  local prompt="$1"
  local reply

  if [[ "${assume_yes}" == "true" ]]; then
    return 0
  fi

  read -r -p "${prompt} [y/N] " reply
  case "${reply}" in
    y|Y|yes|YES|Yes) return 0 ;;
    *) return 1 ;;
  esac
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    Debug|Release)
      configuration="$1"
      shift
      ;;
    -c|--configuration)
      if [[ $# -lt 2 || -z "$2" ]]; then
        echo "Missing value for $1." >&2
        exit 2
      fi
      configuration="$2"
      shift 2
      ;;
    --no-pack)
      pack=false
      shift
      ;;
    --no-upload)
      upload=false
      shift
      ;;
    -y|--yes)
      assume_yes=true
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage >&2
      exit 2
      ;;
  esac
done

if [[ "${pack}" == "true" ]]; then
  ./pack-nugets.sh "${configuration}"
fi

shopt -s nullglob
packages=("${products_dir}"/*.nupkg)

if (( ${#packages[@]} == 0 )); then
  echo "No packages found in ${products_dir}. Run ./pack-nugets.sh first." >&2
  exit 1
fi

echo
echo "Prepared ${#packages[@]} package(s):"
for package in "${packages[@]}"; do
  echo "  ${package}"
done

if [[ "${upload}" != "true" ]]; then
  echo
  echo "Upload skipped."
  exit 0
fi

echo
if ! confirm "Upload these packages to ${source_url}?"; then
  echo "Upload skipped."
  exit 0
fi

api_key="${MAPBOX_NUGET_KEY:-}"
if [[ -z "${api_key}" ]]; then
  echo "Set MAPBOX_NUGET_KEY before uploading." >&2
  exit 1
fi

for package in "${packages[@]}"; do
  echo "Uploading ${package}..."
  dotnet nuget push "${package}" \
    --api-key "${api_key}" \
    --source "${source_url}" \
    --skip-duplicate
done

echo "Upload complete."
