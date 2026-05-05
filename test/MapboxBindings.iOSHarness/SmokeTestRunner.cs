using System.Reflection;
using CoreGraphics;
using CoreLocation;
using Foundation;
using MapboxCommon;
using MapboxCoreMaps;
using MapboxMaps;
using MapboxMapsObjC;
using UIKit;

namespace MapboxBindings.iOSHarness;

internal static class SmokeTestRunner
{
    public const double DefaultLatitude = 40.7128;
    public const double DefaultLongitude = -74.0060;
    public const double DefaultZoom = 10;
    public const string ModelGlbFileName = "monte-sordo-mapbox-smoke.glb";

    private const string TokenMetadataKey = "MapboxAccessToken";
    private const string TokenEnvironmentVariable = "MAPBOX_TESTHARNESS_TOKEN";
    private const string PlaceholderToken = "YOUR_MAPBOX_ACCESS_TOKEN";
    private const string ModelGlbResourceName = "monte-sordo-mapbox-smoke";
    private const string ModelGlbResourceExtension = "glb";

    public static IReadOnlyList<SmokeTestResult> RunStartupChecks()
    {
        var results = new List<SmokeTestResult>();
        var accessToken = ResolveAccessToken();
        var hasToken = IsAccessTokenConfigured(accessToken);

        results.Add(hasToken
            ? Pass("Access token", "Mapbox access token is configured.")
            : Warn("Access token", $"Set {TokenEnvironmentVariable} or pass -p:MapboxAccessToken=... before expecting tiles to render."));

        AddCheck(results, "MapboxCommon access token API", () =>
        {
            if (!hasToken)
            {
                return Warn("MapboxCommon access token API", "Skipped setter check because no real token is configured.");
            }

            MBXMapboxOptions.SetAccessTokenForToken(accessToken!);
            return MBXMapboxOptions.AccessToken == accessToken
                ? Pass("MapboxCommon access token API", "MBXMapboxOptions accepted the token.")
                : Fail("MapboxCommon access token API", "MBXMapboxOptions returned a different token after setting it.");
        });

        AddCheck(results, "Managed assemblies", () =>
        {
            var assemblies = new[]
            {
                typeof(MBXMapboxOptions).Assembly,
                typeof(MBMMapOptions).Assembly,
                typeof(MapInitOptions).Assembly,
                typeof(MapViewFactory).Assembly,
                Assembly.Load("Turf.iOS")
            };

            var details = string.Join(", ", assemblies.Select(assembly =>
            {
                var name = assembly.GetName();
                return $"{name.Name} {name.Version}";
            }));

            return Pass("Managed assemblies", details);
        });

        AddCheck(results, "MapboxMaps native binding", () =>
        {
            var layerClass = MapView.LayerClass;
            return layerClass is not null
                ? Pass("MapboxMaps native binding", $"MapView.LayerClass resolved as {layerClass.Name}.")
                : Fail("MapboxMaps native binding", "MapView.LayerClass returned null.");
        });

        AddCheck(results, "MapboxMapsObjC bridge", () =>
        {
            var streetsStyle = BuiltInStyles.Streets;
            return string.IsNullOrWhiteSpace(streetsStyle)
                ? Fail("MapboxMapsObjC bridge", "BuiltInStyles.Streets returned an empty value.")
                : Pass("MapboxMapsObjC bridge", $"BuiltInStyles.Streets resolved to {streetsStyle}.");
        });

        AddCheck(results, "3D layer binding surface", () =>
        {
            using var layer = new TMBFillExtrusionLayer("dotnet-3d-binding-check", "composite")
            {
                SourceLayer = "building",
                MinZoom = NSNumber.FromDouble(13),
                FillExtrusionHeight = TMBValue.DoubleValue(42)
            };

            return string.Equals(layer.Type.RawValue, TMBLayerType.FillExtrusion.RawValue, StringComparison.OrdinalIgnoreCase)
                ? Pass("3D layer binding surface", $"TMBFillExtrusionLayer resolved as {layer.Type.RawValue}.")
                : Fail("3D layer binding surface", $"Expected {TMBLayerType.FillExtrusion.RawValue}, got {layer.Type.RawValue}.");
        });

        AddCheck(results, "3D terrain binding surface", () =>
        {
            using var source = new TMBRasterDemSource("dotnet-terrain-binding-check")
            {
                Url = "mapbox://mapbox.mapbox-terrain-dem-v1",
                TileSize = NSNumber.FromInt32(512),
                Maxzoom = NSNumber.FromDouble(14)
            };
            using var terrain = new TMBTerrain(source.Id)
            {
                Exaggeration = TMBValue.DoubleValue(1.5)
            };

            return string.Equals(source.Type.RawValue, TMBSourceType.RasterDem.RawValue, StringComparison.OrdinalIgnoreCase)
                && string.Equals(terrain.Source, source.Id, StringComparison.Ordinal)
                ? Pass("3D terrain binding surface", $"TMBRasterDemSource resolved as {source.Type.RawValue}; TMBTerrain targets {terrain.Source}.")
                : Fail("3D terrain binding surface", $"Source type={source.Type.RawValue}, terrain source={terrain.Source}.");
        });

        AddCheck(results, "3D model binding surface", () =>
        {
            using var modelUri = CreateModelGlbUrl();
            if (modelUri is null)
            {
                return Fail("3D model binding surface", $"Bundled GLB asset {ModelGlbFileName} could not be resolved.");
            }

            using var model = new TMBModel(
                modelUri,
                new[] { NSNumber.FromDouble(24.945389), NSNumber.FromDouble(60.171957), NSNumber.FromDouble(0) },
                new[] { NSNumber.FromDouble(0), NSNumber.FromDouble(0), NSNumber.FromDouble(0) });

            return string.Equals(model.Uri?.AbsoluteString, modelUri.AbsoluteString, StringComparison.Ordinal)
                && string.Equals(TMBModelType.Common3d.RawValue, "common-3d", StringComparison.Ordinal)
                && string.Equals(TMBSourceType.Model.RawValue, "model", StringComparison.Ordinal)
                ? Pass("3D model binding surface", $"TMBModel, TMBModelType, and model source type are exposed for {ModelGlbFileName}.")
                : Fail("3D model binding surface", $"Model URI={model.Uri?.AbsoluteString ?? "missing"}, model type={TMBModelType.Common3d.RawValue}, source type={TMBSourceType.Model.RawValue}.");
        });

        AddCheck(results, "Camera options", () =>
        {
            using var cameraOptions = CreateDefaultCameraOptions();
            return cameraOptions.Zoom > 0
                ? Pass("Camera options", $"Created camera at {cameraOptions.Center.Latitude:0.0000},{cameraOptions.Center.Longitude:0.0000}.")
                : Fail("Camera options", "Camera options were created with an invalid zoom.");
        });

        AddCheck(results, "Map init options", () =>
        {
            using var cameraOptions = CreateDefaultCameraOptions();
            using var mapInitOptions = CreateMapInitOptions(cameraOptions);
            return mapInitOptions is not null
                ? Pass("Map init options", "MapInitOptionsFactory returned a native options object.")
                : Fail("Map init options", "MapInitOptionsFactory returned null.");
        });

        return results;
    }

    public static MapView CreateMapView(CGRect frame)
    {
        var accessToken = ResolveAccessToken();
        if (IsAccessTokenConfigured(accessToken))
        {
            MBXMapboxOptions.SetAccessTokenForToken(accessToken!);
        }

        var cameraOptions = CreateDefaultCameraOptions();
        var mapInitOptions = CreateMapInitOptions(cameraOptions);
        return MapViewFactory.CreateWithFrame(frame, mapInitOptions);
    }

    public static string? ResolveModelGlbUrl() =>
        CreateModelGlbUrl()?.AbsoluteString;

    public static TMBCameraOptions CreateCameraOptions(CLLocationCoordinate2D centerLocation, double zoom) =>
        new(
            centerLocation,
            UIEdgeInsets.Zero,
            CGPoint.Empty,
            (nfloat)zoom,
            0,
            0);

    private static TMBCameraOptions CreateDefaultCameraOptions() =>
        CreateCameraOptions(
            new CLLocationCoordinate2D(DefaultLatitude, DefaultLongitude),
            DefaultZoom);

    private static MapInitOptions CreateMapInitOptions(TMBCameraOptions cameraOptions) =>
        MapInitOptionsFactory.CreateWithMapOptions(
            null,
            cameraOptions,
            BuiltInStyles.Outdoors,
            null,
            (nint)1);

    private static NSUrl? CreateModelGlbUrl()
    {
        var path = NSBundle.MainBundle.PathForResource(ModelGlbResourceName, ModelGlbResourceExtension);
        return string.IsNullOrWhiteSpace(path) ? null : NSUrl.FromFilename(path);
    }

    private static string? ResolveAccessToken()
    {
        var assemblyToken = typeof(SmokeTestRunner)
            .Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == TokenMetadataKey)
            ?.Value;
        if (IsAccessTokenConfigured(assemblyToken))
        {
            return assemblyToken;
        }

        var infoPlistToken = NSBundle.MainBundle.ObjectForInfoDictionary("MBXAccessToken")?.ToString();
        if (IsAccessTokenConfigured(infoPlistToken))
        {
            return infoPlistToken;
        }

        var environmentToken = Environment.GetEnvironmentVariable(TokenEnvironmentVariable);
        return IsAccessTokenConfigured(environmentToken) ? environmentToken : null;
    }

    private static bool IsAccessTokenConfigured(string? accessToken) =>
        !string.IsNullOrWhiteSpace(accessToken)
        && !string.Equals(accessToken, PlaceholderToken, StringComparison.OrdinalIgnoreCase)
        && !accessToken.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase)
        && !accessToken.Contains("$(", StringComparison.Ordinal);

    private static void AddCheck(ICollection<SmokeTestResult> results, string name, Func<SmokeTestResult> check)
    {
        try
        {
            results.Add(check());
        }
        catch (Exception exception)
        {
            results.Add(Fail(name, $"{exception.GetType().Name}: {exception.Message}"));
        }
    }

    public static SmokeTestResult Pass(string name, string details) =>
        new(name, SmokeTestStatus.Passed, details);

    public static SmokeTestResult Warn(string name, string details) =>
        new(name, SmokeTestStatus.Warning, details);

    public static SmokeTestResult Fail(string name, string details) =>
        new(name, SmokeTestStatus.Failed, details);
}
