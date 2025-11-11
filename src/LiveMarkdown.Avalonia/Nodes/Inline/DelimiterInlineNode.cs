using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace LiveMarkdown.Avalonia;

public class DelimiterInlineNode : InlineNode
{
    public override global::Avalonia.Controls.Documents.Inline Inline { get; }

    private readonly global::Avalonia.Controls.Documents.Run run;

    public DelimiterInlineNode()
    {
        Inline = run = new global::Avalonia.Controls.Documents.Run
        {
            Classes = { "Delimiter" }
        };
    }

    protected override bool IsCompatible(MarkdownObject markdownObject)
    {
        return markdownObject.GetType() == typeof(DelimiterInline);
    }

    protected override bool UpdateCore(
        DocumentNode documentNode,
        MarkdownObject markdownObject,
        in ObservableStringBuilderChangedEventArgs change,
        CancellationToken cancellationToken)
    {
        var delimiter = (DelimiterInline)markdownObject;
        run.Text = delimiter.ToLiteral();
        return true;
    }
}