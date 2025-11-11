using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Reflection;

namespace LiveMarkdown.Avalonia.Demo.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public ObservableStringBuilder MarkdownBuilder { get; } = new();

    public ObservableCollection<string> MarkdownList { get; } = [];

    [ObservableProperty]
    public partial string? RawMarkdownText { get; private set; }

    [ObservableProperty]
    public partial double RenderSpeed { get; set; } = 30d;

    public string? SelectedMarkdown
    {
        get;
        set
        {
            if (!SetProperty(ref field, value)) return;

            _ = RenderMarkdownAsync(value);
        }
    }

    private CancellationTokenSource? cancellationTokenSource;

    public MainViewModel()
    {
        // We don't use embedded resources here to allow easy modification of sample files.
        var markdownFolderPath = Path.Combine(AppContext.BaseDirectory, "samples");
        foreach (var markdownFilePath in Directory.EnumerateFiles(markdownFolderPath, "*.md"))
        {
            MarkdownList.Add(Path.GetFileNameWithoutExtension(markdownFilePath));
        }
    }

    [RelayCommand]
    private async Task OpenUriAsync(InlineHyperlinkClickedEventArgs args)
    {
        if (args.HRef is { IsAbsoluteUri: true, Scheme: "http" or "https" } url)
        {
            await LaunchUriAsync(url);
        }
    }

    [RelayCommand]
    public void ClearMarkdown()
    {
        RawMarkdownText = string.Empty;
        MarkdownBuilder.Clear();
    }

    private async Task RenderMarkdownAsync(string? markdownFileName)
    {
        try
        {
            if (cancellationTokenSource is not null)
                await cancellationTokenSource.CancelAsync();

            if (string.IsNullOrWhiteSpace(markdownFileName))
                return;

            var markdownFilePath = Path.Combine(AppContext.BaseDirectory, "samples", markdownFileName + ".md");
            if (!File.Exists(markdownFilePath)) return;

            ClearMarkdown();

            cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith($"{markdownFileName}.md", StringComparison.OrdinalIgnoreCase));

            if (resourceName is null)
            {
                await Console.Error.WriteLineAsync($"Resource not found: {markdownFileName}.md");
                return;
            }

            async IAsyncEnumerable<string> ReadBlocksAsync()
            {
                var buffer = Array.Empty<char>();
                using var reader = new StreamReader(markdownFilePath);
                while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    var speed = Math.Max((int)RenderSpeed, 1);
                    if (buffer.Length != speed)
                    {
                        // RenderSpeed can be changed dynamically, so adjust buffer size accordingly
                        buffer = new char[speed];
                    }

                    var readCount = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
                    if (readCount > 0)
                    {
                        var newText = new string(buffer, 0, readCount);
                        RawMarkdownText += newText;
                        yield return newText;
                    }
                }
            }

            await MarkdownBuilder.EnumerateAppendAsync(ReadBlocksAsync(), TimeSpan.FromMilliseconds(100), cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Error rendering markdown: {ex.Message}");
        }
    }

    private async static Task LaunchUriAsync(Uri uri)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
        {
            var launcher = TopLevel.GetTopLevel(window)?.Launcher;
            if (launcher is not null)
            {
                await launcher.LaunchUriAsync(uri);
            }
        }

        if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime { MainView: { } mainView })
        {
            var launcher = TopLevel.GetTopLevel(mainView)?.Launcher;
            if (launcher is not null)
            {
                await launcher.LaunchUriAsync(uri);
            }
        }
    }
}