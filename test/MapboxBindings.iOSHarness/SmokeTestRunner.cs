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

    private const string TokenMetadataKey = "MapboxAccessToken";
    private const string TokenEnvironmentVariable = "MAPBOX_TESTHARNESS_TOKEN";
    private const string PlaceholderToken = "YOUR_MAPBOX_ACCESS_TOKEN";

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
