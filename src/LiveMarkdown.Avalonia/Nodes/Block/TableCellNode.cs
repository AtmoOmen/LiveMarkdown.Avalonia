using Avalonia.Controls;
using Markdig.Extensions.Tables;
using Markdig.Syntax;

namespace LiveMarkdown.Avalonia;

public class TableCellNode : ContainerBlockNode
{
    public TableCellNode()
    {
        Classes.Add("TableCell");
        control = new Border
        {
            Classes = { "TableCell" },
            Child = base.Control
        };
    }

    protected override bool IsCompatible(MarkdownObject markdownObject)
    {
        return markdownObject.GetType() == typeof(TableCell);
    }
}