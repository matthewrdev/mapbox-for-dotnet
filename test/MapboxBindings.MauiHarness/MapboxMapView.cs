namespace MapboxBindings.MauiHarness;

public sealed class MapboxMapView : View
{
    internal static string ResolveAccessToken() =>
        Environment.GetEnvironmentVariable("MAPBOX_ACCESS_TOKEN")
        ?? "YOUR_MAPBOX_ACCESS_TOKEN";
}
