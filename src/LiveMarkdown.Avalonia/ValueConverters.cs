using Avalonia.Data.Converters;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Provides common value converters for Avalonia data binding.
/// </summary>
public static class ValueConverters
{
    /// <summary>
    /// Converts a string to an ObservableStringBuilder. If the string is null or empty, it returns null; otherwise, it creates a new ObservableStringBuilder and appends the string to it.
    /// </summary>
    public static IValueConverter ToObservableStringBuilder { get; } = new FuncValueConverter<string?, ObservableStringBuilder?>(
        convert: x => x is { Length: > 0 } ? new ObservableStringBuilder(x) : null
    );
}