using Avalonia;
using Avalonia.Media;
using Mermaider.Models;
using Point = Avalonia.Point;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Native Avalonia renderer entry point for Mermaid sequence diagrams.
/// </summary>
/// <remarks>
/// Sequence diagrams are expected to share text measurement and arrow helpers with flowcharts, while
/// keeping participant/lifeline/message ordering logic inside this renderer.
/// </remarks>
public class SequenceRenderer : MermaidRenderer
{
    /// <summary>
    /// Defines the <see cref="ArrowSize"/> property.
    /// </summary>
    public static readonly StyledProperty<double> ArrowSizeProperty =
        AvaloniaProperty.Register<SequenceRenderer, double>(nameof(ArrowSize), 8);

    /// <summary>
    /// Size of sequence message arrowheads.
    /// </summary>
    public double ArrowSize
    {
        get => GetValue(ArrowSizeProperty);
        set => SetValue(ArrowSizeProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="SelfMessageWidth"/> property.
    /// </summary>
    public static readonly StyledProperty<double> SelfMessageWidthProperty =
        AvaloniaProperty.Register<SequenceRenderer, double>(nameof(SelfMessageWidth), 30);

    /// <summary>
    /// Horizontal width of the loop used for self messages.
    /// </summary>
    public double SelfMessageWidth
    {
        get => GetValue(SelfMessageWidthProperty);
        set => SetValue(SelfMessageWidthProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="SelfMessageHeight"/> property.
    /// </summary>
    public static readonly StyledProperty<double> SelfMessageHeightProperty =
        AvaloniaProperty.Register<SequenceRenderer, double>(nameof(SelfMessageHeight), 20);

    /// <summary>
    /// Vertical height of the loop used for self messages.
    /// </summary>
    public double SelfMessageHeight
    {
        get => GetValue(SelfMessageHeightProperty);
        set => SetValue(SelfMessageHeightProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="ConditionBadgePaddingX"/> property.
    /// </summary>
    public static readonly StyledProperty<double> ConditionBadgePaddingXProperty =
        AvaloniaProperty.Register<SequenceRenderer, double>(nameof(ConditionBadgePaddingX), 8);

    /// <summary>
    /// Horizontal padding inside loop/alt/opt condition badges.
    /// </summary>
    public double ConditionBadgePaddingX
    {
        get => GetValue(ConditionBadgePaddingXProperty);
        set => SetValue(ConditionBadgePaddingXProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="ConditionBadgePaddingY"/> property.
    /// </summary>
    public static readonly StyledProperty<double> ConditionBadgePaddingYProperty =
        AvaloniaProperty.Register<SequenceRenderer, double>(nameof(ConditionBadgePaddingY), 4);

    /// <summary>
    /// Vertical padding inside loop/alt/opt condition badges.
    /// </summary>
    public double ConditionBadgePaddingY
    {
        get => GetValue(ConditionBadgePaddingYProperty);
        set => SetValue(ConditionBadgePaddingYProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="AutoNumberBadgeRadius"/> property.
    /// </summary>
    public static readonly StyledProperty<double> AutoNumberBadgeRadiusProperty =
        AvaloniaProperty.Register<SequenceRenderer, double>(nameof(AutoNumberBadgeRadius), 11);

    /// <summary>
    /// Radius of auto-number badges drawn beside sequence messages.
    /// </summary>
    public double AutoNumberBadgeRadius
    {
        get => GetValue(AutoNumberBadgeRadiusProperty);
        set => SetValue(AutoNumberBadgeRadiusProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="ActorIconSize"/> property.
    /// </summary>
    public static readonly StyledProperty<double> ActorIconSizeProperty =
        AvaloniaProperty.Register<SequenceRenderer, double>(nameof(ActorIconSize), 24);

    /// <summary>
    /// Natural size of the stick-figure actor icon before it is scaled to the layout box.
    /// </summary>
    public double ActorIconSize
    {
        get => GetValue(ActorIconSizeProperty);
        set => SetValue(ActorIconSizeProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="ParticipantCornerRadius"/> property.
    /// </summary>
    public static readonly StyledProperty<double> ParticipantCornerRadiusProperty =
        AvaloniaProperty.Register<SequenceRenderer, double>(nameof(ParticipantCornerRadius), 6);

    /// <summary>
    /// Corner radius for participant boxes.
    /// </summary>
    public double ParticipantCornerRadius
    {
        get => GetValue(ParticipantCornerRadiusProperty);
        set => SetValue(ParticipantCornerRadiusProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="BlockCornerRadius"/> property.
    /// </summary>
    public static readonly StyledProperty<double> BlockCornerRadiusProperty =
        AvaloniaProperty.Register<SequenceRenderer, double>(nameof(BlockCornerRadius), 8);

    /// <summary>
    /// Corner radius for sequence blocks and boxes.
    /// </summary>
    public double BlockCornerRadius
    {
        get => GetValue(BlockCornerRadiusProperty);
        set => SetValue(BlockCornerRadiusProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="BlockTabHeight"/> property.
    /// </summary>
    public static readonly StyledProperty<double> BlockTabHeightProperty =
        AvaloniaProperty.Register<SequenceRenderer, double>(nameof(BlockTabHeight), 18);

    /// <summary>
    /// Height of the small block type tab in loop/alt/opt/par regions.
    /// </summary>
    public double BlockTabHeight
    {
        get => GetValue(BlockTabHeightProperty);
        set => SetValue(BlockTabHeightProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="SmallCornerRadius"/> property.
    /// </summary>
    public static readonly StyledProperty<double> SmallCornerRadiusProperty =
        AvaloniaProperty.Register<SequenceRenderer, double>(nameof(SmallCornerRadius), 4);

    /// <summary>
    /// Small radius used for activation bars and compact condition badges.
    /// </summary>
    public double SmallCornerRadius
    {
        get => GetValue(SmallCornerRadiusProperty);
        set => SetValue(SmallCornerRadiusProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="NoteCornerRadius"/> property.
    /// </summary>
    public static readonly StyledProperty<double> NoteCornerRadiusProperty =
        AvaloniaProperty.Register<SequenceRenderer, double>(nameof(NoteCornerRadius), 6);

    /// <summary>
    /// Corner radius for sequence notes.
    /// </summary>
    public double NoteCornerRadius
    {
        get => GetValue(NoteCornerRadiusProperty);
        set => SetValue(NoteCornerRadiusProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="DestroyMarkerSize"/> property.
    /// </summary>
    public static readonly StyledProperty<double> DestroyMarkerSizeProperty =
        AvaloniaProperty.Register<SequenceRenderer, double>(nameof(DestroyMarkerSize), 12);

    /// <summary>
    /// Half-size of the cross drawn for destroy markers.
    /// </summary>
    public double DestroyMarkerSize
    {
        get => GetValue(DestroyMarkerSizeProperty);
        set => SetValue(DestroyMarkerSizeProperty, value);
    }

    private readonly record struct Style(
        double ArrowSize,
        double SelfMessageWidth,
        double SelfMessageHeight,
        double ConditionBadgePaddingX,
        double ConditionBadgePaddingY,
        double AutoNumberBadgeRadius,
        double ActorIconSize,
        double ParticipantCornerRadius,
        double BlockCornerRadius,
        double BlockTabHeight,
        double SmallCornerRadius,
        double NoteCornerRadius,
        double DestroyMarkerSize
    );

    /// <summary>
    /// Draws a positioned sequence diagram.
    /// </summary>
    public static void Render(DrawingContext dc, MermaidPresenter presenter, PositionedSequenceDiagram diagram)
    {
        new SequenceRenderer().RenderDiagram(dc, presenter, diagram);
    }

    /// <summary>
    /// Draws a positioned sequence diagram using this renderer part's current styled values.
    /// </summary>
    internal void RenderDiagram(DrawingContext dc, MermaidPresenter presenter, PositionedSequenceDiagram diagram)
    {
        var style = CreateStyleSnapshot();
        foreach (var box in diagram.Boxes)
        {
            DrawBox(dc, presenter, style, box);
        }

        foreach (var block in diagram.Blocks)
        {
            DrawBlock(dc, presenter, style, block);
        }

        foreach (var lifeline in diagram.Lifelines)
        {
            DrawLifeline(dc, presenter, lifeline);
        }

        foreach (var activation in diagram.Activations)
        {
            DrawActivation(dc, presenter, style, activation);
        }

        foreach (var message in diagram.Messages)
        {
            DrawMessage(dc, presenter, style, message);
        }

        foreach (var note in diagram.Notes)
        {
            DrawNote(dc, presenter, style, note);
        }

        foreach (var actor in diagram.Actors)
        {
            DrawActor(dc, presenter, style, actor);
        }

        if (diagram.Actors.Count > 0)
        {
            var actorHeight = diagram.Actors[0].Height;
            var bottomActorY = diagram.Height - actorHeight - 30;
            foreach (var actor in diagram.Actors)
            {
                DrawActor(dc, presenter, style, actor with { Y = bottomActorY });
            }
        }

        foreach (var marker in diagram.DestroyMarkers)
        {
            DrawDestroyMarker(dc, presenter, style, marker);
        }
    }

    private Style CreateStyleSnapshot() =>
        new(
            ArrowSize,
            SelfMessageWidth,
            SelfMessageHeight,
            ConditionBadgePaddingX,
            ConditionBadgePaddingY,
            AutoNumberBadgeRadius,
            ActorIconSize,
            ParticipantCornerRadius,
            BlockCornerRadius,
            BlockTabHeight,
            SmallCornerRadius,
            NoteCornerRadius,
            DestroyMarkerSize);

    private static void DrawActor(DrawingContext dc, MermaidPresenter presenter, Style style, PositionedSequenceActor actor)
    {
        if (actor.Type == SequenceActorType.Actor)
        {
            DrawActorIcon(dc, presenter, style, actor);
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
            style.ParticipantCornerRadius,
            style.ParticipantCornerRadius);
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

    private static void DrawActorIcon(DrawingContext dc, MermaidPresenter presenter, Style style, PositionedSequenceActor actor)
    {
        if (presenter.LinePen is not { } pen)
        {
            return;
        }

        var scale = actor.Height / style.ActorIconSize * 0.9;
        var centerX = actor.X;
        var top = actor.Y + (actor.Height - style.ActorIconSize * scale) / 2;
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

    private static void DrawActivation(DrawingContext dc, MermaidPresenter presenter, Style style, Activation activation)
    {
        dc.DrawRectangle(
            presenter.NodeFill,
            presenter.NodePen,
            new Rect(activation.X, activation.TopY, activation.Width, activation.BottomY - activation.TopY),
            style.SmallCornerRadius,
            style.SmallCornerRadius);
    }

    private static void DrawMessage(DrawingContext dc, MermaidPresenter presenter, Style style, PositionedSequenceMessage message)
    {
        var pen = message.LineStyle == SequenceLineStyle.Dashed ? presenter.DottedLinePen : presenter.LinePen;
        if (pen is null)
        {
            return;
        }

        if (message.IsSelf)
        {
            DrawSelfMessage(dc, presenter, style, message, pen);
            return;
        }

        var from = new Point(message.X1, message.Y);
        var to = new Point(message.X2, message.Y);
        dc.DrawLine(pen, from, to);
        MermaidDrawingHelpers.DrawArrowHead(dc, presenter.ArrowFill, pen, from, to, style.ArrowSize, message.ArrowHead == SequenceArrowHead.Filled);
        if (message.Bidirectional)
        {
            MermaidDrawingHelpers.DrawArrowHead(
                dc,
                presenter.ArrowFill,
                pen,
                to,
                from,
                style.ArrowSize,
                message.ArrowHead == SequenceArrowHead.Filled);
        }

        var label = message.Label;
        DrawAutoNumberBadge(dc, presenter, style, message, ref label);
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
        Style style,
        PositionedSequenceMessage message,
        IPen pen)
    {
        var p1 = new Point(message.X1, message.Y);
        var p2 = new Point(message.X1 + style.SelfMessageWidth, message.Y);
        var p3 = new Point(message.X1 + style.SelfMessageWidth, message.Y + style.SelfMessageHeight);
        var p4 = new Point(message.X2, message.Y + style.SelfMessageHeight);

        dc.DrawLine(pen, p1, p2);
        dc.DrawLine(pen, p2, p3);
        dc.DrawLine(pen, p3, p4);
        MermaidDrawingHelpers.DrawArrowHead(dc, presenter.ArrowFill, pen, p3, p4, style.ArrowSize, message.ArrowHead == SequenceArrowHead.Filled);

        MermaidTextRenderer.DrawInlineText(
            dc,
            presenter,
            message.Label,
            isMarkdown: false,
            message.X1 + style.SelfMessageWidth + 8,
            message.Y + style.SelfMessageHeight / 2,
            presenter.EdgeLabelFontSize,
            presenter.SecondaryForeground,
            TextAlignment.Left,
            centerVertically: true);
    }

    private static void DrawAutoNumberBadge(
        DrawingContext dc,
        MermaidPresenter presenter,
        Style style,
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
        dc.DrawEllipse(presenter.Foreground, null, center, style.AutoNumberBadgeRadius, style.AutoNumberBadgeRadius);
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

    private static void DrawBlock(DrawingContext dc, MermaidPresenter presenter, Style style, PositionedSequenceBlock block)
    {
        dc.DrawRectangle(
            null,
            presenter.LinePen,
            new Rect(block.X, block.Y, block.Width, block.Height),
            style.BlockCornerRadius,
            style.BlockCornerRadius);

        var typeName = block.Type.ToString().ToLowerInvariant();
        var tabText = MermaidTextRenderer.CreateFormattedText(
            presenter,
            typeName,
            presenter.EdgeLabelFontSize,
            presenter.SecondaryForeground,
            TextAlignment.Left,
            FontWeight.SemiBold);
        var tabWidth = tabText.Width + 16;
        dc.DrawRectangle(
            presenter.GroupHeaderFill,
            presenter.LinePen,
            new Rect(block.X, block.Y, tabWidth, style.BlockTabHeight),
            style.NoteCornerRadius,
            style.NoteCornerRadius);
        dc.DrawText(tabText, new Point(block.X + 6, block.Y + (style.BlockTabHeight - tabText.Height) / 2));

        if (block.Label.Length > 0)
        {
            DrawConditionBadge(dc, presenter, style, block.Label, block.X + block.Width / 2, block.Y + style.BlockTabHeight / 2);
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
                DrawConditionBadge(dc, presenter, style, divider.Label, block.X + block.Width / 2, divider.Y + 14);
            }
        }
    }

    private static void DrawConditionBadge(DrawingContext dc, MermaidPresenter presenter, Style style, string label, double centerX, double centerY)
    {
        var text = MermaidTextRenderer.CreateFormattedText(
            presenter,
            label,
            presenter.EdgeLabelFontSize,
            presenter.EdgeLabelBackground,
            TextAlignment.Center,
            FontWeight.SemiBold);
        var width = text.Width + style.ConditionBadgePaddingX * 2;
        var height = text.Height + style.ConditionBadgePaddingY * 2;
        var rect = new Rect(centerX - width / 2, centerY - height / 2, width, height);

        dc.DrawRectangle(presenter.Foreground, null, rect, style.SmallCornerRadius, style.SmallCornerRadius);
        dc.DrawText(text, new Point(rect.X + (width - text.Width) / 2, rect.Y + (height - text.Height) / 2));
    }

    private static void DrawNote(DrawingContext dc, MermaidPresenter presenter, Style style, PositionedSequenceNote note)
    {
        dc.DrawRectangle(
            presenter.AccentFill,
            presenter.AccentPen,
            new Rect(note.X, note.Y, note.Width, note.Height),
            style.NoteCornerRadius,
            style.NoteCornerRadius);

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

    private static void DrawDestroyMarker(DrawingContext dc, MermaidPresenter presenter, Style style, PositionedDestroyMarker marker)
    {
        var pen = presenter.ThickLinePen ?? presenter.LinePen;
        if (pen is null)
        {
            return;
        }

        dc.DrawLine(
            pen,
            new Point(marker.X - style.DestroyMarkerSize, marker.Y - style.DestroyMarkerSize),
            new Point(marker.X + style.DestroyMarkerSize, marker.Y + style.DestroyMarkerSize));
        dc.DrawLine(
            pen,
            new Point(marker.X + style.DestroyMarkerSize, marker.Y - style.DestroyMarkerSize),
            new Point(marker.X - style.DestroyMarkerSize, marker.Y + style.DestroyMarkerSize));
    }

    private static void DrawBox(DrawingContext dc, MermaidPresenter presenter, Style style, PositionedSequenceBox box)
    {
        var fill = presenter.GetCachedBrush(box.Color, presenter.GroupFill);
        dc.DrawRectangle(
            fill,
            presenter.GroupPen,
            new Rect(box.X, box.Y, box.Width, box.Height),
            style.BlockCornerRadius,
            style.BlockCornerRadius);

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