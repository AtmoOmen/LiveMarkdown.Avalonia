using Avalonia.Controls;
using CSharpMath.Avalonia;
using Markdig.Extensions.Mathematics;

namespace LiveMarkdown.Avalonia;

public class MathBlockNode : BlockNode<MathBlock>
{
    public override Control Control { get; }

    private readonly TextBlock _textBlock;
    private readonly MathView _mathView;

    public MathBlockNode()
    {
        Control = new Panel
        {
            Classes = { "MathBlock" },
            Children =
            {
                (_textBlock = new TextBlock
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
        if (math.IsOpen)
        {
            _mathView.IsVisible = false;
            _textBlock.Text = math.ToString();
            _textBlock.IsVisible = true;
        }
        else
        {
            _textBlock.IsVisible = false;
            _mathView.LaTeX = math.Lines.ToString();
            _mathView.IsVisible = true;
        }

        return true;
    }
}