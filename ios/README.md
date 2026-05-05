# iOS Mapbox Bindings

This folder owns the iOS side of `mapbox-for-dotnet`: .NET iOS binding
projects for Mapbox's native SDK frameworks, the Objective-C bridge binding,
and the quickstart solution used to smoke-test the packaged NuGets.

## Primary Attribution

This iOS binding work is based directly on **Tuyen Vu Duc's** repositories:

- [`tuyen-vuduc/mapbox-ios-binding`](https://github.com/tuyen-vuduc/mapbox-ios-binding)
- [`tuyen-vuduc/mapbox-ios-objective-c`](https://github.com/tuyen-vuduc/mapbox-ios-objective-c)

The binding itself and the Objective-C bridge strategy come from Tuyen's work.
This folder keeps that work together with the Android bindings and shared test
harnesses in `mapbox-for-dotnet`.

## Current Binding Set

The checked-in iOS binding is still on the Mapbox `11.8.0` stack:

| Project | Package | Current version |
| --- | --- | ---: |
| `MapboxMaps.iOS` | `Bindings.Mapbox.iOS` | `11.8.0` |
| `MapboxCoreMaps.iOS` | `Bindings.Mapbox.iOS.CoreMaps` | `11.8.0` |
| `MapboxMapsObjC.iOS` | `Bindings.Mapbox.iOS.MapsObjC` | `11.8.0` |
| `MapboxCommon.iOS` | `Bindings.Mapbox.iOS.Common` | `24.8.0` |
| `Turf.iOS` | `Bindings.Mapbox.iOS.Turf` | `3.0.0` |

All binding projects target `net10.0-ios` and set
`SupportedOSPlatformVersion` to `15.0`.

## Package Use

For a map-view app, reference the Mapbox Maps package and the Objective-C bridge
package. The remaining iOS packages are pulled in by package dependencies.

```bash
dotnet add package Bindings.Mapbox.iOS --version 11.8.0
dotnet add package Bindings.Mapbox.iOS.MapsObjC --version 11.8.0
```

Consumer app builds need:

- `MAPBOX_DOWNLOADS_TOKEN`: secret Mapbox downloads token used by the package
  targets to download `MapboxMaps.zip`.
- `MapboxAccessToken`: public Mapbox access token MSBuild property, or an
  `MBXAccessToken` app manifest entry, for runtime tile access.

The package targets currently download Mapbox's native `MapboxMaps.zip` archive
with `Xamarin.Build.Download`. `MapboxMapsObjC.iOS` downloads the separate
`MapboxMapObjC.xcframework` bridge from the Google Drive file id encoded in
`ios/libs/MapboxMapsObjC.iOS/MapboxMapsObjC.iOS.targets`.

## Quickstart

Create the ignored quickstart props file from the template:

```bash
cp ios/qs/Mapbox.iOSQs/Mapbox.iOSQs.props.template \
  ios/qs/Mapbox.iOSQs/Mapbox.iOSQs.props
```

Then fill in:

```xml
<MAPBOX_DOWNLOADS_TOKEN>sk...</MAPBOX_DOWNLOADS_TOKEN>
<MapboxAccessToken>pk...</MapboxAccessToken>
```

Build the quickstart from the repo root:

```bash
dotnet build ios/qs/Mapbox.iOSQs/Mapbox.iOSQs.csproj \
  -f net10.0-ios \
  -p:RuntimeIdentifier=iossimulator-arm64 \
  -p:CodesignKey= \
  -p:CodesignProvision=
```

## Build Packages

From the repo root:

```bash
dotnet pack ios/mapbox-ios.sln -c Release -t:Clean,Rebuild --output ios/nugets
```

The current package build succeeds locally with warnings from generated binding
code about hidden inherited members. Those warnings are expected for the current
hand-reconciled binding surface.

## Regeneration And Upgrade Notes

`ios/gen.sh` is a Sharpie helper, not a full upgrade pipeline. Current behavior:

- It hardcodes `SDK=iphoneos17.4`.
- It only invokes binding generation for `MapboxCommon`.
- The other framework bind calls are commented out.

Use [MAPBOX_IOS_SDK_AUDIT.md](./MAPBOX_IOS_SDK_AUDIT.md) as the current upgrade
runbook before attempting to move beyond the `11.8.0` stack. The key blocker is
the Objective-C bridge: Mapbox does not ship `MapboxMapObjC.xcframework`, so it
must be rebuilt and republished for the target Mapbox SDK version before the
managed bridge package can be updated safely.

## License

The binding code is released under the 3-Clause BSD license. See
[LICENSE](./LICENSE) for details.

This license does not override or replace the Mapbox native SDK license in
[artifacts/LICENSE.md](./artifacts/LICENSE.md).
