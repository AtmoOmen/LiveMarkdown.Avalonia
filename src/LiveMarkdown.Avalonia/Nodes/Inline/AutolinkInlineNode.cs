using Avalonia.Controls.Documents;
using Markdig.Syntax.Inlines;
using Inline = Avalonia.Controls.Documents.Inline;

namespace LiveMarkdown.Avalonia;

public sealed class AutolinkInlineNode : InlineNode<AutolinkInline>
{
    public override Inline Inline { get; }

    private readonly Link link;

    public AutolinkInlineNode()
    {
        Inline = link = new Link
        {
            Classes = { "AutoLink" }
        };
    }

    protected override bool UpdateCore(
        DocumentNode documentNode,
        AutolinkInline autolink,
        in ObservableStringBuilderChangedEventArgs change,
        CancellationToken cancellationToken)
    {
        Uri.TryCreate(autolink.Url, UriKind.RelativeOrAbsolute, out var uri);
        link.HRef = uri;

        if (link.Inlines is [Run run]) run.Text = autolink.Url;
        else
        {
            link.Inlines.Clear();
            link.Inlines.Add(
                new Run
                {
                    Classes = { "Autolink" },
                    Text = autolink.Url
                });
        }

        return true;
    }
}