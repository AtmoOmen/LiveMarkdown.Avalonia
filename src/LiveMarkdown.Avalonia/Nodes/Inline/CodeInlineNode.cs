using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// A node that represents a code inline.
/// </summary>
public class CodeInlineNode : InlineNode
{
    protected override MarkdownTextBlock TextBlock => textBlock;

    public override global::Avalonia.Controls.Documents.Inline Inline => inlineUIContainer;

    private readonly global::Avalonia.Controls.Documents.InlineUIContainer inlineUIContainer;
    private readonly MarkdownTextBlock textBlock;

    public CodeInlineNode()
    {
        inlineUIContainer = new global::Avalonia.Controls.Documents.InlineUIContainer
        {
            Classes = { "Code" },
            Child = new Border
            {
                Child = textBlock = new MarkdownTextBlock()
            }
        };
    }

    protected override bool IsCompatible(MarkdownObject markdownObject)
    {
        return markdownObject.GetType() == typeof(CodeInline);
    }

    protected override bool UpdateCore(
        DocumentNode documentNode,
        MarkdownObject markdownObject,
        in ObservableStringBuilderChangedEventArgs change,
        CancellationToken cancellationToken)
    {
        var code = Unsafe.As<CodeInline>(markdownObject);
        textBlock.Text = code.Content;
        return true;
    }
}