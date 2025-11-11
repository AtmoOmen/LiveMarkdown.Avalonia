using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// A node that represents an inline that is not yet implemented.
/// </summary>
/// <param name="markdownInline"></param>
public class NotImplementedInlineNode(Inline markdownInline) : InlineNode
{
    public override global::Avalonia.Controls.Documents.Inline Inline { get; } = new global::Avalonia.Controls.Documents.Run
    {
        Classes = { "NotImplementedInline" }
    };

    protected override bool IsCompatible(MarkdownObject markdownObject)
    {
        return markdownObject.GetType() == markdownInline.GetType();
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