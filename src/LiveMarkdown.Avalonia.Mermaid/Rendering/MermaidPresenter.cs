using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Mermaider;
using Mermaider.Layout;
using Mermaider.Models;
using Mermaider.Parsing;

namespace LiveMarkdown.Avalonia;

public class MermaidPresenter : Control
{
    /// <summary>
    /// Defines the <see cref="Text"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<MermaidPresenter, string?>(nameof(Text));

    /// <summary>
    /// Mermaid diagram definition text.
    /// </summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="FontFamily"/> property.
    /// </summary>
    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        TextBlock.FontFamilyProperty.AddOwner<MermaidPresenter>();

    /// <summary>
    /// Font family for diagram text. Default is system default font.
    /// </summary>
    public FontFamily FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="FontSize"/> property.
    /// </summary>
    public static readonly StyledProperty<double> FontSizeProperty =
        TextBlock.FontSizeProperty.AddOwner<MermaidPresenter>();

    /// <summary>
    /// Font size for diagram text. Default is 12.0.
    /// </summary>
    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="BackgroundBrush"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> BackgroundBrushProperty =
        AvaloniaProperty.Register<MermaidPresenter, IBrush?>(nameof(BackgroundBrush), Brushes.Transparent);

    /// <summary>
    /// Background brush for the entire diagram. Default is Transparent.
    /// </summary>
    public IBrush? BackgroundBrush
    {
        get => GetValue(BackgroundBrushProperty);
        set => SetValue(BackgroundBrushProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="GroupFill"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> GroupFillProperty =
        AvaloniaProperty.Register<MermaidPresenter, IBrush?>(nameof(GroupFill), new SolidColorBrush(new Color(0xFF, 0xF2, 0xF2, 0xF2)));

    /// <summary>
    /// Fill brush for groups/clusters. Default is a light gray color (#FFF2F2F2).
    /// </summary>
    public IBrush? GroupFill
    {
        get => GetValue(GroupFillProperty);
        set => SetValue(GroupFillProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="GroupHeaderFill"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> GroupHeaderFillProperty =
        AvaloniaProperty.Register<MermaidPresenter, IBrush?>(nameof(GroupHeaderFill), new SolidColorBrush(new Color(0xFF, 0xE2, 0xE2, 0xE2)));

    /// <summary>
    /// Fill brush for group headers. Default is a slightly darker gray color (#FFE2E2E2).
    /// </summary>
    public IBrush? GroupHeaderFill
    {
        get => GetValue(GroupHeaderFillProperty);
        set => SetValue(GroupHeaderFillProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="NodeFill"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> NodeFillProperty =
        AvaloniaProperty.Register<MermaidPresenter, IBrush?>(nameof(NodeFill), new SolidColorBrush(new Color(0xFF, 0xEC, 0xEC, 0xFF)));

    /// <summary>
    /// Fill brush for nodes. Default is a light blue color (#FFECECFF).
    /// </summary>
    public IBrush? NodeFill
    {
        get => GetValue(NodeFillProperty);
        set => SetValue(NodeFillProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="AccentFill"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> AccentFillProperty =
        AvaloniaProperty.Register<MermaidPresenter, IBrush?>(nameof(AccentFill), new SolidColorBrush(new Color(0xFF, 0xFF, 0xF5, 0xAD)));

    /// <summary>
    /// Fill brush for accents (e.g. highlighted nodes). Default is a light orange color (#FFFFF5AD).
    /// </summary>
    public IBrush? AccentFill
    {
        get => GetValue(AccentFillProperty);
        set => SetValue(AccentFillProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="Foreground"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextBlock.ForegroundProperty.AddOwner<MermaidPresenter>();

    /// <summary>
    /// Foreground brush for diagram text. Default is system default foreground color (usually black).
    /// </summary>
    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="SecondaryForeground"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> SecondaryForegroundProperty =
        AvaloniaProperty.Register<MermaidPresenter, IBrush?>(nameof(SecondaryForeground), new SolidColorBrush(new Color(0xFF, 0x33, 0x33, 0x33)));

    /// <summary>
    /// Foreground brush for secondary text (e.g. edge labels). Default is a dark gray color (#FF333333).
    /// </summary>
    public IBrush? SecondaryForeground
    {
        get => GetValue(SecondaryForegroundProperty);
        set => SetValue(SecondaryForegroundProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="AccentForeground"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> AccentForegroundProperty =
        AvaloniaProperty.Register<MermaidPresenter, IBrush?>(nameof(AccentForeground), Brushes.Black);

    /// <summary>
    /// Foreground brush for accents (e.g. highlighted nodes). Default is black.
    /// </summary>
    public IBrush? AccentForeground
    {
        get => GetValue(AccentForegroundProperty);
        set => SetValue(AccentForegroundProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="ArrowFill"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> ArrowFillProperty =
        AvaloniaProperty.Register<MermaidPresenter, IBrush?>(nameof(ArrowFill), Brushes.White);

    /// <summary>
    /// Fill brush for arrows. Default is white.
    /// </summary>
    public IBrush? ArrowFill
    {
        get => GetValue(ArrowFillProperty);
        set => SetValue(ArrowFillProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="EdgeLabelBackground"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> EdgeLabelBackgroundProperty =
        AvaloniaProperty.Register<MermaidPresenter, IBrush?>(nameof(EdgeLabelBackground), Brushes.White);

    /// <summary>
    /// Background brush for edge labels. Default is white.
    /// </summary>
    public IBrush? EdgeLabelBackground
    {
        get => GetValue(EdgeLabelBackgroundProperty);
        set => SetValue(EdgeLabelBackgroundProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="GroupStroke"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> GroupStrokeProperty =
        AvaloniaProperty.Register<MermaidPresenter, IBrush?>(nameof(GroupStroke), new SolidColorBrush(new Color(0xFF, 0xCC, 0xCC, 0xCC)));

    /// <summary>
    /// Stroke brush for groups/clusters. Default is a medium gray color (#FFCCCCCC).
    /// </summary>
    public IBrush? GroupStroke
    {
        get => GetValue(GroupStrokeProperty);
        set => SetValue(GroupStrokeProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="GroupStrokeThickness"/> property.
    /// </summary>
    public static readonly StyledProperty<double> GroupStrokeThicknessProperty =
        AvaloniaProperty.Register<MermaidPresenter, double>(nameof(GroupStrokeThickness), 2);

    /// <summary>
    /// Stroke thickness for groups/clusters. Default is 2.
    /// </summary>
    public double GroupStrokeThickness
    {
        get => GetValue(GroupStrokeThicknessProperty);
        set => SetValue(GroupStrokeThicknessProperty, value);
    }

    /// <summary>
    /// Cached GroupPen based on the current GroupStroke and GroupStrokeThickness properties. This is used to optimize pen creation during rendering.
    /// </summary>
    internal IPen? GroupPen => GetCachedPen(ref _groupPen, GroupStroke, GroupStrokeThickness);

    /// <summary>
    /// Defines the <see cref="NodeStroke"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> NodeStrokeProperty =
        AvaloniaProperty.Register<MermaidPresenter, IBrush?>(nameof(NodeStroke), new SolidColorBrush(new Color(0xFF, 0x93, 0x70, 0xDB)));

    /// <summary>
    /// Stroke brush for nodes. Default is a purple color (#FF9370DB).
    /// </summary>
    public IBrush? NodeStroke
    {
        get => GetValue(NodeStrokeProperty);
        set => SetValue(NodeStrokeProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="NodeStrokeThickness"/> property.
    /// </summary>
    public static readonly StyledProperty<double> NodeStrokeThicknessProperty =
        AvaloniaProperty.Register<MermaidPresenter, double>(nameof(NodeStrokeThickness), 1);

    /// <summary>
    /// Stroke thickness for nodes. Default is 1.
    /// </summary>
    public double NodeStrokeThickness
    {
        get => GetValue(NodeStrokeThicknessProperty);
        set => SetValue(NodeStrokeThicknessProperty, value);
    }

    /// <summary>
    /// Cached NodePen based on the current NodeStroke and NodeStrokeThickness properties. This is used to optimize pen creation during rendering.
    /// </summary>
    internal IPen? NodePen => GetCachedPen(ref _nodePen, NodeStroke, NodeStrokeThickness);

    /// <summary>
    /// Defines the <see cref="AccentStroke"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> AccentStrokeProperty =
        AvaloniaProperty.Register<MermaidPresenter, IBrush?>(nameof(AccentStroke), new SolidColorBrush(new Color(0xFF, 0xCC, 0xCC, 0x00)));

    /// <summary>
    /// Stroke brush for accents (e.g. highlighted nodes). Default is a transparent yellow color (#FFCCCC00).
    /// </summary>
    public IBrush? AccentStroke
    {
        get => GetValue(AccentStrokeProperty);
        set => SetValue(AccentStrokeProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="AccentStrokeThickness"/> property.
    /// </summary>
    public static readonly StyledProperty<double> AccentStrokeThicknessProperty =
        AvaloniaProperty.Register<MermaidPresenter, double>(nameof(AccentStrokeThickness), 1);

    /// <summary>
    /// Stroke thickness for accents (e.g. highlighted nodes). Default is 1.
    /// </summary>
    public double AccentStrokeThickness
    {
        get => GetValue(AccentStrokeThicknessProperty);
        set => SetValue(AccentStrokeThicknessProperty, value);
    }

    /// <summary>
    /// Cached AccentPen based on the current AccentStroke and AccentStrokeThickness properties. This is used to optimize pen creation during rendering.
    /// </summary>
    internal IPen? AccentPen => GetCachedPen(ref _accentPen, AccentStroke, AccentStrokeThickness);

    /// <summary>
    /// Defines the <see cref="LineStroke"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> LineStrokeProperty =
        AvaloniaProperty.Register<MermaidPresenter, IBrush?>(nameof(LineStroke), Brushes.White);

    /// <summary>
    /// Stroke brush for lines (e.g. edges). Default is white.
    /// </summary>
    public IBrush? LineStroke
    {
        get => GetValue(LineStrokeProperty);
        set => SetValue(LineStrokeProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="LineStrokeThickness"/> property.
    /// </summary>
    public static readonly StyledProperty<double> LineStrokeThicknessProperty =
        AvaloniaProperty.Register<MermaidPresenter, double>(nameof(LineStrokeThickness), 1.5);

    /// <summary>
    /// Stroke thickness for lines (e.g. edges). Default is 1.5.
    /// </summary>
    public double LineStrokeThickness
    {
        get => GetValue(LineStrokeThicknessProperty);
        set => SetValue(LineStrokeThicknessProperty, value);
    }

    /// <summary>
    /// Cached LinePen based on the current LineStroke and LineStrokeThickness properties. This is used to optimize pen creation during rendering.
    /// </summary>
    internal IPen? LinePen => GetCachedPen(ref _linePen, LineStroke, LineStrokeThickness);

    /// <summary>
    /// Defines the <see cref="ThickLineStroke"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> ThickLineStrokeProperty =
        AvaloniaProperty.Register<MermaidPresenter, IBrush?>(nameof(ThickLineStroke), Brushes.White);

    /// <summary>
    /// Stroke brush for thick lines (e.g. bold edges). Default is white.
    /// </summary>
    public IBrush? ThickLineStroke
    {
        get => GetValue(ThickLineStrokeProperty);
        set => SetValue(ThickLineStrokeProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="ThickLineStrokeThickness"/> property.
    /// </summary>
    public static readonly StyledProperty<double> ThickLineStrokeThicknessProperty =
        AvaloniaProperty.Register<MermaidPresenter, double>(nameof(ThickLineStrokeThickness), 2.5);

    /// <summary>
    /// Stroke thickness for thick lines (e.g. bold edges). Default is 2.5.
    /// </summary>
    public double ThickLineStrokeThickness
    {
        get => GetValue(ThickLineStrokeThicknessProperty);
        set => SetValue(ThickLineStrokeThicknessProperty, value);
    }

    /// <summary>
    /// Cached ThickLinePen based on the current ThickLineStroke and ThickLineStrokeThickness properties. This is used to optimize pen creation during rendering.
    /// </summary>
    internal IPen? ThickLinePen => GetCachedPen(ref _thickLinePen, ThickLineStroke, ThickLineStrokeThickness);

    /// <summary>
    /// Defines the <see cref="DottedLineStroke"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> DottedLineStrokeProperty =
        AvaloniaProperty.Register<MermaidPresenter, IBrush?>(nameof(DottedLineStroke), Brushes.White);

    /// <summary>
    /// Stroke brush for dotted lines (e.g. dotted edges). Default is white.
    /// </summary>
    public IBrush? DottedLineStroke
    {
        get => GetValue(DottedLineStrokeProperty);
        set => SetValue(DottedLineStrokeProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="DottedLineStrokeThickness"/> property.
    /// </summary>
    public static readonly StyledProperty<double> DottedLineStrokeThicknessProperty =
        AvaloniaProperty.Register<MermaidPresenter, double>(nameof(DottedLineStrokeThickness), 1.5);

    /// <summary>
    /// Stroke thickness for dotted lines (e.g. dotted edges). Default is 1.5.
    /// </summary>
    public double DottedLineStrokeThickness
    {
        get => GetValue(DottedLineStrokeThicknessProperty);
        set => SetValue(DottedLineStrokeThicknessProperty, value);
    }

    /// <summary>
    /// Cached DottedLinePen based on the current DottedLineStroke and DottedLineStrokeThickness properties. This is used to optimize pen creation during rendering.
    /// </summary>
    internal IPen? DottedLinePen => GetCachedPen(ref _dottedLinePen, DottedLineStroke, DottedLineStrokeThickness);

    /// <summary>
    /// Defines the <see cref="EdgeLabelStroke"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> EdgeLabelStrokeProperty =
        AvaloniaProperty.Register<MermaidPresenter, IBrush?>(nameof(EdgeLabelStroke), new SolidColorBrush(new Color(0xFF, 0xDD, 0xDD, 0xDD)));

    /// <summary>
    /// Stroke brush for edge label borders. Default is a light gray color (#FFDDDDDD).
    /// </summary>
    public IBrush? EdgeLabelStroke
    {
        get => GetValue(EdgeLabelStrokeProperty);
        set => SetValue(EdgeLabelStrokeProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="EdgeLabelStrokeThickness"/> property.
    /// </summary>
    public static readonly StyledProperty<double> EdgeLabelStrokeThicknessProperty =
        AvaloniaProperty.Register<MermaidPresenter, double>(nameof(EdgeLabelStrokeThickness), 1);

    /// <summary>
    /// Stroke thickness for edge label borders. Default is 1.
    /// </summary>
    public double EdgeLabelStrokeThickness
    {
        get => GetValue(EdgeLabelStrokeThicknessProperty);
        set => SetValue(EdgeLabelStrokeThicknessProperty, value);
    }

    /// <summary>
    /// Cached EdgeLabelPen based on the current EdgeLabelStroke and EdgeLabelStrokeThickness properties. This is used to optimize pen creation during rendering.
    /// </summary>
    internal IPen? EdgeLabelPen => GetCachedPen(ref _edgeLabelPen, EdgeLabelStroke, EdgeLabelStrokeThickness);

    private readonly Dictionary<string, IBrush> _brushCache = new();
    private readonly Dictionary<string, IPen> _penCache = new();

    private IPen? _groupPen;
    private IPen? _nodePen;
    private IPen? _accentPen;
    private IPen? _linePen;
    private IPen? _thickLinePen;
    private IPen? _dottedLinePen;
    private IPen? _edgeLabelPen;

    private object? _pendingDiagram;

    static MermaidPresenter()
    {
        AffectsMeasure<MermaidPresenter>(TextProperty);
    }

    public IBrush? GetCachedBrush(string? colorHex, IBrush? fallback)
    {
        if (string.IsNullOrWhiteSpace(colorHex) || colorHex == "none") return fallback;
        if (_brushCache.TryGetValue(colorHex, out var brush)) return brush;

        try
        {
            var parsed = new SolidColorBrush(Color.Parse(colorHex));
            _brushCache[colorHex] = parsed;
            return parsed;
        }
        catch
        {
            return fallback;
        }
    }

    public IPen? GetCachedPen(string? colorHex, string? widthString, IPen? fallback)
    {
        if (string.IsNullOrWhiteSpace(colorHex) && string.IsNullOrWhiteSpace(widthString)) return fallback;
        var key = $"{colorHex}_{widthString}";
        if (_penCache.TryGetValue(key, out var existingPen)) return existingPen;

        var brush = GetCachedBrush(colorHex, fallback?.Brush);
        var width = fallback?.Thickness ?? 0d;
        if (double.TryParse(widthString, out var w)) width = w;
        if (brush is null || width <= 0) return fallback;

        var pen = new Pen(brush, width);
        _penCache[key] = pen;
        return pen;
    }

    private static IPen? GetCachedPen(ref IPen? cachedPen, IBrush? brush, double thickness)
    {
        if (cachedPen is not null) return cachedPen;
        if (brush is null || thickness <= 0) return null;
        cachedPen = new Pen(brush, thickness);
        return cachedPen;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        switch (change.Property.Name)
        {
            case nameof(GroupStroke):
            case nameof(GroupStrokeThickness):
                _groupPen = null;
                break;
            case nameof(NodeStroke):
            case nameof(NodeStrokeThickness):
                _nodePen = null;
                break;
            case nameof(AccentStroke):
            case nameof(AccentStrokeThickness):
                _accentPen = null;
                break;
            case nameof(LineStroke):
            case nameof(LineStrokeThickness):
                _linePen = null;
                break;
            case nameof(ThickLineStroke):
            case nameof(ThickLineStrokeThickness):
                _thickLinePen = null;
                break;
            case nameof(DottedLineStroke):
            case nameof(DottedLineStrokeThickness):
                _dottedLinePen = null;
                break;
            case nameof(EdgeLabelStroke):
            case nameof(EdgeLabelStrokeThickness):
                _edgeLabelPen = null;
                break;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Text is not { Length: > 0 } text) return default;

        var lines = MermaidRenderer.PreprocessLines(text);
        if (lines.Length == 0) return default;

        var size = default(Size);
        var diagramType = DiagramDetector.Detect(text.AsSpan());
        switch (diagramType)
        {
            case DiagramType.Sequence:
            {
                var positionedSequenceDiagram = SequenceLayout.Layout(SequenceParser.Parse(lines));
                size = new Size(positionedSequenceDiagram.Width, positionedSequenceDiagram.Height);
                _pendingDiagram = positionedSequenceDiagram;
                break;
            }
            case DiagramType.Class:
            {
                var positionedClassDiagram = MermaidRenderer.LayoutProvider.LayoutClass(ClassParser.Parse(lines));
                size = new Size(positionedClassDiagram.Width, positionedClassDiagram.Height);
                _pendingDiagram = positionedClassDiagram;
                break;
            }
            case DiagramType.Er:
            {
                var positionedErDiagram = MermaidRenderer.LayoutProvider.LayoutEr(ErParser.Parse(lines));
                size = new Size(positionedErDiagram.Width, positionedErDiagram.Height);
                _pendingDiagram = positionedErDiagram;
                break;
            }
            case DiagramType.Pie:
            {
                var pieChart = PieParser.Parse(lines); // TODO: size
                _pendingDiagram = pieChart;
                break;
            }
            case DiagramType.Quadrant:
            {
                var quadrantChart = QuadrantParser.Parse(lines); // TODO: size
                _pendingDiagram = quadrantChart;
                break;
            }
            case DiagramType.Timeline:
            {
                var timelineDiagram = TimelineParser.Parse(lines); // TODO: size
                _pendingDiagram = timelineDiagram;
                break;
            }
            case DiagramType.GitGraph:
            {
                var gitGraphDiagram = GitGraphParser.Parse(lines); // TODO: size
                _pendingDiagram = gitGraphDiagram;
                break;
            }
            case DiagramType.Radar:
            {
                var radarDiagram = RadarParser.Parse(lines); // TODO: size
                _pendingDiagram = radarDiagram;
                break;
            }
            case DiagramType.Treemap:
            {
                var treemapDiagram = TreemapParser.Parse(MermaidRenderer.PreprocessLinesPreserveIndent(text)); // TODO: size
                _pendingDiagram = treemapDiagram;
                break;
            }
            case DiagramType.Venn:
            {
                var vennDiagram = VennParser.Parse(MermaidRenderer.PreprocessLinesPreserveIndent(text)); // TODO: size
                _pendingDiagram = vennDiagram;
                break;
            }
            case DiagramType.Flowchart:
            {
                var positionedGraph = MermaidRenderer.LayoutProvider.LayoutFlowchart(FlowchartParser.Parse(lines));
                size = new Size(positionedGraph.Width, positionedGraph.Height);
                _pendingDiagram = positionedGraph;
                break;
            }
            case DiagramType.State:
            {
                var positionedGraph = MermaidRenderer.LayoutProvider.LayoutFlowchart(StateParser.Parse(lines));
                size = new Size(positionedGraph.Width, positionedGraph.Height);
                _pendingDiagram = positionedGraph;
                break;
            }
        }

        return size;
    }

    public override void Render(DrawingContext dc)
    {
        switch (_pendingDiagram)
        {
            case PositionedSequenceDiagram positionedSequenceDiagram:
            {
                SequenceRenderer.Render(dc, this, positionedSequenceDiagram);
                break;
            }
            case PositionedClassDiagram positionedClassDiagram:
            {
                ClassRenderer.Render(dc, this, positionedClassDiagram);
                break;
            }
            case PositionedErDiagram positionedErDiagram:
            {
                ErRenderer.Render(dc, this, positionedErDiagram);
                break;
            }
            case PieChart pieChart:
            {
                PieRenderer.Render(dc, this, pieChart);
                break;
            }
            case QuadrantChart quadrantChart:
            {
                QuadrantRenderer.Render(dc, this, quadrantChart);
                break;
            }
            case TimelineDiagram timelineDiagram:
            {
                TimelineRenderer.Render(dc, this, timelineDiagram);
                break;
            }
            case GitGraph gitGraph:
            {
                GitGraphRenderer.Render(dc, this, gitGraph);
                break;
            }
            case RadarChart radarChart:
            {
                RadarRenderer.Render(dc, this, radarChart);
                break;
            }
            case TreemapDiagram treemapDiagram:
            {
                TreemapRenderer.Render(dc, this, treemapDiagram);
                break;
            }
            case VennDiagram vennDiagram:
            {
                VennRenderer.Render(dc, this, vennDiagram);
                break;
            }
            case PositionedGraph positionedGraph:
            {
                DefaultRenderer.Render(dc, this, positionedGraph);
                break;
            }
        }
    }
}