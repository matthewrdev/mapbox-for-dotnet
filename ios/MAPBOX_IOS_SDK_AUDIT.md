# iOS Mapbox SDK Audit

Audit date: 2026-05-05

## Target SDK

The current stable Mapbox Maps SDK for iOS is `11.23.0`.

Official references:

- Mapbox install guide: https://docs.mapbox.com/ios/maps/guides/install/
- Mapbox release feed: https://github.com/mapbox/mapbox-maps-ios/releases
- `v11.23.0` SwiftPM manifest: https://raw.githubusercontent.com/mapbox/mapbox-maps-ios/v11.23.0/Package.swift
- `v11.23.0` binary SwiftPM manifest: https://raw.githubusercontent.com/mapbox/mapbox-maps-ios-binary/v11.23.0/Package.swift

For `11.23.0`, Mapbox pins the dependency stack to:

| Component | Target version |
| --- | ---: |
| MapboxMaps | `11.23.0` |
| MapboxCoreMaps | `11.23.0` |
| MapboxCommon | `24.23.0` |
| Turf | `4.0.0` |

## Current Repository State

The iOS binding is still on the older `11.8.0` stack:

| Area | Current version |
| --- | ---: |
| `Bindings.Mapbox.iOS` package | `11.8.0` |
| `Bindings.Mapbox.iOS.CoreMaps` package | `11.8.0` |
| `Bindings.Mapbox.iOS.MapsObjC` package | `11.8.0` |
| `Bindings.Mapbox.iOS.Common` package | `24.8.0` |
| `Bindings.Mapbox.iOS.Turf` package | `3.0.0` |
| iOS and MAUI harness package references | `11.8.0` |
| iOS quickstart package references | `11.8.0` |
| Native artifact license | `11.8.0` |

The version pins are duplicated across project files and `.targets` files, so a safe upgrade needs both build metadata and package metadata updated together.

## Verification Performed

- Confirmed Mapbox `11.23.0` is the current stable version from official Mapbox documentation and GitHub releases.
- Downloaded the official `11.23.0` direct archive from Mapbox and confirmed it contains `MapboxMaps`, `MapboxCoreMaps`, `MapboxCommon`, and `Turf` XCFrameworks.
- Confirmed direct native download endpoints are reachable for:
  - `MapboxMaps.zip` `11.23.0`
  - `MapboxMaps.xcframework.zip` `11.23.0`
  - `MapboxCoreMaps.xcframework-dynamic.zip` `11.23.0`
  - `MapboxCommon.zip` `24.23.0`
  - `Turf.xcframework.zip` `4.0.0`
- Confirmed the existing iOS package build succeeds at the current pinned versions with:
  - `dotnet pack ios/mapbox-ios.sln -c Release -t:Clean,Rebuild --output ios/nugets`
- Confirmed app-level verification is currently blocked by the local toolchain:
  - Installed Xcode: `26.0`
  - Installed .NET iOS workload: `26.2.10233`
  - The workload requires Xcode `26.3` for iOS simulator/app builds.
- Confirmed `sharpie` is installed locally.
- Confirmed CocoaPods is not installed locally.
- Confirmed `MAPBOX_DOWNLOADS_TOKEN` and `MAPBOX_ACCESS_TOKEN` are not set in the local environment.

## Required Work

### 1. Refresh native artifacts

Download and refresh `ios/artifacts` from the official Mapbox `11.23.0` archive. The repository currently tracks headers and metadata while ignoring the binary framework payloads, so the checked-in artifact headers and license need to be brought forward even if the large XCFramework binaries remain ignored.

Expected artifact versions:

- `MapboxMaps.xcframework` `11.23.0`
- `MapboxCoreMaps.xcframework` `11.23.0`
- `MapboxCommon.xcframework` `24.23.0`
- `Turf.xcframework` `4.0.0`

### 2. Regenerate and reconcile binding APIs

Run Objective Sharpie against the refreshed headers, then manually reconcile the generated bindings.

The current `ios/gen.sh` only binds `MapboxCommon` and hardcodes `iphoneos17.4`. It needs to be modernized before it is reliable:

- Resolve the active iPhoneOS SDK dynamically with `xcrun` or accept `SDK` from the environment.
- Support binding all four native Mapbox frameworks intentionally.
- Keep generated output reviewable, because Swift-generated Objective-C headers usually need hand cleanup before the .NET binding compiles cleanly.

High-risk binding areas:

- Swift generics and async APIs exposed through generated Objective-C headers.
- Enum and option-set changes between `11.8.0` and `11.23.0`.
- Type renames or removed symbols in `MapboxCoreMaps` and `MapboxCommon`.
- Any hand-edited C# in `ApiDefinitions.cs` and `StructsAndEnums.cs`.

### 3. Rebuild the Objective-C bridge

`MapboxMapsObjC.iOS` is the critical blocker. Mapbox does not ship this framework; it comes from the separate Objective-C bridge strategy based on `tuyen-vuduc/mapbox-ios-objective-c`.

Current state:

- The package version is `11.8.0`.
- The `.targets` file downloads `MapboxMapObjC.xcframework` from a Google Drive file id.
- There is no reproducible bridge build integrated into this repository.
- The upstream bridge repo still has stale version pins in its CocoaPods metadata.

Work required:

- Update the bridge project to MapboxMaps `11.23.0`.
- Build a new `MapboxMapObjC.xcframework`.
- Validate that the bridge Swift code still compiles against the `11.23.0` API surface.
- Publish the resulting bridge archive somewhere deterministic.
- Replace the Google Drive-only download reference with a reproducible or explicitly versioned artifact source.
- Update `MapboxMapsObjC.iOS` binding definitions if the bridge API changed.

Local attempt:

- A temporary SwiftPM manifest was able to resolve and download the `11.23.0` binary dependency stack.
- The plain `swift build` path failed because SwiftPM treated the package as a macOS build.
- A proper iOS XCFramework build still needs either a working CocoaPods setup or an Xcode package/archive setup for the bridge target.

### 4. Update package and build metadata

After the refreshed artifacts and bridge are in place, update these version pins together:

- `ios/libs/MapboxMaps.iOS/MapboxMaps.iOS.csproj`
- `ios/libs/MapboxMaps.iOS/MapboxMaps.iOS.targets`
- `ios/libs/MapboxCoreMaps.iOS/MapboxCoreMaps.iOS.csproj`
- `ios/libs/MapboxCoreMaps.iOS/MapboxCoreMaps.iOS.targets`
- `ios/libs/MapboxCommon.iOS/MapboxCommon.iOS.csproj`
- `ios/libs/MapboxCommon.iOS/MapboxCommon.iOS.targets`
- `ios/libs/Turf.iOS/Turf.iOS.csproj`
- `ios/libs/Turf.iOS/Turf.iOS.targets`
- `ios/libs/MapboxMapsObjC.iOS/MapboxMapsObjC.iOS.csproj`
- `ios/libs/MapboxMapsObjC.iOS/MapboxMapsObjC.iOS.targets`
- `test/MapboxBindings.iOSHarness/MapboxBindings.iOSHarness.csproj`
- `test/MapboxBindings.MauiHarness/MapboxBindings.MauiHarness.csproj`
- `ios/qs/Mapbox.iOSQs/Mapbox.iOSQs.csproj`

Recommended cleanup:

- Add shared iOS version properties so future Mapbox upgrades do not require editing every `.csproj` and `.targets` file by hand.
- Keep package assembly versions aligned with the native SDK versions.
- Document the `MAPBOX_DOWNLOADS_TOKEN` requirement for native downloads and `MAPBOX_ACCESS_TOKEN` for running harnesses.

### 5. Verify with the correct toolchain

Verification should include:

- `dotnet pack ios/mapbox-ios.sln -c Release -t:Clean,Rebuild --output ios/nugets`
- Build `test/MapboxBindings.iOSHarness` for `net10.0-ios`. This harness now includes startup smoke checks for token plumbing, managed assembly loading, native binding resolution, the Objective-C bridge, map init options, and map view creation.
- Build `test/MapboxBindings.MauiHarness` for `net10.0-ios`.
- Launch an iOS simulator harness and confirm a map renders with a valid `MBXAccessToken`.
- Confirm native downloads work from a clean NuGet/XamarinBuildDownload cache using `MAPBOX_DOWNLOADS_TOKEN`.

Local verification currently requires upgrading Xcode to the version expected by the installed .NET iOS workload.

## Completion Criteria

The iOS Mapbox SDK update should be considered complete only when:

- All native Mapbox package pins match the `11.23.0` dependency stack.
- The checked-in artifact headers and license match the `11.23.0` archive.
- `MapboxMapObjC.xcframework` is rebuilt for `11.23.0` and consumed from a deterministic source.
- Binding definitions compile without generated-code errors.
- NuGet packages are produced for the updated versions.
- At least one iOS app harness builds and renders a Mapbox map on simulator/device.

Until the Objective-C bridge is rebuilt and app-level validation runs on a compatible Xcode, the update should not be published as complete.
