# Mapbox .NET iOS Smoke Harness

This is a `net10.0-ios` app that smokes the packaged iOS Mapbox bindings from `ios/nugets`.

It verifies:

- `MapboxAccessToken`/`MAPBOX_TESTHARNESS_TOKEN` plumbing.
- `MapboxCommon`, `MapboxCoreMaps`, `MapboxMaps`, `MapboxMapsObjC`, and `Turf.iOS` managed assembly loading.
- `MapboxCommon.MBXMapboxOptions` access token setup.
- `MapboxMaps.MapView` native binding availability.
- `MapboxMapsObjC` bridge availability.
- `TMBCameraOptions`, `MapInitOptionsFactory`, and `MapViewFactory` startup.

The harness UI mirrors the Android smoke harness:

- Top coordinate entry accepts `lat,lng` or `lat,lng,zoom`.
- Bottom-left buttons switch between street, terrain/outdoors, and satellite styles.
- Bottom-right buttons zoom in and out around the current camera center.

## Build Local Packages

From the repository root:

```sh
dotnet pack ios/mapbox-ios.sln -c Release -t:Clean,Rebuild --output ios/nugets
```

## Build the Harness

Use a real public Mapbox access token so the map can render tiles.

```sh
export MAPBOX_TESTHARNESS_TOKEN="pk..."
export MAPBOX_DOWNLOADS_TOKEN="sk..."

dotnet build test/MapboxBindings.iOSHarness/MapboxBindings.iOSHarness.csproj \
  -f net10.0-ios \
  -p:RuntimeIdentifier=iossimulator-arm64 \
  -p:MapboxAccessToken="$MAPBOX_TESTHARNESS_TOKEN"
```

`MAPBOX_DOWNLOADS_TOKEN` is used by the native Mapbox package download targets. `MAPBOX_TESTHARNESS_TOKEN` is the same token variable used by the Android test harness; the iOS harness turns it into `MBXAccessToken` in the generated app manifest and embeds it into local harness assembly metadata for simulator smoke testing. Do not distribute a harness build that contains a real token.

For simulator smoke runs, use a separate configuration so stale Debug app bundles do not mask runtime-pack changes:

```sh
dotnet build test/MapboxBindings.iOSHarness/MapboxBindings.iOSHarness.csproj \
  -c SmokeTest \
  -f net10.0-ios \
  -p:RuntimeIdentifier=iossimulator-arm64 \
  -p:UseInterpreter=true \
  -p:MtouchInterpreter=all \
  -p:RunAOTCompilation=false \
  -p:MapboxAccessToken="$MAPBOX_TESTHARNESS_TOKEN"
```

## Run

Run from an iOS simulator target after building packages:

```sh
dotnet build test/MapboxBindings.iOSHarness/MapboxBindings.iOSHarness.csproj \
  -f net10.0-ios \
  -t:Run \
  -p:RuntimeIdentifier=iossimulator-arm64 \
  -p:MapboxAccessToken="$MAPBOX_TESTHARNESS_TOKEN"
```

The app renders a map and overlays smoke results on screen. It also writes one line per check to the device log with the `MapboxBindings.iOSHarness` prefix.
