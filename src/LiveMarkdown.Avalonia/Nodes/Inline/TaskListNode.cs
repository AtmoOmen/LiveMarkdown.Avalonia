using Avalonia.Controls;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;

namespace LiveMarkdown.Avalonia;

public class TaskListNode : InlineNode
{
    public override global::Avalonia.Controls.Documents.Inline Inline { get; }

    private readonly CheckBox checkBox;

    public TaskListNode()
    {
        Inline = new global::Avalonia.Controls.Documents.InlineUIContainer
        {
            Classes = { "TaskList" },
            Child = checkBox = new CheckBox
            {
                Classes = { "TaskList" },
                IsHitTestVisible = false
            }
        };
    }

    protected override bool IsCompatible(MarkdownObject markdownObject)
    {
        return markdownObject.GetType() == typeof(TaskList);
    }

    protected override bool UpdateCore(
        DocumentNode documentNode,
        MarkdownObject markdownObject,
        in ObservableStringBuilderChangedEventArgs change,
        CancellationToken cancellationToken)
    {
        var taskList = (TaskList)markdownObject;
        checkBox.IsChecked = taskList.Checked;
        return true;
    }
}