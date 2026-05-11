using Avalonia.Controls;
using CSharpMath.Avalonia;
using Markdig.Extensions.Mathematics;
using AvaloniaDocs = Avalonia.Controls.Documents;

namespace LiveMarkdown.Avalonia;

public class MathInlineNode : InlineNode<MathInline>
{
    public override AvaloniaDocs.Inline Inline => _inlineUIContainer;

    private readonly AvaloniaDocs.InlineUIContainer _inlineUIContainer;
    private readonly MathView _mathView;

    public MathInlineNode()
    {
        _inlineUIContainer = new AvaloniaDocs.InlineUIContainer
        {
            Classes = { "Math" },
            Child = new Border
            {
                Child = _mathView = new MathView
                {
                    DisplayErrorInline = false,
                    Classes = { "Math" }
                }
            }
        };
    }

    protected override bool UpdateCore(
        DocumentNode documentNode,
        MathInline math,
        in ObservableStringBuilderChangedEventArgs change,
        CancellationToken cancellationToken)
    {
        _mathView.LaTeX = math.Content.ToString();
        return true;
    }
}