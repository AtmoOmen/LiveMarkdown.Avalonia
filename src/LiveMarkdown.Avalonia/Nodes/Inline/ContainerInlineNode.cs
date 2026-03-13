using Avalonia.Controls.Documents;
using Markdig.Syntax.Inlines;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// A node that represents a container inline (e.g., Span).
/// </summary>
public class ContainerInlineNode<TContainerInline> : InlinesNode<TContainerInline> where TContainerInline : ContainerInline
{
    public ContainerInlineNode() : base(new Span())
    {
    }

    public ContainerInlineNode(string className) : base(new Span { Classes = { className }})
    {
    }
}