using System.Reflection;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;
using Markdig.Syntax;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Represents a Markdown text block that can be rendered and interacted with.
/// This class extends <see cref="SelectableTextBlock"/> to fix its selection bugs.
/// </summary>
public class MarkdownTextBlock : SelectableTextBlock
{
    public SourceSpan SourceSpan { get; internal set; }

    public string ActualText
    {
        get
        {
            if (Inlines is not { Count: > 0 } inlines) return Text ?? string.Empty;

            var stringBuilder = new StringBuilder();
            foreach (var inline in inlines) AppendInline(inline);
            return stringBuilder.ToString();

            void AppendInline(Inline inline)
            {
                switch (inline)
                {
                    case Run run:
                    {
                        stringBuilder.Append(run.Text);
                        break;
                    }
                    case Span span:
                    {
                        foreach (var childInline in span.Inlines) AppendInline(childInline);
                        break;
                    }
                    case LineBreak:
                    {
                        stringBuilder.Append(Environment.NewLine);
                        break;
                    }
                    case InlineUIContainer { Child: { } logicalChild }:
                    {
                        AppendLogicalText(logicalChild);
                        break;
                    }
                }
            }

            void AppendLogicalText(ILogical logical)
            {
                if (logical is MarkdownTextBlock markdownTextBlock)
                {
                    stringBuilder.Append(markdownTextBlock.ActualText);
                    return; // markdownTextBlock.ActualText will handle its own inlines
                }

                foreach (var child in logical.LogicalChildren) AppendLogicalText(child);
            }
        }
    }

    public string ActualSelectedText
    {
        get
        {
            var selectionStart = SelectionStart;
            var selectionEnd = SelectionEnd;
            (selectionStart, selectionEnd) = (Math.Min(selectionStart, selectionEnd), Math.Max(selectionStart, selectionEnd));

            var stringBuilder = new StringBuilder();
            var currentIndex = 0;
            if (Inlines is not { Count: > 0 } inlines)
            {
                AppendText(Text);
            }
            else
            {
                foreach (var inline in inlines) AppendInline(inline);
            }

            return stringBuilder.ToString();

            void AppendInline(Inline inline)
            {
                switch (inline)
                {
                    case Run run:
                    {
                        AppendText(run.Text);
                        break;
                    }
                    case Span span:
                    {
                        foreach (var childInline in span.Inlines) AppendInline(childInline);
                        return;
                    }
                    case LineBreak:
                    {
                        AppendText(Environment.NewLine);
                        break;
                    }
                    case InlineUIContainer { Child: { } logicalChild }:
                    {
                        AppendLogicalText(logicalChild);
                        return;
                    }
                    default:
                    {
                        return;
                    }
                }
            }

            void AppendText(string? text)
            {
                if (currentIndex >= selectionEnd)
                {
                    // Already passed the selection range
                    return;
                }

                text ??= string.Empty;

                if (currentIndex + text.Length <= selectionStart)
                {
                    // This run is before the selection range
                    currentIndex += text.Length;
                    return;
                }

                var start = Math.Max(selectionStart - currentIndex, 0);
                var end = Math.Min(selectionEnd - currentIndex, text.Length);
                stringBuilder.Append(text[start..end]);
                currentIndex += text.Length;
            }

            void AppendLogicalText(ILogical logical)
            {
                if (logical is MarkdownTextBlock textBlock)
                {
                    var actualText = textBlock.ActualText;
                    var actualSelectedText = textBlock.ActualSelectedText;

                    if (actualText.Equals(actualSelectedText, StringComparison.Ordinal))
                    {
                        selectionEnd += actualText.Length - 1;
                    }
                    else if (actualText.StartsWith(actualSelectedText, StringComparison.Ordinal))
                    {
                        selectionEnd += actualText.Length - actualSelectedText.Length - 1;
                    }
                    else
                    {
                        selectionEnd += actualText.Length - 1;
                    }

                    if (actualText.EndsWith(actualSelectedText, StringComparison.Ordinal))
                    {
                        selectionStart += actualText.Length - actualSelectedText.Length - 1;
                    }

                    stringBuilder.Append(actualSelectedText);
                    currentIndex += actualText.Length;
                    return; // no need to traverse its children, because ActualSelectedText will handle that
                }

                foreach (var child in logical.LogicalChildren) AppendLogicalText(child);
            }
        }
    }

    /// <summary>
    /// Gets the length of the text content, counting inline elements escaped as single characters.
    /// </summary>
    public int EscapedTextLength
    {
        get
        {
            if (Text is { } text) return text.Length;
            if (Inlines is not { Count: > 0 } inlines) return 0;

            var length = 0;
            foreach (var inline in inlines) CalculateInlineLength(inline);
            return length;

            void CalculateInlineLength(Inline inline)
            {
                switch (inline)
                {
                    case Run run:
                    {
                        length += run.Text?.Length ?? 0;
                        break;
                    }
                    case Span span:
                    {
                        foreach (var childInline in span.Inlines) CalculateInlineLength(childInline);
                        break;
                    }
                    case LineBreak:
                    case InlineUIContainer:
                    {
                        length++;
                        break;
                    }
                }
            }
        }
    }

    static MarkdownTextBlock()
    {
        CopyingToClipboardEvent.AddClassHandler<MarkdownTextBlock>(
            async void (o, e) =>
            {
                try
                {
                    e.Handled = true;

                    if (TopLevel.GetTopLevel(o) is not { Clipboard: { } clipboard }) return;
                    var selectedText = o.ActualSelectedText;
                    if (!string.IsNullOrEmpty(selectedText)) await clipboard.SetTextAsync(selectedText);
                }
                catch
                {
                    // Ignore clipboard exceptions
                }
            },
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);

        if (ContextFlyout is not { IsOpen: true } && ContextMenu is not { IsOpen: true })
        {
            ClearSelection();
        }
    }

    // As a workaround to fix selection rendering bugs in SelectableTextBlock,
    // we override CreateTextLayout methods to handle selection rendering ourselves.
    private readonly PropertyInfo _lineSpacingPropertyInfo =
        typeof(TextParagraphProperties).GetProperty("LineSpacing", BindingFlags.Instance | BindingFlags.NonPublic)!;

    protected override TextLayout CreateTextLayout(string? text)
    {
        if (_textRuns is null) return base.CreateTextLayout(text);

        var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);

        var defaultProperties = new GenericTextRunProperties(
            typeface,
            FontFeatures,
            FontSize,
            TextDecorations,
            Foreground);

        var paragraphProperties = new GenericTextParagraphProperties(
            FlowDirection,
            TextAlignment,
            true,
            false,
            defaultProperties,
            TextWrapping,
            LineHeight,
            0,
            LetterSpacing);
        _lineSpacingPropertyInfo.SetValue(paragraphProperties, LineSpacing);

        List<ValueSpan<TextRunProperties>>? textStyleOverrides = null;
        var selectionStart = SelectionStart;
        var selectionEnd = SelectionEnd;
        var start = Math.Min(selectionStart, selectionEnd);
        var length = Math.Max(selectionStart, selectionEnd) - start;

        if (length > 0 && SelectionForegroundBrush != null)
        {
            // This is a fixture to apply selection foreground color.
            // Original implementation in SelectableTextBlock has some bugs that
            // cause incorrect rendering when selection and SelectionForegroundBrush exists.
            // It overrides original typeface, fontFeatures and fontSize which causes issues like missing italic/bold style.

            var accumulatedLength = 0;
            foreach (var textRun in _textRuns)
            {
                var runLength = textRun.Text.Length;
                if (accumulatedLength + runLength <= start ||
                    accumulatedLength >= start + length)
                {
                    accumulatedLength += runLength;
                    continue;
                }

                var overlapStart = Math.Max(start, accumulatedLength);
                var overlapEnd = Math.Min(start + length, accumulatedLength + runLength);
                var overlapLength = overlapEnd - overlapStart;

                textStyleOverrides ??= [];

                textStyleOverrides.Add(
                    new ValueSpan<TextRunProperties>(
                        overlapStart,
                        overlapLength,
                        new GenericTextRunProperties(
                            textRun.Properties?.Typeface ?? typeface,
                            textRun.Properties?.FontFeatures ?? FontFeatures,
                            FontSize,
                            foregroundBrush: SelectionForegroundBrush)));

                accumulatedLength += runLength;
            }
        }

        var textSource = new InlinesTextSource(_textRuns, textStyleOverrides);
        var maxWidth = double.IsNaN(_constraint.Width) ? 0.0 : _constraint.Width;
        var maxHeight = double.IsNaN(_constraint.Height) ? 0.0 : _constraint.Height;
        var maxSize = new Size(maxWidth, maxHeight);

        return new TextLayout(
            textSource,
            paragraphProperties,
            TextTrimming,
            maxSize.Width,
            maxSize.Height,
            MaxLines);
    }

    protected override void RenderTextLayout(DrawingContext context, Point origin)
    {
        var selectionStart = SelectionStart;
        var selectionEnd = SelectionEnd;
        var selectionBrush = SelectionBrush;

        if (selectionStart != selectionEnd && selectionBrush != null)
        {
            var start = Math.Min(selectionStart, selectionEnd);
            var length = Math.Max(selectionStart, selectionEnd) - start;

            using (context.PushTransform(Matrix.CreateTranslation(origin)))
            {
                foreach (var rect in TextLayoutHitTestTextRange(start, length))
                {
                    context.FillRectangle(selectionBrush, PixelRect.FromRect(rect, 1).ToRect(1));
                }
            }
        }

        TextLayout.Draw(context, origin);
    }

    private IEnumerable<Rect> TextLayoutHitTestTextRange(int start, int length)
    {
        if (start + length <= 0) yield break;

        var currentY = 0d;
        foreach (var textLine in TextLayout.TextLines)
        {
            // Current line isn't covered.
            if (textLine.FirstTextSourceIndex + textLine.Length <= start)
            {
                currentY += textLine.Height;
                continue;
            }

            var textBounds = textLine.GetTextBounds(start, length);
            if (textBounds.Count > 0)
            {
                Rect? last = null;
                foreach (var bounds in textBounds)
                {
                    if (last.HasValue &&
#pragma warning disable CS0618 // MathUtilities is obsolete, but still works
                        MathUtilities.AreClose(last.Value.Right, bounds.Rectangle.Left) &&
                        MathUtilities.AreClose(last.Value.Top, currentY))
#pragma warning restore CS0618 // MathUtilities is obsolete, but still works
                    {
                        last = last.Value.WithWidth(last.Value.Width + bounds.Rectangle.Width);
                    }
                    else
                    {
                        if (last.HasValue) yield return last.Value;
                        last = bounds.Rectangle.WithY(currentY);
                    }

                    foreach (var runBounds in bounds.TextRunBounds)
                    {
                        start += runBounds.Length;
                        length -= runBounds.Length;
                    }
                }

                if (last.HasValue) yield return last.Value;
            }

            if (textLine.FirstTextSourceIndex + textLine.Length >= start + length) break;
            currentY += textLine.Height;
        }
    }

    protected override void OnMeasureInvalidated()
    {
        var textRuns = _textRuns;
        base.OnMeasureInvalidated();
        _textRuns = textRuns;
    }

    public new void SelectAll()
    {
        SetCurrentValue(SelectionStartProperty, 0);
        SetCurrentValue(SelectionEndProperty, EscapedTextLength);
    }
}