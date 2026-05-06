# mapbox-for-dotnet

.NET bindings for Mapbox on iOS and Android.

## Primary Attribution

This repository is heavily and deliberately based on the work of [**Tuyen Vu Duc**](https://github.com/tuyen-vuduc). 

https://www.linkedin.com/in/tuyen-vuduc/
https://tuyen-vuduc.tech

The iOS binding work comes directly from:

- [`tuyen-vuduc/mapbox-ios-binding`](https://github.com/tuyen-vuduc/mapbox-ios-binding)
- [`tuyen-vuduc/mapbox-ios-objective-c`](https://github.com/tuyen-vuduc/mapbox-ios-objective-c)

The Android binding-generation approach and supporting tooling are based on:

- [`tuyen-vuduc/dotnet-binding-utils`](https://github.com/tuyen-vuduc/dotnet-binding-utils/tree/main)

This repo would not exist in its current form without Tuyen's prior work on the
binding strategy, the iOS bridge layer, and the practical path for shipping
Mapbox bindings to .NET developers.

[Please consider sponsoring Tuyen as a thank you for his incredible work building .NET MAUI bindings.](https://github.com/sponsors/tuyen-vuduc)

## Why An All-In-One Repo

We are building an all-in-one repository so Android bindings, iOS bindings, and cross-platform validation live in one place and can be versioned together. 

The goal is to reduce drift between platforms, make packaging and test harnesses
consistent, and give .NET app developers a single source tree to build,
validate, and integrate from.

## Layout

- [android](./android): Android binding sources, shared build props, generated solution, and local NuGet output.
- [ios](./ios): iOS binding sources, imported bridge code, and local NuGet output.
- [test](./test): Native Android, native iOS, and MAUI harness apps used to validate integration.

## Current Binding Pins

- Android graph root: `com.mapbox.maps:android-ndk27:11.23.0`;
  root NuGet package version `11.23.0.2`.
- iOS packages: `Bindings.Mapbox.iOS`,
  `Bindings.Mapbox.iOS.CoreMaps`, and `Bindings.Mapbox.iOS.MapsObjC`
  `11.23.0.2`; `Bindings.Mapbox.iOS.Common` `24.23.0.2`; and
  `Bindings.Mapbox.iOS.Turf` `4.0.0.2`.

NuGet package versions use the native Mapbox SDK version plus a final package
revision component for binding-only maintenance updates.

## Validation

Run:

```bash
./test/validate.sh
```

That script repacks the Android and iOS bindings and builds all harness targets
against the local packages.

## Release Packaging

Double-click `release-nugets.command` in Finder to pack and optionally publish
the Android and iOS NuGets. From a terminal, run:

```bash
./release-nugets.sh
```

The release flow writes platform-local packages to `android/nugets` and
`ios/nugets`, then copies the publishable set into `products`.

Required environment:

- `MAPBOX_DOWNLOADS_TOKEN` for Mapbox native downloads. The Android scripts also
  accept `ORG_GRADLE_PROJECT_MAPBOX_DOWNLOADS_TOKEN` or a `MAPBOX_DOWNLOADS_TOKEN`
  entry in `~/.gradle/gradle.properties` for Gradle/Maven resolution, but iOS
  app and harness builds read `MAPBOX_DOWNLOADS_TOKEN` directly.
- `MAPBOX_NUGET_KEY` for publishing.

Optional environment:

- `NUGET_SOURCE` to publish somewhere other than `https://api.nuget.org/v3/index.json`.
- `NUGET_PRODUCTS_DIR` to override the root `products` output folder.
