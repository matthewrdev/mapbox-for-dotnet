using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.AppCompat.App;
using Com.Mapbox.Common;
using Com.Mapbox.Maps;
using System.Globalization;
using GeoPoint = Com.Mapbox.Geojson.Point;

namespace MapboxBindings.AndroidHarness;

[Activity(Label = "@string/app_name", MainLauncher = true, Theme = "@style/AppTheme")]
public class MainActivity : AppCompatActivity
{
    private const double DefaultJumpZoom = 14.0;
    private const double MaximumWebMercatorLatitude = 85.05112878;
    private const double MaximumZoom = 24.0;
    private const double MinimumZoom = 0.0;
    private const double ZoomStep = 1.0;

    private EditText? coordinateEntry;
    private MapView? mapView;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var runtimeAccessToken = Intent?.GetStringExtra("MAPBOX_ACCESS_TOKEN")
            ?? System.Environment.GetEnvironmentVariable("MAPBOX_ACCESS_TOKEN");
        if (!string.IsNullOrWhiteSpace(runtimeAccessToken))
        {
            MapboxOptions.AccessToken = runtimeAccessToken;
        }

        var mapInitOptions = new MapInitOptions(
            this,
            MapInitOptions.CompanionField.GetDefaultMapOptions(this),
            MapInitOptions.CompanionField.DefaultPluginList,
            cameraOptions: null,
            textureView: false,
            styleUri: Style.Outdoors);

        mapView = new MapView(this, mapInitOptions);

        var root = new FrameLayout(this);
        root.AddView(
            mapView,
            new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent));

        root.AddView(CreateCoordinateControls(), CreateCoordinateControlsLayoutParams());
        SetContentView(root);
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

    private LinearLayout CreateCoordinateControls()
    {
        var controls = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };
        controls.SetPadding(Dp(8), Dp(8), Dp(8), Dp(8));
        controls.SetBackgroundColor(Color.Argb(230, 255, 255, 255));

        var coordinateRow = new LinearLayout(this)
        {
            Orientation = Orientation.Horizontal
        };

        coordinateEntry = new EditText(this)
        {
            Hint = "lat,lng"
        };
        coordinateEntry.SetSingleLine(true);
        coordinateEntry.InputType = Android.Text.InputTypes.ClassText;
        coordinateEntry.ImeOptions = ImeAction.Go;
        coordinateEntry.EditorAction += (_, args) =>
        {
            if (args.ActionId == ImeAction.Go)
            {
                JumpToCoordinate();
                args.Handled = true;
            }
        };
        coordinateRow.AddView(
            coordinateEntry,
            new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1));

        var jumpButton = CreateActionButton("Go", JumpToCoordinate);
        coordinateRow.AddView(
            jumpButton,
            new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent));
        controls.AddView(
            coordinateRow,
            new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent));

        var actionRow = new LinearLayout(this)
        {
            Orientation = Orientation.Horizontal
        };
        actionRow.AddView(CreateActionButton("-", () => ZoomBy(-ZoomStep)), CreateWeightedButtonLayoutParams());
        actionRow.AddView(CreateActionButton("+", () => ZoomBy(ZoomStep)), CreateWeightedButtonLayoutParams());
        actionRow.AddView(CreateActionButton("Random", GenerateRandomCoordinate), CreateWeightedButtonLayoutParams());
        controls.AddView(
            actionRow,
            new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent));

        return controls;
    }

    private Button CreateActionButton(string text, Action click)
    {
        var button = new Button(this)
        {
            Text = text
        };
        button.Click += (_, _) => click();
        return button;
    }

    private static LinearLayout.LayoutParams CreateWeightedButtonLayoutParams() =>
        new(0, ViewGroup.LayoutParams.WrapContent, 1);

    private FrameLayout.LayoutParams CreateCoordinateControlsLayoutParams()
    {
        var layoutParams = new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        {
            Gravity = GravityFlags.Top
        };
        layoutParams.SetMargins(Dp(8), Dp(8), Dp(8), 0);
        return layoutParams;
    }

    private void JumpToCoordinate()
    {
        if (coordinateEntry?.Text is not { } coordinateText ||
            !TryParseCoordinate(coordinateText, out var latitude, out var longitude, out var zoom))
        {
            Toast.MakeText(this, "Enter lat,lng", ToastLength.Short)?.Show();
            return;
        }

        var cameraOptions = new CameraOptions.Builder()
            .Center(GeoPoint.FromLngLat(longitude, latitude))
            ?.Zoom(Java.Lang.Double.ValueOf(zoom))
            ?.Build();

        if (cameraOptions is null)
        {
            Toast.MakeText(this, "Unable to build camera", ToastLength.Short)?.Show();
            return;
        }

        mapView?.MapboxMap.SetCamera(cameraOptions);
        HideKeyboard();
    }

    private void ZoomBy(double delta)
    {
        if (mapView?.MapboxMap is not { } mapboxMap)
        {
            return;
        }

        var cameraState = mapboxMap.CameraState;
        var cameraOptions = new CameraOptions.Builder()
            .Center(cameraState.Center)
            ?.Zoom(Java.Lang.Double.ValueOf(Math.Clamp(cameraState.Zoom + delta, MinimumZoom, MaximumZoom)))
            ?.Bearing(Java.Lang.Double.ValueOf(cameraState.Bearing))
            ?.Pitch(Java.Lang.Double.ValueOf(cameraState.Pitch))
            ?.Padding(cameraState.Padding)
            ?.Build();

        if (cameraOptions is null)
        {
            return;
        }

        mapboxMap.SetCamera(cameraOptions);
    }

    private void GenerateRandomCoordinate()
    {
        var latitude = NextRandomDouble(-MaximumWebMercatorLatitude, MaximumWebMercatorLatitude);
        var longitude = NextRandomDouble(-180, 180);

        var coordinateText = string.Format(
            CultureInfo.InvariantCulture,
            "{0:F4},{1:F4}",
            latitude,
            longitude);
        if (coordinateEntry is null)
        {
            return;
        }

        coordinateEntry.Text = coordinateText;
        coordinateEntry?.SetSelection(coordinateText.Length);

        JumpToCoordinate();
    }

    private static bool TryParseCoordinate(
        string coordinateText,
        out double latitude,
        out double longitude,
        out double zoom)
    {
        latitude = 0;
        longitude = 0;
        zoom = DefaultJumpZoom;

        var parts = coordinateText
            .Split(new[] { ',', ' ', '\t', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Trim())
            .ToArray();

        if (parts.Length < 2 ||
            !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out latitude) ||
            !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out longitude))
        {
            return false;
        }

        if (parts.Length >= 3 &&
            !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out zoom))
        {
            return false;
        }

        return latitude is >= -90 and <= 90 &&
            longitude is >= -180 and <= 180 &&
            zoom is >= MinimumZoom and <= MaximumZoom;
    }

    private void HideKeyboard()
    {
        var inputMethodManager = (InputMethodManager?)GetSystemService(InputMethodService);
        if (CurrentFocus?.WindowToken is { } windowToken)
        {
            inputMethodManager?.HideSoftInputFromWindow(windowToken, HideSoftInputFlags.None);
        }
        coordinateEntry?.ClearFocus();
    }

    private int Dp(int value) =>
        (int)(value * (Resources?.DisplayMetrics?.Density ?? 1f) + 0.5f);

    private static double NextRandomDouble(double minimum, double maximum) =>
        minimum + (Random.Shared.NextDouble() * (maximum - minimum));
}
