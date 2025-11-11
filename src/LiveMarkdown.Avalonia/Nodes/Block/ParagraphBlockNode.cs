using System.Runtime.CompilerServices;
using Markdig.Syntax;

namespace LiveMarkdown.Avalonia;

public class ParagraphBlockNode : InlineCollectionNode
{
    public ParagraphBlockNode()
    {
        Classes.Add("ParagraphBlock");
    }

    protected override bool IsCompatible(MarkdownObject markdownObject)
    {
        return markdownObject.GetType() == typeof(ParagraphBlock);
    }

    protected override bool UpdateCore(
        DocumentNode documentNode,
        MarkdownObject markdownObject,
        in ObservableStringBuilderChangedEventArgs change,
        CancellationToken cancellationToken)
    {
        var paragraphBlock = Unsafe.As<ParagraphBlock>(markdownObject);
        return paragraphBlock.Inline is not null && base.UpdateCore(documentNode, paragraphBlock.Inline, change, cancellationToken);
    }
}