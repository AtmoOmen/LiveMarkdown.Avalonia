using Avalonia.Controls;
using Markdig.Syntax;

namespace LiveMarkdown.Avalonia;

public class QuoteBlockNode : ContainerBlockNode
{
    public QuoteBlockNode()
    {
        Classes.Add("QuoteBlock");
        control = new Border
        {
            Classes = { "QuoteBlock" },
            Child = base.Control
        };
    }

    protected override bool IsCompatible(MarkdownObject markdownObject)
    {
        return markdownObject.GetType() == typeof(QuoteBlock);
    }
}