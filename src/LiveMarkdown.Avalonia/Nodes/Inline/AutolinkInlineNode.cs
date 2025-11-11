using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace LiveMarkdown.Avalonia;

public sealed class AutolinkInlineNode : InlineNode
{
    public override global::Avalonia.Controls.Documents.Inline Inline { get; }

    private readonly InlineHyperlink inlineHyperlink;

    public AutolinkInlineNode()
    {
        Inline = inlineHyperlink = new InlineHyperlink
        {
            Classes = { "AutoLink" }
        };
    }

    protected override bool IsCompatible(MarkdownObject markdownObject)
    {
        return markdownObject is AutolinkInline;
    }

    protected override bool UpdateCore(
        DocumentNode documentNode,
        MarkdownObject markdownObject,
        in ObservableStringBuilderChangedEventArgs change,
        CancellationToken cancellationToken)
    {
        var autolink = (AutolinkInline)markdownObject;
        Uri.TryCreate(autolink.Url, UriKind.RelativeOrAbsolute, out var uri);
        inlineHyperlink.HRef = uri;

        if (inlineHyperlink.Inlines is [global::Avalonia.Controls.Documents.Run run]) run.Text = autolink.Url;
        else
        {
            inlineHyperlink.Inlines.Clear();
            inlineHyperlink.Inlines.Add(
                new global::Avalonia.Controls.Documents.Run
                {
                    Classes = { "Autolink" },
                    Text = autolink.Url
                });
        }

        return true;
    }
}