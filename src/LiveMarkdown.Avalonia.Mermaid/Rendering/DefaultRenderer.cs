using Avalonia;
using Avalonia.Media;
using Mermaider.Models;
using Mermaider.Rendering;
using AvaloniaPoint = Avalonia.Point;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Native Avalonia renderer for Mermaider positioned flowchart and state graph models.
/// </summary>
/// <remarks>
/// The renderer follows Mermaider's SVG renderer ordering: group bodies, edges, group headers,
/// edge labels, nodes, then notes. That order keeps labels and nodes above connectors while allowing
/// groups to act as a visual backdrop.
/// </remarks>
public static class DefaultRenderer
{
    private const double CornerRadius = 6;
    private const double ArrowSize = 6;

    /// <summary>
    /// Draws a positioned graph using the brushes, pens, and font sizes supplied by the presenter.
    /// </summary>
    public static void Render(DrawingContext dc, MermaidPresenter presenter, PositionedGraph graph)
    {
        foreach (var group in graph.Groups)
            DrawGroupBody(dc, presenter, group);

        foreach (var edge in graph.Edges)
        {
            if (edge.Style != EdgeStyle.Invisible)
                DrawEdge(dc, presenter, edge);
        }

        foreach (var group in graph.Groups)
            DrawGroupHeader(dc, presenter, group);

        foreach (var edge in graph.Edges)
        {
            if (edge.Style != EdgeStyle.Invisible && edge.Label is not null)
                DrawEdgeLabel(dc, presenter, edge);
        }

        foreach (var node in graph.Nodes)
            DrawNode(dc, presenter, node);

        foreach (var note in graph.Notes)
            DrawNote(dc, presenter, note);
    }

    // ========================================================================
    // Group rendering
    // ========================================================================

    private static void DrawGroupBody(DrawingContext dc, MermaidPresenter presenter, PositionedGroup group)
    {
        var rect = new Rect(group.X, group.Y, group.Width, group.Height);
        dc.DrawRectangle(presenter.GroupFill, presenter.GroupPen, rect, RenderConstants.Radii.Group, RenderConstants.Radii.Group);

        foreach (var child in group.Children)
            DrawGroupBody(dc, presenter, child);
    }

    private static void DrawGroupHeader(DrawingContext dc, MermaidPresenter presenter, PositionedGroup group)
    {
        var headerHeight = presenter.GroupHeaderFontSize + 16;
        var rect = new Rect(group.X, group.Y, group.Width, headerHeight);

        dc.DrawRectangle(presenter.GroupHeaderFill, presenter.GroupPen, rect, RenderConstants.Radii.Group, RenderConstants.Radii.Group);

        MermaidTextRenderer.DrawInlineText(
            dc,
            presenter,
            group.Label,
            isMarkdown: false,
            group.X + 12,
            group.Y + headerHeight / 2.0,
            presenter.GroupHeaderFontSize,
            presenter.SecondaryForeground,
            TextAlignment.Left,
            centerVertically: true);

        foreach (var child in group.Children)
            DrawGroupHeader(dc, presenter, child);
    }

    // ========================================================================
    // Edge rendering
    // ========================================================================

    private static void DrawEdge(DrawingContext dc, MermaidPresenter presenter, PositionedEdge edge)
    {
        if (edge.Points.Count < 2) return;

        var pen = edge.Style switch
        {
            EdgeStyle.Thick => presenter.ThickLinePen,
            EdgeStyle.Dotted => presenter.DottedLinePen,
            _ => presenter.LinePen
        };

        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            MermaidDrawingHelpers.BuildRoundedPathContext(context, edge.Points, CornerRadius);
        }

        dc.DrawGeometry(null, pen, geometry);

        if (edge.HasArrowEnd)
            MermaidDrawingHelpers.DrawArrowHead(dc, presenter.ArrowFill, edge.Points[^2], edge.Points[^1], false, ArrowSize);

        if (edge.HasArrowStart)
            MermaidDrawingHelpers.DrawArrowHead(dc, presenter.ArrowFill, edge.Points[1], edge.Points[0], true, ArrowSize);
    }

    private static void DrawEdgeLabel(DrawingContext dc, MermaidPresenter presenter, PositionedEdge edge)
    {
        var mid = edge.LabelPosition ?? MermaidDrawingHelpers.Midpoint(edge.Points);
        var label = edge.Label!;
        MermaidTextRenderer.DrawTextWithBackground(
            dc,
            presenter,
            label,
            isMarkdown: false,
            mid.X,
            mid.Y,
            presenter.EdgeLabelFontSize,
            presenter.SecondaryForeground,
            presenter.EdgeLabelBackground,
            presenter.EdgeLabelPen,
            padding: 8,
            radius: RenderConstants.Radii.EdgeLabel);
    }

    // ========================================================================
    // Node rendering
    // ========================================================================

    private static void DrawNode(DrawingContext dc, MermaidPresenter presenter, PositionedNode node)
    {
        var (x, y, w, h) = (node.X, node.Y, node.Width, node.Height);

        var styleFill = node.InlineStyle?.GetValueOrDefault("fill");
        var styleStroke = node.InlineStyle?.GetValueOrDefault("stroke");
        var styleStrokeWidth = node.InlineStyle?.GetValueOrDefault("stroke-width");

        var fill = presenter.GetCachedBrush(styleFill, presenter.NodeFill);
        var stroke = presenter.GetCachedPen(styleStroke, styleStrokeWidth, presenter.NodePen);
        if (fill is null && stroke is null) return;

        switch (node.Shape)
        {
            case NodeShape.Rectangle:
                dc.DrawRectangle(fill, stroke, new Rect(x, y, w, h), RenderConstants.Radii.Rectangle, RenderConstants.Radii.Rectangle);
                break;
            case NodeShape.Rounded:
                dc.DrawRectangle(fill, stroke, new Rect(x, y, w, h), RenderConstants.Radii.Rounded, RenderConstants.Radii.Rounded);
                break;
            case NodeShape.Stadium:
                dc.DrawRectangle(fill, stroke, new Rect(x, y, w, h), h / 2, h / 2);
                break;
            case NodeShape.Diamond:
                MermaidDrawingHelpers.DrawPolygon(
                    dc,
                    fill,
                    stroke,
                    new AvaloniaPoint(x + w / 2, y),
                    new AvaloniaPoint(x + w, y + h / 2),
                    new AvaloniaPoint(x + w / 2, y + h),
                    new AvaloniaPoint(x, y + h / 2));
                break;
            case NodeShape.Circle:
                dc.DrawEllipse(fill, stroke, new AvaloniaPoint(x + w / 2, y + h / 2), Math.Min(w, h) / 2, Math.Min(w, h) / 2);
                break;
            case NodeShape.DoubleCircle:
                var cx = x + w / 2;
                var cy = y + h / 2;
                var outerR = Math.Min(w, h) / 2;
                dc.DrawEllipse(fill, stroke, new AvaloniaPoint(cx, cy), outerR, outerR);
                dc.DrawEllipse(fill, stroke, new AvaloniaPoint(cx, cy), outerR - 5, outerR - 5);
                break;
            case NodeShape.Subroutine when stroke is not null:
                const int inset = 8;
                dc.DrawRectangle(fill, stroke, new Rect(x, y, w, h), RenderConstants.Radii.Rectangle, RenderConstants.Radii.Rectangle);
                dc.DrawLine(stroke, new AvaloniaPoint(x + inset, y), new AvaloniaPoint(x + inset, y + h));
                dc.DrawLine(stroke, new AvaloniaPoint(x + w - inset, y), new AvaloniaPoint(x + w - inset, y + h));
                break;
            case NodeShape.Hexagon:
                var hexInset = h / 4;
                MermaidDrawingHelpers.DrawPolygon(
                    dc,
                    fill,
                    stroke,
                    new AvaloniaPoint(x + hexInset, y),
                    new AvaloniaPoint(x + w - hexInset, y),
                    new AvaloniaPoint(x + w, y + h / 2),
                    new AvaloniaPoint(x + w - hexInset, y + h),
                    new AvaloniaPoint(x + hexInset, y + h),
                    new AvaloniaPoint(x, y + h / 2));
                break;
            case NodeShape.Cylinder:
                const int ry = 7;
                var bodyTop = y + ry;
                var bodyH = h - 2 * ry;
                dc.DrawRectangle(fill, null, new Rect(x, bodyTop, w, bodyH));
                if (stroke is not null) dc.DrawLine(stroke, new AvaloniaPoint(x, bodyTop), new AvaloniaPoint(x, bodyTop + bodyH));
                if (stroke is not null) dc.DrawLine(stroke, new AvaloniaPoint(x + w, bodyTop), new AvaloniaPoint(x + w, bodyTop + bodyH));
                dc.DrawEllipse(fill, stroke, new AvaloniaPoint(x + w / 2, y + h - ry), w / 2, ry); // Bottom
                dc.DrawEllipse(fill, stroke, new AvaloniaPoint(x + w / 2, bodyTop), w / 2, ry); // Top
                break;
            case NodeShape.Asymmetric:
                const int asymmetricIndent = 12;
                MermaidDrawingHelpers.DrawPolygon(
                    dc,
                    fill,
                    stroke,
                    new AvaloniaPoint(x + asymmetricIndent, y),
                    new AvaloniaPoint(x + w, y),
                    new AvaloniaPoint(x + w, y + h),
                    new AvaloniaPoint(x + asymmetricIndent, y + h),
                    new AvaloniaPoint(x, y + h / 2));
                break;
            case NodeShape.Trapezoid:
                var trapInset = w * 0.15;
                MermaidDrawingHelpers.DrawPolygon(
                    dc,
                    fill,
                    stroke,
                    new AvaloniaPoint(x + trapInset, y),
                    new AvaloniaPoint(x + w - trapInset, y),
                    new AvaloniaPoint(x + w, y + h),
                    new AvaloniaPoint(x, y + h));
                break;
            case NodeShape.TrapezoidAlt:
                var altInset = w * 0.15;
                MermaidDrawingHelpers.DrawPolygon(
                    dc,
                    fill,
                    stroke,
                    new AvaloniaPoint(x, y),
                    new AvaloniaPoint(x + w, y),
                    new AvaloniaPoint(x + w - altInset, y + h),
                    new AvaloniaPoint(x + altInset, y + h));
                break;
            case NodeShape.StateStart:
                dc.DrawEllipse(
                    presenter.Foreground,
                    null,
                    new AvaloniaPoint(x + w / 2, y + h / 2),
                    (Math.Min(w, h) / 2) - 2,
                    (Math.Min(w, h) / 2) - 2);
                break;
            case NodeShape.StateEnd:
                var seCx = x + w / 2;
                var seCy = y + h / 2;
                var seOuter = (Math.Min(w, h) / 2) - 2;
                dc.DrawEllipse(
                    null,
                    presenter.StateEndPen,
                    new AvaloniaPoint(seCx, seCy),
                    seOuter,
                    seOuter);
                dc.DrawEllipse(presenter.Foreground, null, new AvaloniaPoint(seCx, seCy), seOuter - 4, seOuter - 4);
                break;
            case NodeShape.ForkJoin:
                dc.DrawRectangle(presenter.Foreground, null, new Rect(x, y, w, h), 2, 2);
                break;
        }

        DrawNodeLabel(dc, presenter, node);
    }

    private static void DrawNote(DrawingContext dc, MermaidPresenter presenter, PositionedGraphNote note)
    {
        var rect = new Rect(note.X, note.Y, note.Width, note.Height);
        dc.DrawRectangle(presenter.AccentFill, presenter.AccentPen, rect, 6, 6);

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
            true);
    }

    // ========================================================================
    // Text / Markdown rendering
    // ========================================================================

    private static void DrawNodeLabel(DrawingContext dc, MermaidPresenter presenter, PositionedNode node)
    {
        if (node.Shape is NodeShape.StateStart or NodeShape.StateEnd or NodeShape.ForkJoin && string.IsNullOrEmpty(node.Label))
            return;

        var cx = node.X + (node.Width / 2);
        var cy = node.Y + (node.Height / 2);

        var colorStr = node.InlineStyle?.GetValueOrDefault("color");
        var textBrush = presenter.GetCachedBrush(colorStr, presenter.Foreground);

        MermaidTextRenderer.DrawInlineText(
            dc,
            presenter,
            node.Label,
            node.IsMarkdown,
            cx,
            cy,
            presenter.NodeLabelFontSize,
            textBrush,
            TextAlignment.Center,
            centerVertically: true);
    }
}