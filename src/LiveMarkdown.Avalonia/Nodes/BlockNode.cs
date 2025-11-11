using Avalonia.Controls;
using Markdig.Extensions.Tables;
using Markdig.Syntax;

namespace LiveMarkdown.Avalonia;

public abstract class BlockNode : MarkdownNode
{
    public abstract Control Control { get; }

    public Classes Classes => Control.Classes;

    protected static BlockNode CreateBlockNode(
        DocumentNode documentNode,
        Block block,
        in ObservableStringBuilderChangedEventArgs change,
        CancellationToken cancellationToken)
    {
        BlockNode node = block switch
        {
            Table => new TableNode(),
            TableCell => new TableCellNode(),
            ListBlock => new ListBlockNode(),
            Markdig.Syntax.CodeBlock => new CodeBlockNode(),
            QuoteBlock => new QuoteBlockNode(),
            HeadingBlock => new HeadingBlockNode(),
            ParagraphBlock => new ParagraphBlockNode(),
            ContainerBlock => new ContainerBlockNode(),
            ThematicBreakBlock => new ThematicBreakBlockNode(),
            _ => new NotImplementedBlockNode(block)
        };
        node.Update(documentNode, block, change, cancellationToken);
        return node;
    }
}