using Avalonia;
using Avalonia.Media;
using Mermaider.Models;
using Mermaider.Rendering;
using Point = Avalonia.Point;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Native Avalonia renderer entry point for Mermaid class diagrams.
/// </summary>
/// <remarks>
/// This type is intentionally shaped like the flowchart renderer: Mermaider owns parsing and layout,
/// while this renderer converts the positioned model to <see cref="DrawingContext"/> calls.
/// </remarks>
public static class ClassRenderer
{
    private const double RelationshipCornerRadius = 6;
    private const double MarkerSize = 12;
    private const double MemberRowHeight = 20;
    private const double MemberPaddingX = 8;

    private static readonly FontFamily MemberFontFamily = FontFamily.Parse("Cascadia Mono, Consolas, Menlo, monospace");

    /// <summary>
    /// Draws a positioned class diagram.
    /// </summary>
    public static void Render(DrawingContext dc, MermaidPresenter presenter, PositionedClassDiagram diagram)
    {
        foreach (var relationship in diagram.Relationships)
        {
            DrawRelationship(dc, presenter, relationship);
        }

        foreach (var cls in diagram.Classes)
        {
            DrawClassBox(dc, presenter, cls);
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

    private static void DrawClassBox(DrawingContext dc, MermaidPresenter presenter, PositionedClassNode cls)
    {
        var rect = new Rect(cls.X, cls.Y, cls.Width, cls.Height);
        using (dc.PushClip(new RoundedRect(rect, RenderConstants.Radii.Rectangle, RenderConstants.Radii.Rectangle)))
        {
            var headerRect = new Rect(cls.X, cls.Y, cls.Width, cls.HeaderHeight);
            dc.DrawRectangle(presenter.GroupHeaderFill, presenter.NodePen, headerRect);
        }
        dc.DrawRectangle(presenter.NodeFill, presenter.NodePen, rect, RenderConstants.Radii.Rectangle, RenderConstants.Radii.Rectangle);

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
            var y = attrTop + 4 + i * MemberRowHeight + MemberRowHeight / 2;
            DrawMember(dc, presenter, cls.Attributes[i], cls.X + MemberPaddingX, y);
        }

        var methodTop = attrTop + cls.AttrHeight;
        DrawSeparator(dc, presenter, cls.X, methodTop, cls.X + cls.Width);

        for (var i = 0; i < cls.Methods.Count; i++)
        {
            var y = methodTop + 4 + i * MemberRowHeight + MemberRowHeight / 2;
            DrawMember(dc, presenter, cls.Methods[i], cls.X + MemberPaddingX, y);
        }
    }

    private static void DrawMember(DrawingContext dc, MermaidPresenter presenter, ClassMember member, double x, double y)
    {
        var text = BuildMemberText(member);
        var formatted = MermaidTextRenderer.CreateFormattedText(
            presenter,
            text,
            presenter.MemberFontSize,
            presenter.SecondaryForeground,
            TextAlignment.Left,
            FontWeight.Medium);

        formatted.SetFontFamily(MemberFontFamily);
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
        return member.Type is { Length: > 0 }
            ? $"{visibility}{name}: {member.Type}"
            : $"{visibility}{name}";
    }

    private static void DrawRelationship(DrawingContext dc, MermaidPresenter presenter, PositionedClassRelationship relationship)
    {
        if (relationship.Points.Count < 2)
        {
            return;
        }

        var pen = relationship.Type is ClassRelationType.Dependency or ClassRelationType.Realization
            ? presenter.DottedLinePen
            : presenter.LinePen;
        MermaidDrawingHelpers.DrawRoundedPath(dc, relationship.Points, pen, RelationshipCornerRadius);

        DrawRelationshipMarker(dc, presenter, relationship);
    }

    private static void DrawRelationshipMarker(DrawingContext dc, MermaidPresenter presenter, PositionedClassRelationship relationship)
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
                DrawTriangleMarker(dc, background, presenter.LinePen, from, to, MarkerSize);
                break;
            case ClassRelationType.Composition:
                MermaidDrawingHelpers.DrawDiamondMarker(dc, presenter.ArrowFill, presenter.LinePen, from, to, MarkerSize);
                break;
            case ClassRelationType.Aggregation:
                MermaidDrawingHelpers.DrawDiamondMarker(dc, background, presenter.LinePen, from, to, MarkerSize);
                break;
            case ClassRelationType.Association:
            case ClassRelationType.Dependency:
                MermaidDrawingHelpers.DrawArrowHead(dc, null, presenter.LinePen, from, to, MarkerSize, filled: false);
                break;
            case ClassRelationType.Lollipop:
                MermaidDrawingHelpers.DrawCircleMarker(dc, background, presenter.LinePen, to, MarkerSize / 2);
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
        return Math.Abs(dx) > Math.Abs(dy)
            ? (dx > 0 ? 14 : -14, -10)
            : (-14, dy > 0 ? 14 : -14);
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