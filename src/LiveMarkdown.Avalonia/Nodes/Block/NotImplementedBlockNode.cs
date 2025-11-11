using Avalonia.Controls;
using Markdig.Syntax;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// A block node for markdown blocks that are not yet implemented.
/// </summary>
/// <param name="markdownBlock"></param>
public class NotImplementedBlockNode(Block markdownBlock) : BlockNode
{
    public override Control Control { get; } = new()
    {
        Classes = { "NotImplementedBlock" }
    };

    protected override bool IsCompatible(MarkdownObject markdownObject)
    {
        return markdownObject.GetType() == markdownBlock.GetType();
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