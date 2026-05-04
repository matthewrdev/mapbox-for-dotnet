using Android.App;
using Android.OS;
using AndroidX.AppCompat.App;
using Com.Mapbox.Common;
using Com.Mapbox.Maps;

namespace MapboxBindings.AndroidHarness;

[Activity(Label = "@string/app_name", MainLauncher = true, Theme = "@style/AppTheme")]
public class MainActivity : AppCompatActivity
{
    private MapView? mapView;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        MapboxOptions.AccessToken = Intent?.GetStringExtra("MAPBOX_ACCESS_TOKEN")
            ?? System.Environment.GetEnvironmentVariable("MAPBOX_ACCESS_TOKEN")
            ?? "YOUR_MAPBOX_ACCESS_TOKEN";

        var mapInitOptions = new MapInitOptions(
            this,
            MapInitOptions.CompanionField.GetDefaultMapOptions(this),
            MapInitOptions.CompanionField.DefaultPluginList,
            cameraOptions: null,
            textureView: false,
            styleUri: Style.Outdoors);

        mapView = new MapView(this, mapInitOptions);
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
