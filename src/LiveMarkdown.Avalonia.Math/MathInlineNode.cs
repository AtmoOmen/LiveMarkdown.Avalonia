using Avalonia.Controls;
using CSharpMath.Avalonia;
using Markdig.Extensions.Mathematics;
using AvaloniaDocs = Avalonia.Controls.Documents;

namespace LiveMarkdown.Avalonia;

public class MathInlineNode : InlineNode<MathInline>
{
    public override AvaloniaDocs.Inline Inline => _inlineUIContainer;

    private readonly AvaloniaDocs.InlineUIContainer _inlineUIContainer;
    private readonly MarkdownTextBlock _textBlock;
    private readonly MathView _mathView;

    public MathInlineNode()
    {
        _inlineUIContainer = new AvaloniaDocs.InlineUIContainer
        {
            Classes = { "Math" },
            Child = new Panel
            {
                Classes = { "Math" },
                Children =
                {
                    (_textBlock = new MarkdownTextBlock
                    {
                        Classes = { "Math" }
                    }),
                    (_mathView = new MathView
                    {
                        DisplayErrorInline = false,
                        Classes = { "Math" }
                    })
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

        if (_mathView.ErrorMessage is not null)
        {
            _mathView.IsVisible = false;

            _textBlock.Classes.Add("Error");
            _textBlock.Text = _mathView.LaTeX;
            _textBlock.IsVisible = true;
        }
        else
        {
            _textBlock.IsVisible = false;
            _mathView.IsVisible = true;
        }

        return true;
    }
}