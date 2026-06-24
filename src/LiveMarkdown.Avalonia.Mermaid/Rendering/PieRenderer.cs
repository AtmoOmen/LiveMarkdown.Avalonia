using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Mermaider.Models;
using Point = Avalonia.Point;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Native Avalonia renderer for Mermaid pie charts.
/// </summary>
/// <remarks>
/// The layout mirrors Mermaider's SVG renderer: a fixed-radius pie on the left and a legend column
/// on the right. Geometry tokens live on this styleable renderer part so applications can tune chart
/// layout without adding one-off properties to <see cref="MermaidPresenter"/>.
/// </remarks>
public class PieRenderer : MermaidRenderer
{
    private static readonly IReadOnlyList<Color> DefaultSlicePalette =
    [
        Color.Parse("#4e79a7"), Color.Parse("#f28e2b"), Color.Parse("#e15759"), Color.Parse("#76b7b2"),
        Color.Parse("#59a14f"), Color.Parse("#edc948"), Color.Parse("#b07aa1"), Color.Parse("#ff9da7"),
        Color.Parse("#9c755f"), Color.Parse("#bab0ac")
    ];

    // Mermaider's SVG renderer suppresses tiny labels below 3% to avoid unreadable slice text.
    private const double SliceLabelMinimumPercentage = 3;

    // Visual compatibility constants from Mermaider's SVG renderer; these do not affect layout.
    private const double FullCircleTolerance = 0.0001;
    private const double SliceStrokeThickness = 2;
    private const double LegendSwatchCornerRadius = 3;

    /// <summary>
    /// Defines the <see cref="Radius"/> property.
    /// </summary>
    public static readonly StyledProperty<double> RadiusProperty =
        AvaloniaProperty.Register<PieRenderer, double>(nameof(Radius), 140);

    /// <summary>
    /// Radius of the pie circle in diagram pixels.
    /// </summary>
    public double Radius
    {
        get => GetValue(RadiusProperty);
        set => SetValue(RadiusProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="CenterX"/> property.
    /// </summary>
    public static readonly StyledProperty<double> CenterXProperty =
        AvaloniaProperty.Register<PieRenderer, double>(nameof(CenterX), 200);

    /// <summary>
    /// X coordinate of the pie center before title-driven vertical offset is applied.
    /// </summary>
    public double CenterX
    {
        get => GetValue(CenterXProperty);
        set => SetValue(CenterXProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="LegendX"/> property.
    /// </summary>
    public static readonly StyledProperty<double> LegendXProperty =
        AvaloniaProperty.Register<PieRenderer, double>(nameof(LegendX), 400);

    /// <summary>
    /// Left edge of the legend column.
    /// </summary>
    public double LegendX
    {
        get => GetValue(LegendXProperty);
        set => SetValue(LegendXProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="LegendWidth"/> property.
    /// </summary>
    public static readonly StyledProperty<double> LegendWidthProperty =
        AvaloniaProperty.Register<PieRenderer, double>(nameof(LegendWidth), 200);

    /// <summary>
    /// Width reserved to the right of <see cref="LegendX"/> for legend labels.
    /// </summary>
    public double LegendWidth
    {
        get => GetValue(LegendWidthProperty);
        set => SetValue(LegendWidthProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="LegendSwatchSize"/> property.
    /// </summary>
    public static readonly StyledProperty<double> LegendSwatchSizeProperty =
        AvaloniaProperty.Register<PieRenderer, double>(nameof(LegendSwatchSize), 14);

    /// <summary>
    /// Size of the square color swatch drawn for each legend row.
    /// </summary>
    public double LegendSwatchSize
    {
        get => GetValue(LegendSwatchSizeProperty);
        set => SetValue(LegendSwatchSizeProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="LegendRowHeight"/> property.
    /// </summary>
    public static readonly StyledProperty<double> LegendRowHeightProperty =
        AvaloniaProperty.Register<PieRenderer, double>(nameof(LegendRowHeight), 22);

    /// <summary>
    /// Vertical distance between legend rows.
    /// </summary>
    public double LegendRowHeight
    {
        get => GetValue(LegendRowHeightProperty);
        set => SetValue(LegendRowHeightProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="LegendTextOffset"/> property.
    /// </summary>
    public static readonly StyledProperty<double> LegendTextOffsetProperty =
        AvaloniaProperty.Register<PieRenderer, double>(nameof(LegendTextOffset), 22);

    /// <summary>
    /// Horizontal offset from swatch left edge to legend text anchor.
    /// </summary>
    public double LegendTextOffset
    {
        get => GetValue(LegendTextOffsetProperty);
        set => SetValue(LegendTextOffsetProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="TitleHeight"/> property.
    /// </summary>
    public static readonly StyledProperty<double> TitleHeightProperty =
        AvaloniaProperty.Register<PieRenderer, double>(nameof(TitleHeight), 36);

    /// <summary>
    /// Vertical space reserved before the pie when a title is present.
    /// </summary>
    public double TitleHeight
    {
        get => GetValue(TitleHeightProperty);
        set => SetValue(TitleHeightProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="TitleY"/> property.
    /// </summary>
    public static readonly StyledProperty<double> TitleYProperty =
        AvaloniaProperty.Register<PieRenderer, double>(nameof(TitleY), 24);

    /// <summary>
    /// Y coordinate of the title anchor inside the reserved title area.
    /// </summary>
    public double TitleY
    {
        get => GetValue(TitleYProperty);
        set => SetValue(TitleYProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="PieTopPadding"/> property.
    /// </summary>
    public static readonly StyledProperty<double> PieTopPaddingProperty =
        AvaloniaProperty.Register<PieRenderer, double>(nameof(PieTopPadding), 20);

    /// <summary>
    /// Gap between the title area and the top of the pie circle.
    /// </summary>
    public double PieTopPadding
    {
        get => GetValue(PieTopPaddingProperty);
        set => SetValue(PieTopPaddingProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="PieBottomPadding"/> property.
    /// </summary>
    public static readonly StyledProperty<double> PieBottomPaddingProperty =
        AvaloniaProperty.Register<PieRenderer, double>(nameof(PieBottomPadding), 30);

    /// <summary>
    /// Extra vertical padding below the pie circle.
    /// </summary>
    public double PieBottomPadding
    {
        get => GetValue(PieBottomPaddingProperty);
        set => SetValue(PieBottomPaddingProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="PieRightPadding"/> property.
    /// </summary>
    public static readonly StyledProperty<double> PieRightPaddingProperty =
        AvaloniaProperty.Register<PieRenderer, double>(nameof(PieRightPadding), 30);

    /// <summary>
    /// Minimum horizontal padding to the right of the pie when no legend is wider.
    /// </summary>
    public double PieRightPadding
    {
        get => GetValue(PieRightPaddingProperty);
        set => SetValue(PieRightPaddingProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="LegendTopPadding"/> property.
    /// </summary>
    public static readonly StyledProperty<double> LegendTopPaddingProperty =
        AvaloniaProperty.Register<PieRenderer, double>(nameof(LegendTopPadding), 30);

    /// <summary>
    /// Distance from the title area to the first legend row.
    /// </summary>
    public double LegendTopPadding
    {
        get => GetValue(LegendTopPaddingProperty);
        set => SetValue(LegendTopPaddingProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="LegendBottomPadding"/> property.
    /// </summary>
    public static readonly StyledProperty<double> LegendBottomPaddingProperty =
        AvaloniaProperty.Register<PieRenderer, double>(nameof(LegendBottomPadding), 20);

    /// <summary>
    /// Extra vertical padding below the legend rows.
    /// </summary>
    public double LegendBottomPadding
    {
        get => GetValue(LegendBottomPaddingProperty);
        set => SetValue(LegendBottomPaddingProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="LabelRadiusRatio"/> property.
    /// </summary>
    public static readonly StyledProperty<double> LabelRadiusRatioProperty =
        AvaloniaProperty.Register<PieRenderer, double>(nameof(LabelRadiusRatio), 0.75);

    /// <summary>
    /// Fraction of <see cref="Radius"/> used to place percentage labels inside slices.
    /// </summary>
    public double LabelRadiusRatio
    {
        get => GetValue(LabelRadiusRatioProperty);
        set => SetValue(LabelRadiusRatioProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="TitleFontSize"/> property.
    /// </summary>
    public static readonly StyledProperty<double> TitleFontSizeProperty =
        AvaloniaProperty.Register<PieRenderer, double>(nameof(TitleFontSize), 18);

    /// <summary>
    /// Font size used for the optional chart title.
    /// </summary>
    public double TitleFontSize
    {
        get => GetValue(TitleFontSizeProperty);
        set => SetValue(TitleFontSizeProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="LabelFontSize"/> property.
    /// </summary>
    public static readonly StyledProperty<double> LabelFontSizeProperty =
        AvaloniaProperty.Register<PieRenderer, double>(nameof(LabelFontSize), 14);

    /// <summary>
    /// Font size used for percentage labels inside pie slices.
    /// </summary>
    public double LabelFontSize
    {
        get => GetValue(LabelFontSizeProperty);
        set => SetValue(LabelFontSizeProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="LegendFontSize"/> property.
    /// </summary>
    public static readonly StyledProperty<double> LegendFontSizeProperty =
        AvaloniaProperty.Register<PieRenderer, double>(nameof(LegendFontSize), 14);

    /// <summary>
    /// Font size used for legend labels.
    /// </summary>
    public double LegendFontSize
    {
        get => GetValue(LegendFontSizeProperty);
        set => SetValue(LegendFontSizeProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="SlicePalette"/> property.
    /// </summary>
    public static readonly StyledProperty<IReadOnlyList<Color>> SlicePaletteProperty =
        AvaloniaProperty.Register<PieRenderer, IReadOnlyList<Color>>(nameof(SlicePalette), DefaultSlicePalette);

    /// <summary>
    /// Repeating color palette used for pie slices and legend swatches.
    /// </summary>
    public IReadOnlyList<Color> SlicePalette
    {
        get => GetValue(SlicePaletteProperty);
        set => SetValue(SlicePaletteProperty, value);
    }

    private readonly record struct Style(
        double Radius,
        double CenterX,
        double LegendX,
        double LegendWidth,
        double LegendSwatchSize,
        double LegendRowHeight,
        double LegendTextOffset,
        double TitleHeight,
        double TitleY,
        double PieTopPadding,
        double PieBottomPadding,
        double PieRightPadding,
        double LegendTopPadding,
        double LegendBottomPadding,
        double LabelRadiusRatio,
        double TitleFontSize,
        double LabelFontSize,
        double LegendFontSize,
        IReadOnlyList<Color> SlicePalette
    );

    /// <summary>
    /// Calculates the desired size for the chart using this renderer part's current styled values.
    /// </summary>
    public Size MeasureDiagram(PieChart chart) => Measure(chart, CreateStyleSnapshot());

    /// <summary>
    /// Draws a pie chart using this renderer part's current styled values.
    /// </summary>
    public void RenderDiagram(DrawingContext dc, MermaidPresenter presenter, PieChart chart)
    {
        var style = CreateStyleSnapshot();
        var total = chart.Slices.Sum(static slice => slice.Value);
        var hasTitle = chart.Title is { Length: > 0 };
        var metrics = CreateLayoutMetrics(chart, style, hasTitle);

        if (hasTitle)
        {
            MermaidTextRenderer.DrawText(
                dc,
                presenter,
                chart.Title!,
                style.CenterX,
                style.TitleY,
                style.TitleFontSize,
                presenter.Foreground,
                TextAlignment.Center,
                centerVertically: true,
                FontWeight.Bold);
        }

        if (total <= 0 || chart.Slices.Count == 0)
        {
            return;
        }

        var startAngle = 0.0;
        for (var i = 0; i < chart.Slices.Count; i++)
        {
            var slice = chart.Slices[i];
            var fraction = slice.Value / total;
            var sweepAngle = fraction * 2 * Math.PI;
            var color = GetPaletteColor(style.SlicePalette, i);
            var fill = new SolidColorBrush(color);
            var stroke = new Pen(presenter.BackgroundBrush ?? Brushes.Transparent, SliceStrokeThickness);

            DrawSlice(dc, fill, stroke, style.CenterX, metrics.CenterY, style.Radius, startAngle, sweepAngle);
            DrawSliceLabel(dc, presenter, style, fraction, metrics.CenterY, startAngle, sweepAngle);

            startAngle += sweepAngle;
        }

        DrawLegend(dc, presenter, style, chart, metrics.LegendTop);
    }

    /// <inheritdoc/>
    protected override bool ShouldInvalidatePresenterMeasure(AvaloniaProperty property) =>
        property.Name is nameof(Radius) or nameof(CenterX) or nameof(LegendX) or nameof(LegendWidth) or nameof(LegendRowHeight) or
            nameof(TitleHeight) or
            nameof(PieTopPadding) or nameof(PieBottomPadding) or nameof(PieRightPadding) or nameof(LegendTopPadding) or nameof(LegendBottomPadding);

    private static Size Measure(PieChart chart, Style style)
    {
        var hasTitle = chart.Title is { Length: > 0 };
        var metrics = CreateLayoutMetrics(chart, style, hasTitle);
        return new Size(metrics.Width, metrics.Height);
    }

    private static PieLayoutMetrics CreateLayoutMetrics(PieChart chart, Style style, bool hasTitle)
    {
        var titleHeight = hasTitle ? style.TitleHeight : 0.0;
        var centerY = titleHeight + style.PieTopPadding + style.Radius;
        var chartHeight = centerY + style.Radius + style.PieBottomPadding;
        var legendTop = titleHeight + style.LegendTopPadding;
        var legendHeight = chart.Slices.Count * style.LegendRowHeight;
        var height = Math.Max(chartHeight, legendTop + legendHeight + style.LegendBottomPadding);
        var width = Math.Max(style.CenterX + style.Radius + style.PieRightPadding, style.LegendX + style.LegendWidth);
        return new PieLayoutMetrics(centerY, legendTop, width, height);
    }

    private Style CreateStyleSnapshot() =>
        new(
            Radius,
            CenterX,
            LegendX,
            LegendWidth,
            LegendSwatchSize,
            LegendRowHeight,
            LegendTextOffset,
            TitleHeight,
            TitleY,
            PieTopPadding,
            PieBottomPadding,
            PieRightPadding,
            LegendTopPadding,
            LegendBottomPadding,
            LabelRadiusRatio,
            TitleFontSize,
            LabelFontSize,
            LegendFontSize,
            SlicePalette);

    private static void DrawSlice(
        DrawingContext dc,
        IBrush fill,
        IPen stroke,
        double centerX,
        double centerY,
        double radius,
        double startAngle,
        double sweepAngle)
    {
        if (sweepAngle >= (2 * Math.PI) - FullCircleTolerance)
        {
            dc.DrawEllipse(fill, stroke, new Point(centerX, centerY), radius, radius);
            return;
        }

        var x1 = centerX + radius * Math.Cos(startAngle);
        var y1 = centerY + radius * Math.Sin(startAngle);
        var x2 = centerX + radius * Math.Cos(startAngle + sweepAngle);
        var y2 = centerY + radius * Math.Sin(startAngle + sweepAngle);

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(centerX, centerY), isFilled: true);
            ctx.LineTo(new Point(x1, y1));
            ctx.ArcTo(new Point(x2, y2), new Size(radius, radius), 0, sweepAngle > Math.PI, SweepDirection.Clockwise);
            ctx.EndFigure(isClosed: true);
        }

        dc.DrawGeometry(fill, stroke, geometry);
    }

    private static void DrawSliceLabel(
        DrawingContext dc,
        MermaidPresenter presenter,
        Style style,
        double fraction,
        double centerY,
        double startAngle,
        double sweepAngle)
    {
        var pct = fraction * 100;
        if (pct < SliceLabelMinimumPercentage)
        {
            return;
        }

        var midAngle = startAngle + sweepAngle / 2;
        var labelR = style.Radius * style.LabelRadiusRatio;
        var x = style.CenterX + labelR * Math.Cos(midAngle);
        var y = centerY + labelR * Math.Sin(midAngle);

        MermaidTextRenderer.DrawText(
            dc,
            presenter,
            $"{FormatNumber(pct)}%",
            x,
            y,
            style.LabelFontSize,
            Brushes.White,
            TextAlignment.Center,
            centerVertically: true,
            FontWeight.SemiBold);
    }

    private static void DrawLegend(DrawingContext dc, MermaidPresenter presenter, Style style, PieChart chart, double legendTop)
    {
        for (var i = 0; i < chart.Slices.Count; i++)
        {
            var slice = chart.Slices[i];
            var color = GetPaletteColor(style.SlicePalette, i);
            var y = legendTop + i * style.LegendRowHeight;

            dc.DrawRectangle(
                new SolidColorBrush(color),
                null,
                new Rect(style.LegendX, y, style.LegendSwatchSize, style.LegendSwatchSize),
                LegendSwatchCornerRadius,
                LegendSwatchCornerRadius);

            var text = chart.ShowData ? $"{slice.Label} ({FormatNumber(slice.Value)})" : slice.Label;
            MermaidTextRenderer.DrawText(
                dc,
                presenter,
                text,
                style.LegendX + style.LegendTextOffset,
                y + style.LegendSwatchSize / 2,
                style.LegendFontSize,
                presenter.Foreground,
                TextAlignment.Left,
                centerVertically: true);
        }
    }

    private static Color GetPaletteColor(IReadOnlyList<Color> palette, int index) =>
        palette.Count > 0 ? palette[index % palette.Count] : DefaultSlicePalette[index % DefaultSlicePalette.Count];

    private static string FormatNumber(double value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);

    private readonly record struct PieLayoutMetrics(double CenterY, double LegendTop, double Width, double Height);
}