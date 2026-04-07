#if ANDROID
using Com.Mapbox.Common;
using Com.Mapbox.Maps;
using PlatformView = Com.Mapbox.Maps.MapView;
#elif IOS
using CoreGraphics;
using CoreLocation;
using MapboxMaps;
using MapboxMapsObjC;
using PlatformView = MapboxMaps.MapView;
#endif

using Microsoft.Maui.Handlers;

namespace MapboxBindings.MauiHarness;

public sealed class MapboxMapViewHandler : ViewHandler<MapboxMapView, PlatformView>
{
    public static IPropertyMapper<MapboxMapView, MapboxMapViewHandler> Mapper =
        new PropertyMapper<MapboxMapView, MapboxMapViewHandler>(ViewHandler.ViewMapper);

    public MapboxMapViewHandler()
        : base(Mapper)
    {
    }

    protected override PlatformView CreatePlatformView()
    {
#if ANDROID
        MapboxOptions.AccessToken = MapboxMapView.ResolveAccessToken();
        return new PlatformView(Context, new MapInitOptions(Context));
#elif IOS
        var centerLocation = new CLLocationCoordinate2D(40.7128, -74.0060);
        var cameraOptions = new TMBCameraOptions(
            centerLocation,
            UIKit.UIEdgeInsets.Zero,
            CGPoint.Empty,
            10,
            0,
            0);

        var options = MapInitOptionsFactory.CreateWithMapOptions(
            null,
            cameraOptions,
            BuiltInStyles.Streets,
            null,
            (nint)1);

        return MapViewFactory.CreateWithFrame(CGRect.Empty, options);
#else
        throw new PlatformNotSupportedException();
#endif
    }

#if ANDROID
    protected override void ConnectHandler(PlatformView platformView)
    {
        base.ConnectHandler(platformView);
        platformView.OnStart();
    }

    protected override void DisconnectHandler(PlatformView platformView)
    {
        platformView.OnStop();
        platformView.OnDestroy();
        base.DisconnectHandler(platformView);
    }
#endif
}
