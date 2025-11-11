using Avalonia.Controls;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Works as <see cref="MarkdownTextBlock"/>
/// </summary>
public class InlineCollectionNode : BlockNode
{
    protected override MarkdownTextBlock TextBlock => textBlock;

    public override Control Control => textBlock;

    private readonly InlinesNode inlinesNode;
    private readonly MarkdownTextBlock textBlock;

    public InlineCollectionNode()
    {
        inlinesNode = new InlinesNode(new global::Avalonia.Controls.Documents.Span());
        textBlock = new MarkdownTextBlock
        {
            Inlines = inlinesNode.Inlines
        };
    }

    protected override bool IsCompatible(MarkdownObject markdownObject)
    {
        return markdownObject is IEnumerable<Inline>;
    }

    protected override bool UpdateCore(
        DocumentNode documentNode,
        MarkdownObject markdownObject,
        in ObservableStringBuilderChangedEventArgs change,
        CancellationToken cancellationToken)
    {
        return inlinesNode.Update(
            documentNode,
            markdownObject,
            change,
            cancellationToken);
    }
}