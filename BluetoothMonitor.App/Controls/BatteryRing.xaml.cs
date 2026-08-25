using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace BluetoothMonitor.App.Controls;

public sealed partial class BatteryRing : UserControl
{
    public BatteryRing()
    {
        InitializeComponent();
        Loaded += (_, _) => RedrawArc();
        SizeChanged += (_, _) => RedrawArc();
    }

    public static readonly DependencyProperty PercentLevelProperty = DependencyProperty.Register(
        nameof(PercentLevel),
        typeof(byte),
        typeof(BatteryRing),
        new PropertyMetadata((byte)0, OnVisualPropertyChanged)
    );

    public byte PercentLevel
    {
        get => (byte)GetValue(PercentLevelProperty);
        set => SetValue(PercentLevelProperty, value);
    }

    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
        nameof(Size),
        typeof(double),
        typeof(BatteryRing),
        new PropertyMetadata(80.0, OnVisualPropertyChanged)
    );

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
        nameof(StrokeThickness),
        typeof(double),
        typeof(BatteryRing),
        new PropertyMetadata(6.0, OnVisualPropertyChanged)
    );

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public string PercentText => $"{PercentLevel}%";

    private static void OnVisualPropertyChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is BatteryRing ring)
        {
            ring.RedrawArc();
        }
    }

    private void RedrawArc()
    {
        Bindings?.Update();
        if (ArcPath is null)
            return;

        double size = Size;
        double stroke = StrokeThickness;
        double r = (size - stroke) / 2;
        Width = size;
        Height = size;
        double cx = size / 2;
        double cy = size / 2;

        double pct = Math.Clamp(PercentLevel / 100.0, 0.0, 1.0);
        if (pct <= 0.0)
        {
            ArcPath.Data = null;
            return;
        }

        double angle = pct * 360.0;
        double startAngle = -90.0;
        double endAngle = startAngle + angle;
        var start = PolarToPoint(cx, cy, r, startAngle);
        var end = PolarToPoint(cx, cy, r, endAngle);

        var figure = new PathFigure { StartPoint = start, IsClosed = false };

        var arcSegment = new ArcSegment
        {
            Point = end,
            Size = new Size(r, r),
            IsLargeArc = angle > 180.0,
            SweepDirection = SweepDirection.Clockwise,
        };
        figure.Segments.Add(arcSegment);

        var geo = new PathGeometry();
        geo.Figures.Add(figure);
        ArcPath.Data = geo;

        if (pct >= 0.999)
        {
            ArcPath.Data = CreateFullCircle(cx, cy, r);
        }
    }

    private static PathGeometry CreateFullCircle(double cx, double cy, double r)
    {
        var figure = new PathFigure { StartPoint = new Point(cx, cy - r), IsClosed = true };
        figure.Segments.Add(
            new ArcSegment
            {
                Point = new Point(cx, cy + r),
                Size = new Size(r, r),
                IsLargeArc = true,
                SweepDirection = SweepDirection.Clockwise,
            }
        );
        figure.Segments.Add(
            new ArcSegment
            {
                Point = new Point(cx, cy - r),
                Size = new Size(r, r),
                IsLargeArc = true,
                SweepDirection = SweepDirection.Clockwise,
            }
        );
        var geo = new PathGeometry();
        geo.Figures.Add(figure);
        return geo;
    }

    private static Point PolarToPoint(double cx, double cy, double r, double angleDeg)
    {
        double rad = angleDeg * Math.PI / 180.0;
        return new Point(cx + r * Math.Cos(rad), cy + r * Math.Sin(rad));
    }
}
