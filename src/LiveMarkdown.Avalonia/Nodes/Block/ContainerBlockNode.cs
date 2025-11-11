using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Layout;
using Markdig.Extensions.Tables;
using Markdig.Syntax;

namespace LiveMarkdown.Avalonia;

public class ContainerBlockNode : BlockNode
{
    public override Control Control => control;

    protected Control control;

    protected readonly MarkdownRenderer.BlocksProxy proxy;

    public ContainerBlockNode()
    {
        var container = new StackPanel
        {
            Orientation = Orientation.Vertical
        };
        control = container;
        proxy = new MarkdownRenderer.BlocksProxy(container.Children);
    }

    protected override bool IsCompatible(MarkdownObject markdownObject)
    {
        return markdownObject is ContainerBlock and
            not MarkdownDocument and
            not QuoteBlock and
            not Table and
            not TableCell and
            not ListBlock;
    }

    protected override bool UpdateCore(
        DocumentNode documentNode,
        MarkdownObject markdownObject,
        in ObservableStringBuilderChangedEventArgs change,
        CancellationToken cancellationToken)
    {
        var containerBlock = Unsafe.As<ContainerBlock>(markdownObject);
        if (containerBlock.Count == 0) return false;

        var i = 0;
        for (; i < containerBlock.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var block = containerBlock[i];

            if (i < proxy.Count)
            {
                var oldNode = proxy[i];
                if (oldNode.Update(documentNode, block, change, cancellationToken)) continue;

                // if Update returned false, it means the block needs to be removed
                var newNode = CreateBlockNode(documentNode, block, change, cancellationToken);
                proxy[i] = newNode;
            }
            else
            {
                var newNode = CreateBlockNode(documentNode, block, change, cancellationToken);
                proxy.Add(newNode);
            }
        }

        for (var j = proxy.Count - 1; j >= i; j--)
        {
            cancellationToken.ThrowIfCancellationRequested();

            proxy.RemoveAt(j);
        }

        return true;
    }
}