using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.AppCompat.App;
using Com.Mapbox.Bindgen;
using Com.Mapbox.Common;
using Com.Mapbox.Maps;
using System.Globalization;
using GeoPoint = Com.Mapbox.Geojson.Point;

namespace MapboxBindings.AndroidHarness;

[Activity(Label = "@string/app_name", MainLauncher = true, Theme = "@style/AppTheme")]
public class MainActivity : AppCompatActivity
{
    private const double DefaultJumpZoom = 14.0;
    private const double MaximumZoom = 24.0;
    private const double MinimumZoom = 0.0;
    private const double SmokeModelLatitude = 60.171957;
    private const double SmokeModelLongitude = 24.945389;
    private const string SmokeModelGlbAssetUri = "asset://monte-sordo-mapbox-smoke.glb";
    private const string SmokeModelId = "monte-sordo-uld";
    private const string SmokeModelLayerId = "dotnet-smoke-glb-model-layer";
    private const string SmokeModelSourceId = "dotnet-smoke-glb-model-source";

    private EditText? coordinateEntry;
    private readonly Dictionary<string, bool> layerGroupVisibility = new(StringComparer.Ordinal);
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
        root.AddView(CreateStyleControls(), CreateBottomStackLayoutParams(GravityFlags.Bottom | GravityFlags.Left));
        root.AddView(CreateLayerControls(), CreateBottomStackLayoutParams(GravityFlags.Bottom | GravityFlags.Right));
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

        return controls;
    }

    private LinearLayout CreateStyleControls() =>
        CreateIconStack(
            new IconAction(Resource.Drawable.ic_material_map, "Street style", () => ChangeStyle("Street", Style.MapboxStreets)),
            new IconAction(Resource.Drawable.ic_material_terrain, "Terrain style", () => ChangeStyle("Terrain", Style.Outdoors)),
            new IconAction(Resource.Drawable.ic_material_satellite, "Satellite style", () => ChangeStyle("Satellite", Style.SatelliteStreets)),
            new IconAction(Resource.Drawable.ic_material_view_in_ar, "GLB model", EnableModel));

    private LinearLayout CreateLayerControls() =>
        CreateIconStack(
            new IconAction(Resource.Drawable.ic_material_label, "Toggle labels", () => ToggleLayerGroup("labels", IsLabelLayer)),
            new IconAction(Resource.Drawable.ic_material_polyline, "Toggle lines", () => ToggleLayerGroup("lines", IsLineLayer)),
            new IconAction(Resource.Drawable.ic_material_view_in_ar, "Toggle 3D", () => ToggleLayerGroup("3d", IsExtrusionLayer)));

    private LinearLayout CreateIconStack(params IconAction[] actions)
    {
        var controls = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };

        foreach (var action in actions)
        {
            controls.AddView(
                CreateIconButton(action),
                new LinearLayout.LayoutParams(Dp(48), Dp(48))
                {
                    BottomMargin = Dp(8)
                });
        }

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

    private ImageButton CreateIconButton(IconAction action)
    {
        var button = new ImageButton(this)
        {
            ContentDescription = action.ContentDescription
        };
        button.SetImageResource(action.IconResourceId);
        button.SetColorFilter(Color.Rgb(32, 33, 36));
        button.SetBackgroundColor(Color.Argb(230, 255, 255, 255));
        button.SetPadding(Dp(10), Dp(10), Dp(10), Dp(10));
        button.Click += (_, _) => action.Click();
        return button;
    }

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

    private FrameLayout.LayoutParams CreateBottomStackLayoutParams(GravityFlags gravity)
    {
        var layoutParams = new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent,
            ViewGroup.LayoutParams.WrapContent)
        {
            Gravity = gravity
        };
        layoutParams.SetMargins(Dp(12), 0, Dp(12), Dp(12));
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

    private void ChangeStyle(string label, string styleUri)
    {
        if (mapView?.MapboxMap is not { } mapboxMap)
        {
            return;
        }

        layerGroupVisibility.Clear();
        mapboxMap.LoadStyleUri(styleUri, _ =>
        {
            Toast.MakeText(this, label, ToastLength.Short)?.Show();
        });
    }

    private void EnableModel()
    {
        if (mapView?.MapboxMap is not { } mapboxMap)
        {
            return;
        }

        layerGroupVisibility.Clear();
        mapboxMap.LoadStyleUri(Style.Standard, style =>
        {
            var sourceResult = style.StyleSourceExists(SmokeModelSourceId)
                ? null
                : style.AddStyleSource(SmokeModelSourceId, CreateSmokeModelSourceProperties());
            if (sourceResult?.IsError == true)
            {
                Toast.MakeText(this, "Unable to add GLB source", ToastLength.Long)?.Show();
                return;
            }

            var layerResult = style.StyleLayerExists(SmokeModelLayerId)
                ? null
                : style.AddStyleLayer(CreateSmokeModelLayerProperties(), null);
            if (layerResult?.IsError == true)
            {
                Toast.MakeText(this, "Unable to add GLB layer", ToastLength.Long)?.Show();
                return;
            }

            SetCameraForModel();
            Toast.MakeText(this, "GLB model", ToastLength.Short)?.Show();
        });
    }

    private void ToggleLayerGroup(string groupKey, Func<StyleObjectInfo, bool> layerMatcher)
    {
        if (mapView?.MapboxMap is not { } mapboxMap)
        {
            return;
        }

        mapboxMap.GetStyle(style =>
        {
            var shouldShow = layerGroupVisibility.TryGetValue(groupKey, out var isVisible) && !isVisible;
            var changedLayerCount = SetLayerGroupVisibility(style, layerMatcher, shouldShow);
            layerGroupVisibility[groupKey] = shouldShow;

            Toast.MakeText(
                this,
                $"{(shouldShow ? "Show" : "Hide")} {changedLayerCount} layer(s)",
                ToastLength.Short)?.Show();
        });
    }

    private static int SetLayerGroupVisibility(
        Style style,
        Func<StyleObjectInfo, bool> layerMatcher,
        bool isVisible)
    {
        var visibility = new Value(isVisible ? "visible" : "none");
        var changedLayerCount = 0;

        foreach (var layer in style.StyleLayers.Where(layerMatcher))
        {
            if (!style.StyleLayerExists(layer.Id))
            {
                continue;
            }

            var result = style.SetStyleLayerProperty(layer.Id, "visibility", visibility);
            if (!result.IsError)
            {
                changedLayerCount++;
            }
        }

        return changedLayerCount;
    }

    private static bool IsLabelLayer(StyleObjectInfo layer) =>
        string.Equals(layer.Type, "symbol", StringComparison.OrdinalIgnoreCase);

    private static bool IsLineLayer(StyleObjectInfo layer) =>
        string.Equals(layer.Type, "line", StringComparison.OrdinalIgnoreCase);

    private static bool IsExtrusionLayer(StyleObjectInfo layer) =>
        string.Equals(layer.Type, "fill-extrusion", StringComparison.OrdinalIgnoreCase);

    private static Value CreateSmokeModelSourceProperties()
    {
        var model = CreateValueObject(
            ("uri", new Value(SmokeModelGlbAssetUri)),
            ("position", CreateValueArray(SmokeModelLongitude, SmokeModelLatitude)),
            ("orientation", CreateValueArray(0, 0, 0)));
        var models = CreateValueObject(
            (SmokeModelId, model));

        return CreateValueObject(
            ("type", new Value("model")),
            ("models", models));
    }

    private static Value CreateSmokeModelLayerProperties()
    {
        var paintProperties = CreateValueObject(
            ("model-scale", CreateValueArray(180, 180, 180)),
            ("model-translation", CreateValueArray(0, 0, 0)),
            ("model-rotation", CreateValueArray(0, 0, 0)),
            ("model-opacity", new Value(1.0)),
            ("model-type", new Value("common-3d")));

        return CreateValueObject(
            ("id", new Value(SmokeModelLayerId)),
            ("type", new Value("model")),
            ("source", new Value(SmokeModelSourceId)),
            ("paint", paintProperties));
    }

    private static Value CreateValueArray(params double[] values) =>
        Value.ValueOf(values.Select(value => new Value(value)).ToList());

    private static Value CreateValueObject(params (string Key, Value Value)[] properties) =>
        Value.ValueOf(properties.ToDictionary(property => property.Key, property => property.Value));

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

    private void SetCameraForModel()
    {
        var cameraOptions = new CameraOptions.Builder()
            .Center(GeoPoint.FromLngLat(SmokeModelLongitude, SmokeModelLatitude))
            ?.Zoom(Java.Lang.Double.ValueOf(16.2))
            ?.Bearing(Java.Lang.Double.ValueOf(25))
            ?.Pitch(Java.Lang.Double.ValueOf(62))
            ?.Build();

        if (cameraOptions is null)
        {
            Toast.MakeText(this, "Unable to build GLB camera", ToastLength.Short)?.Show();
            return;
        }

        mapView?.MapboxMap.SetCamera(cameraOptions);
    }

    private int Dp(int value) =>
        (int)(value * (Resources?.DisplayMetrics?.Density ?? 1f) + 0.5f);

    private sealed record IconAction(int IconResourceId, string ContentDescription, Action Click);
}
