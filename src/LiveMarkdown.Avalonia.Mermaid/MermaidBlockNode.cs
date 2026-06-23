using Avalonia.Controls;

namespace LiveMarkdown.Avalonia;

public class MermaidBlockNode : BlockNode<MermaidCodeBlock>
{
    public override Control Control { get; }

    private readonly MarkdownTextBlock _textBlock;
    private readonly MermaidPresenter _presenter;

    public MermaidBlockNode()
    {
        Control = new Panel
        {
            Classes = { "MermaidBlock" },
            Children =
            {
                (_textBlock = new MarkdownTextBlock
                {
                    Classes = { "MermaidBlock" }
                }),
                new PanAndZoom
                {
                    Classes = { "MermaidBlock" },
                    Content = _presenter = new MermaidPresenter
                    {
                        Classes = { "MermaidBlock" }
                    }
                }
            }
        };
    }

    protected override bool UpdateCore(
        DocumentNode documentNode,
        MermaidCodeBlock mermaid,
        in ObservableStringBuilderChangedEventArgs change,
        CancellationToken cancellationToken)
    {
        if (mermaid.IsOpen)
        {
            _presenter.IsVisible = false;
            _textBlock.Text = mermaid.ToString();
            _textBlock.IsVisible = true;
        }
        else
        {
            _textBlock.IsVisible = false;
            _presenter.Text = mermaid.Lines.ToString();
            _presenter.IsVisible = true;
        }

        return true;
    }
}