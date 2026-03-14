using Avalonia;
using Avalonia.Controls;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// A robust, crash-free panel designed for Markdown tables.
/// Automatically infers rows, columns, and spans from children's Grid attached properties.
/// Content dictates the width of the columns and the height of the rows.
/// Completely immune to IndexOutOfRangeException.
/// </summary>
public class MarkdownTableGrid : Panel
{
    private readonly Dictionary<int, double> _rowHeights = new();
    private readonly Dictionary<int, double> _colWidths = new();

    protected override Size MeasureOverride(Size availableSize)
    {
        _rowHeights.Clear();
        _colWidths.Clear();

        if (Children.Count == 0) return new Size();

        // Pass 1: Measure all children with infinite space to get their true Auto desired size.
        foreach (var child in Children)
        {
            child?.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        }

        // Pass 2: Calculate sizes for single-cell elements (Span == 1)
        foreach (var child in Children)
        {
            if (child == null) continue;

            var row = Grid.GetRow(child);
            var col = Grid.GetColumn(child);
            var rowSpan = Math.Max(1, Grid.GetRowSpan(child));
            var colSpan = Math.Max(1, Grid.GetColumnSpan(child));

            var desiredSize = child.DesiredSize;

            if (rowSpan == 1)
            {
                if (_rowHeights.TryGetValue(row, out var h)) _rowHeights[row] = Math.Max(h, desiredSize.Height);
                else _rowHeights[row] = desiredSize.Height;
            }

            if (colSpan == 1)
            {
                if (_colWidths.TryGetValue(col, out var w)) _colWidths[col] = Math.Max(w, desiredSize.Width);
                else _colWidths[col] = desiredSize.Width;
            }
        }

        // Pass 3: Distribute extra space required by multi-cell elements (Span > 1)
        foreach (var child in Children)
        {
            if (child == null) continue;

            var row = Grid.GetRow(child);
            var col = Grid.GetColumn(child);
            var rowSpan = Math.Max(1, Grid.GetRowSpan(child));
            var colSpan = Math.Max(1, Grid.GetColumnSpan(child));

            var desiredSize = child.DesiredSize;

            // Handle Column Spans
            if (colSpan > 1)
            {
                double currentSpannedWidth = 0;
                for (var i = 0; i < colSpan; i++)
                {
                    var targetCol = col + i;
                    if (!_colWidths.ContainsKey(targetCol)) _colWidths[targetCol] = 0;
                    currentSpannedWidth += _colWidths[targetCol];
                }

                var extraWidth = desiredSize.Width - currentSpannedWidth;
                if (extraWidth > 0)
                {
                    // Distribute the extra required width equally across all spanned columns
                    var addedWidthPerCol = extraWidth / colSpan;
                    for (var i = 0; i < colSpan; i++)
                    {
                        _colWidths[col + i] += addedWidthPerCol;
                    }
                }
            }

            // Handle Row Spans
            if (rowSpan > 1)
            {
                double currentSpannedHeight = 0;
                for (var i = 0; i < rowSpan; i++)
                {
                    var targetRow = row + i;
                    if (!_rowHeights.ContainsKey(targetRow)) _rowHeights[targetRow] = 0;
                    currentSpannedHeight += _rowHeights[targetRow];
                }

                var extraHeight = desiredSize.Height - currentSpannedHeight;
                if (extraHeight > 0)
                {
                    // Distribute the extra required height equally across all spanned rows
                    var addedHeightPerRow = extraHeight / rowSpan;
                    for (var i = 0; i < rowSpan; i++)
                    {
                        _rowHeights[row + i] += addedHeightPerRow;
                    }
                }
            }
        }

        // The total size of the table is the sum of all columns and rows
        var totalWidth = _colWidths.Values.Sum();
        var totalHeight = _rowHeights.Values.Sum();

        return new Size(totalWidth, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count == 0) return finalSize;

        // Sort keys to ensure proper spatial layout even if indices are non-sequential
        var sortedRows = _rowHeights.Keys.ToList();
        sortedRows.Sort();

        var sortedCols = _colWidths.Keys.ToList();
        sortedCols.Sort();

        // Pre-calculate X offsets
        var colOffsets = new Dictionary<int, double>();
        double currentX = 0;
        foreach (var col in sortedCols)
        {
            colOffsets[col] = currentX;
            currentX += _colWidths[col];
        }

        // Pre-calculate Y offsets
        var rowOffsets = new Dictionary<int, double>();
        double currentY = 0;
        foreach (var row in sortedRows)
        {
            rowOffsets[row] = currentY;
            currentY += _rowHeights[row];
        }

        // Arrange each child based on its offset and total spanned size
        foreach (var child in Children)
        {
            if (child == null) continue;

            var row = Grid.GetRow(child);
            var col = Grid.GetColumn(child);
            var rowSpan = Math.Max(1, Grid.GetRowSpan(child));
            var colSpan = Math.Max(1, Grid.GetColumnSpan(child));

            // Determine starting coordinates (default to 0 if something is completely missing)
            var x = colOffsets.TryGetValue(col, out var startX) ? startX : 0;
            var y = rowOffsets.TryGetValue(row, out var startY) ? startY : 0;

            // Calculate total width across spanned columns
            var w = 0d;
            for (var i = 0; i < colSpan; i++)
            {
                if (_colWidths.TryGetValue(col + i, out var cw)) w += cw;
            }

            // Calculate total height across spanned rows
            var h = 0d;
            for (var i = 0; i < rowSpan; i++)
            {
                if (_rowHeights.TryGetValue(row + i, out var rh)) h += rh;
            }

            // Arrange the child within its calculated bounding box
            child.Arrange(new Rect(x, y, w, h));
        }

        return finalSize;
    }
}