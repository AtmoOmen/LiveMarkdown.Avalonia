using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace LiveMarkdown.Avalonia;

public class HtmlEntityInlineNode : InlineNode
{
    public override global::Avalonia.Controls.Documents.Inline Inline { get; }

    private readonly global::Avalonia.Controls.Documents.Run run;

    public HtmlEntityInlineNode()
    {
        Inline = run = new global::Avalonia.Controls.Documents.Run
        {
            Classes = { "HtmlEntity" }
        };
    }

    protected override bool IsCompatible(MarkdownObject markdownObject)
    {
        return markdownObject.GetType() == typeof(HtmlEntityInline);
    }

    protected override bool UpdateCore(
        DocumentNode documentNode,
        MarkdownObject markdownObject,
        in ObservableStringBuilderChangedEventArgs change,
        CancellationToken cancellationToken)
    {
        var htmlEntity = (HtmlEntityInline)markdownObject;
        run.Text = htmlEntity.Transcoded.ToString();
        return true;
    }
}