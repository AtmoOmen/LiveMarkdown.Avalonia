using Avalonia.Controls;
using CSharpMath.Avalonia;
using Markdig.Extensions.Mathematics;

namespace LiveMarkdown.Avalonia;

public class MathBlockNode : BlockNode<MathBlock>
{
    public override Control Control { get; }

    private readonly MarkdownTextBlock _textBlock;
    private readonly MathView _mathView;

    public MathBlockNode()
    {
        Control = new Panel
        {
            Classes = { "MathBlock" },
            Children =
            {
                (_textBlock = new MarkdownTextBlock
                {
                    Classes = { "MathBlock" }
                }),
                (_mathView = new MathView
                {
                    DisplayErrorInline = false,
                    Classes = { "MathBlock" }
                })
            }
        };
    }

    protected override bool UpdateCore(
        DocumentNode documentNode,
        MathBlock math,
        in ObservableStringBuilderChangedEventArgs change,
        CancellationToken cancellationToken)
    {
        if (math.IsOpen || _mathView.ErrorMessage is not null)
        {
            _mathView.IsVisible = false;

            if (_mathView.ErrorMessage is not null)
            {
                _textBlock.Classes.Add("Error");
            }
            else
            {
                _textBlock.Classes.Remove("Error");
            }

            _textBlock.Text = math.ToString();
            _textBlock.IsVisible = true;
        }
        else
        {
            _mathView.LaTeX = math.Lines.ToString();

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
        }

        return true;
    }
}