# android-mapbox-bindings

`android-mapbox-bindings` is a focused repo for generating and publishing the Android Mapbox binding set.

## Primary Attribution

This Android binding work is directly based on the binding-generation approach
created by **Tuyen Vu Duc** in
[`tuyen-vuduc/dotnet-binding-utils`](https://github.com/tuyen-vuduc/dotnet-binding-utils/tree/main).
The structure in this folder is part of a larger `mapbox-for-dotnet` effort, but
the underlying generation strategy and practical foundation came from Tuyen's
work.

## What Is Source-Owned

- `src/android`
  Artifact metadata plus the small set of hand-maintained binding overrides.
- `src/libs`
  The generator, dependency resolver, and host used to regenerate the active graph.
- `src/BindingProject.Shared.props`
  Shared MSBuild defaults for every generated binding project.
- `src/Mapbox.Shared.props`
  Shared Mapbox Gradle repository and namespace fixes.

## What Is Generated

- `bindings.g.sln`
- `src/android/*/*/binding/*.csproj`
- `src/android/*/*/binding/*.targets`
- `src/android/*/*/binding/README.md`
- `src/android/*/*/binding/LICENSE`

Empty `Additions.cs` and empty `Metadata.xml` placeholders are no longer kept. If one exists now, it is expected to contain a real override.

## Main Commands

Generate or regenerate the current graph:

```bash
sh bind.sh --artifact com.mapbox.maps:android-ndk27:11.19.0
```

Build the generated solution:

```bash
dotnet build bindings.g.sln -c Release -v minimal
```

Pack the generated solution:

```bash
dotnet pack bindings.g.sln -c Release -o ./nugets
```

## Docs

- [Architecture](docs/architecture.md)
- [Runbook](docs/runbook.md)
- [Overrides](docs/overrides.md)

`guide.md` is now a compatibility pointer to the docs above.
