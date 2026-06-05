using System.Collections.Concurrent;
using System.Text;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;
using TextMateSharp.Themes;
using FontStyle = Avalonia.Media.FontStyle;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Handles syntax highlighting for source code using TextMateSharp
/// and renders it into an Avalonia InlineCollection.
/// </summary>
public sealed class SyntaxHighlighting
{
    /// <summary>
    /// The axaml class name used to mark formatted runs.
    /// </summary>
    public const string FormattedClassName = "formatted";

    /// <summary>
    /// Checks if a Run has already been formatted.
    /// </summary>
    /// <param name="run"></param>
    /// <returns></returns>
    public static bool IsRunFormatted(Run run) => run.Classes.Contains(FormattedClassName);

    private readonly static RegistryOptions RegistryOptions;
    private readonly static Registry Registry;

    private readonly static Dictionary<string, WeakReference<SyntaxHighlighting>> LanguageCache = [];
    private readonly static Dictionary<ThemeName, ThemeCacheEntry> ThemeCache = [];

    private readonly IGrammar? _grammar;

    static SyntaxHighlighting()
    {
        // Initialize default registry options.
        // We only need to get a Registry from it, so ThemeName here is not used
        // This ensures that grammars are loaded only once and shared across instances.
        RegistryOptions = new RegistryOptions(default);
        Registry = new Registry(RegistryOptions);
    }

    /// <summary>
    /// Creates or retrieves a cached SyntaxHighlighting instance for the specified language.
    /// </summary>
    /// <param name="languageName"></param>
    /// <returns></returns>
    public static SyntaxHighlighting Create(string languageName)
    {
        lock (LanguageCache)
        {
            if (LanguageCache.TryGetValue(languageName, out var weakRef) && weakRef.TryGetTarget(out var cached)) return cached;

            var instance = new SyntaxHighlighting(languageName);
            LanguageCache[languageName] = new WeakReference<SyntaxHighlighting>(instance);
            return instance;
        }
    }

    private static ThemeCacheEntry GetThemeCacheEntry(ThemeName themeName)
    {
        lock (ThemeCache)
        {
            if (ThemeCache.TryGetValue(themeName, out var cacheEntry)) return cacheEntry;

            cacheEntry = new ThemeCacheEntry(themeName);
            ThemeCache[themeName] = cacheEntry;

            return cacheEntry;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyntaxHighlighting"/> class.
    /// </summary>
    /// <param name="languageName"></param>
    private SyntaxHighlighting(string languageName)
    {
        // Initialize the registry with the DarkPlus theme.
        // Get the scope name from the language name (e.g., "csharp" -> "source.cs") and load the grammar.
        var scopeName = RegistryOptions.GetScopeByLanguageId(languageName) ?? RegistryOptions.GetScopeByExtension('.' + languageName);
        if (scopeName == null) return;

        _grammar = Registry.LoadGrammar(scopeName);
    }

    /// <summary>
    /// Formats the source code and populates the InlineCollection with styled runs.
    /// </summary>
    public void FormatInlines(InlineCollection inlines, ThemeName themeName = ThemeName.DarkPlus)
    {
        if (_grammar is null) return;

        IStateStack? ruleStack = null;

        // Tokenize each line of the source code.
        for (var i = 0; i < inlines.Count; i++)
        {
            switch (inlines[i])
            {
                case Run { Text: { } line } run:
                {
                    var result = _grammar.TokenizeLine(line, ruleStack, TimeSpan.MaxValue);
                    ruleStack = result.RuleStack;
                    if (IsRunFormatted(run)) continue;

                    if (result.Tokens.Length == 1)
                    {
                        StyleRun(run, result.Tokens[0].Scopes, themeName);
                    }
                    else
                    {
                        // Create and style a Run for each token.
                        Span span;
                        inlines[i] = span = new Span();
                        foreach (var token in result.Tokens)
                        {
                            var text = line.Substring(token.StartIndex, Math.Min(token.EndIndex - token.StartIndex, line.Length - token.StartIndex));
                            run = new Run(text);
                            StyleRun(run, token.Scopes, themeName);
                            span.Inlines.Add(run);
                        }
                    }
                    break;
                }
                case Span { Inlines: { Count: > 0 } spanInlines }:
                {
                    var lineBuilder = new StringBuilder();
                    foreach (var run in spanInlines.OfType<Run>())
                    {
                        lineBuilder.Append(run.Text);
                    }

                    var result = _grammar.TokenizeLine(lineBuilder.ToString(), ruleStack, TimeSpan.MaxValue);
                    ruleStack = result.RuleStack;
                    continue;
                }
                default:
                {
                    continue;
                }
            }
        }
    }

    /// <summary>
    /// Applies styling to a Run based on the token's scopes and the current theme.
    /// </summary>
    /// <param name="run">The Run to style.</param>
    /// <param name="scopes">The scopes associated with the token.</param>
    /// <param name="themeName">The theme to use for styling.</param>
    private static void StyleRun(Run run, IList<string> scopes, ThemeName themeName)
    {
        if (!IsRunFormatted(run)) run.Classes.Add(FormattedClassName);

        var entry = GetThemeCacheEntry(themeName);
        var themeRules = entry.Theme.Match(scopes);

        var foregroundId = -1;
        var backgroundId = -1;
        var fontStyle = TextMateSharp.Themes.FontStyle.NotSet;

        // Determine the style from the matched theme rules.
        foreach (var themeRule in themeRules)
        {
            if (foregroundId == -1 && themeRule.foreground > 0)
                foregroundId = themeRule.foreground;

            if (backgroundId == -1 && themeRule.background > 0)
                backgroundId = themeRule.background;

            if (fontStyle == TextMateSharp.Themes.FontStyle.NotSet && themeRule.fontStyle > 0)
                fontStyle = themeRule.fontStyle;
        }

        // Apply foreground color.
        if (foregroundId != -1)
        {
            var colorStr = entry.Theme.GetColor(foregroundId);
            if (Color.TryParse(colorStr, out var color))
            {
                run.Foreground = entry.GetBrush(color);
            }
        }

        // Apply background color.
        if (backgroundId != -1)
        {
            var colorStr = entry.Theme.GetColor(backgroundId);
            if (Color.TryParse(colorStr, out var color))
            {
                run.Background = entry.GetBrush(color);
            }
        }

        // Apply font styles.
        if (fontStyle == TextMateSharp.Themes.FontStyle.NotSet) return;

        if ((fontStyle & TextMateSharp.Themes.FontStyle.Italic) != 0) run.FontStyle = FontStyle.Italic;
        if ((fontStyle & TextMateSharp.Themes.FontStyle.Bold) != 0) run.FontWeight = FontWeight.Bold;
        if ((fontStyle & TextMateSharp.Themes.FontStyle.Underline) != 0) ApplyDecoration(TextDecorations.Underline);
        if ((fontStyle & TextMateSharp.Themes.FontStyle.Strikethrough) != 0) ApplyDecoration(TextDecorations.Strikethrough);

        void ApplyDecoration(TextDecorationCollection decorations)
        {
            if (run.TextDecorations is null)
            {
                run.TextDecorations = decorations;
            }
            else
            {
                run.TextDecorations.AddRange(decorations);
            }
        }
    }

    /// <summary>
    /// A context for the TextMateSharp registry, theme, and options.
    /// Properties are cached for performance.
    /// </summary>
    private record ThemeCacheEntry
    {
        public Theme Theme { get; }

        private readonly ConcurrentDictionary<Color, SolidColorBrush> _colorBrushCache = new();

        public ThemeCacheEntry(ThemeName themeName)
        {
            Theme = Theme.CreateFromRawTheme(RegistryOptions.LoadTheme(themeName), RegistryOptions);
        }

        public SolidColorBrush GetBrush(Color color)
        {
            return _colorBrushCache.GetOrAdd(color, static c => new SolidColorBrush(c));
        }
    }
}