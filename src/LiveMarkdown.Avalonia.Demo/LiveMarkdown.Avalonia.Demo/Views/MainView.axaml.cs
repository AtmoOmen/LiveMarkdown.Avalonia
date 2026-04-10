using Avalonia;
using Avalonia.Controls;
using LiveMarkdown.Avalonia.Demo.ViewModels;

namespace LiveMarkdown.Avalonia.Demo.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        MarkdownRenderer.ImageBasePath = Path.Combine(AppContext.BaseDirectory, "samples");

        var rawScrollHelper = new AutoScrollHelper(RawMarkdownTextBlockScrollViewer);
        var renderScrollHelper = new AutoScrollHelper(MarkdownRendererScrollViewer);

        MainViewModel viewModel;
        DataContext = viewModel = new MainViewModel();
        viewModel.AutoScrollEnabledChanged += OnAutoScrollEnabledChanged;

        void OnAutoScrollEnabledChanged(object? sender, bool enabled)
        {
            rawScrollHelper.IsEnabled = enabled;
            renderScrollHelper.IsEnabled = enabled;

            if (!enabled)
            {
                RawMarkdownTextBlockScrollViewer.Offset = Vector.Zero;
                MarkdownRendererScrollViewer.Offset = Vector.Zero;
            }
        }
    }
}

public class AutoScrollHelper
{
    private bool isAtEnd = true;

    public bool IsEnabled 
    { 
        get;
        set
        {
            field = value;
            if (value)
            {
                // Whenever enabled again (e.g. Reset), assume we are starting from top but want tracking
                isAtEnd = true;
            }
        }
    }

    public AutoScrollHelper(ScrollViewer scrollViewer)
    {
        scrollViewer.PropertyChanged += OnScrollViewerPropertyChanged;
    }

    private void OnScrollViewerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (!IsEnabled) return;

        if (sender is not ScrollViewer scrollViewer) return;

        if (e.Property != ScrollViewer.OffsetProperty &&
            e.Property != ScrollViewer.ViewportProperty &&
            e.Property != ScrollViewer.ExtentProperty) return;

        if (e.Property == ScrollViewer.OffsetProperty)
        {
            isAtEnd = ((Vector)e.NewValue!).Y >= scrollViewer.Extent.Height - scrollViewer.Viewport.Height;
        }

        if (isAtEnd)
        {
            scrollViewer.Offset = new Vector(scrollViewer.Offset.X, double.PositiveInfinity);
        }
    }
}