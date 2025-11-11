using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Markdig.Syntax;

namespace LiveMarkdown.Avalonia;

public class HeadingBlockNode : BlockNode
{
    public override Control Control { get; }

    private readonly InlineCollectionNode headingInlines;

    public HeadingBlockNode()
    {
        headingInlines = new InlineCollectionNode();
        Control = new Border
        {
            Classes = { "HeadingBlock" },
            Child = headingInlines.Control
        };
    }

    protected override bool IsCompatible(MarkdownObject markdownObject)
    {
        return markdownObject.GetType() == typeof(HeadingBlock);
    }

    protected override bool UpdateCore(
        DocumentNode documentNode,
        MarkdownObject markdownObject,
        in ObservableStringBuilderChangedEventArgs change,
        CancellationToken cancellationToken)
    {
        var headingBlock = Unsafe.As<HeadingBlock>(markdownObject);
        if (headingBlock.Inline is null) return false;

        if (!headingInlines.Update(documentNode, headingBlock.Inline, change, cancellationToken)) return false;

        cancellationToken.ThrowIfCancellationRequested();
        headingInlines.Classes.EnsureClassName("Heading", headingBlock.Level);
        return true;
    }
}