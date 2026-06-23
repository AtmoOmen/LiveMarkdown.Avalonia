using Avalonia;
using Avalonia.Media;
using AvaloniaPoint = Avalonia.Point;
using MermaidPoint = Mermaider.Models.Point;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Shared geometry helpers for Mermaid renderers that draw Mermaider layout models onto Avalonia.
/// </summary>
/// <remarks>
/// These helpers stay deliberately small: they cover repeated low-level drawing operations, but do
/// not hide diagram-specific layout choices from each renderer.
/// </remarks>
internal static class MermaidDrawingHelpers
{
    /// <summary>
    /// Adds a polyline to an open <see cref="StreamGeometryContext"/>, replacing sharp intermediate
    /// corners with quadratic curves when there is enough segment length.
    /// </summary>
    /// <remarks>
    /// The first and last points remain exact. The effective radius is clamped per corner so short
    /// orthogonal segments do not overshoot or invert.
    /// </remarks>
    public static void BuildRoundedPathContext(StreamGeometryContext context, IReadOnlyList<MermaidPoint> points, double radius)
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

    /// <summary>
    /// Creates a rounded polyline geometry from Mermaider layout points.
    /// </summary>
    public static StreamGeometry CreateRoundedPath(IReadOnlyList<MermaidPoint> points, double radius)
    {
        var geometry = new StreamGeometry();
        if (points.Count == 0)
        {
            return geometry;
        }

        using (var context = geometry.Open())
        {
            BuildRoundedPathContext(context, points, radius);
        }

        return geometry;
    }

    /// <summary>
    /// Draws a rounded polyline path when at least two points are available.
    /// </summary>
    public static void DrawRoundedPath(DrawingContext dc, IReadOnlyList<MermaidPoint> points, IPen? pen, double radius)
    {
        if (points.Count < 2 || pen is null)
        {
            return;
        }

        dc.DrawGeometry(null, pen, CreateRoundedPath(points, radius));
    }

    /// <summary>
    /// Draws a filled triangular arrowhead aligned with the segment from <paramref name="from"/> to
    /// <paramref name="to"/>.
    /// </summary>
    /// <remarks>
    /// <paramref name="isStart"/> mirrors the head so the same method can render both start and end
    /// markers for Mermaid edges.
    /// </remarks>
    public static void DrawArrowHead(
        DrawingContext dc,
        IBrush? brush,
        MermaidPoint from,
        MermaidPoint to,
        bool isStart,
        double arrowSize)
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
                ctx.BeginFigure(new AvaloniaPoint(arrowSize, 0), true);
                ctx.LineTo(new AvaloniaPoint(0, arrowSize / 2.0));
                ctx.LineTo(new AvaloniaPoint(arrowSize, arrowSize));
            }
            else
            {
                ctx.BeginFigure(new AvaloniaPoint(0, 0), true);
                ctx.LineTo(new AvaloniaPoint(arrowSize, arrowSize / 2.0));
                ctx.LineTo(new AvaloniaPoint(0, arrowSize));
            }

            ctx.EndFigure(true);
        }

        var offsetMatrix = Matrix.CreateTranslation(isStart ? arrowSize : -arrowSize, -arrowSize / 2.0);
        using (dc.PushTransform(offsetMatrix * transform))
        {
            dc.DrawGeometry(brush, null, arrowGeom);
        }
    }

    /// <summary>
    /// Draws an arrow marker at an arbitrary Avalonia endpoint.
    /// </summary>
    public static void DrawArrowHead(
        DrawingContext dc,
        IBrush? fill,
        IPen? stroke,
        AvaloniaPoint from,
        AvaloniaPoint to,
        double arrowSize,
        bool filled)
    {
        if (!TryGetDirection(from, to, out var ux, out var uy))
        {
            return;
        }

        var px = -uy;
        var py = ux;
        var backX = to.X - ux * arrowSize;
        var backY = to.Y - uy * arrowSize;
        var half = arrowSize / 2;

        var p1 = new AvaloniaPoint(backX + px * half, backY + py * half);
        var p2 = to;
        var p3 = new AvaloniaPoint(backX - px * half, backY - py * half);

        if (filled)
        {
            DrawPolygon(dc, fill, null, p1, p2, p3);
        }
        else if (stroke is not null)
        {
            dc.DrawLine(stroke, p1, p2);
            dc.DrawLine(stroke, p2, p3);
        }
    }

    /// <summary>
    /// Draws a diamond marker for class composition and aggregation relationships.
    /// </summary>
    public static void DrawDiamondMarker(
        DrawingContext dc,
        IBrush? fill,
        IPen? stroke,
        AvaloniaPoint from,
        AvaloniaPoint to,
        double size)
    {
        if (!TryGetDirection(from, to, out var ux, out var uy))
        {
            return;
        }

        var px = -uy;
        var py = ux;
        var half = size / 2;
        var center = new AvaloniaPoint(to.X - ux * half, to.Y - uy * half);
        var back = new AvaloniaPoint(to.X - ux * size, to.Y - uy * size);

        DrawPolygon(
            dc,
            fill,
            stroke,
            to,
            new AvaloniaPoint(center.X + px * half, center.Y + py * half),
            back,
            new AvaloniaPoint(center.X - px * half, center.Y - py * half));
    }

    /// <summary>
    /// Draws a lollipop marker at an endpoint.
    /// </summary>
    public static void DrawCircleMarker(
        DrawingContext dc,
        IBrush? fill,
        IPen? stroke,
        AvaloniaPoint center,
        double radius)
    {
        dc.DrawEllipse(fill, stroke, center, radius, radius);
    }

    /// <summary>
    /// Draws a closed polygon when at least three points are available.
    /// </summary>
    public static void DrawPolygon(DrawingContext dc, IBrush? fill, IPen? stroke, params AvaloniaPoint[] points)
    {
        if (points.Length < 3)
        {
            return;
        }

        var geom = new StreamGeometry();
        using (var ctx = geom.Open())
        {
            ctx.BeginFigure(points[0], isFilled: true);
            for (var i = 1; i < points.Length; i++)
            {
                ctx.LineTo(points[i]);
            }

            ctx.EndFigure(isClosed: true);
        }

        dc.DrawGeometry(fill, stroke, geom);
    }

    /// <summary>
    /// Finds the visual midpoint along a polyline by length, not by averaging all vertices.
    /// </summary>
    /// <remarks>
    /// Edge labels look more stable when centered by travelled path distance. For example, a long
    /// horizontal segment followed by a tiny vertical tail will place the label on the long segment
    /// instead of near the arithmetic center of the vertices.
    /// </remarks>
    public static MermaidPoint Midpoint(IReadOnlyList<MermaidPoint> points)
    {
        switch (points.Count)
        {
            case 0: return new MermaidPoint(0, 0);
            case 1: return points[0];
        }

        var totalLength = 0.0;
        for (var i = 1; i < points.Count; i++)
        {
            totalLength += Distance(points[i - 1], points[i]);
        }

        var remaining = totalLength / 2;
        for (var i = 1; i < points.Count; i++)
        {
            var segLen = Distance(points[i - 1], points[i]);
            if (remaining <= segLen)
            {
                var t = remaining / segLen;
                return new MermaidPoint(
                    points[i - 1].X + t * (points[i].X - points[i - 1].X),
                    points[i - 1].Y + t * (points[i].Y - points[i - 1].Y));
            }

            remaining -= segLen;
        }

        return points[^1];
    }

    private static double Distance(MermaidPoint a, MermaidPoint b) =>
        Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));

    private static bool TryGetDirection(AvaloniaPoint from, AvaloniaPoint to, out double ux, out double uy)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length <= 0.001)
        {
            ux = 0;
            uy = 0;
            return false;
        }

        ux = dx / length;
        uy = dy / length;
        return true;
    }
}