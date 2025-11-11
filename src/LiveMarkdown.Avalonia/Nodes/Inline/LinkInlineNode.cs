using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// A node that represents a link inline.
/// </summary>
public class LinkInlineNode() : InlinesNode(
    new InlineHyperlink
    {
        Classes = { "Link" }
    })
{
    protected override bool IsCompatible(MarkdownObject markdownObject)
    {
        return markdownObject.GetType() == typeof(LinkInline);
    }

    protected override bool UpdateCore(
        DocumentNode documentNode,
        MarkdownObject markdownObject,
        in ObservableStringBuilderChangedEventArgs change,
        CancellationToken cancellationToken)
    {
        var linkInline = Unsafe.As<LinkInline>(markdownObject);
        if (linkInline.Url == null) return false;

        var inlineHyperlink = Unsafe.As<InlineHyperlink>(Inline);
        Uri.TryCreate(linkInline.Url, UriKind.RelativeOrAbsolute, out var uri);

        if (linkInline.IsImage)
        {
            Image img;
            if (inlineHyperlink.Image is { } image)
            {
                img = image;
            }
            else
            {
                inlineHyperlink.Image = img = new Image
                {
                    Classes = { "Link" },
                };
            }

            if (uri is { IsAbsoluteUri: false })
            {
                if (documentNode.Owner.ImageBasePath is { } imageBasePath)
                {
                    // If the URL is a relative path, combine it with the base path
                    Uri.TryCreate(Path.GetFullPath(Path.Combine(imageBasePath, linkInline.Url)), UriKind.Absolute, out uri);
                }
                else
                {
                    // If no base path is set, set the URI to null, preventing unexpected behavior
                    uri = null;
                }
            }

            inlineHyperlink.HRef = uri;
            AsyncImageLoader.SetSource(img, uri?.ToString());
        }
        else
        {
            inlineHyperlink.HRef = uri;
            inlineHyperlink.Image = null;
            base.UpdateCore(documentNode, markdownObject, change, cancellationToken);
        }

        return true;
    }
}