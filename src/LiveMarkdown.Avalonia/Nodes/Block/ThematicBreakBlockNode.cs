using Avalonia.Controls;
using Markdig.Syntax;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// A node that represents a thematic break (horizontal rule).
/// </summary>
public class ThematicBreakBlockNode : BlockNode
{
    public override Control Control { get; } = new Border
    {
        Classes = { "ThematicBreak" }
    };

    protected override bool IsCompatible(MarkdownObject markdownObject)
    {
        return markdownObject.GetType() == typeof(ThematicBreakBlock);
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