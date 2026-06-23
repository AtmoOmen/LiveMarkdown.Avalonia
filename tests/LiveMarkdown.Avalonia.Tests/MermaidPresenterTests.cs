using Avalonia;
using Avalonia.Media;
using Markdig;
using Mermaider;
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
    public void MermaidStyleValue_NormalizesCssLikeColorAndLengthValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(MermaidStyleValue.NormalizeColor("#123;"), Is.EqualTo("#112233"));
            Assert.That(MermaidStyleValue.NormalizeColor("#123"), Is.EqualTo("#112233"));
            Assert.That(MermaidStyleValue.NormalizeColor("#112233;"), Is.EqualTo("#112233"));
            Assert.That(MermaidStyleValue.NormalizeColor(" none ; "), Is.EqualTo("none"));
            Assert.That(MermaidStyleValue.NormalizeColor("#abcd"), Is.EqualTo("#ddaabbcc"));
            Assert.That(MermaidStyleValue.NormalizeLength("2px;"), Is.EqualTo("2"));
            Assert.That(MermaidStyleValue.NormalizeLength("1.5;"), Is.EqualTo("1.5"));
        });
    }

    [Test]
    public void GetCachedBrush_UsesNormalizedMermaidStyleColor()
    {
        var presenter = new MermaidPresenter();

        var brush = presenter.GetCachedBrush("#123;", Brushes.White);

        Assert.That(brush, Is.TypeOf<SolidColorBrush>());
        Assert.That(((SolidColorBrush)brush!).Color, Is.EqualTo(Color.Parse("#112233")));
        Assert.That(presenter.GetCachedBrush("definitely-not-a-color;", Brushes.White), Is.SameAs(Brushes.White));
    }

    [Test]
    public void GetCachedPen_UsesNormalizedStrokeWidth()
    {
        var presenter = new MermaidPresenter();
        var fallback = new Pen(Brushes.White, 1);

        var pen = presenter.GetCachedPen("#123;", "2px;", fallback);

        Assert.That(pen, Is.Not.Null);
        Assert.That(pen!.Thickness, Is.EqualTo(2));
        Assert.That(((SolidColorBrush)pen.Brush!).Color, Is.EqualTo(Color.Parse("#112233")));
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

    [Test]
    public void PreparedPositionedGraph_FlowchartCachesParsedLabels()
    {
        var positioned = MermaidRenderer.LayoutProvider.LayoutFlowchart(MermaidRenderer.Parse(
            """
            flowchart LR
                A["`**Bold** *Italic*`"] -->|yes| B
            """));

        var prepared = PreparedPositionedGraph.Prepare(positioned);

        Assert.That(prepared.Edges, Has.One.Matches<PreparedPositionedEdge>(
            edge => edge.LabelLayout is { Text: "yes" }));
        Assert.That(prepared.Nodes, Has.One.Matches<PreparedPositionedNode>(
            node => node.Node.Id == "A" &&
                    node.LabelLayout.Text == "Bold Italic" &&
                    node.LabelLayout.Spans.Count >= 2));
    }

    [Test]
    public void PreparedPositionedGraph_FlowchartCachesEdgeGeometryWhenPlatformIsAvailable()
    {
        var positioned = MermaidRenderer.LayoutProvider.LayoutFlowchart(MermaidRenderer.Parse(
            """
            flowchart LR
                A --> B
            """));

        var prepared = PreparedPositionedGraph.Prepare(positioned);
        var edge = prepared.Edges.Single(edge => edge.Edge.Points.Count >= 2);

        var first = GetRoundedPathOrIgnore(edge);
        var second = GetRoundedPathOrIgnore(edge);

        Assert.That(first, Is.Not.Null);
        Assert.That(second, Is.SameAs(first));
    }

    [Test]
    public void PreparedPositionedGraph_StateCachesEdgeGeometryWhenPlatformIsAvailable()
    {
        var positioned = MermaidRenderer.LayoutProvider.LayoutFlowchart(MermaidRenderer.Parse(
            """
            stateDiagram-v2
                [*] --> Idle
                Idle --> [*]
            """));

        var prepared = PreparedPositionedGraph.Prepare(positioned);

        foreach (var edge in prepared.Edges.Where(edge => edge.Edge.Points.Count >= 2))
        {
            Assert.That(GetRoundedPathOrIgnore(edge), Is.Not.Null);
        }
    }

    [Test]
    public void Measure_SequenceDiagramSmokeTestDoesNotThrow()
    {
        AssertMermaidMeasures(
            """
            sequenceDiagram
                autonumber
                actor User
                participant API
                participant DB
                User->>API: Request
                API-->>User: Accepted
                API->>API: Validate
                loop Retry
                    API->>DB: Write
                end
                alt Success
                    DB-->>API: OK
                else Failure
                    DB-->>API: Error
                end
                Note over API,DB: smoke test note
                destroy DB
                API-xDB: Close
            """);
    }

    [Test]
    public void Measure_ClassDiagramSmokeTestDoesNotThrow()
    {
        AssertMermaidMeasures(
            """
            classDiagram
                class Animal {
                    <<abstract>>
                    +string Name
                    +Speak() void
                }
                class Dog {
                    +Run() void
                }
                class IRepository~T~ {
                    <<interface>>
                    +Save(T item) void
                }
                Animal <|-- Dog
                Dog *-- "1" Collar : owns
                IRepository <|.. Dog : persists
                note for Dog "Loyal companion"
            """);
    }

    [Test]
    public void Measure_ErDiagramSmokeTestDoesNotThrow()
    {
        AssertMermaidMeasures(
            """
            erDiagram
                CUSTOMER ||--o{ ORDER : places
                ORDER ||--|{ ORDER_ITEM : contains
                PRODUCT ||--o{ ORDER_ITEM : appears_in
                CUSTOMER {
                    string id PK
                    string email UK "contact email"
                }
                ORDER {
                    string id PK
                    date createdAt
                }
                ORDER_ITEM {
                    string id PK
                    int quantity
                }
                PRODUCT {
                    string id PK
                    string name
                }
            """);
    }

    private static void AssertMermaidMeasures(string text)
    {
        var presenter = new MermaidPresenter { Text = text };
        Assert.DoesNotThrow(() => presenter.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)));
        Assert.That(presenter.DesiredSize.Width, Is.GreaterThan(0));
        Assert.That(presenter.DesiredSize.Height, Is.GreaterThan(0));
    }

    private static StreamGeometry? GetRoundedPathOrIgnore(PreparedPositionedEdge edge)
    {
        try
        {
            return edge.RoundedPath;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("IPlatformRenderInterface", StringComparison.Ordinal))
        {
            Assert.Ignore("StreamGeometry creation requires an Avalonia platform render interface.");
            return null;
        }
    }
}
