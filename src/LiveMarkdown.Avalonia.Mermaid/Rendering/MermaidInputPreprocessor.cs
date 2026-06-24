using Mermaider.Models;
using Mermaider.Parsing;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Mermaid source after the SVG renderer-compatible preprocessing steps have run.
/// </summary>
/// <remarks>
/// Native rendering must not feed Mermaid frontmatter, <c>%%{init}%%</c>, or accessibility directives
/// directly into diagram parsers. This record keeps the filtered parser input together with metadata
/// that can later be mapped to Avalonia automation or optional theme support.
/// </remarks>
/// <param name="CleanedText">Source text after Mermaider's diagram preprocessor removes frontmatter and init directives.</param>
/// <param name="Lines">Trimmed parser lines for diagram types whose Mermaider parser expects normalized indentation.</param>
/// <param name="PreserveIndentLines">Parser lines that preserve indentation for diagram types where leading whitespace is semantic.</param>
/// <param name="Metadata">Metadata extracted from YAML frontmatter and <c>%%{init}%%</c> directives.</param>
/// <param name="Accessibility">Accessibility title and description extracted from <c>accTitle</c> and <c>accDescr</c>.</param>
internal sealed record MermaidInput(
    string CleanedText,
    string[] Lines,
    string[] PreserveIndentLines,
    DiagramMetadata Metadata,
    AccessibilityInfo Accessibility
);

/// <summary>
/// Applies the same frontmatter, init, line, and accessibility preprocessing used by Mermaider's
/// renderer path before native parsers are called.
/// </summary>
internal static class MermaidInputPreprocessor
{
    /// <summary>
    /// Produces parser-ready source and preserved metadata from raw Mermaid text.
    /// </summary>
    public static MermaidInput Process(string text)
    {
        var (cleaned, metadata) = DiagramPreprocessor.Process(text);
        var lines = Mermaider.MermaidRenderer.PreprocessLines(cleaned);
        var (accessibility, filteredLines) = AccessibilityParser.Extract(lines);

        return new MermaidInput(
            cleaned,
            filteredLines,
            Mermaider.MermaidRenderer.PreprocessLinesPreserveIndent(cleaned, accessibility),
            metadata,
            accessibility);
    }
}