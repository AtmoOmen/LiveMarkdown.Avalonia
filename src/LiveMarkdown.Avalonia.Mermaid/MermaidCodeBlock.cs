using Markdig;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Syntax;

namespace LiveMarkdown.Avalonia;

public class MermaidCodeBlock(BlockParser parser) : FencedCodeBlock(parser);

public class MermaidBlockParser : FencedBlockParserBase<MermaidCodeBlock>
{
    public MermaidBlockParser()
    {
        OpeningCharacters = ['`', '~'];
        InfoPrefix = "mermaid";
    }

    public override BlockState TryOpen(BlockProcessor processor)
    {
        if (!processor.Line.AsSpan().EndsWith(InfoPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return BlockState.None;
        }

        return base.TryOpen(processor);
    }

    protected override MermaidCodeBlock CreateFencedBlock(BlockProcessor processor)
    {
        return new MermaidCodeBlock(this);
    }
}

public class MermaidExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (!pipeline.BlockParsers.Contains<MermaidBlockParser>())
        {
            pipeline.BlockParsers.Insert(0, new MermaidBlockParser());
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
    }
}

public static class MermaidExtensionBuilder
{
    public static MarkdownPipelineBuilder UseMermaid(this MarkdownPipelineBuilder pipeline)
    {
        pipeline.Extensions.AddIfNotAlready<MermaidExtension>();
        return pipeline;
    }
}