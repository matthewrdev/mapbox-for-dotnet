# Test Harnesses

This folder contains three minimal integration apps for validating the imported
Mapbox bindings:

- `MapboxBindings.AndroidHarness`: native .NET Android harness
- `MapboxBindings.iOSHarness`: native .NET iOS harness
- `MapboxBindings.MauiHarness`: .NET MAUI harness targeting Android and iOS

Open `MapboxBindings.TestHarnesses.sln` to work with all three from one
solution.

## Required Tokens

- `MAPBOX_DOWNLOADS_TOKEN`: required at build time for Android Mapbox Maven
  artifacts. The current Gradle repository setup reads it from a Gradle
  property, for example `~/.gradle/gradle.properties`.
- `MAPBOX_TESTHARNESS_TOKEN`: public Mapbox access token used by the native
  Android and iOS test harnesses. Android writes it to a generated string
  resource; iOS writes it to generated `MBXAccessToken` app manifest metadata.
- `MAPBOX_ACCESS_TOKEN`: optional runtime override for the Android and MAUI map
  views.
- `MapboxAccessToken`: optional MSBuild property override used by iOS harnesses
  when you do not want to read `MAPBOX_TESTHARNESS_TOKEN` from the environment.

## Validation Script

Run the repo-local validation flow with:

```bash
./test/validate.sh
```

That script repacks the Android and iOS bindings into the local `nugets`
folders, clears stale NuGet cache entries, and builds all four harness targets.

## Build Commands

```bash
dotnet pack android/bindings.g.sln -c Debug -p:PackageOutputPath=/absolute/path/to/mapbox-for-dotnet/android/nugets
dotnet pack ios/mapbox-ios.sln -c Debug -p:PackageOutputPath=/absolute/path/to/mapbox-for-dotnet/ios/nugets
dotnet build test/MapboxBindings.AndroidHarness/MapboxBindings.AndroidHarness.csproj -f net10.0-android
dotnet build test/MapboxBindings.iOSHarness/MapboxBindings.iOSHarness.csproj -f net10.0-ios -p:RuntimeIdentifier=iossimulator-arm64 -p:CodesignKey= -p:CodesignProvision=
dotnet build test/MapboxBindings.MauiHarness/MapboxBindings.MauiHarness.csproj -f net10.0-android
dotnet build test/MapboxBindings.MauiHarness/MapboxBindings.MauiHarness.csproj -f net10.0-ios -p:RuntimeIdentifier=iossimulator-arm64 -p:CodesignKey= -p:CodesignProvision=
```

## Notes

- The Android harnesses were validated against locally packed binding NuGets in
  `../android/nugets`.
- The iOS harnesses were validated against locally packed binding NuGets in
  `../ios/nugets`.
- The native iOS harness currently emits the expected `IL2104` trim warning from
  `Microsoft.iOS` during simulator builds.
