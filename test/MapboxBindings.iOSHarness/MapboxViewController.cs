using CoreGraphics;
using CoreLocation;
using MapboxMaps;
using MapboxMapsObjC;
using UIKit;

namespace MapboxBindings.iOSHarness;

public sealed class MapboxViewController : UIViewController
{
    public override void ViewDidLoad()
    {
        base.ViewDidLoad();

        var view = View;
        if (view is null)
        {
            return;
        }

        view.BackgroundColor = UIColor.SystemBackground;

        var centerLocation = new CLLocationCoordinate2D(40.7128, -74.0060);
        var cameraOptions = new TMBCameraOptions(
            centerLocation,
            UIEdgeInsets.Zero,
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

        var mapView = MapViewFactory.CreateWithFrame(view.Bounds, options);
        mapView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
        view.AddSubview(mapView);
    }
}
