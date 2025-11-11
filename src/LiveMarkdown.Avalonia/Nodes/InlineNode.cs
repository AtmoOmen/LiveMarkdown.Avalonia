using Markdig.Extensions.TaskLists;
using Markdig.Syntax.Inlines;

namespace LiveMarkdown.Avalonia;

public abstract class InlineNode : MarkdownNode
{
    public abstract global::Avalonia.Controls.Documents.Inline Inline { get; }

    protected static InlineNode CreateInlineNode(
        DocumentNode documentNode,
        Inline inline,
        in ObservableStringBuilderChangedEventArgs change,
        CancellationToken cancellationToken)
    {
        InlineNode node = inline switch
        {
            LiteralInline => new LiteralInlineNode(),
            LineBreakInline => new LineBreakInlineNode(),
            AutolinkInline => new AutolinkInlineNode(),
            DelimiterInline => new DelimiterInlineNode(),
            TaskList => new TaskListNode(),
            HtmlEntityInline => new HtmlEntityInlineNode(),
            CodeInline => new CodeInlineNode(),
            LinkInline => new LinkInlineNode(),
            EmphasisInline => new EmphasisInlineNode(),
            ContainerInline => new ContainerInlineNode(),
            _ => new NotImplementedInlineNode(inline)
        };
        node.Update(documentNode, inline, change, cancellationToken);
        return node;
    }
}