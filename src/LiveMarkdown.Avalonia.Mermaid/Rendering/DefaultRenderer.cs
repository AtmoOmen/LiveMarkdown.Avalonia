using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Media;
using Mermaider.Models;
using Mermaider.Rendering;
using Point = Mermaider.Models.Point;
using AvaloniaPoint = Avalonia.Point;

namespace LiveMarkdown.Avalonia;

public static class DefaultRenderer
{
    private const double CornerRadius = 6;
    private const double ArrowSize = 6;

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
        const int headerHeight = RenderConstants.FontSizes.GroupHeader + 16;
        var rect = new Rect(group.X, group.Y, group.Width, headerHeight);

        dc.DrawRectangle(presenter.GroupHeaderFill, presenter.GroupPen, rect, RenderConstants.Radii.Group, RenderConstants.Radii.Group);

        DrawSimpleText(
            dc,
            presenter,
            group.Label,
            group.X + 12,
            group.Y + headerHeight / 2.0,
            RenderConstants.FontSizes.GroupHeader,
            presenter.SecondaryForeground,
            TextAlignment.Left);

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
            BuildRoundedPathContext(context, edge.Points, CornerRadius);
        }

        dc.DrawGeometry(null, pen, geometry);

        if (edge.HasArrowEnd)
            DrawArrowHead(dc, presenter.ArrowFill, edge.Points[^2], edge.Points[^1], false);

        if (edge.HasArrowStart)
            DrawArrowHead(dc, presenter.ArrowFill, edge.Points[1], edge.Points[0], true);
    }

    private static void BuildRoundedPathContext(StreamGeometryContext context, IReadOnlyList<Point> points, double radius)
    {
        context.BeginFigure(new AvaloniaPoint(points[0].X, points[0].Y), isFilled: false);

        if (points.Count == 2)
        {
            context.LineTo(new AvaloniaPoint(points[1].X, points[1].Y));
            return;
        }

        for (var i = 1; i < points.Count - 1; i++)
        {
            var prev = points[i - 1];
            var curr = points[i];
            var next = points[i + 1];

            var dx1 = curr.X - prev.X;
            var dy1 = curr.Y - prev.Y;
            var len1 = Math.Sqrt((dx1 * dx1) + (dy1 * dy1));

            var dx2 = next.X - curr.X;
            var dy2 = next.Y - curr.Y;
            var len2 = Math.Sqrt((dx2 * dx2) + (dy2 * dy2));

            if (len1 < 0.1 || len2 < 0.1)
            {
                context.LineTo(new AvaloniaPoint(curr.X, curr.Y));
                continue;
            }

            var r = Math.Min(radius, Math.Min(len1 / 2, len2 / 2));

            var startX = curr.X - (dx1 / len1 * r);
            var startY = curr.Y - (dy1 / len1 * r);
            var endX = curr.X + (dx2 / len2 * r);
            var endY = curr.Y + (dy2 / len2 * r);

            context.LineTo(new AvaloniaPoint(startX, startY));
            context.QuadraticBezierTo(new AvaloniaPoint(curr.X, curr.Y), new AvaloniaPoint(endX, endY));
        }

        context.LineTo(new AvaloniaPoint(points[^1].X, points[^1].Y));
    }

    private static void DrawArrowHead(DrawingContext dc, IBrush? brush, Point from, Point to, bool isStart)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var angle = Math.Atan2(dy, dx);

        var transform = Matrix.CreateRotation(angle) * Matrix.CreateTranslation(to.X, to.Y);
        var arrowGeom = new StreamGeometry();
        using (var ctx = arrowGeom.Open())
        {
            if (isStart)
            {
                ctx.BeginFigure(new AvaloniaPoint(ArrowSize, 0), true);
                ctx.LineTo(new AvaloniaPoint(0, ArrowSize / 2.0));
                ctx.LineTo(new AvaloniaPoint(ArrowSize, ArrowSize));
            }
            else
            {
                ctx.BeginFigure(new AvaloniaPoint(0, 0), true);
                ctx.LineTo(new AvaloniaPoint(ArrowSize, ArrowSize / 2.0));
                ctx.LineTo(new AvaloniaPoint(0, ArrowSize));
            }
            ctx.EndFigure(true);
        }

        var offsetMatrix = Matrix.CreateTranslation(isStart ? ArrowSize : -ArrowSize, -ArrowSize / 2.0);
        using (dc.PushTransform(offsetMatrix * transform))
        {
            dc.DrawGeometry(brush, null, arrowGeom);
        }
    }

    private static void DrawEdgeLabel(DrawingContext dc, MermaidPresenter presenter, PositionedEdge edge)
    {
        var mid = edge.LabelPosition ?? EdgeMidpoint(edge.Points);
        var label = edge.Label!;
        const double padding = 8.0;

        var ft = CreateFormattedText(
            presenter,
            label,
            RenderConstants.FontSizes.EdgeLabel,
            presenter.SecondaryForeground,
            FontWeight.Normal,
            TextAlignment.Center);
        var width = ft.Width + padding * 2;
        var height = ft.Height + padding;

        var bgRect = new Rect(mid.X - width / 2, mid.Y - height / 2, width, height);
        dc.DrawRectangle(
            presenter.EdgeLabelBackground,
            presenter.EdgeLabelPen,
            bgRect,
            RenderConstants.Radii.EdgeLabel,
            RenderConstants.Radii.EdgeLabel);

        dc.DrawText(ft, new AvaloniaPoint(bgRect.X + padding, bgRect.Y + padding / 2));
    }

    private static Point EdgeMidpoint(IReadOnlyList<Point> points)
    {
        switch (points.Count)
        {
            case 0: return new Point(0, 0);
            case 1: return points[0];
        }

        var totalLength = 0.0;
        for (var i = 1; i < points.Count; i++) totalLength += Dist(points[i - 1], points[i]);

        var remaining = totalLength / 2;
        for (var i = 1; i < points.Count; i++)
        {
            var segLen = Dist(points[i - 1], points[i]);
            if (remaining <= segLen)
            {
                var t = remaining / segLen;
                return new Point(
                    points[i - 1].X + t * (points[i].X - points[i - 1].X),
                    points[i - 1].Y + t * (points[i].Y - points[i - 1].Y));
            }
            remaining -= segLen;
        }
        return points[^1];
    }

    private static double Dist(Point a, Point b) => Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));

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
                DrawPolygon(
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
                DrawPolygon(
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
                DrawPolygon(
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
                DrawPolygon(
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
                DrawPolygon(
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
                    new Pen(presenter.Foreground, RenderConstants.StrokeWidths.InnerBox * 2),
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

    private static void DrawPolygon(DrawingContext dc, IBrush? fill, IPen? stroke, params AvaloniaPoint[] points)
    {
        if (points.Length < 3) return;
        var geom = new StreamGeometry();
        using (var ctx = geom.Open())
        {
            ctx.BeginFigure(points[0], isFilled: true);
            for (var i = 1; i < points.Length; i++) ctx.LineTo(points[i]);
            ctx.EndFigure(isClosed: true);
        }
        dc.DrawGeometry(fill, stroke, geom);
    }

    private static void DrawNote(DrawingContext dc, MermaidPresenter presenter, PositionedGraphNote note)
    {
        var rect = new Rect(note.X, note.Y, note.Width, note.Height);
        dc.DrawRectangle(presenter.AccentFill, presenter.AccentPen, rect, 6, 6);

        DrawSimpleText(
            dc,
            presenter,
            note.Text,
            note.X + note.Width / 2,
            note.Y + note.Height / 2,
            RenderConstants.FontSizes.EdgeLabel,
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

        if (node.IsMarkdown) DrawMarkdownText(dc, presenter, node.Label, cx, cy, RenderConstants.FontSizes.NodeLabel, textBrush);
        else DrawSimpleText(dc, presenter, node.Label, cx, cy, RenderConstants.FontSizes.NodeLabel, textBrush, TextAlignment.Center, true);
    }

    private static void DrawSimpleText(
        DrawingContext dc,
        MermaidPresenter presenter,
        string text,
        double x,
        double y,
        double fontSize,
        IBrush? brush,
        TextAlignment alignment,
        bool centerVertically = false)
    {
        var ft = CreateFormattedText(presenter, text, fontSize, brush, FontWeight.Normal, alignment);
        var originY = centerVertically ? y - ft.Height / 2 : y - ft.Height;
        var originX = alignment == TextAlignment.Center ? x - ft.Width / 2 : x;
        dc.DrawText(ft, new AvaloniaPoint(originX, originY));
    }

    private static void DrawMarkdownText(
        DrawingContext dc,
        MermaidPresenter presenter,
        string label,
        double cx,
        double cy,
        double fontSize,
        IBrush? brush)
    {
        // TODO: markdown
        var (pureText, spans) = ParseMarkdown(label);

        var ft = CreateFormattedText(presenter, pureText, fontSize, brush, FontWeight.Normal, TextAlignment.Center);
        foreach (var span in spans)
        {
            if (span.IsBold) ft.SetFontWeight(FontWeight.Bold, span.Start, span.Length);
            if (span.IsItalic) ft.SetFontStyle(FontStyle.Italic, span.Start, span.Length);
        }

        dc.DrawText(ft, new AvaloniaPoint(cx - ft.Width / 2, cy - ft.Height / 2));
    }

    private static FormattedText CreateFormattedText(
        MermaidPresenter presenter,
        string text,
        double fontSize,
        IBrush? brush,
        FontWeight weight,
        TextAlignment alignment)
    {
        return new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(presenter.FontFamily, FontStyle.Normal, weight),
            fontSize,
            brush)
        {
            TextAlignment = alignment,
            MaxTextWidth = double.PositiveInfinity
        };
    }

    // --- Markdown Parser Helper ---

    private readonly record struct TextSpan(int Start, int Length, bool IsBold, bool IsItalic);

    private static (string PureText, List<TextSpan> Spans) ParseMarkdown(string input)
    {
        if (string.IsNullOrEmpty(input)) return ("", []);

        var pureText = new StringBuilder();
        var spans = new List<TextSpan>();

        int i = 0;
        while (i < input.Length)
        {
            if (i + 1 < input.Length && input[i] == '*' && input[i + 1] == '*')
            {
                var end = input.IndexOf("**", i + 2, StringComparison.Ordinal);
                if (end > 0)
                {
                    var startIdx = pureText.Length;
                    var content = input.Substring(i + 2, end - i - 2);
                    pureText.Append(content);
                    spans.Add(new TextSpan(startIdx, content.Length, true, false));
                    i = end + 2;
                    continue;
                }
            }
            if (input[i] == '*')
            {
                var end = input.IndexOf('*', i + 1);
                if (end > 0)
                {
                    var startIdx = pureText.Length;
                    var content = input.Substring(i + 1, end - i - 1);
                    pureText.Append(content);
                    spans.Add(new TextSpan(startIdx, content.Length, false, true));
                    i = end + 1;
                    continue;
                }
            }

            pureText.Append(input[i]);
            i++;
        }

        return (pureText.ToString(), spans);
    }
}