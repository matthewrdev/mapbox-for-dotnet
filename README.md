# mapbox-for-dotnet

## Primary Attribution

This repository is heavily and deliberately based on the work of **Tuyen Vu Duc**.
The iOS binding work comes directly from:

- [`tuyen-vuduc/mapbox-ios-binding`](https://github.com/tuyen-vuduc/mapbox-ios-binding)
- [`tuyen-vuduc/mapbox-ios-objective-c`](https://github.com/tuyen-vuduc/mapbox-ios-objective-c)

The Android binding-generation approach and supporting tooling are based on:

- [`tuyen-vuduc/dotnet-binding-utils`](https://github.com/tuyen-vuduc/dotnet-binding-utils/tree/main)

This repo would not exist in its current form without Tuyen's prior work on the
binding strategy, the iOS bridge layer, and the practical path for shipping
Mapbox bindings to .NET developers.

## Why An All-In-One Repo

We are building an all-in-one repository so Android bindings, iOS bindings, and
cross-platform validation live in one place and can be versioned together. That
reduces drift between platforms, makes packaging and test harnesses consistent,
and gives .NET app developers a single source tree to build, validate, and
integrate from.

## Layout

- [android](./android): Android binding sources, shared build props, generated solution, and local NuGet output.
- [ios](./ios): iOS binding sources, imported bridge code, and local NuGet output.
- [test](./test): Native Android, native iOS, and MAUI harness apps used to validate integration.

## Validation

Run:

```bash
./test/validate.sh
```

That script repacks the Android and iOS bindings and builds all harness targets
against the local packages.
