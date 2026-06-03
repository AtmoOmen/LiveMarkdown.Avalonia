using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging;
using Avalonia.Markup.Xaml;
using LiveMarkdown.Avalonia.Demo.Views;

namespace LiveMarkdown.Avalonia.Demo;

public partial class App : Application, ILogSink
{
    public override void Initialize()
    {
        Logger.Sink = this;

        MarkdownRenderer.ConfigurePipeline += x => x
            .UseMermaid()
            .UseExtendedMathematics();
        MarkdownNode.Edit(builder => builder
            .Register<MathInlineNode>()
            .Register<MathBlockNode>()
            .Register<MermaidBlockNode>()
        );

        AsyncImageLoader.DefaultDecoders =
        [
            SvgImageDecoder.Shared,
            DefaultBitmapDecoder.Shared
        ];

        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainViewModel()
                };
                break;
            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = new MainViewModel()
                };
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToList();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    public bool IsEnabled(LogEventLevel level, string area) => area == nameof(MarkdownRenderer);

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
    {
        Console.WriteLine($"[{source}] {messageTemplate}");
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
    {
        var index = 0;
        var formattedMessage = LogTemplateRegex().Replace(
            messageTemplate,
            match => propertyValues.Length > index ? propertyValues[index++]?.ToString() ?? string.Empty : match.Value);
        Log(level, area, source, formattedMessage);
    }

    [GeneratedRegex(@"\{[^\}]+\}")]
    private static partial Regex LogTemplateRegex();
}