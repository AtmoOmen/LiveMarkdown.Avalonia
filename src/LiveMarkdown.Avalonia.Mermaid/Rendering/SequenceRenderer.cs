using Avalonia;
using Avalonia.Media;
using Mermaider.Models;
using Mermaider.Rendering;
using Point = Avalonia.Point;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Native Avalonia renderer entry point for Mermaid sequence diagrams.
/// </summary>
/// <remarks>
/// Sequence diagrams are expected to share text measurement and arrow helpers with flowcharts, while
/// keeping participant/lifeline/message ordering logic inside this renderer.
/// </remarks>
internal static class SequenceRenderer
{
    private const double ArrowSize = 8;
    private const double SelfMessageWidth = 30;
    private const double SelfMessageHeight = 20;
    private const double ConditionBadgePaddingX = 8;
    private const double ConditionBadgePaddingY = 4;
    private const double AutoNumberBadgeRadius = 11;
    private const double ActorIconSize = 24;

    /// <summary>
    /// Draws a positioned sequence diagram.
    /// </summary>
    public static void Render(DrawingContext dc, MermaidPresenter presenter, PositionedSequenceDiagram diagram)
    {
        foreach (var box in diagram.Boxes)
        {
            DrawBox(dc, presenter, box);
        }

        foreach (var block in diagram.Blocks)
        {
            DrawBlock(dc, presenter, block);
        }

        foreach (var lifeline in diagram.Lifelines)
        {
            DrawLifeline(dc, presenter, lifeline);
        }

        foreach (var activation in diagram.Activations)
        {
            DrawActivation(dc, presenter, activation);
        }

        foreach (var message in diagram.Messages)
        {
            DrawMessage(dc, presenter, message);
        }

        foreach (var note in diagram.Notes)
        {
            DrawNote(dc, presenter, note);
        }

        foreach (var actor in diagram.Actors)
        {
            DrawActor(dc, presenter, actor);
        }

        if (diagram.Actors.Count > 0)
        {
            var actorHeight = diagram.Actors[0].Height;
            var bottomActorY = diagram.Height - actorHeight - 30;
            foreach (var actor in diagram.Actors)
            {
                DrawActor(dc, presenter, actor with { Y = bottomActorY });
            }
        }

        foreach (var marker in diagram.DestroyMarkers)
        {
            DrawDestroyMarker(dc, presenter, marker);
        }
    }

    private static void DrawActor(DrawingContext dc, MermaidPresenter presenter, PositionedSequenceActor actor)
    {
        if (actor.Type == SequenceActorType.Actor)
        {
            DrawActorIcon(dc, presenter, actor);
            MermaidTextRenderer.DrawInlineText(
                dc,
                presenter,
                actor.Label,
                isMarkdown: false,
                actor.X,
                actor.Y + actor.Height + 14,
                presenter.NodeLabelFontSize,
                presenter.Foreground,
                TextAlignment.Center,
                centerVertically: true);
            return;
        }

        var rect = new Rect(actor.X - actor.Width / 2, actor.Y, actor.Width, actor.Height);
        dc.DrawRectangle(
            presenter.NodeFill,
            presenter.NodePen,
            rect,
            RenderConstants.Radii.Rectangle,
            RenderConstants.Radii.Rectangle);
        MermaidTextRenderer.DrawInlineText(
            dc,
            presenter,
            actor.Label,
            isMarkdown: false,
            actor.X,
            actor.Y + actor.Height / 2,
            presenter.NodeLabelFontSize,
            presenter.Foreground,
            TextAlignment.Center,
            centerVertically: true,
            FontWeight.SemiBold);
    }

    private static void DrawActorIcon(DrawingContext dc, MermaidPresenter presenter, PositionedSequenceActor actor)
    {
        if (presenter.LinePen is not { } pen)
        {
            return;
        }

        var scale = actor.Height / ActorIconSize * 0.9;
        var centerX = actor.X;
        var top = actor.Y + (actor.Height - ActorIconSize * scale) / 2;
        var headCenter = new Point(centerX, top + 7 * scale);
        var bodyTop = top + 11 * scale;
        var bodyBottom = top + 18 * scale;
        var armY = top + 14 * scale;

        dc.DrawEllipse(null, pen, headCenter, 4 * scale, 4 * scale);
        dc.DrawLine(pen, new Point(centerX, bodyTop), new Point(centerX, bodyBottom));
        dc.DrawLine(pen, new Point(centerX - 7 * scale, armY), new Point(centerX + 7 * scale, armY));
        dc.DrawLine(pen, new Point(centerX, bodyBottom), new Point(centerX - 6 * scale, top + 23 * scale));
        dc.DrawLine(pen, new Point(centerX, bodyBottom), new Point(centerX + 6 * scale, top + 23 * scale));
    }

    private static void DrawLifeline(DrawingContext dc, MermaidPresenter presenter, Lifeline lifeline)
    {
        if (presenter.DottedLinePen is not { } pen)
        {
            return;
        }

        dc.DrawLine(
            pen,
            new Point(lifeline.X, lifeline.TopY),
            new Point(lifeline.X, lifeline.BottomY));
    }

    private static void DrawActivation(DrawingContext dc, MermaidPresenter presenter, Activation activation)
    {
        dc.DrawRectangle(
            presenter.NodeFill,
            presenter.NodePen,
            new Rect(activation.X, activation.TopY, activation.Width, activation.BottomY - activation.TopY),
            4,
            4);
    }

    private static void DrawMessage(DrawingContext dc, MermaidPresenter presenter, PositionedSequenceMessage message)
    {
        var pen = message.LineStyle == SequenceLineStyle.Dashed ? presenter.DottedLinePen : presenter.LinePen;
        if (pen is null)
        {
            return;
        }

        if (message.IsSelf)
        {
            DrawSelfMessage(dc, presenter, message, pen);
            return;
        }

        var from = new Point(message.X1, message.Y);
        var to = new Point(message.X2, message.Y);
        dc.DrawLine(pen, from, to);
        MermaidDrawingHelpers.DrawArrowHead(dc, presenter.ArrowFill, pen, from, to, ArrowSize, message.ArrowHead == SequenceArrowHead.Filled);
        if (message.Bidirectional)
        {
            MermaidDrawingHelpers.DrawArrowHead(dc, presenter.ArrowFill, pen, to, from, ArrowSize, message.ArrowHead == SequenceArrowHead.Filled);
        }

        var label = message.Label;
        DrawAutoNumberBadge(dc, presenter, message, ref label);
        MermaidTextRenderer.DrawInlineText(
            dc,
            presenter,
            label,
            isMarkdown: false,
            (message.X1 + message.X2) / 2,
            message.Y - 14,
            presenter.EdgeLabelFontSize,
            presenter.SecondaryForeground,
            TextAlignment.Center,
            centerVertically: true);
    }

    private static void DrawSelfMessage(
        DrawingContext dc,
        MermaidPresenter presenter,
        PositionedSequenceMessage message,
        IPen pen)
    {
        var p1 = new Point(message.X1, message.Y);
        var p2 = new Point(message.X1 + SelfMessageWidth, message.Y);
        var p3 = new Point(message.X1 + SelfMessageWidth, message.Y + SelfMessageHeight);
        var p4 = new Point(message.X2, message.Y + SelfMessageHeight);

        dc.DrawLine(pen, p1, p2);
        dc.DrawLine(pen, p2, p3);
        dc.DrawLine(pen, p3, p4);
        MermaidDrawingHelpers.DrawArrowHead(dc, presenter.ArrowFill, pen, p3, p4, ArrowSize, message.ArrowHead == SequenceArrowHead.Filled);

        MermaidTextRenderer.DrawInlineText(
            dc,
            presenter,
            message.Label,
            isMarkdown: false,
            message.X1 + SelfMessageWidth + 8,
            message.Y + SelfMessageHeight / 2,
            presenter.EdgeLabelFontSize,
            presenter.SecondaryForeground,
            TextAlignment.Left,
            centerVertically: true);
    }

    private static void DrawAutoNumberBadge(
        DrawingContext dc,
        MermaidPresenter presenter,
        PositionedSequenceMessage message,
        ref string label)
    {
        var dotIndex = label.IndexOf(". ", StringComparison.Ordinal);
        if (dotIndex is <= 0 or > 4)
        {
            return;
        }

        var number = label[..dotIndex];
        foreach (var c in number)
        {
            if (!char.IsDigit(c))
            {
                return;
            }
        }

        label = label[(dotIndex + 2)..];
        var center = new Point(message.X1, message.Y);
        dc.DrawEllipse(presenter.Foreground, null, center, AutoNumberBadgeRadius, AutoNumberBadgeRadius);
        MermaidTextRenderer.DrawText(
            dc,
            presenter,
            number,
            center.X,
            center.Y,
            presenter.KeyBadgeFontSize,
            presenter.EdgeLabelBackground,
            TextAlignment.Center,
            centerVertically: true,
            FontWeight.Bold);
    }

    private static void DrawBlock(DrawingContext dc, MermaidPresenter presenter, PositionedSequenceBlock block)
    {
        dc.DrawRectangle(
            null,
            presenter.LinePen,
            new Rect(block.X, block.Y, block.Width, block.Height),
            RenderConstants.Radii.Group,
            RenderConstants.Radii.Group);

        var typeName = block.Type.ToString().ToLowerInvariant();
        var tabText = MermaidTextRenderer.CreateFormattedText(
            presenter,
            typeName,
            presenter.EdgeLabelFontSize,
            presenter.SecondaryForeground,
            TextAlignment.Left,
            FontWeight.SemiBold);
        var tabWidth = tabText.Width + 16;
        const double tabHeight = 18;
        dc.DrawRectangle(
            presenter.GroupHeaderFill,
            presenter.LinePen,
            new Rect(block.X, block.Y, tabWidth, tabHeight),
            6,
            6);
        dc.DrawText(tabText, new Point(block.X + 6, block.Y + (tabHeight - tabText.Height) / 2));

        if (block.Label.Length > 0)
        {
            DrawConditionBadge(dc, presenter, block.Label, block.X + block.Width / 2, block.Y + tabHeight / 2);
        }

        foreach (var divider in block.Dividers)
        {
            if (presenter.LinePen is { } pen)
            {
                dc.DrawLine(
                    pen,
                    new Point(block.X, divider.Y),
                    new Point(block.X + block.Width, divider.Y));
            }

            if (divider.Label.Length > 0)
            {
                DrawConditionBadge(dc, presenter, divider.Label, block.X + block.Width / 2, divider.Y + 14);
            }
        }
    }

    private static void DrawConditionBadge(DrawingContext dc, MermaidPresenter presenter, string label, double centerX, double centerY)
    {
        var text = MermaidTextRenderer.CreateFormattedText(
            presenter,
            label,
            presenter.EdgeLabelFontSize,
            presenter.EdgeLabelBackground,
            TextAlignment.Center,
            FontWeight.SemiBold);
        var width = text.Width + ConditionBadgePaddingX * 2;
        var height = text.Height + ConditionBadgePaddingY * 2;
        var rect = new Rect(centerX - width / 2, centerY - height / 2, width, height);

        dc.DrawRectangle(presenter.Foreground, null, rect, 4, 4);
        dc.DrawText(text, new Point(rect.X + (width - text.Width) / 2, rect.Y + (height - text.Height) / 2));
    }

    private static void DrawNote(DrawingContext dc, MermaidPresenter presenter, PositionedSequenceNote note)
    {
        dc.DrawRectangle(
            presenter.AccentFill,
            presenter.AccentPen,
            new Rect(note.X, note.Y, note.Width, note.Height),
            6,
            6);

        var textX = note.X + note.Width / 2;
        if (note.Position == SequenceNotePosition.Right)
        {
            textX -= 6;
        }
        else if (note.Position == SequenceNotePosition.Left)
        {
            textX += 6;
        }

        MermaidTextRenderer.DrawInlineText(
            dc,
            presenter,
            note.Text,
            isMarkdown: false,
            textX,
            note.Y + note.Height / 2,
            presenter.EdgeLabelFontSize,
            presenter.AccentForeground,
            TextAlignment.Center,
            centerVertically: true);
    }

    private static void DrawDestroyMarker(DrawingContext dc, MermaidPresenter presenter, PositionedDestroyMarker marker)
    {
        const double size = 12;
        var pen = presenter.ThickLinePen ?? presenter.LinePen;
        if (pen is null)
        {
            return;
        }

        dc.DrawLine(pen, new Point(marker.X - size, marker.Y - size), new Point(marker.X + size, marker.Y + size));
        dc.DrawLine(pen, new Point(marker.X + size, marker.Y - size), new Point(marker.X - size, marker.Y + size));
    }

    private static void DrawBox(DrawingContext dc, MermaidPresenter presenter, PositionedSequenceBox box)
    {
        var fill = presenter.GetCachedBrush(box.Color, presenter.GroupFill);
        dc.DrawRectangle(
            fill,
            presenter.GroupPen,
            new Rect(box.X, box.Y, box.Width, box.Height),
            RenderConstants.Radii.Group,
            RenderConstants.Radii.Group);

        if (box.Title.Length > 0)
        {
            MermaidTextRenderer.DrawInlineText(
                dc,
                presenter,
                box.Title,
                isMarkdown: false,
                box.X + box.Width / 2,
                box.Y + 10,
                presenter.EdgeLabelFontSize,
                presenter.SecondaryForeground,
                TextAlignment.Center,
                centerVertically: true,
                FontWeight.SemiBold);
        }
    }
}