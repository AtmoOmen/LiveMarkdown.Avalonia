using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace LiveMarkdown.Avalonia;

public class LineBreakInlineNode : InlineNode
{
    public override global::Avalonia.Controls.Documents.Inline Inline { get; } = new global::Avalonia.Controls.Documents.LineBreak
    {
        Classes = { "LineBreak" }
    };

    protected override bool IsCompatible(MarkdownObject markdownObject)
    {
        return markdownObject.GetType() == typeof(LineBreakInline);
    }

    protected override bool UpdateCore(
        DocumentNode documentNode,
        MarkdownObject markdownObject,
        in ObservableStringBuilderChangedEventArgs change,
        CancellationToken cancellationToken)
    {
        return true;
    }
}