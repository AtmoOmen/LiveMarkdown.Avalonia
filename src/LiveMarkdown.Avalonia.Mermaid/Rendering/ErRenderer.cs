using Avalonia;
using Avalonia.Media;
using Mermaider.Models;
using Mermaider.Rendering;
using Point = Avalonia.Point;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Native Avalonia renderer entry point for Mermaid entity-relationship diagrams.
/// </summary>
/// <remarks>
/// The parser and layout model come from Mermaider; this renderer is responsible only for translating
/// the positioned diagram into Avalonia drawing primitives.
/// </remarks>
public static class ErRenderer
{
    private const double RelationshipCornerRadius = 6;
    private const double AttributePaddingX = 8;
    private const double KeyBadgePaddingX = 8;
    private const double KeyBadgeHeight = 14;

    private static readonly FontFamily AttributeFontFamily = FontFamily.Parse("Cascadia Mono, Consolas, Menlo, monospace");

    /// <summary>
    /// Draws a positioned entity-relationship diagram.
    /// </summary>
    public static void Render(DrawingContext dc, MermaidPresenter presenter, PositionedErDiagram diagram)
    {
        foreach (var relationship in diagram.Relationships)
        {
            DrawRelationshipLine(dc, presenter, relationship);
        }

        foreach (var entity in diagram.Entities)
        {
            DrawEntityBox(dc, presenter, entity);
        }

        foreach (var relationship in diagram.Relationships)
        {
            DrawCardinality(dc, presenter, relationship);
        }

        foreach (var relationship in diagram.Relationships)
        {
            DrawRelationshipLabel(dc, presenter, relationship);
        }
    }

    private static void DrawEntityBox(DrawingContext dc, MermaidPresenter presenter, PositionedErEntity entity)
    {
        var rect = new Rect(entity.X, entity.Y, entity.Width, entity.Height);
        using (dc.PushClip(new RoundedRect(rect, RenderConstants.Radii.Rectangle, RenderConstants.Radii.Rectangle)))
        {
            var headerRect = new Rect(entity.X, entity.Y, entity.Width, entity.HeaderHeight);
            dc.DrawRectangle(presenter.GroupHeaderFill, presenter.NodePen, headerRect);
        }
        dc.DrawRectangle(presenter.NodeFill, presenter.NodePen, rect, RenderConstants.Radii.Rectangle, RenderConstants.Radii.Rectangle);

        MermaidTextRenderer.DrawInlineText(
            dc,
            presenter,
            entity.Label,
            isMarkdown: false,
            entity.X + entity.Width / 2,
            entity.Y + entity.HeaderHeight / 2,
            presenter.NodeLabelFontSize,
            presenter.Foreground,
            TextAlignment.Center,
            centerVertically: true,
            FontWeight.Bold);

        var attrTop = entity.Y + entity.HeaderHeight;
        if (presenter.NodePen is { } pen)
        {
            dc.DrawLine(pen, new Point(entity.X, attrTop), new Point(entity.X + entity.Width, attrTop));
        }

        if (entity.Attributes.Count == 0)
        {
            DrawEmptyAttributesPlaceholder(dc, presenter, entity, attrTop);
            return;
        }

        for (var i = 0; i < entity.Attributes.Count; i++)
        {
            var rowY = attrTop + i * entity.RowHeight + entity.RowHeight / 2;
            DrawAttribute(dc, presenter, entity.Attributes[i], entity.X, rowY, entity.Width);
        }
    }

    private static void DrawEmptyAttributesPlaceholder(
        DrawingContext dc,
        MermaidPresenter presenter,
        PositionedErEntity entity,
        double attrTop)
    {
        var text = MermaidTextRenderer.CreateFormattedText(
            presenter,
            "(no attributes)",
            presenter.MemberFontSize,
            presenter.SecondaryForeground,
            TextAlignment.Center);
        text.SetFontStyle(FontStyle.Italic);
        MermaidTextRenderer.DrawFormattedText(
            dc,
            text,
            entity.X + entity.Width / 2,
            attrTop + entity.RowHeight / 2,
            TextAlignment.Center,
            centerVertically: true);
    }

    private static void DrawAttribute(
        DrawingContext dc,
        MermaidPresenter presenter,
        ErAttributeInfo attribute,
        double boxX,
        double y,
        double boxWidth)
    {
        var keyWidth = 0.0;
        if (attribute.Keys.Count > 0)
        {
            keyWidth = DrawKeyBadge(dc, presenter, attribute, boxX + 6, y);
        }

        var typeX = boxX + AttributePaddingX + (keyWidth > 0 ? keyWidth + 6 : 0);
        var typeText = MermaidTextRenderer.CreateFormattedText(
            presenter,
            attribute.Type,
            presenter.MemberFontSize,
            presenter.SecondaryForeground,
            TextAlignment.Left,
            FontWeight.Medium);
        typeText.SetFontFamily(AttributeFontFamily);
        MermaidTextRenderer.DrawFormattedText(dc, typeText, typeX, y, TextAlignment.Left, centerVertically: true);

        var nameText = MermaidTextRenderer.CreateFormattedText(
            presenter,
            attribute.Name,
            presenter.MemberFontSize,
            presenter.Foreground,
            TextAlignment.Right,
            FontWeight.Medium);
        nameText.SetFontFamily(AttributeFontFamily);
        MermaidTextRenderer.DrawFormattedText(
            dc,
            nameText,
            boxX + boxWidth - AttributePaddingX,
            y,
            TextAlignment.Right,
            centerVertically: true);
    }

    private static double DrawKeyBadge(DrawingContext dc, MermaidPresenter presenter, ErAttributeInfo attribute, double x, double centerY)
    {
        var keyText = string.Join(",", attribute.Keys);
        var text = MermaidTextRenderer.CreateFormattedText(
            presenter,
            keyText,
            presenter.KeyBadgeFontSize,
            presenter.SecondaryForeground,
            TextAlignment.Center,
            FontWeight.Bold);
        var width = text.Width + KeyBadgePaddingX;
        var rect = new Rect(x, centerY - KeyBadgeHeight / 2, width, KeyBadgeHeight);

        dc.DrawRectangle(presenter.AccentFill, null, rect, KeyBadgeHeight / 2, KeyBadgeHeight / 2);
        dc.DrawText(text, new Point(rect.X + (rect.Width - text.Width) / 2, rect.Y + (rect.Height - text.Height) / 2));
        return width;
    }

    private static void DrawRelationshipLine(DrawingContext dc, MermaidPresenter presenter, PositionedErRelationship relationship)
    {
        if (relationship.Points.Count < 2)
        {
            return;
        }

        var pen = relationship.Identifying ? presenter.LinePen : presenter.DottedLinePen;
        MermaidDrawingHelpers.DrawRoundedPath(dc, relationship.Points, pen, RelationshipCornerRadius);
    }

    private static void DrawRelationshipLabel(DrawingContext dc, MermaidPresenter presenter, PositionedErRelationship relationship)
    {
        if (relationship.Label.Length == 0 || relationship.Points.Count < 2)
        {
            return;
        }

        var midpoint = MermaidDrawingHelpers.Midpoint(relationship.Points);
        MermaidTextRenderer.DrawTextWithBackground(
            dc,
            presenter,
            relationship.Label,
            isMarkdown: false,
            midpoint.X,
            midpoint.Y,
            presenter.EdgeLabelFontSize,
            presenter.SecondaryForeground,
            presenter.EdgeLabelBackground,
            presenter.EdgeLabelPen,
            padding: 8,
            radius: RenderConstants.Radii.EdgeLabel);
    }

    private static void DrawCardinality(DrawingContext dc, MermaidPresenter presenter, PositionedErRelationship relationship)
    {
        if (relationship.Points.Count < 2)
        {
            return;
        }

        DrawCrowsFoot(dc, presenter, relationship.Points[0], relationship.Points[1], relationship.Cardinality1);
        DrawCrowsFoot(dc, presenter, relationship.Points[^1], relationship.Points[^2], relationship.Cardinality2);
    }

    private static void DrawCrowsFoot(
        DrawingContext dc,
        MermaidPresenter presenter,
        Mermaider.Models.Point point,
        Mermaider.Models.Point toward,
        ErCardinality cardinality)
    {
        if (presenter.LinePen is not { } pen)
        {
            return;
        }

        var dx = point.X - toward.X;
        var dy = point.Y - toward.Y;
        var len = Math.Sqrt(dx * dx + dy * dy);
        if (len <= 0.001)
        {
            return;
        }

        var ux = dx / len;
        var uy = dy / len;
        var px = -uy;
        var py = ux;

        var tipX = point.X - ux * 4;
        var tipY = point.Y - uy * 4;
        var backX = point.X - ux * 16;
        var backY = point.Y - uy * 16;

        var hasOneLine = cardinality is ErCardinality.One or ErCardinality.ZeroOne;
        var hasCrowsFoot = cardinality is ErCardinality.Many or ErCardinality.ZeroMany;
        var hasCircle = cardinality is ErCardinality.ZeroOne or ErCardinality.ZeroMany;

        if (hasOneLine)
        {
            const double halfWidth = 6;
            DrawCardinalityLine(dc, pen, tipX, tipY, px, py, halfWidth);
            DrawCardinalityLine(dc, pen, tipX - ux * 4, tipY - uy * 4, px, py, halfWidth);
        }

        if (hasCrowsFoot)
        {
            const double fanWidth = 7;
            dc.DrawLine(pen, new Point(tipX + px * fanWidth, tipY + py * fanWidth), new Point(backX, backY));
            dc.DrawLine(pen, new Point(tipX, tipY), new Point(backX, backY));
            dc.DrawLine(pen, new Point(tipX - px * fanWidth, tipY - py * fanWidth), new Point(backX, backY));
        }

        if (hasCircle)
        {
            var circleOffset = hasCrowsFoot ? 20 : 12;
            var circleCenter = new Point(point.X - ux * circleOffset, point.Y - uy * circleOffset);
            dc.DrawEllipse(presenter.BackgroundBrush ?? presenter.EdgeLabelBackground, pen, circleCenter, 4, 4);
        }
    }

    private static void DrawCardinalityLine(
        DrawingContext dc,
        IPen pen,
        double cx,
        double cy,
        double px,
        double py,
        double halfWidth)
    {
        dc.DrawLine(
            pen,
            new Point(cx + px * halfWidth, cy + py * halfWidth),
            new Point(cx - px * halfWidth, cy - py * halfWidth));
    }
}