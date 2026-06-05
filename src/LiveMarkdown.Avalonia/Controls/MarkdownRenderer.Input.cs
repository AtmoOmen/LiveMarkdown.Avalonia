using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace LiveMarkdown.Avalonia;

public partial class MarkdownRenderer
{
    /// <summary>
    /// Defines the <see cref="CanCopy"/> property.
    /// </summary>
    public static readonly DirectProperty<MarkdownRenderer, bool> CanCopyProperty =
        AvaloniaProperty.RegisterDirect<MarkdownRenderer, bool>(
            nameof(CanCopy),
            o => o.CanCopy);

    /// <summary>
    /// Defines the <see cref="CopyGesture"/> property.
    /// </summary>
    public static readonly StyledProperty<KeyGesture> CopyGestureProperty =
        AvaloniaProperty.Register<MarkdownRenderer, KeyGesture>(
            nameof(CopyGesture),
            new KeyGesture(Key.C, KeyModifiers.Control));

    /// <summary>
    /// Gets whether there is any text selected that can be copied.
    /// </summary>
    public bool CanCopy
    {
        get;
        private set => SetAndRaise(CanCopyProperty, ref field, value);
    }

    /// <summary>
    /// Gets or sets the keyboard gesture for the Copy command.
    /// </summary>
    public KeyGesture CopyGesture
    {
        get => GetValue(CopyGestureProperty);
        set => SetValue(CopyGestureProperty, value);
    }

    public string SelectedText
    {
        get
        {
            var allBlocks = GetAllSelectableBlocksInScope(GetSelectionScopeRoot());

            var sb = new StringBuilder();
            var isFirst = true;

            foreach (var block in allBlocks.Where(b => !IsNestedBlock(b)))
            {
                var text = block.ActualSelectedText;
                if (string.IsNullOrEmpty(text)) continue;

                if (!isFirst) sb.AppendLine();
                sb.Append(text);
                isFirst = false;
            }

            return sb.ToString();
        }
    }

    // Anchor point where the drag started (Block + Offset)
    private (MarkdownTextBlock Block, int Offset)? selectionAnchor;

    // Cache of all blocks in the current scope to avoid frequent VisualTree traversal during move.
    // Built on PointerPressed, cleared on PointerReleased.
    private List<MarkdownTextBlock>? activeScopeBlocks;

    private DispatcherTimer? selectionAutoScrollTimer;
    private Point? lastSelectionPointerPosition;
    private Point? lastSelectionPointerTopLevelPosition;
    private MarkdownTextBlock? lastSelectionTargetBlock;

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        EndSelectionDrag();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (e.Handled)
        {
            base.OnPointerPressed(e);
            return;
        }

        var point = e.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed)
        {
            base.OnPointerPressed(e);
            return;
        }

        // Find the exact text block first, then resolve the topmost shared scope from it.
        var localBlocks = GetAllSelectableBlocksInScope(this).ToList();
        var targetBlock = FindNearestBlockInList(localBlocks, this, point.Position);
        if (targetBlock == null) // Truly nothing to select
        {
            base.OnPointerPressed(e);
            return;
        }

        e.Handled = true;
        Focus();
        e.Pointer.Capture(this);

        activeScopeBlocks = GetAllSelectableBlocksInScope(ResolveSelectionScopeRoot(targetBlock, this)).ToList();
        TrackSelectionPointer(e);
        lastSelectionTargetBlock = targetBlock;

        ClearSelection(activeScopeBlocks);
        UpdateCanCopy();

        var position = GetCaretPosition(targetBlock, e);
        selectionAnchor = (targetBlock, position);

        HandleClickSelection(targetBlock, position, e.ClickCount);
        StartSelectionAutoScroll();

        base.OnPointerPressed(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (selectionAnchor == null || activeScopeBlocks == null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            base.OnPointerMoved(e);
            return;
        }

        TrackSelectionPointer(e);
        UpdateSelectionRangeFromPoint(e.GetPosition(this));

        e.Handled = true;

        base.OnPointerMoved(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (selectionAnchor == null || e.InitialPressMouseButton != MouseButton.Left)
        {
            e.Source = this;
            base.OnPointerReleased(e);
            return;
        }

        EndSelectionDrag(e.Pointer);
        e.Handled = true;

        base.OnPointerReleased(e);
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        EndSelectionDrag();
        base.OnPointerCaptureLost(e);
    }

    private void StartSelectionAutoScroll()
    {
        if (selectionAutoScrollTimer is null)
        {
            selectionAutoScrollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            selectionAutoScrollTimer.Tick += HandleSelectionAutoScrollTick;
        }

        if (!selectionAutoScrollTimer.IsEnabled)
        {
            selectionAutoScrollTimer.Start();
        }
    }

    private void EndSelectionDrag(IPointer? pointer = null)
    {
        selectionAnchor = null;
        activeScopeBlocks = null;
        lastSelectionPointerPosition = null;
        lastSelectionPointerTopLevelPosition = null;
        lastSelectionTargetBlock = null;
        selectionAutoScrollTimer?.Stop();
        pointer?.Capture(null);
    }

    private void TrackSelectionPointer(PointerEventArgs e)
    {
        lastSelectionPointerPosition = e.GetPosition(this);

        if (TopLevel.GetTopLevel(this) is Visual topLevel)
        {
            lastSelectionPointerTopLevelPosition = e.GetPosition(topLevel);
        }
        else
        {
            lastSelectionPointerTopLevelPosition = null;
        }
    }

    private void HandleSelectionAutoScrollTick(object? sender, EventArgs e)
    {
        if (selectionAnchor is null || activeScopeBlocks is null || lastSelectionPointerPosition is null)
        {
            selectionAutoScrollTimer?.Stop();
            return;
        }

        if (!TryAutoScrollSelection()) return;

        if (GetLastSelectionPointerPosition(this) is { } point)
        {
            UpdateSelectionRangeFromPoint(point);
        }
    }

    private bool TryAutoScrollSelection()
    {
        var currentPoint = GetLastSelectionPointerPosition(this) ?? lastSelectionPointerPosition ?? default;
        var targetBlock = activeScopeBlocks is { } blocks
            ? FindNearestBlockInList(blocks, this, currentPoint)
            : lastSelectionTargetBlock;

        if (targetBlock is not null)
        {
            lastSelectionTargetBlock = targetBlock;
        }

        var start = (Visual?)lastSelectionTargetBlock ?? this;
        foreach (var scrollViewer in GetAutoScrollViewers(start))
        {
            if (GetLastSelectionPointerPosition(scrollViewer) is not { } pointerInViewer) continue;

            var delta = GetAutoScrollDelta(scrollViewer, pointerInViewer);
            if (delta == default) continue;

            if (TryApplyAutoScroll(scrollViewer, delta)) return true;

            if (!scrollViewer.IsScrollChainingEnabled)
            {
                return false;
            }
        }

        return false;
    }

    private Point? GetLastSelectionPointerPosition(Visual relativeTo)
    {
        if (lastSelectionPointerTopLevelPosition is { } topLevelPoint &&
            TopLevel.GetTopLevel(this) is Visual topLevel &&
            topLevel.TranslatePoint(topLevelPoint, relativeTo) is { } translatedFromTopLevel)
        {
            return translatedFromTopLevel;
        }

        if (lastSelectionPointerPosition is { } rendererPoint)
        {
            return this.TranslatePoint(rendererPoint, relativeTo);
        }

        return null;
    }

    private static IEnumerable<ScrollViewer> GetAutoScrollViewers(Visual start)
    {
        for (Visual? current = start; current is not null; current = current.GetVisualParent())
        {
            if (current is ScrollViewer scrollViewer)
            {
                yield return scrollViewer;
            }
        }
    }

    private static Vector GetAutoScrollDelta(ScrollViewer scrollViewer, Point pointer)
    {
        return GetAutoScrollDelta(
            scrollViewer.Bounds.Size,
            pointer,
            scrollViewer.HorizontalScrollBarVisibility,
            scrollViewer.VerticalScrollBarVisibility);
    }

    internal static Vector GetAutoScrollDelta(
        Size boundsSize,
        Point pointer,
        ScrollBarVisibility horizontalScrollBarVisibility,
        ScrollBarVisibility verticalScrollBarVisibility)
    {
        var bounds = new Rect(boundsSize);
        var x = horizontalScrollBarVisibility == ScrollBarVisibility.Disabled
            ? 0
            : GetAutoScrollAxisDelta(pointer.X, bounds.Left, bounds.Right);
        var y = verticalScrollBarVisibility == ScrollBarVisibility.Disabled
            ? 0
            : GetAutoScrollAxisDelta(pointer.Y, bounds.Top, bounds.Bottom);

        return new Vector(x, y);
    }

    private static double GetAutoScrollAxisDelta(double coordinate, double min, double max)
    {
        if (coordinate < min) return -GetAutoScrollVelocity(min - coordinate);
        if (coordinate > max) return GetAutoScrollVelocity(coordinate - max);
        return 0;
    }

    private static double GetAutoScrollVelocity(double distance)
    {
        if (distance <= 0) return 0;
        return Math.Clamp(distance * 0.35, 2, 48);
    }

    private static bool TryApplyAutoScroll(ScrollViewer scrollViewer, Vector delta)
    {
        var oldOffset = scrollViewer.Offset;
        var newOffset = CoerceAutoScrollOffset(
            oldOffset,
            scrollViewer.Extent,
            scrollViewer.Viewport,
            delta,
            scrollViewer.HorizontalScrollBarVisibility,
            scrollViewer.VerticalScrollBarVisibility);
        if (newOffset == oldOffset) return false;

        scrollViewer.Offset = newOffset;
        return true;
    }

    internal static Vector CoerceAutoScrollOffset(
        Vector oldOffset,
        Size extent,
        Size viewport,
        Vector delta,
        ScrollBarVisibility horizontalScrollBarVisibility,
        ScrollBarVisibility verticalScrollBarVisibility)
    {
        var maxX = GetScrollMaximum(extent.Width, viewport.Width);
        var maxY = GetScrollMaximum(extent.Height, viewport.Height);

        var x = oldOffset.X;
        if (delta.X != 0 && horizontalScrollBarVisibility != ScrollBarVisibility.Disabled && maxX > 0)
        {
            x = Math.Clamp(oldOffset.X + delta.X, 0, maxX);
        }

        var y = oldOffset.Y;
        if (delta.Y != 0 && verticalScrollBarVisibility != ScrollBarVisibility.Disabled && maxY > 0)
        {
            y = Math.Clamp(oldOffset.Y + delta.Y, 0, maxY);
        }

        return new Vector(x, y);
    }

    private static double GetScrollMaximum(double extent, double viewport)
    {
        var maximum = Math.Max(extent - viewport, 0);
        return double.IsNaN(maximum) ? 0 : maximum;
    }

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        var keymap = Application.Current?.PlatformSettings?.HotkeyConfiguration;
        if (keymap == null) return;

        if (Match(keymap.Copy))
        {
            Copy();
            e.Handled = true;
        }
        else if (Match(keymap.SelectAll))
        {
            e.Handled = true;
        }

        bool Match(List<KeyGesture> gestures) => gestures.Any(g => g.Matches(e));
    }

    public async void Copy()
    {
        if (TopLevel.GetTopLevel(this) is not { Clipboard: { } clipboard }) return;

        var text = SelectedText;
        if (string.IsNullOrEmpty(text)) return;

        await clipboard.SetTextAsync(text);
    }

    // -----------------------------------------------------------------------
    // Core Logic: Selection Range Calculation
    // -----------------------------------------------------------------------

    private void UpdateSelectionRange((MarkdownTextBlock Block, int Offset) anchor, (MarkdownTextBlock Block, int Offset) focus)
    {
        if (activeScopeBlocks == null) return;

        var (startNode, endNode) = OrderPositions(anchor, focus);

        foreach (var block in activeScopeBlocks)
        {
            var selectionStart = GetEffectiveStart(block, startNode);
            var selectionEnd = GetEffectiveEnd(block, endNode);

            if (selectionStart < selectionEnd)
            {
                block.SetCurrentValue(SelectableTextBlock.SelectionStartProperty, selectionStart);
                block.SetCurrentValue(SelectableTextBlock.SelectionEndProperty, selectionEnd);
            }
            else
            {
                block.ClearSelection();
            }
        }

        UpdateCanCopy();
    }

    private int GetEffectiveStart(MarkdownTextBlock block, (MarkdownTextBlock Block, int Offset) startNode)
    {
        if (block == startNode.Block) return startNode.Offset;

        // If block contains startNode (Parent of Start)
        var pIndex = GetPlaceholderIndex(block, startNode.Block);
        if (pIndex != -1) return pIndex + 1;

        // If startNode contains block (Child of Start)
        var parentIndex = GetPlaceholderIndex(startNode.Block, block);
        if (parentIndex != -1)
        {
            // If start is before the placeholder for this block, then this block is fully after start.
            if (startNode.Offset <= parentIndex) return 0;

            // If start is after the placeholder, this block is fully before start.
            return block.EscapedTextLength;
        }

        // Unrelated
        if (ComparePositions(block, 0, startNode.Block, startNode.Offset) >= 0) return 0;
        return block.EscapedTextLength;
    }

    private int GetEffectiveEnd(MarkdownTextBlock block, (MarkdownTextBlock Block, int Offset) endNode)
    {
        if (block == endNode.Block) return endNode.Offset;

        // If block contains endNode (Parent of End)
        var pIndex = GetPlaceholderIndex(block, endNode.Block);
        if (pIndex != -1) return pIndex;

        // If endNode contains block (Child of End)
        var parentIndex = GetPlaceholderIndex(endNode.Block, block);
        if (parentIndex != -1)
        {
            // If end is after the placeholder, this block is fully before end.
            if (endNode.Offset > parentIndex) return block.EscapedTextLength;

            // If end is before the placeholder, this block is fully after end.
            return 0;
        }

        // Unrelated
        var textLength = block.EscapedTextLength;
        if (ComparePositions(block, textLength, endNode.Block, endNode.Offset) <= 0) return textLength;
        return 0;
    }

    private ((MarkdownTextBlock Block, int Offset) Start, (MarkdownTextBlock Block, int Offset) End) OrderPositions(
        (MarkdownTextBlock Block, int Offset) a,
        (MarkdownTextBlock Block, int Offset) b)
    {
        return ComparePositions(a.Block, a.Offset, b.Block, b.Offset) <= 0 ? (a, b) : (b, a);
    }

    private int ComparePositions(MarkdownTextBlock blockA, int offsetA, MarkdownTextBlock blockB, int offsetB)
    {
        if (blockA == blockB) return offsetA.CompareTo(offsetB);

        // Check hierarchy
        // If A contains B
        var placeholderInA = GetPlaceholderIndex(blockA, blockB);
        if (placeholderInA != -1)
        {
            return offsetA <= placeholderInA ? -1 : 1;
        }

        // If B contains A
        var placeholderInB = GetPlaceholderIndex(blockB, blockA);
        if (placeholderInB != -1)
        {
            return offsetB <= placeholderInB ? 1 : -1;
        }

        // Fallback to list index
        var idxA = activeScopeBlocks!.IndexOf(blockA);
        var idxB = activeScopeBlocks!.IndexOf(blockB);
        return idxA.CompareTo(idxB);
    }

    private static int GetPlaceholderIndex(MarkdownTextBlock parent, MarkdownTextBlock child)
    {
        if (parent.Inlines == null) return -1;

        var currentOffset = 0;
        if (parent.Inlines.Any(ProcessInlineOffset))
        {
            return currentOffset;
        }

        return -1;

        // Helper to process inlines recursively
        // returns true if child found
        bool ProcessInlineOffset(Inline inline)
        {
            switch (inline)
            {
                case InlineUIContainer { Child: Visual childVisual } when IsVisualAncestor(childVisual, child):
                    return true;
                case InlineUIContainer:
                    currentOffset++;
                    break;
                case Run run:
                    currentOffset += run.Text?.Length ?? 0;
                    break;
                case LineBreak:
                    currentOffset += Environment.NewLine.Length;
                    break;
                case Span span:
                {
                    if (span.Inlines.Any(ProcessInlineOffset)) return true;
                    break;
                }
            }

            return false;
        }
    }

    private static bool IsVisualAncestor(Visual? ancestor, Visual? target)
    {
        while (target != null)
        {
            if (target == ancestor) return true;
            target = target.GetVisualParent();
        }
        return false;
    }

    /// <summary>
    /// Finds the nearest MarkdownTextBlock in the provided list to the given point.
    /// </summary>
    private static MarkdownTextBlock? FindNearestBlockInList(List<MarkdownTextBlock> blocks, Visual relativeTo, Point point)
    {
        if (blocks.Count == 0) return null;

        MarkdownTextBlock? bestBlock = null;
        var minDistance = double.MaxValue;

        // reverse order so that inner blocks are prioritized
        for (var i = blocks.Count - 1; i >= 0; i--)
        {
            var block = blocks[i];

            var topLeft = block.TranslatePoint(new Point(0, 0), relativeTo) ?? new Point();
            var bottomRight = block.TranslatePoint(new Point(block.Bounds.Width, block.Bounds.Height), relativeTo) ?? new Point();
            var rect = new Rect(topLeft, bottomRight);

            var distance = GetDistanceToRect(point, rect);

            if (distance < minDistance)
            {
                minDistance = distance;
                bestBlock = block;
            }

            if (distance < 0.1) return block;
        }

        return bestBlock;
    }

    private static double GetDistanceToRect(Point p, Rect r)
    {
        var dx = Math.Max(Math.Max(r.Left - p.X, 0), p.X - r.Right);
        var dy = Math.Max(Math.Max(r.Top - p.Y, 0), p.Y - r.Bottom);
        return Math.Sqrt(dx * dx + dy * dy);
    }

    internal Visual GetSelectionScopeRoot() => ResolveSelectionScopeRoot(this, this);

    internal static Visual ResolveSelectionScopeRoot(Visual visual, Visual fallback)
    {
        Visual? scopeRoot = null;
        for (var current = visual; current is not null; current = current.GetVisualParent())
        {
            if (MarkdownTextBlock.GetIsSelectionScope(current))
            {
                scopeRoot = current;
            }
        }

        return scopeRoot ?? fallback;
    }

    internal static IEnumerable<MarkdownTextBlock> GetAllSelectableBlocksInScope(Visual scopeRoot)
    {
        // We want all blocks, including nested ones, because hierarchy is handled by
        // GetEffectiveStart/GetEffectiveEnd. DFS order provides the document order.
        return scopeRoot.GetSelfAndVisualDescendants().OfType<MarkdownTextBlock>();
    }

    private static bool IsNestedBlock(MarkdownTextBlock child) => child.FindAncestorOfType<MarkdownTextBlock>() is not null;

    private static int GetCaretPosition(MarkdownTextBlock block, PointerEventArgs e)
    {
        return GetCaretPosition(block, e.GetPosition(block));
    }

    private static int GetCaretPosition(MarkdownTextBlock block, Point point)
    {
        // Clamp point to block bounds to avoid HitTestPoint failures
        var x = Math.Clamp(point.X, 0, Math.Max(0, block.Bounds.Width));
        var y = Math.Clamp(point.Y, 0, Math.Max(0, block.Bounds.Height));

        return block.TextLayout.HitTestPoint(new Point(x, y)).TextPosition;
    }

    private bool UpdateSelectionRangeFromPoint(Point currentPoint)
    {
        if (selectionAnchor is not { } anchor || activeScopeBlocks is not { } blocks) return false;

        // Use nearest block so selection keeps tracking even after the pointer leaves exact text bounds.
        var targetBlock = FindNearestBlockInList(blocks, this, currentPoint);
        if (targetBlock is null) return false;

        var pointInBlock = this.TranslatePoint(currentPoint, targetBlock) ?? new Point();
        var focusOffset = GetCaretPosition(targetBlock, pointInBlock);
        lastSelectionTargetBlock = targetBlock;

        UpdateSelectionRange(anchor, (targetBlock, focusOffset));
        return true;
    }

    private void HandleClickSelection(MarkdownTextBlock block, int position, int clickCount)
    {
        var text = block.ActualText;
        var start = position;
        var end = position;

        switch (clickCount % 6)
        {
            // select word
            case 2:
            {
                if (!StringUtils.IsStartOfWord(text, position)) start = StringUtils.PreviousWord(text, position);
                if (!StringUtils.IsEndOfWord(text, position)) end = StringUtils.NextWord(text, position);
                break;
            }
            // select section
            case 3:
            {
                SelectAll(block.GetSelfAndVisualDescendants().OfType<MarkdownTextBlock>());
                UpdateCanCopy();
                return;
            }
            // select all
            case 4 when activeScopeBlocks is not null:
            {
                SelectAll(activeScopeBlocks);
                UpdateCanCopy();
                return;
            }
            // clear selection
            case 5 when activeScopeBlocks is not null:
            {
                ClearSelection(activeScopeBlocks);
                UpdateCanCopy();
                return;
            }
        }

        block.SetCurrentValue(SelectableTextBlock.SelectionStartProperty, start);
        block.SetCurrentValue(SelectableTextBlock.SelectionEndProperty, end);

        if (clickCount > 1)
        {
            selectionAnchor = (block, start);
        }

        UpdateCanCopy();
    }

    /// <summary>
    /// Selects all text in the current selection scope.
    /// </summary>
    public void SelectAll()
    {
        SelectAll(GetAllSelectableBlocksInScope(GetSelectionScopeRoot()));
        UpdateCanCopy();
    }

    /// <summary>
    /// Clears selection in the current selection scope.
    /// </summary>
    public void ClearSelection()
    {
        ClearSelection(GetAllSelectableBlocksInScope(GetSelectionScopeRoot()));
        UpdateCanCopy();
    }

    private static void SelectAll(IEnumerable<MarkdownTextBlock> blocks)
    {
        foreach (var block in blocks) block.SelectAll();
    }

    private static void ClearSelection(IEnumerable<MarkdownTextBlock> blocks)
    {
        foreach (var block in blocks) block.ClearSelection();
    }

    private void UpdateCanCopy()
    {
        CanCopy = !string.IsNullOrEmpty(SelectedText);
    }
}
