using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Diagnostics;

namespace LiveMarkdown.Avalonia.Demo;

public partial class MainWindow : Window
{
    public AvaloniaList<string> MarkdownList { get; } = [];

    public string? SelectedMarkdown
    {
        get;
        set => _ = RenderMarkdownAsync(field = value);
    }

    public ObservableStringBuilder MarkdownBuilder { get; } = new();

    private CancellationTokenSource? cancellationTokenSource;

    public MainWindow()
    {
        InitializeComponent();

        MarkdownRenderer.ImageBasePath = Path.Combine(AppContext.BaseDirectory, "samples");

        _ = new AutoScrollHelper(RawMarkdownTextBlockScrollViewer);
        _ = new AutoScrollHelper(MarkdownRendererScrollViewer);
    }

    [RelayCommand]
    private async Task OpenUriAsync(InlineHyperlinkClickedEventArgs args)
    {
        if (args.HRef is { IsAbsoluteUri: true, Scheme: "http" or "https" } url && GetTopLevel(this)?.Launcher is { } launcher)
        {
            await launcher.LaunchUriAsync(url);
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        var markdownFolderPath = Path.Combine(AppContext.BaseDirectory, "samples");
        foreach (var markdownFilePath in Directory.EnumerateFiles(markdownFolderPath, "*.md"))
        {
            MarkdownList.Add(Path.GetFileNameWithoutExtension(markdownFilePath));
        }
    }

    private async Task RenderMarkdownAsync(string? markdownFileName)
    {
        try
        {
            if (cancellationTokenSource is not null)
                await cancellationTokenSource.CancelAsync();

            if (markdownFileName is null) return;

            var markdownFilePath = Path.Combine(AppContext.BaseDirectory, "samples", markdownFileName + ".md");
            if (!File.Exists(markdownFilePath)) return;

            ClearMarkdown();

            cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var speed = Math.Max((int)RenderSpeedSlider.Value, 1);

            async IAsyncEnumerable<string> ReadBlocksAsync()
            {
                var buffer = new char[speed];
                using var reader = new StreamReader(markdownFilePath);
                while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    var readCount = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
                    if (readCount > 0)
                    {
                        var newText = new string(buffer, 0, readCount);
                        RawMarkdownTextBlock.Text += newText;
                        yield return newText;
                    }
                }
            }
            await MarkdownBuilder.EnumerateAppendAsync(ReadBlocksAsync(), TimeSpan.FromMilliseconds(100), cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Error rendering markdown: {ex.Message}");
        }
    }



    private void ClearMarkdown()
    {
        RawMarkdownTextBlock.Text = string.Empty;
        MarkdownBuilder.Clear();
    }

    private void HandleClearButtonClick(object? sender, RoutedEventArgs e) => ClearMarkdown();

}

public class AutoScrollHelper
{
    private bool isAtEnd = true;

    public AutoScrollHelper(ScrollViewer scrollViewer)
    {
        scrollViewer.PropertyChanged += OnScrollViewerPropertyChanged;
    }

    private void OnScrollViewerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
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