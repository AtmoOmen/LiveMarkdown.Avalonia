using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace LiveMarkdown.Avalonia;

public class LiteralInlineNode : InlineNode
{
    public override global::Avalonia.Controls.Documents.Inline Inline { get; }

    private readonly global::Avalonia.Controls.Documents.Run run;

    public LiteralInlineNode()
    {
        Inline = run = new global::Avalonia.Controls.Documents.Run
        {
            Classes = { "Literal" }
        };
    }

    protected override bool IsCompatible(MarkdownObject markdownObject)
    {
        return markdownObject.GetType() == typeof(LiteralInline);
    }

    protected override bool UpdateCore(
        DocumentNode documentNode,
        MarkdownObject markdownObject,
        in ObservableStringBuilderChangedEventArgs change,
        CancellationToken cancellationToken)
    {
        var literal = (LiteralInline)markdownObject;
        run.Text = literal.Content.ToString();
        return true;
    }
}