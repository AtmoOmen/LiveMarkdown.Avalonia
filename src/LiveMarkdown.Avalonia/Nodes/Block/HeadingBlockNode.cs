using Avalonia.Controls;
using Markdig.Syntax;

namespace LiveMarkdown.Avalonia;

public class HeadingBlockNode : BlockNode<HeadingBlock>
{
    public override Control Control { get; }

    private readonly InlineCollectionNode<HeadingBlock> headingInlines;

    public HeadingBlockNode()
    {
        headingInlines = new InlineCollectionNode<HeadingBlock>();
        Control = new Border
        {
            Child = headingInlines.Control
        };
    }

    protected override bool UpdateCore(
        DocumentNode documentNode,
        HeadingBlock headingBlock,
        in ObservableStringBuilderChangedEventArgs change,
        CancellationToken cancellationToken)
    {
        if (headingBlock.Inline is null) return false;

        var result = headingInlines.Update(documentNode, headingBlock, change, cancellationToken);
        switch (result)
        {
            case true:
            {
                cancellationToken.ThrowIfCancellationRequested();
                Control.Classes.EnsureClassName("Heading", $"{headingBlock.Level}Block");
                headingInlines.Control.Classes.EnsureClassName("Heading", headingBlock.Level);
                return true;
            }
            case false:
            {
                return false;
            }
            case null: // Not dirty
            {
                return true;
            }
        }
    }
}