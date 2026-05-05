using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CoreGraphics;
using CoreLocation;
using Foundation;
using MapboxMaps;
using MapboxMapsObjC;
using UIKit;

namespace MapboxBindings.iOSHarness;

public sealed class MapboxViewController : UIViewController
{
    private const double DefaultJumpZoom = 14.0;
    private const double MaximumZoom = 24.0;
    private const double MinimumZoom = 0.0;
    private const string Smoke3DLayerId = "dotnet-smoke-3d-buildings";
    private const string Smoke3DLayerSourceId = "composite";
    private const string Smoke3DLayerSourceLayer = "building";
    private const string SmokeTerrainSourceId = "dotnet-smoke-terrain-dem";
    private const string SmokeTerrainSourceUrl = "mapbox://mapbox.mapbox-terrain-dem-v1";

    private readonly List<SmokeTestResult> smokeResults = new();
    private UITextField? coordinateEntry;
    private double currentZoom = SmokeTestRunner.DefaultZoom;
    private CLLocationCoordinate2D currentCenter = new(SmokeTestRunner.DefaultLatitude, SmokeTestRunner.DefaultLongitude);
    private UILabel? messageLabel;
    private MapView? mapView;
    private TMBMapboxMap? mapboxMap;
    private UITextView? statusOverlay;

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();

        var view = View;
        if (view is null)
        {
            return;
        }

        view.BackgroundColor = UIColor.SystemBackground;

        smokeResults.Clear();
        smokeResults.AddRange(SmokeTestRunner.RunStartupChecks());

        try
        {
            mapView = SmokeTestRunner.CreateMapView(view.Bounds);
            mapView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
            mapboxMap = mapView.MapboxMap();
            view.AddSubview(mapView);
            smokeResults.Add(SmokeTestRunner.Pass("Map view startup", "MapViewFactory created and attached a MapView."));
        }
        catch (Exception exception)
        {
            smokeResults.Add(SmokeTestRunner.Fail("Map view startup", $"{exception.GetType().Name}: {exception.Message}"));
        }

        LogResults(smokeResults);

        var coordinateControls = CreateCoordinateControls();
        statusOverlay = CreateStatusOverlay(smokeResults);
        var styleControls = CreateStyleControls();
        var zoomControls = CreateZoomControls();
        messageLabel = CreateMessageLabel();

        view.AddSubview(coordinateControls);
        view.AddSubview(statusOverlay);
        view.AddSubview(styleControls);
        view.AddSubview(zoomControls);
        view.AddSubview(messageLabel);

        NSLayoutConstraint.ActivateConstraints(new[]
        {
            coordinateControls.LeadingAnchor.ConstraintEqualTo(view.SafeAreaLayoutGuide.LeadingAnchor, 12),
            coordinateControls.TrailingAnchor.ConstraintEqualTo(view.SafeAreaLayoutGuide.TrailingAnchor, -12),
            coordinateControls.TopAnchor.ConstraintEqualTo(view.SafeAreaLayoutGuide.TopAnchor, 12),

            statusOverlay.LeadingAnchor.ConstraintEqualTo(view.SafeAreaLayoutGuide.LeadingAnchor, 12),
            statusOverlay.TrailingAnchor.ConstraintEqualTo(view.SafeAreaLayoutGuide.TrailingAnchor, -12),
            statusOverlay.TopAnchor.ConstraintEqualTo(coordinateControls.BottomAnchor, 8),
            statusOverlay.HeightAnchor.ConstraintLessThanOrEqualTo(view.HeightAnchor, 0.3f),
            statusOverlay.HeightAnchor.ConstraintGreaterThanOrEqualTo(112),

            styleControls.LeadingAnchor.ConstraintEqualTo(view.SafeAreaLayoutGuide.LeadingAnchor, 12),
            styleControls.BottomAnchor.ConstraintEqualTo(view.SafeAreaLayoutGuide.BottomAnchor, -12),

            zoomControls.TrailingAnchor.ConstraintEqualTo(view.SafeAreaLayoutGuide.TrailingAnchor, -12),
            zoomControls.BottomAnchor.ConstraintEqualTo(view.SafeAreaLayoutGuide.BottomAnchor, -12),

            messageLabel.CenterXAnchor.ConstraintEqualTo(view.SafeAreaLayoutGuide.CenterXAnchor),
            messageLabel.BottomAnchor.ConstraintEqualTo(styleControls.TopAnchor, -12),
            messageLabel.WidthAnchor.ConstraintLessThanOrEqualTo(view.SafeAreaLayoutGuide.WidthAnchor, 0.82f)
        });

        Run3DSmokeChecks();
    }

    private UIView CreateCoordinateControls()
    {
        var controls = new UIStackView
        {
            Axis = UILayoutConstraintAxis.Horizontal,
            Alignment = UIStackViewAlignment.Fill,
            Distribution = UIStackViewDistribution.Fill,
            Spacing = 8,
            LayoutMargins = new UIEdgeInsets(8, 8, 8, 8),
            LayoutMarginsRelativeArrangement = true,
            BackgroundColor = UIColor.White.ColorWithAlpha(0.92f),
            TranslatesAutoresizingMaskIntoConstraints = false
        };
        controls.Layer.CornerRadius = 8;
        controls.Layer.MasksToBounds = true;

        coordinateEntry = new UITextField
        {
            Placeholder = "lat,lng[,zoom]",
            BorderStyle = UITextBorderStyle.RoundedRect,
            KeyboardType = UIKeyboardType.NumbersAndPunctuation,
            ReturnKeyType = UIReturnKeyType.Go,
            ClearButtonMode = UITextFieldViewMode.WhileEditing,
            BackgroundColor = UIColor.SystemBackground
        };
        coordinateEntry.EditingDidEndOnExit += (_, _) => JumpToCoordinate();

        var jumpButton = CreateTextButton("Go", "Jump to coordinate", JumpToCoordinate);
        jumpButton.WidthAnchor.ConstraintEqualTo(56).Active = true;

        controls.AddArrangedSubview(coordinateEntry);
        controls.AddArrangedSubview(jumpButton);

        return controls;
    }

    private UIView CreateStyleControls() =>
        CreateButtonStack(
            new ControlAction("Map", "Street style", () => ChangeStyle("Street", BuiltInStyles.Streets)),
            new ControlAction("Ter", "3D terrain", () => EnableTerrain(false)),
            new ControlAction("Sat", "Satellite style", () => ChangeStyle("Satellite", BuiltInStyles.SatelliteStreets)));

    private UIView CreateZoomControls() =>
        CreateButtonStack(
            new ControlAction("+", "Zoom in", () => ChangeZoom(1)),
            new ControlAction("-", "Zoom out", () => ChangeZoom(-1)));

    private UIView CreateButtonStack(params ControlAction[] actions)
    {
        var controls = new UIStackView
        {
            Axis = UILayoutConstraintAxis.Vertical,
            Alignment = UIStackViewAlignment.Fill,
            Distribution = UIStackViewDistribution.FillEqually,
            Spacing = 8,
            TranslatesAutoresizingMaskIntoConstraints = false
        };

        foreach (var action in actions)
        {
            var button = CreateTextButton(action.Title, action.AccessibilityLabel, action.Click);
            button.WidthAnchor.ConstraintEqualTo(48).Active = true;
            button.HeightAnchor.ConstraintEqualTo(48).Active = true;
            controls.AddArrangedSubview(button);
        }

        return controls;
    }

    private static UIButton CreateTextButton(string title, string accessibilityLabel, Action click)
    {
        var button = UIButton.FromType(UIButtonType.System);
        button.SetTitle(title, UIControlState.Normal);
        button.AccessibilityLabel = accessibilityLabel;
        button.BackgroundColor = UIColor.White.ColorWithAlpha(0.92f);
        button.TintColor = UIColor.FromRGB(32, 33, 36);
        button.TitleLabel.Font = UIFont.BoldSystemFontOfSize(15);
        button.Layer.CornerRadius = 8;
        button.Layer.MasksToBounds = true;
        button.TouchUpInside += (_, _) => click();
        return button;
    }

    private static UITextView CreateStatusOverlay(IReadOnlyCollection<SmokeTestResult> results)
    {
        var statusView = new UITextView
        {
            Editable = false,
            ScrollEnabled = true,
            Text = BuildStatusText(results),
            TextColor = UIColor.White,
            BackgroundColor = UIColor.Black.ColorWithAlpha(0.76f),
            Font = UIFont.SystemFontOfSize(12),
            TextContainerInset = new UIEdgeInsets(12, 12, 12, 12),
            TranslatesAutoresizingMaskIntoConstraints = false
        };

        statusView.TextContainer.LineFragmentPadding = 0;
        statusView.Layer.CornerRadius = 8;
        statusView.Layer.MasksToBounds = true;
        return statusView;
    }

    private static UILabel CreateMessageLabel()
    {
        var label = new UILabel
        {
            Alpha = 0,
            BackgroundColor = UIColor.Black.ColorWithAlpha(0.78f),
            TextColor = UIColor.White,
            Font = UIFont.BoldSystemFontOfSize(13),
            Lines = 0,
            TextAlignment = UITextAlignment.Center,
            TranslatesAutoresizingMaskIntoConstraints = false
        };
        label.Layer.CornerRadius = 8;
        label.Layer.MasksToBounds = true;
        return label;
    }

    private void JumpToCoordinate()
    {
        if (coordinateEntry?.Text is not { } coordinateText ||
            !TryParseCoordinate(coordinateText, out var latitude, out var longitude, out var zoom))
        {
            ShowMessage("Enter lat,lng[,zoom]");
            return;
        }

        SetCamera(new CLLocationCoordinate2D(latitude, longitude), zoom);
        coordinateEntry.ResignFirstResponder();
    }

    private void ChangeZoom(double delta)
    {
        SyncCameraState();
        SetCamera(currentCenter, currentZoom + delta);
    }

    private void SetCamera(CLLocationCoordinate2D center, double zoom)
    {
        if (mapboxMap is null)
        {
            ShowMessage("Map not ready");
            return;
        }

        currentCenter = center;
        currentZoom = Math.Clamp(zoom, MinimumZoom, MaximumZoom);

        using var cameraOptions = SmokeTestRunner.CreateCameraOptions(currentCenter, currentZoom);
        mapboxMap.SetCameraTo(cameraOptions);
        ShowMessage($"Zoom {currentZoom:0.#}");
    }

    private void ChangeStyle(string label, string styleUri)
    {
        if (mapboxMap is null)
        {
            ShowMessage("Map not ready");
            return;
        }

        mapboxMap.LoadStyleWithUri(styleUri, null, error =>
        {
            InvokeOnMainThread(() =>
            {
                ShowMessage(error is null ? label : error.LocalizedDescription);
            });
        });
    }

    private void Run3DSmokeChecks()
    {
        Run3DLayerSmokeCheck(() => EnableTerrain(true));
    }

    private void Run3DLayerSmokeCheck(Action complete)
    {
        var completed = false;
        void CompleteOnce()
        {
            if (completed)
            {
                return;
            }

            completed = true;
            InvokeOnMainThread(complete);
        }

        if (mapboxMap is null)
        {
            AddSmokeResult(SmokeTestRunner.Fail("3D layer access", "Map is not ready."));
            CompleteOnce();
            return;
        }

        mapboxMap.LoadStyleWithUri(BuiltInStyles.Streets, null, error =>
        {
            if (completed)
            {
                return;
            }

            if (error is not null)
            {
                AddSmokeResult(SmokeTestRunner.Fail("3D layer access", error.LocalizedDescription));
                CompleteOnce();
                return;
            }

            try
            {
                AddSmoke3DLayer(CompleteOnce);
            }
            catch (Exception exception)
            {
                AddSmokeResult(SmokeTestRunner.Fail("3D layer access", $"{exception.GetType().Name}: {exception.Message}"));
                CompleteOnce();
            }
        });
    }

    private void AddSmoke3DLayer(Action complete)
    {
        if (mapboxMap is null)
        {
            AddSmokeResult(SmokeTestRunner.Fail("3D layer access", "Map is not ready."));
            InvokeOnMainThread(complete);
            return;
        }

        var layerProperties = CreateSmoke3DLayerProperties();
        mapboxMap.AddLayerWith(layerProperties, null, error =>
        {
            if (error is not null)
            {
                AddSmokeResult(SmokeTestRunner.Fail("3D layer access", error.LocalizedDescription));
                InvokeOnMainThread(complete);
                return;
            }

            var exists = mapboxMap.LayerExistsWithId(Smoke3DLayerId);
            var layerInfo = mapboxMap.AllLayerIdentifiers.FirstOrDefault(layer => layer.Id == Smoke3DLayerId);
            var isFillExtrusion = string.Equals(
                layerInfo?.Type.RawValue,
                TMBLayerType.FillExtrusion.RawValue,
                StringComparison.OrdinalIgnoreCase);

            if (!exists || !isFillExtrusion)
            {
                AddSmokeResult(SmokeTestRunner.Fail(
                    "3D layer access",
                    $"Layer exists={exists}, type={layerInfo?.Type.RawValue ?? "missing"}."));
                InvokeOnMainThread(complete);
                return;
            }

            SetCameraFor3DLayer();
            AddSmokeResult(SmokeTestRunner.Pass(
                "3D layer access",
                $"Added {Smoke3DLayerId} as {layerInfo!.Type.RawValue} from {Smoke3DLayerSourceId}/{Smoke3DLayerSourceLayer}."));
            InvokeOnMainThread(complete);
        });
    }

    private static NSDictionary<NSString, NSObject> CreateSmoke3DLayerProperties()
    {
        var paintProperties = NSDictionary<NSString, NSObject>.FromObjectsAndKeys(
            new NSObject[]
            {
                NSNumber.FromDouble(0),
                new NSString("#d2693c"),
                NSNumber.FromDouble(90),
                NSNumber.FromDouble(0.78),
                NSNumber.FromBoolean(true)
            },
            new NSString[]
            {
                new("fill-extrusion-base"),
                new("fill-extrusion-color"),
                new("fill-extrusion-height"),
                new("fill-extrusion-opacity"),
                new("fill-extrusion-vertical-gradient")
            });

        return NSDictionary<NSString, NSObject>.FromObjectsAndKeys(
            new NSObject[]
            {
                new NSString(Smoke3DLayerId),
                new NSString(TMBLayerType.FillExtrusion.RawValue),
                new NSString(Smoke3DLayerSourceId),
                new NSString(Smoke3DLayerSourceLayer),
                NSNumber.FromDouble(13),
                paintProperties
            },
            new NSString[]
            {
                new("id"),
                new("type"),
                new("source"),
                new("source-layer"),
                new("minzoom"),
                new("paint")
            });
    }

    private void SetCameraFor3DLayer()
    {
        if (mapboxMap is null)
        {
            return;
        }

        currentCenter = new CLLocationCoordinate2D(40.7484, -73.9857);
        currentZoom = 15.5;

        using var cameraOptions = new TMBCameraOptions(
            currentCenter,
            UIEdgeInsets.Zero,
            CGPoint.Empty,
            (nfloat)currentZoom,
            0,
            60);
        mapboxMap.SetCameraTo(cameraOptions);
    }

    private void EnableTerrain(bool recordSmokeResult)
    {
        if (mapboxMap is null)
        {
            ReportTerrainResult(SmokeTestRunner.Fail("3D terrain access", "Map is not ready."), recordSmokeResult);
            return;
        }

        mapboxMap.LoadStyleWithUri(BuiltInStyles.Outdoors, null, error =>
        {
            if (error is not null)
            {
                ReportTerrainResult(SmokeTestRunner.Fail("3D terrain access", error.LocalizedDescription), recordSmokeResult);
                return;
            }

            try
            {
                AddTerrainSource(recordSmokeResult);
            }
            catch (Exception exception)
            {
                ReportTerrainResult(SmokeTestRunner.Fail("3D terrain access", $"{exception.GetType().Name}: {exception.Message}"), recordSmokeResult);
            }
        });
    }

    private void AddTerrainSource(bool recordSmokeResult)
    {
        if (mapboxMap is null)
        {
            ReportTerrainResult(SmokeTestRunner.Fail("3D terrain access", "Map is not ready."), recordSmokeResult);
            return;
        }

        if (mapboxMap.SourceExistsWithId(SmokeTerrainSourceId))
        {
            SetTerrain(recordSmokeResult);
            return;
        }

        var sourceProperties = CreateSmokeTerrainSourceProperties();
        mapboxMap.AddSourceWithId(SmokeTerrainSourceId, sourceProperties, error =>
        {
            if (error is not null)
            {
                ReportTerrainResult(SmokeTestRunner.Fail("3D terrain access", error.LocalizedDescription), recordSmokeResult);
                return;
            }

            SetTerrain(recordSmokeResult);
        });
    }

    private void SetTerrain(bool recordSmokeResult)
    {
        if (mapboxMap is null)
        {
            ReportTerrainResult(SmokeTestRunner.Fail("3D terrain access", "Map is not ready."), recordSmokeResult);
            return;
        }

        var terrainProperties = CreateSmokeTerrainProperties();
        mapboxMap.SetTerrainWithProperties(terrainProperties, error =>
        {
            if (error is not null)
            {
                ReportTerrainResult(SmokeTestRunner.Fail("3D terrain access", error.LocalizedDescription), recordSmokeResult);
                return;
            }

            var sourceExists = mapboxMap.SourceExistsWithId(SmokeTerrainSourceId);
            var terrainSource = mapboxMap.TerrainPropertyValue("source")?.ToString();
            if (!sourceExists || !string.Equals(terrainSource, SmokeTerrainSourceId, StringComparison.Ordinal))
            {
                ReportTerrainResult(
                    SmokeTestRunner.Fail("3D terrain access", $"Source exists={sourceExists}, terrain source={terrainSource ?? "missing"}."),
                    recordSmokeResult);
                return;
            }

            SetCameraForTerrain();
            var elevation = mapboxMap.ElevationAt(currentCenter);
            var elevationDetails = elevation is null ? "elevation pending" : $"elevation {elevation.DoubleValue:0}m";
            ReportTerrainResult(
                SmokeTestRunner.Pass("3D terrain access", $"Set terrain from {SmokeTerrainSourceId}; {elevationDetails}."),
                recordSmokeResult);
        });
    }

    private static NSDictionary<NSString, NSObject> CreateSmokeTerrainSourceProperties() =>
        NSDictionary<NSString, NSObject>.FromObjectsAndKeys(
            new NSObject[]
            {
                new NSString(TMBSourceType.RasterDem.RawValue),
                new NSString(SmokeTerrainSourceUrl),
                NSNumber.FromInt32(512),
                NSNumber.FromDouble(14)
            },
            new NSString[]
            {
                new("type"),
                new("url"),
                new("tileSize"),
                new("maxzoom")
            });

    private static NSDictionary<NSString, NSObject> CreateSmokeTerrainProperties() =>
        NSDictionary<NSString, NSObject>.FromObjectsAndKeys(
            new NSObject[]
            {
                new NSString(SmokeTerrainSourceId),
                NSNumber.FromDouble(1.5)
            },
            new NSString[]
            {
                new("source"),
                new("exaggeration")
            });

    private void SetCameraForTerrain()
    {
        if (mapboxMap is null)
        {
            return;
        }

        currentCenter = new CLLocationCoordinate2D(37.7459, -119.5936);
        currentZoom = 12.4;

        using var cameraOptions = new TMBCameraOptions(
            currentCenter,
            UIEdgeInsets.Zero,
            CGPoint.Empty,
            (nfloat)currentZoom,
            35,
            70);
        mapboxMap.SetCameraTo(cameraOptions);
    }

    private void ReportTerrainResult(SmokeTestResult result, bool recordSmokeResult)
    {
        if (recordSmokeResult)
        {
            AddSmokeResult(result);
            return;
        }

        InvokeOnMainThread(() =>
        {
            ShowMessage(result.Status == SmokeTestStatus.Passed ? "3D terrain" : result.Details);
        });
    }

    private void AddSmokeResult(SmokeTestResult result)
    {
        InvokeOnMainThread(() =>
        {
            smokeResults.Add(result);
            if (statusOverlay is not null)
            {
                statusOverlay.Text = BuildStatusText(smokeResults);
            }

            LogResults(new[] { result });
            ShowMessage(result.Status == SmokeTestStatus.Passed ? result.Name : result.Details);
        });
    }

    private void SyncCameraState()
    {
        if (mapboxMap is null)
        {
            return;
        }

        try
        {
            var cameraState = mapboxMap.CameraState;
            currentCenter = cameraState.Center;
            currentZoom = cameraState.Zoom;
        }
        catch (Exception)
        {
        }
    }

    private void ShowMessage(string message)
    {
        if (messageLabel is null)
        {
            return;
        }

        messageLabel.Text = $"  {message}  ";
        UIView.Animate(
            0.15,
            () => messageLabel.Alpha = 1,
            () =>
            {
                UIView.AnimateNotify(
                    0.25,
                    1.2,
                    UIViewAnimationOptions.CurveEaseOut,
                    () => messageLabel.Alpha = 0,
                    null);
            });
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

    private static string BuildStatusText(IReadOnlyCollection<SmokeTestResult> results)
    {
        var overall = results.Any(result => result.Status == SmokeTestStatus.Failed)
            ? "FAIL"
            : results.Any(result => result.Status == SmokeTestStatus.Warning)
                ? "WARN"
                : "PASS";

        var lines = results.Select(result => $"[{FormatStatus(result.Status)}] {result.Name}: {result.Details}");
        return $"Mapbox .NET iOS smoke harness\nOverall: {overall}\n\n{string.Join("\n", lines)}";
    }

    private static string FormatStatus(SmokeTestStatus status) =>
        status switch
        {
            SmokeTestStatus.Passed => "PASS",
            SmokeTestStatus.Warning => "WARN",
            SmokeTestStatus.Failed => "FAIL",
            _ => "UNKNOWN"
        };

    private static void LogResults(IEnumerable<SmokeTestResult> results)
    {
        foreach (var result in results)
        {
            Console.WriteLine($"[MapboxBindings.iOSHarness] {FormatStatus(result.Status)} {result.Name}: {result.Details}");
        }
    }

    private sealed record ControlAction(string Title, string AccessibilityLabel, Action Click);
}
