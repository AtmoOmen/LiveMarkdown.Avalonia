using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Event arguments for changes in the ObservableStringBuilder.
/// </summary>
/// <param name="NewString">The new content of the string builder.</param>
/// <param name="StartIndex">The starting index of the change.</param>
/// <param name="Length">The length of the change.</param>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly record struct ObservableStringBuilderChangedEventArgs(string NewString, int StartIndex, int Length)
{
    private string DebuggerDisplay
    {
        get
        {
            var endIndex = StartIndex + Length;
            return $"[{StartIndex}..{endIndex}]{NewString[StartIndex..endIndex]}";
        }
    }
}

/// <summary>
/// A delegate for handling changes in the ObservableStringBuilder.
/// </summary>
public delegate void ObservableStringBuilderChangedEventHandler(in ObservableStringBuilderChangedEventArgs e);

/// <summary>
/// A string builder that raises events when its content changes.
/// </summary>
/// <remarks>
/// This is **not** thread-safe!
/// If used for <see cref="MarkdownRenderer"/>, you must ensure that all changes are made on the UI thread.
/// </remarks>
public class ObservableStringBuilder : INotifyPropertyChanged
{
    /// <summary>
    /// Raised when a property changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the current length of the string builder.
    /// </summary>
    public int Length { get; private set; }

    /// <summary>
    /// Raised when the content of the string builder changes.
    /// </summary>
    public event ObservableStringBuilderChangedEventHandler? Changed;

    private readonly StringBuilder stringBuilder = new();

    /// <summary>
    /// Appends a string to the string builder and raises an event.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public ObservableStringBuilder Append(string? value)
    {
        if (string.IsNullOrEmpty(value)) return this;
        stringBuilder.Append(value);
        UpdateLength();
        Changed?.Invoke(
            new ObservableStringBuilderChangedEventArgs(
                ToString(),
                // ReSharper disable once RedundantSuppressNullableWarningExpression
                stringBuilder.Length - value!.Length,
                value.Length));
        return this;
    }

    /// <summary>
    /// Appends a string followed by a newline to the string builder and raises an event.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public ObservableStringBuilder AppendLine(string? value = null)
    {
        if (string.IsNullOrEmpty(value)) return this;
        stringBuilder.AppendLine(value);
        UpdateLength();
        Changed?.Invoke(
            new ObservableStringBuilderChangedEventArgs(
                ToString(),
                // ReSharper disable once RedundantSuppressNullableWarningExpression
                stringBuilder.Length - value!.Length - Environment.NewLine.Length,
                value.Length + Environment.NewLine.Length));
        return this;
    }

    /// <summary>
    /// Clears the string builder and raises an event with the previous content.
    /// </summary>
    /// <returns></returns>
    public ObservableStringBuilder Clear()
    {
        var length = stringBuilder.Length;
        stringBuilder.Clear();
        UpdateLength();
        Changed?.Invoke(
            new ObservableStringBuilderChangedEventArgs(
                string.Empty,
                0,
                length));
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString()
    {
        return stringBuilder.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void UpdateLength()
    {
        if (Length == stringBuilder.Length) return;

        Length = stringBuilder.Length;
        OnPropertyChanged(nameof(Length));
    }
}