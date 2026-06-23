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
        if (!IsMermaidFenceOpening(processor.Line.AsSpan()))
        {
            return BlockState.None;
        }

        return base.TryOpen(processor);
    }

    protected override MermaidCodeBlock CreateFencedBlock(BlockProcessor processor)
    {
        return new MermaidCodeBlock(this);
    }

    private static bool IsMermaidFenceOpening(ReadOnlySpan<char> line)
    {
        line = line.TrimStart();
        if (line.Length < 4 || line[0] is not ('`' or '~'))
        {
            return false;
        }

        var fence = line[0];
        var index = 0;
        while (index < line.Length && line[index] == fence)
        {
            index++;
        }

        if (index < 3)
        {
            return false;
        }

        var info = line[index..].TrimStart();
        if (info.Length == 0)
        {
            return false;
        }

        var tokenEnd = 0;
        while (tokenEnd < info.Length && !char.IsWhiteSpace(info[tokenEnd]))
        {
            tokenEnd++;
        }

        return info[..tokenEnd].Equals("mermaid", StringComparison.OrdinalIgnoreCase);
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