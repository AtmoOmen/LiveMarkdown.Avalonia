using Markdig.Syntax;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// A node that represents the entire Markdown document (Root).
/// </summary>
public sealed class DocumentNode : ContainerBlockNode<MarkdownDocument>
{
    public MarkdownRenderer Owner { get; }

    public DocumentNode(MarkdownRenderer owner)
    {
        Owner = owner;
        Control.Classes.Add("MarkdownDocument");
    }

    /// <remarks>
    /// This method always returns true because the DocumentNode is the root node and should always be considered dirty when any change occurs in the document.
    /// </remarks>
    /// <param name="markdownObject"></param>
    /// <param name="change"></param>
    /// <returns></returns>
    protected override bool IsDirty(MarkdownObject markdownObject, in ObservableStringBuilderChangedEventArgs change) => true;

    protected override bool UpdateCore(
        DocumentNode documentNode,
        MarkdownDocument markdownObject,
        in ObservableStringBuilderChangedEventArgs change,
        CancellationToken cancellationToken)
    {
        var result = base.UpdateCore(documentNode, markdownObject, in change, cancellationToken);

        // (#1) DocumentNode is the outest node, so if it has no children, we clear the proxy
        if (!result) proxy.Clear();

        return result;
    }
}