using Avalonia;
using Avalonia.Media;
using Mermaider.Models;
using Point = Avalonia.Point;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Native Avalonia renderer entry point for Mermaid class diagrams.
/// </summary>
/// <remarks>
/// This type is intentionally shaped like the flowchart renderer: Mermaider owns parsing and layout,
/// while this renderer converts the positioned model to <see cref="DrawingContext"/> calls.
/// </remarks>
public class ClassRenderer : MermaidRenderer
{
    /// <summary>
    /// Defines the <see cref="RelationshipCornerRadius"/> property.
    /// </summary>
    public static readonly StyledProperty<double> RelationshipCornerRadiusProperty =
        AvaloniaProperty.Register<ClassRenderer, double>(nameof(RelationshipCornerRadius), 6);

    /// <summary>
    /// Radius used when rounding class relationship connector corners.
    /// </summary>
    public double RelationshipCornerRadius
    {
        get => GetValue(RelationshipCornerRadiusProperty);
        set => SetValue(RelationshipCornerRadiusProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="MarkerSize"/> property.
    /// </summary>
    public static readonly StyledProperty<double> MarkerSizeProperty =
        AvaloniaProperty.Register<ClassRenderer, double>(nameof(MarkerSize), 12);

    /// <summary>
    /// Size of relationship markers such as inheritance triangles and composition diamonds.
    /// </summary>
    public double MarkerSize
    {
        get => GetValue(MarkerSizeProperty);
        set => SetValue(MarkerSizeProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="MemberRowHeight"/> property.
    /// </summary>
    public static readonly StyledProperty<double> MemberRowHeightProperty =
        AvaloniaProperty.Register<ClassRenderer, double>(nameof(MemberRowHeight), 20);

    /// <summary>
    /// Vertical spacing allocated to each attribute or method row.
    /// </summary>
    public double MemberRowHeight
    {
        get => GetValue(MemberRowHeightProperty);
        set => SetValue(MemberRowHeightProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="MemberPaddingX"/> property.
    /// </summary>
    public static readonly StyledProperty<double> MemberPaddingXProperty =
        AvaloniaProperty.Register<ClassRenderer, double>(nameof(MemberPaddingX), 8);

    /// <summary>
    /// Left padding used when drawing class attributes and methods.
    /// </summary>
    public double MemberPaddingX
    {
        get => GetValue(MemberPaddingXProperty);
        set => SetValue(MemberPaddingXProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="MemberFontFamily"/> property.
    /// </summary>
    public static readonly StyledProperty<FontFamily> MemberFontFamilyProperty =
        AvaloniaProperty.Register<ClassRenderer, FontFamily>(
            nameof(MemberFontFamily),
            FontFamily.Parse("Cascadia Mono, Consolas, Menlo, monospace"));

    /// <summary>
    /// Font family used for class attributes and methods.
    /// </summary>
    public FontFamily MemberFontFamily
    {
        get => GetValue(MemberFontFamilyProperty);
        set => SetValue(MemberFontFamilyProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="BoxCornerRadius"/> property.
    /// </summary>
    public static readonly StyledProperty<double> BoxCornerRadiusProperty =
        AvaloniaProperty.Register<ClassRenderer, double>(nameof(BoxCornerRadius), 6);

    /// <summary>
    /// Corner radius for class boxes and their clipped header area.
    /// </summary>
    public double BoxCornerRadius
    {
        get => GetValue(BoxCornerRadiusProperty);
        set => SetValue(BoxCornerRadiusProperty, value);
    }

    private readonly record struct Style(
        double RelationshipCornerRadius,
        double MarkerSize,
        double MemberRowHeight,
        double MemberPaddingX,
        FontFamily MemberFontFamily,
        double BoxCornerRadius
    );

    /// <summary>
    /// Draws a positioned class diagram using this renderer part's current styled values.
    /// </summary>
    internal void RenderDiagram(DrawingContext dc, MermaidPresenter presenter, PositionedClassDiagram diagram)
    {
        var style = CreateStyleSnapshot();
        foreach (var relationship in diagram.Relationships)
        {
            DrawRelationship(dc, presenter, style, relationship);
        }

        foreach (var cls in diagram.Classes)
        {
            DrawClassBox(dc, presenter, style, cls);
        }

        foreach (var relationship in diagram.Relationships)
        {
            DrawRelationshipLabels(dc, presenter, relationship);
        }

        foreach (var note in diagram.Notes)
        {
            DrawNote(dc, presenter, note);
        }
    }

    private Style CreateStyleSnapshot() =>
        new(
            RelationshipCornerRadius,
            MarkerSize,
            MemberRowHeight,
            MemberPaddingX,
            MemberFontFamily,
            BoxCornerRadius);

    private static void DrawClassBox(DrawingContext dc, MermaidPresenter presenter, Style style, PositionedClassNode cls)
    {
        var rect = new Rect(cls.X, cls.Y, cls.Width, cls.Height);
        using (dc.PushClip(new RoundedRect(rect, style.BoxCornerRadius, style.BoxCornerRadius)))
        {
            var headerRect = new Rect(cls.X, cls.Y, cls.Width, cls.HeaderHeight);
            dc.DrawRectangle(presenter.GroupHeaderFill, presenter.NodePen, headerRect);
        }
        dc.DrawRectangle(presenter.NodeFill, presenter.NodePen, rect, style.BoxCornerRadius, style.BoxCornerRadius);

        var nameY = cls.Y + cls.HeaderHeight / 2;
        if (cls.Annotation is { Length: > 0 } annotation)
        {
            var annotationText = MermaidTextRenderer.CreateFormattedText(
                presenter,
                $"<<{annotation}>>",
                presenter.AnnotationFontSize,
                presenter.SecondaryForeground,
                TextAlignment.Center,
                FontWeight.SemiBold);
            annotationText.SetFontStyle(FontStyle.Italic);
            MermaidTextRenderer.DrawFormattedText(
                dc,
                annotationText,
                cls.X + cls.Width / 2,
                cls.Y + 12,
                TextAlignment.Center,
                centerVertically: true);
            nameY += 6;
        }

        MermaidTextRenderer.DrawInlineText(
            dc,
            presenter,
            cls.Label,
            isMarkdown: false,
            cls.X + cls.Width / 2,
            nameY,
            presenter.NodeLabelFontSize,
            presenter.Foreground,
            TextAlignment.Center,
            centerVertically: true,
            FontWeight.Bold);

        var attrTop = cls.Y + cls.HeaderHeight;
        DrawSeparator(dc, presenter, cls.X, attrTop, cls.X + cls.Width);

        for (var i = 0; i < cls.Attributes.Count; i++)
        {
            var y = attrTop + 4 + i * style.MemberRowHeight + style.MemberRowHeight / 2;
            DrawMember(dc, presenter, style, cls.Attributes[i], cls.X + style.MemberPaddingX, y);
        }

        var methodTop = attrTop + cls.AttrHeight;
        DrawSeparator(dc, presenter, cls.X, methodTop, cls.X + cls.Width);

        for (var i = 0; i < cls.Methods.Count; i++)
        {
            var y = methodTop + 4 + i * style.MemberRowHeight + style.MemberRowHeight / 2;
            DrawMember(dc, presenter, style, cls.Methods[i], cls.X + style.MemberPaddingX, y);
        }
    }

    private static void DrawMember(DrawingContext dc, MermaidPresenter presenter, Style style, ClassMember member, double x, double y)
    {
        var text = BuildMemberText(member);
        var formatted = MermaidTextRenderer.CreateFormattedText(
            presenter,
            text,
            presenter.MemberFontSize,
            presenter.SecondaryForeground,
            TextAlignment.Left,
            FontWeight.Medium);

        formatted.SetFontFamily(style.MemberFontFamily);
        if (member.IsAbstract)
        {
            formatted.SetFontStyle(FontStyle.Italic);
        }

        if (member.IsStatic)
        {
            formatted.SetTextDecorations(TextDecorations.Underline);
        }

        MermaidTextRenderer.DrawFormattedText(dc, formatted, x, y, TextAlignment.Left, centerVertically: true);
    }

    private static string BuildMemberText(ClassMember member)
    {
        var visibility = member.Visibility switch
        {
            ClassVisibility.Public => "+ ",
            ClassVisibility.Private => "- ",
            ClassVisibility.Protected => "# ",
            ClassVisibility.Package => "~ ",
            _ => string.Empty
        };
        var name = member.IsMethod ? $"{member.Name}({member.Params ?? string.Empty})" : member.Name;
        return member.Type is { Length: > 0 } ? $"{visibility}{name}: {member.Type}" : $"{visibility}{name}";
    }

    private static void DrawRelationship(DrawingContext dc, MermaidPresenter presenter, Style style, PositionedClassRelationship relationship)
    {
        if (relationship.Points.Count < 2)
        {
            return;
        }

        var pen = relationship.Type is ClassRelationType.Dependency or ClassRelationType.Realization ? presenter.DottedLinePen : presenter.LinePen;
        MermaidDrawingHelpers.DrawRoundedPath(dc, relationship.Points, pen, style.RelationshipCornerRadius);

        DrawRelationshipMarker(dc, presenter, style, relationship);
    }

    private static void DrawRelationshipMarker(DrawingContext dc, MermaidPresenter presenter, Style style, PositionedClassRelationship relationship)
    {
        if (relationship.Points.Count < 2)
        {
            return;
        }

        var markerAtFrom = relationship.MarkerAt == ClassMarkerAt.From;
        var endpoint = markerAtFrom ? relationship.Points[0] : relationship.Points[^1];
        var adjacent = markerAtFrom ? relationship.Points[1] : relationship.Points[^2];
        var from = new Point(adjacent.X, adjacent.Y);
        var to = new Point(endpoint.X, endpoint.Y);
        var background = presenter.BackgroundBrush ?? presenter.EdgeLabelBackground;

        switch (relationship.Type)
        {
            case ClassRelationType.Inheritance:
            case ClassRelationType.Realization:
                DrawTriangleMarker(dc, background, presenter.LinePen, from, to, style.MarkerSize);
                break;
            case ClassRelationType.Composition:
                MermaidDrawingHelpers.DrawDiamondMarker(dc, presenter.ArrowFill, presenter.LinePen, from, to, style.MarkerSize);
                break;
            case ClassRelationType.Aggregation:
                MermaidDrawingHelpers.DrawDiamondMarker(dc, background, presenter.LinePen, from, to, style.MarkerSize);
                break;
            case ClassRelationType.Association:
            case ClassRelationType.Dependency:
                MermaidDrawingHelpers.DrawArrowHead(dc, null, presenter.LinePen, from, to, style.MarkerSize, filled: false);
                break;
            case ClassRelationType.Lollipop:
                MermaidDrawingHelpers.DrawCircleMarker(dc, background, presenter.LinePen, to, style.MarkerSize / 2);
                break;
        }
    }

    private static void DrawTriangleMarker(DrawingContext dc, IBrush? fill, IPen? stroke, Point from, Point to, double size)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length <= 0.001)
        {
            return;
        }

        var ux = dx / length;
        var uy = dy / length;
        var px = -uy;
        var py = ux;
        var backX = to.X - ux * size;
        var backY = to.Y - uy * size;
        var half = size / 2;

        MermaidDrawingHelpers.DrawPolygon(
            dc,
            fill,
            stroke,
            to,
            new Point(backX + px * half, backY + py * half),
            new Point(backX - px * half, backY - py * half));
    }

    private static void DrawRelationshipLabels(DrawingContext dc, MermaidPresenter presenter, PositionedClassRelationship relationship)
    {
        if (relationship.Points.Count < 2)
        {
            return;
        }

        if (relationship.Label is { Length: > 0 } label)
        {
            var pos = relationship.LabelPosition ?? MermaidDrawingHelpers.Midpoint(relationship.Points);
            MermaidTextRenderer.DrawInlineText(
                dc,
                presenter,
                label,
                isMarkdown: false,
                pos.X,
                pos.Y - 8,
                presenter.EdgeLabelFontSize,
                presenter.SecondaryForeground,
                TextAlignment.Center,
                centerVertically: true);
        }

        if (relationship.FromCardinality is { Length: > 0 } fromCardinality)
        {
            var p = relationship.Points[0];
            var next = relationship.Points[1];
            var offset = CardinalityOffset(p, next);
            DrawCardinality(dc, presenter, fromCardinality, p.X + offset.X, p.Y + offset.Y);
        }

        if (relationship.ToCardinality is { Length: > 0 } toCardinality)
        {
            var p = relationship.Points[^1];
            var prev = relationship.Points[^2];
            var offset = CardinalityOffset(p, prev);
            DrawCardinality(dc, presenter, toCardinality, p.X + offset.X, p.Y + offset.Y);
        }
    }

    private static void DrawCardinality(DrawingContext dc, MermaidPresenter presenter, string text, double x, double y)
    {
        MermaidTextRenderer.DrawInlineText(
            dc,
            presenter,
            text,
            isMarkdown: false,
            x,
            y,
            presenter.EdgeLabelFontSize,
            presenter.SecondaryForeground,
            TextAlignment.Center,
            centerVertically: true);
    }

    private static (double X, double Y) CardinalityOffset(Mermaider.Models.Point from, Mermaider.Models.Point to)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        return Math.Abs(dx) > Math.Abs(dy) ? (dx > 0 ? 14 : -14, -10) : (-14, dy > 0 ? 14 : -14);
    }

    private static void DrawNote(DrawingContext dc, MermaidPresenter presenter, PositionedGraphNote note)
    {
        dc.DrawRectangle(
            presenter.AccentFill,
            presenter.AccentPen,
            new Rect(note.X, note.Y, note.Width, note.Height),
            6,
            6);
        MermaidTextRenderer.DrawInlineText(
            dc,
            presenter,
            note.Text,
            isMarkdown: false,
            note.X + note.Width / 2,
            note.Y + note.Height / 2,
            presenter.EdgeLabelFontSize,
            presenter.AccentForeground,
            TextAlignment.Center,
            centerVertically: true);
    }

    private static void DrawSeparator(DrawingContext dc, MermaidPresenter presenter, double x1, double y, double x2)
    {
        if (presenter.NodePen is { } pen)
        {
            dc.DrawLine(pen, new Point(x1, y), new Point(x2, y));
        }
    }
}