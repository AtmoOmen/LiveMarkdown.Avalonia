using Avalonia;
using Markdig;
using NUnit.Framework;

namespace LiveMarkdown.Avalonia.Tests;

[TestFixture]
public class MermaidPresenterTests
{
    [Test]
    public void MermaidBlockParser_AcceptsMermaidAsFirstInfoToken()
    {
        var document = Markdown.Parse(
            """
            ```mermaid interactive
            graph TD
                A --> B
            ```
            """,
            new MarkdownPipelineBuilder().UseMermaid().Build());

        Assert.That(document[0], Is.TypeOf<MermaidCodeBlock>());
    }

    [Test]
    public void MermaidBlockParser_RejectsInfoTokenThatOnlyEndsWithMermaid()
    {
        var document = Markdown.Parse(
            """
            ```notmermaid
            graph TD
                A --> B
            ```
            """,
            new MarkdownPipelineBuilder().UseMermaid().Build());

        Assert.That(document[0], Is.Not.TypeOf<MermaidCodeBlock>());
    }

    [Test]
    public void MermaidInputPreprocessor_RemovesInitAndAccessibilityLines()
    {
        var input = MermaidInputPreprocessor.Process(
            """
            ---
            title: Frontmatter Title
            ---
            %%{init: {"theme": "github-dark", "title": "Init Title"}}%%
            graph TD
            accTitle: Accessible title
            accDescr: Accessible description
                A --> B
            """);

        Assert.That(input.Metadata.Title, Is.EqualTo("Init Title"));
        Assert.That(input.Metadata.Theme, Is.EqualTo("github-dark"));
        Assert.That(input.Accessibility.Title, Is.EqualTo("Accessible title"));
        Assert.That(input.Accessibility.Description, Is.EqualTo("Accessible description"));
        Assert.That(input.Lines, Does.Not.Contain("accTitle: Accessible title"));
        Assert.That(input.Lines, Does.Not.Contain("accDescr: Accessible description"));
        Assert.That(input.Lines[0], Is.EqualTo("graph TD"));
    }

    [Test]
    public void Measure_ClearsPreviousDiagramForEmptyText()
    {
        var presenter = new MermaidPresenter
        {
            Text = """
                   graph TD
                       A --> B
                   """
        };

        presenter.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Assert.That(presenter.DesiredSize.Width, Is.GreaterThan(0));
        Assert.That(presenter.DesiredSize.Height, Is.GreaterThan(0));

        presenter.Text = string.Empty;
        presenter.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        Assert.That(presenter.DesiredSize, Is.EqualTo(default(Size)));
    }

    [Test]
    public void Measure_InvalidDiagramDoesNotThrow()
    {
        var presenter = new MermaidPresenter
        {
            Text = """
                   graph TD
                       A -->
                   """
        };

        Assert.DoesNotThrow(() => presenter.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)));
    }

    [Test]
    public void MermaidInlineTextParser_MarkdownParsesInlineStyles()
    {
        var layout = MermaidInlineTextParser.ParseMarkdown("***both*** `code`\nnext");

        Assert.That(layout.Text, Is.EqualTo("both code\nnext"));
        Assert.That(layout.Spans, Has.One.Matches<MermaidTextSpan>(
            span => span.Start == 0 &&
                    span.Length == 4 &&
                    span.Style.HasFlag(MermaidTextStyle.Bold) &&
                    span.Style.HasFlag(MermaidTextStyle.Italic)));
        Assert.That(layout.Spans, Has.One.Matches<MermaidTextSpan>(
            span => span.Start == 5 &&
                    span.Length == 4 &&
                    span.Style.HasFlag(MermaidTextStyle.Code)));
    }

    [Test]
    public void MermaidInlineTextParser_HtmlLikeParsesSupportedTags()
    {
        var layout = MermaidInlineTextParser.ParseMermaiderHtmlLike(
            "<strong>bold</strong> <em>it</em> <u>u</u> <del>s</del><br>next");

        Assert.That(layout.Text, Is.EqualTo("bold it u s\nnext"));
        Assert.That(layout.Spans, Has.One.Matches<MermaidTextSpan>(
            span => span.Start == 0 && span.Length == 4 && span.Style == MermaidTextStyle.Bold));
        Assert.That(layout.Spans, Has.One.Matches<MermaidTextSpan>(
            span => span.Start == 5 && span.Length == 2 && span.Style == MermaidTextStyle.Italic));
        Assert.That(layout.Spans, Has.One.Matches<MermaidTextSpan>(
            span => span.Start == 8 && span.Length == 1 && span.Style == MermaidTextStyle.Underline));
        Assert.That(layout.Spans, Has.One.Matches<MermaidTextSpan>(
            span => span.Start == 10 && span.Length == 1 && span.Style == MermaidTextStyle.Strikethrough));
    }

    [Test]
    public void MermaidInlineTextParser_HtmlLikePreservesUnknownTags()
    {
        var layout = MermaidInlineTextParser.ParseMermaiderHtmlLike("<mark>x</mark>");

        Assert.That(layout.Text, Is.EqualTo("<mark>x</mark>"));
        Assert.That(layout.Spans, Is.Empty);
    }

    [Test]
    public void MermaidInlineTextParser_HtmlLikeDecodesEntitiesWithoutPromotingEscapedTags()
    {
        var layout = MermaidInlineTextParser.ParseMermaiderHtmlLike("&amp; &lt;b&gt; <b>x &amp; y</b>");

        Assert.That(layout.Text, Is.EqualTo("& <b> x & y"));
        Assert.That(layout.Spans, Has.One.Matches<MermaidTextSpan>(
            span => span.Start == 6 && span.Length == 5 && span.Style == MermaidTextStyle.Bold));
    }

    [Test]
    public void Measure_FlowchartMarkdownLabelDoesNotThrow()
    {
        var presenter = new MermaidPresenter
        {
            Text = """
                   graph TD
                       A["`**Bold** *Italic*`"] --> B
                   """
        };

        Assert.DoesNotThrow(() => presenter.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)));
        Assert.That(presenter.DesiredSize.Width, Is.GreaterThan(0));
        Assert.That(presenter.DesiredSize.Height, Is.GreaterThan(0));
    }

    [Test]
    public void Measure_FlowchartHtmlLikeLabelDoesNotThrow()
    {
        var presenter = new MermaidPresenter
        {
            Text = """
                   graph TD
                       A["**Bold** *Italic*"] --> B
                   """
        };

        Assert.DoesNotThrow(() => presenter.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)));
        Assert.That(presenter.DesiredSize.Width, Is.GreaterThan(0));
        Assert.That(presenter.DesiredSize.Height, Is.GreaterThan(0));
    }
}
