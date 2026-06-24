using Avalonia;
using Avalonia.Media;
using Mermaider.Models;
using Point = Avalonia.Point;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Native Avalonia renderer for Mermaid Venn diagrams.
/// </summary>
/// <remarks>
/// Venn diagrams in Mermaider use a compact fixed circle layout, so this renderer owns the set
/// placement math and keeps radius, center, palette, and text sizes on the renderer part.
/// </remarks>
public sealed class VennRenderer : MermaidRenderer
{
    private static readonly IReadOnlyList<Color> DefaultSetPalette =
    [
        Color.Parse("#4e79a7"), Color.Parse("#f28e2b"), Color.Parse("#e15759"), Color.Parse("#76b7b2"),
        Color.Parse("#59a14f"), Color.Parse("#edc948"), Color.Parse("#b07aa1"), Color.Parse("#ff9da7")
    ];

    // SVG-compatible placement multipliers. They encode Mermaid's compact illustrative layout.
    private const double TwoSetOffsetRatio = 0.55;
    private const double MultiSetArrangeRadiusRatio = 0.6;
    private const double SetLabelDistanceRatio = 0.6;

    // Stable visual constants from the SVG renderer.
    private const byte SetFillAlpha = 0x59;
    private const double SetStrokeThickness = 2;

    /// <summary>
    /// Defines the <see cref="BaseRadius"/> property.
    /// </summary>
    public static readonly StyledProperty<double> BaseRadiusProperty =
        AvaloniaProperty.Register<VennRenderer, double>(nameof(BaseRadius), 120);

    /// <summary>
    /// Radius used for each Venn set circle.
    /// </summary>
    public double BaseRadius
    {
        get => GetValue(BaseRadiusProperty);
        set => SetValue(BaseRadiusProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="CenterX"/> property.
    /// </summary>
    public static readonly StyledProperty<double> CenterXProperty =
        AvaloniaProperty.Register<VennRenderer, double>(nameof(CenterX), 300);

    /// <summary>
    /// X coordinate of the diagram center; desired width is twice this value.
    /// </summary>
    public double CenterX
    {
        get => GetValue(CenterXProperty);
        set => SetValue(CenterXProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="CenterY"/> property.
    /// </summary>
    public static readonly StyledProperty<double> CenterYProperty =
        AvaloniaProperty.Register<VennRenderer, double>(nameof(CenterY), 200);

    /// <summary>
    /// Y coordinate of the diagram center; desired height is twice this value.
    /// </summary>
    public double CenterY
    {
        get => GetValue(CenterYProperty);
        set => SetValue(CenterYProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="SetLabelFontSize"/> property.
    /// </summary>
    public static readonly StyledProperty<double> SetLabelFontSizeProperty =
        AvaloniaProperty.Register<VennRenderer, double>(nameof(SetLabelFontSize), 16);

    /// <summary>
    /// Font size used for individual set labels.
    /// </summary>
    public double SetLabelFontSize
    {
        get => GetValue(SetLabelFontSizeProperty);
        set => SetValue(SetLabelFontSizeProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="UnionLabelFontSize"/> property.
    /// </summary>
    public static readonly StyledProperty<double> UnionLabelFontSizeProperty =
        AvaloniaProperty.Register<VennRenderer, double>(nameof(UnionLabelFontSize), 14);

    /// <summary>
    /// Font size used for intersection and union labels.
    /// </summary>
    public double UnionLabelFontSize
    {
        get => GetValue(UnionLabelFontSizeProperty);
        set => SetValue(UnionLabelFontSizeProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="SetPalette"/> property.
    /// </summary>
    public static readonly StyledProperty<IReadOnlyList<Color>> SetPaletteProperty =
        AvaloniaProperty.Register<VennRenderer, IReadOnlyList<Color>>(nameof(SetPalette), DefaultSetPalette);

    /// <summary>
    /// Repeating palette used for set circle fills and strokes.
    /// </summary>
    public IReadOnlyList<Color> SetPalette
    {
        get => GetValue(SetPaletteProperty);
        set => SetValue(SetPaletteProperty, value);
    }

    private readonly record struct Style(
        double BaseRadius,
        double CenterX,
        double CenterY,
        double SetLabelFontSize,
        double UnionLabelFontSize,
        IReadOnlyList<Color> SetPalette
    );

    /// <summary>
    /// Calculates the desired size for the diagram using this renderer part's current styled values.
    /// </summary>
    public Size MeasureDiagram(VennDiagram diagram)
    {
        var style = CreateStyleSnapshot();
        return new Size(style.CenterX * 2, style.CenterY * 2);
    }

    /// <summary>
    /// Draws a Venn diagram using this renderer part's current styled values.
    /// </summary>
    public void RenderDiagram(DrawingContext dc, MermaidPresenter presenter, VennDiagram diagram)
    {
        var style = CreateStyleSnapshot();
        if (diagram.Sets.Count == 0)
        {
            return;
        }

        var positions = ComputePositions(diagram.Sets.Count, style);
        var setPositions = new Dictionary<string, Point>();

        for (var i = 0; i < diagram.Sets.Count; i++)
        {
            var set = diagram.Sets[i];
            var position = positions[i];
            var color = GetPaletteColor(style.SetPalette, i);
            var stroke = new Pen(new SolidColorBrush(color), SetStrokeThickness);

            setPositions[set.Id] = position;
            dc.DrawEllipse(
                new SolidColorBrush(WithAlpha(color, SetFillAlpha)),
                stroke,
                position,
                style.BaseRadius,
                style.BaseRadius);

            DrawSetLabel(dc, presenter, style, set, position, i, diagram.Sets.Count);
        }

        foreach (var union in diagram.Unions)
        {
            DrawUnionLabel(dc, presenter, style, union, setPositions);
        }
    }

    /// <inheritdoc/>
    protected override bool ShouldInvalidatePresenterMeasure(AvaloniaProperty property) =>
        property.Name is nameof(BaseRadius) or nameof(CenterX) or nameof(CenterY);

    private Style CreateStyleSnapshot() =>
        new(BaseRadius, CenterX, CenterY, SetLabelFontSize, UnionLabelFontSize, SetPalette);

    private static IReadOnlyList<Point> ComputePositions(int count, Style style)
    {
        var positions = new List<Point>(count);
        if (count == 1)
        {
            positions.Add(new Point(style.CenterX, style.CenterY));
        }
        else if (count == 2)
        {
            var offset = style.BaseRadius * TwoSetOffsetRatio;
            positions.Add(new Point(style.CenterX - offset, style.CenterY));
            positions.Add(new Point(style.CenterX + offset, style.CenterY));
        }
        else
        {
            var arrangeRadius = style.BaseRadius * MultiSetArrangeRadiusRatio;
            for (var i = 0; i < count; i++)
            {
                var angle = (2 * Math.PI * i / count) - (Math.PI / 2);
                positions.Add(
                    new Point(
                        style.CenterX + arrangeRadius * Math.Cos(angle),
                        style.CenterY + arrangeRadius * Math.Sin(angle)));
            }
        }

        return positions;
    }

    private static void DrawSetLabel(
        DrawingContext dc,
        MermaidPresenter presenter,
        Style style,
        VennSet set,
        Point position,
        int index,
        int totalSets)
    {
        var angle = totalSets <= 1 ? 0 : (2 * Math.PI * index / totalSets) - (Math.PI / 2);
        var labelDistance = totalSets <= 1 ? 0 : style.BaseRadius * SetLabelDistanceRatio;
        var x = position.X + labelDistance * Math.Cos(angle);
        var y = position.Y + labelDistance * Math.Sin(angle);

        MermaidTextRenderer.DrawText(
            dc,
            presenter,
            set.Label,
            x,
            y,
            style.SetLabelFontSize,
            presenter.Foreground,
            TextAlignment.Center,
            centerVertically: true,
            FontWeight.SemiBold);
    }

    private static void DrawUnionLabel(
        DrawingContext dc,
        MermaidPresenter presenter,
        Style style,
        VennUnion union,
        IReadOnlyDictionary<string, Point> setPositions)
    {
        if (union.Label is not { Length: > 0 })
        {
            return;
        }

        var x = 0.0;
        var y = 0.0;
        var count = 0;
        foreach (var id in union.SetIds)
        {
            if (setPositions.TryGetValue(id, out var position))
            {
                x += position.X;
                y += position.Y;
                count++;
            }
        }

        if (count == 0)
        {
            return;
        }

        MermaidTextRenderer.DrawText(
            dc,
            presenter,
            union.Label,
            x / count,
            y / count,
            style.UnionLabelFontSize,
            presenter.Foreground,
            TextAlignment.Center,
            centerVertically: true,
            FontWeight.Medium);
    }

    private static Color GetPaletteColor(IReadOnlyList<Color> palette, int index) =>
        palette.Count > 0 ? palette[index % palette.Count] : DefaultSetPalette[index % DefaultSetPalette.Count];

    private static Color WithAlpha(Color color, byte alpha) => new(alpha, color.R, color.G, color.B);
}