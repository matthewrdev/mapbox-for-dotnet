using Android.App;
using Android.OS;
using Com.Mapbox.Common;
using Com.Mapbox.Maps;

namespace MapboxBindings.AndroidHarness;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : Activity
{
    private MapView? mapView;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        MapboxOptions.AccessToken = System.Environment.GetEnvironmentVariable("MAPBOX_ACCESS_TOKEN")
            ?? "YOUR_MAPBOX_ACCESS_TOKEN";

        mapView = new MapView(this, new MapInitOptions(this));
        SetContentView(mapView);
    }

    protected override void OnStart()
    {
        base.OnStart();
        mapView?.OnStart();
    }

    protected override void OnResume()
    {
        base.OnResume();
        mapView?.OnResume();
    }

    protected override void OnStop()
    {
        mapView?.OnStop();
        base.OnStop();
    }

    protected override void OnDestroy()
    {
        mapView?.OnDestroy();
        base.OnDestroy();
    }

    public override void OnLowMemory()
    {
        base.OnLowMemory();
        mapView?.OnLowMemory();
    }
}
