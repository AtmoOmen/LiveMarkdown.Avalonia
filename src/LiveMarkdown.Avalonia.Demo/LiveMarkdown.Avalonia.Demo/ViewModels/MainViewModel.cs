using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;
using TextMateSharp.Grammars;

namespace LiveMarkdown.Avalonia.Demo.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public ObservableStringBuilder MarkdownBuilder { get; } = new();

    public ObservableCollection<NavigationBarItem> NavigationItems { get; } = [];

    [ObservableProperty]
    public partial string? RawMarkdownText { get; private set; }

    [ObservableProperty]
    public partial double RenderSpeed { get; set; } = 30d;

    [ObservableProperty]
    public partial bool IsSidebarExpanded { get; set; } = true;

    public string? SelectedMarkdown { get; private set; }

    public NavigationBarItem? SelectedItem
    {
        get;
        set
        {
            if (!SetProperty(ref field, value)) return;

            if (value?.Content is string markdownName)
            {
                SelectedMarkdown = markdownName;
                _ = RenderMarkdownAsync(markdownName, animate: false);
            }
        }
    }

    public ThemeName[] AvailableColorThemes { get; } = Enum.GetValues<ThemeName>();

    [ObservableProperty]
    public partial ThemeName SelectedColorTheme { get; set; }

    public event EventHandler<bool>? AutoScrollEnabledChanged;

    private CancellationTokenSource? cancellationTokenSource;

    public MainViewModel()
    {
        // We don't use embedded resources here to allow easy modification of sample files.
        var markdownFolderPath = Path.Combine(AppContext.BaseDirectory, "samples");
        foreach (var markdownFilePath in Directory.EnumerateFiles(markdownFolderPath, "*.md")
                     .OrderByDescending(path => path.EndsWith("README.md", StringComparison.OrdinalIgnoreCase))
                     .ThenBy(path => path))
        {
            var fileName = Path.GetFileNameWithoutExtension(markdownFilePath);
            NavigationItems.Add(
                new NavigationBarItem
                {
                    Content = fileName,
                    Route = fileName
                });
        }
        SelectedItem = NavigationItems.FirstOrDefault();
    }

    [RelayCommand]
    private static async Task OpenUriAsync(LinkClickedEventArgs args)
    {
        if (args.HRef is { IsAbsoluteUri: true, Scheme: "http" or "https" } url)
        {
            await LaunchUriAsync(url);
        }
    }

    private void ClearMarkdown()
    {
        RawMarkdownText = string.Empty;
        MarkdownBuilder.Clear();
    }

    [RelayCommand]
    private void ResetMarkdown()
    {
        if (!string.IsNullOrEmpty(SelectedMarkdown))
        {
            _ = RenderMarkdownAsync(SelectedMarkdown, animate: true);
        }
    }

    [RelayCommand]
    private static void ToggleTheme()
    {
        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = app.ActualThemeVariant == ThemeVariant.Light ? ThemeVariant.Dark : ThemeVariant.Light;
        }
    }

    private async Task RenderMarkdownAsync(string? markdownFileName, bool animate = true)
    {
        try
        {
            AutoScrollEnabledChanged?.Invoke(this, animate);

            if (cancellationTokenSource is not null)
                await cancellationTokenSource.CancelAsync();

            if (string.IsNullOrWhiteSpace(markdownFileName))
                return;

            var markdownFilePath = Path.Combine(AppContext.BaseDirectory, "samples", markdownFileName + ".md");
            if (!File.Exists(markdownFilePath)) return;

            ClearMarkdown();

            cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            if (!animate)
            {
                using var reader = new StreamReader(markdownFilePath);
                var fullText = await reader.ReadToEndAsync(cancellationToken);
                RawMarkdownText = fullText;
                MarkdownBuilder.Append(fullText);
                return;
            }

            async IAsyncEnumerable<string> ReadBlocksAsync()
            {
                var buffer = Array.Empty<char>();
                using var reader = new StreamReader(markdownFilePath);
                while (!cancellationToken.IsCancellationRequested)
                {
                    var speed = Math.Max((int)RenderSpeed, 1);
                    if (buffer.Length != speed)
                    {
                        // RenderSpeed can be changed dynamically, so adjust buffer size accordingly
                        buffer = new char[speed];
                    }

                    var readCount = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
                    if (readCount <= 0) break;

                    var newText = new string(buffer, 0, readCount);
                    RawMarkdownText += newText;
                    yield return newText;
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