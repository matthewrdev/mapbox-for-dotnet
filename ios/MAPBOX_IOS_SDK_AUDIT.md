# iOS Mapbox SDK Audit

Audit date: 2026-05-06

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

The iOS package and native download metadata has been advanced to the `11.23.0`
stack, with NuGet package versions using a final binding-maintenance revision:

| Area | Current version |
| --- | ---: |
| `Bindings.Mapbox.iOS` package | `11.23.0.2` |
| `Bindings.Mapbox.iOS.CoreMaps` package | `11.23.0.2` |
| `Bindings.Mapbox.iOS.MapsObjC` package | `11.23.0.2` |
| `Bindings.Mapbox.iOS.Common` package | `24.23.0.2` |
| `Bindings.Mapbox.iOS.Turf` package | `4.0.0.2` |
| iOS and MAUI harness package references | `11.23.0.2` |
| iOS quickstart package references | `11.23.0.2` |
| Native artifact license | `11.8.0` |

The Objective-C bridge has been rebuilt for `11.23.0` and packaged with the
managed bridge NuGet. The checked-in native artifact license still needs a
separate refresh before the iOS package set should be treated as fully
validated for publication.

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
  - Local .NET SDK: `10.0.201`
  - Installed Xcode: `26.3`
  - Active iPhoneOS/iPhoneSimulator SDK: `26.2`
  - Installed .NET iOS workload: `26.2.10233`
- Rebuilt `MapboxMapObjC.xcframework` from a temporary SwiftPM/Xcode package
  against `mapbox-maps-ios-binary` `11.23.0`.
- Replaced the Google Drive bridge download with packaged
  `MapboxMapsObjC-11.23.0.zip` extraction from `MapboxMapsObjC.iOS.targets`.
- Updated the bridge binding surface for `TMBStylePropertyValue` and current
  `TMBLayerType` static values.
- Built `test/MapboxBindings.iOSHarness` for `net10.0-ios` and
  `iossimulator-arm64`, confirming the packaged bridge extracts and is embedded
  into the simulator app bundle.
  - The build still emits expected generated binding warnings about hidden
    inherited members.
- Confirmed app-level verification is not currently runnable from a clean
  environment because `MAPBOX_DOWNLOADS_TOKEN`, `MAPBOX_ACCESS_TOKEN`, and
  `MAPBOX_TESTHARNESS_TOKEN` are not set locally. A cached native download may
  hide that in some builds, but a clean app or harness build needs the token.
- Confirmed `sharpie` is installed locally: `3.5.61-c2b0b612`.
- Confirmed CocoaPods is not installed locally.

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

### 3. Objective-C bridge

`MapboxMapsObjC.iOS` is the local Objective-C bridge. Mapbox does not ship this
framework; it comes from the separate Objective-C bridge strategy based on
`tuyen-vuduc/mapbox-ios-objective-c`.

Current state:

- The package version is `11.23.0.2`.
- The bridge was rebuilt against MapboxMaps `11.23.0`, MapboxCommon `24.23.0`,
  and Turf `4.0.0`.
- The `.targets` file extracts the packaged `MapboxMapsObjC-11.23.0.zip`
  archive into the consuming project's intermediate output folder.
- The checked-in `MapboxMapObjC-Swift.h` header reflects the rebuilt bridge API.
- The upstream bridge repo still has stale version pins in its CocoaPods metadata.

Compatibility patches used for the rebuild:

- Added explicit Swift imports needed by the SwiftPM/Xcode package build.
- Wrapped Mapbox's Swift-only `StylePropertyValue` in `TMBStylePropertyValue`
  for Objective-C and .NET binding exposure.
- Updated bridge code for current `PuckBearing`, model layer, layer type, and
  expression cases.

Future SDK updates should:

- Rebuild the bridge against the new Mapbox SDK version.
- Replace `ios/artifacts/MapboxMapsObjC-<version>.zip`.
- Refresh `MapboxMapObjC-Swift.h` from the device archive.
- Reconcile `MapboxMapsObjC.iOS` binding definitions against the generated
  header.

### 4. Package and build metadata

The package/build metadata currently targets:

- MapboxMaps, MapboxCoreMaps, and MapboxMapsObjC package version `11.23.0.2`
- MapboxCommon package version `24.23.0.2`
- Turf package version `4.0.0.2`
- Native MapboxMaps/CoreMaps download version `11.23.0`
- Native MapboxCommon version `24.23.0`
- Native Turf version `4.0.0`

Future Mapbox or binding-maintenance updates must keep these files in sync:

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
- Keep assembly versions aligned with the native SDK versions and keep NuGet
  package versions as `<native-version>.<binding-revision>`.
- Document the `MAPBOX_DOWNLOADS_TOKEN` requirement for native downloads and `MAPBOX_ACCESS_TOKEN` for running harnesses.

### 5. Verify with the correct toolchain

Verification should include:

- `dotnet pack ios/mapbox-ios.sln -c Release -t:Clean,Rebuild --output ios/nugets`
- Build `test/MapboxBindings.iOSHarness` for `net10.0-ios`. This harness now includes startup smoke checks for token plumbing, managed assembly loading, native binding resolution, the Objective-C bridge, map init options, and map view creation.
- Build `test/MapboxBindings.MauiHarness` for `net10.0-ios`.
- Launch an iOS simulator harness and confirm a map renders with a valid `MBXAccessToken`.
- Confirm native downloads work from a clean NuGet/XamarinBuildDownload cache using `MAPBOX_DOWNLOADS_TOKEN`.

Local app-level verification currently requires valid Mapbox tokens. No local
Xcode version mismatch is present in this checkout.

## Completion Criteria

The iOS Mapbox SDK update should be considered complete only when:

- Package and native download metadata match the `11.23.0` dependency stack.
- The checked-in artifact headers and license match the `11.23.0` archive.
- `MapboxMapObjC.xcframework` is rebuilt for `11.23.0` and consumed from a deterministic source.
- Binding definitions compile without generated-code errors.
- NuGet packages are produced for the updated versions.
- At least one iOS app harness builds and renders a Mapbox map on simulator/device.

Until app-level validation runs with valid Mapbox tokens on simulator/device,
the update should not be published as complete.
