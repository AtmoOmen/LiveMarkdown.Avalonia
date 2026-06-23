using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Provides mouse and touch pan/zoom interaction for arbitrary content.
/// </summary>
[TemplatePart(Name = ResetButtonPartName, Type = typeof(Button), IsRequired = false)]
public sealed class PanAndZoom : ContentControl
{
    private const string ResetButtonPartName = "PART_ResetButton";
    private const double WheelZoomBase = 1.2;

    /// <summary>
    /// Identifies the <see cref="MousePanAcceleration"/> styled property.
    /// </summary>
    public static readonly StyledProperty<double> MousePanAccelerationProperty =
        AvaloniaProperty.Register<PanAndZoom, double>(nameof(MousePanAcceleration), 1.0);

    /// <summary>
    /// Identifies the <see cref="MouseWheelZoomAcceleration"/> styled property.
    /// </summary>
    public static readonly StyledProperty<double> MouseWheelZoomAccelerationProperty =
        AvaloniaProperty.Register<PanAndZoom, double>(nameof(MouseWheelZoomAcceleration), 1.0);

    /// <summary>
    /// Identifies the <see cref="TouchPanAcceleration"/> styled property.
    /// </summary>
    public static readonly StyledProperty<double> TouchPanAccelerationProperty =
        AvaloniaProperty.Register<PanAndZoom, double>(nameof(TouchPanAcceleration), 1.0);

    /// <summary>
    /// Identifies the <see cref="TouchZoomAcceleration"/> styled property.
    /// </summary>
    public static readonly StyledProperty<double> TouchZoomAccelerationProperty =
        AvaloniaProperty.Register<PanAndZoom, double>(nameof(TouchZoomAcceleration), 1.0);

    /// <summary>
    /// Identifies the <see cref="MinZoom"/> styled property.
    /// </summary>
    public static readonly StyledProperty<double> MinZoomProperty =
        AvaloniaProperty.Register<PanAndZoom, double>(nameof(MinZoom), 0.1);

    /// <summary>
    /// Identifies the <see cref="MaxZoom"/> styled property.
    /// </summary>
    public static readonly StyledProperty<double> MaxZoomProperty =
        AvaloniaProperty.Register<PanAndZoom, double>(nameof(MaxZoom), 8.0);

    /// <summary>
    /// Identifies the <see cref="FitToViewport"/> styled property.
    /// </summary>
    public static readonly StyledProperty<bool> FitToViewportProperty =
        AvaloniaProperty.Register<PanAndZoom, bool>(nameof(FitToViewport));

    /// <summary>
    /// Gets or sets the multiplier applied to mouse drag panning deltas.
    /// </summary>
    public double MousePanAcceleration
    {
        get => GetValue(MousePanAccelerationProperty);
        set => SetValue(MousePanAccelerationProperty, value);
    }

    /// <summary>
    /// Gets or sets the multiplier applied to mouse wheel zoom deltas.
    /// </summary>
    public double MouseWheelZoomAcceleration
    {
        get => GetValue(MouseWheelZoomAccelerationProperty);
        set => SetValue(MouseWheelZoomAccelerationProperty, value);
    }

    /// <summary>
    /// Gets or sets the multiplier applied to single-touch panning deltas.
    /// </summary>
    public double TouchPanAcceleration
    {
        get => GetValue(TouchPanAccelerationProperty);
        set => SetValue(TouchPanAccelerationProperty, value);
    }

    /// <summary>
    /// Gets or sets the multiplier applied to two-touch pinch zoom deltas.
    /// </summary>
    public double TouchZoomAcceleration
    {
        get => GetValue(TouchZoomAccelerationProperty);
        set => SetValue(TouchZoomAccelerationProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimum allowed zoom scale.
    /// </summary>
    public double MinZoom
    {
        get => GetValue(MinZoomProperty);
        set => SetValue(MinZoomProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum allowed zoom scale.
    /// </summary>
    public double MaxZoom
    {
        get => GetValue(MaxZoomProperty);
        set => SetValue(MaxZoomProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether reset should scale the content to fit fully inside the viewport.
    /// </summary>
    public bool FitToViewport
    {
        get => GetValue(FitToViewportProperty);
        set => SetValue(FitToViewportProperty, value);
    }

    /// <summary>
    /// Gets the current zoom scale.
    /// </summary>
    /// <remarks>
    /// The scale is applied as a render transform to the content after it has been measured and
    /// arranged at its natural size.
    /// </remarks>
    public double Scale { get; private set; } = 1;

    /// <summary>
    /// Gets the current offset of the content relative to the viewport.
    /// </summary>
    /// <remarks>
    /// The offset is expressed in viewport pixels and is applied after scaling. For example,
    /// <c>Offset = (20, 10)</c> moves the already-scaled content 20 pixels right and 10 pixels down.
    /// </remarks>
    public Vector Offset { get; private set; }

    private readonly Dictionary<int, PointerState> _pointers = new();
    private IDisposable? _resetButtonClickSubscription;
    private Size _contentSize;
    private Size _viewportSize;
    private bool _hasUserView;

    /// <summary>
    /// Initializes a new instance of the <see cref="PanAndZoom"/> class.
    /// </summary>
    public PanAndZoom()
    {
        ClipToBounds = true;
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _resetButtonClickSubscription?.Dispose();
        _resetButtonClickSubscription = e.NameScope.Find<Button>(ResetButtonPartName)?.AddDisposableHandler(
            Button.ClickEvent,
            delegate
            {
                ResetView();
            });
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ContentProperty)
        {
            _hasUserView = false;
            _pointers.Clear();
            ApplyTransform();
            InvalidateMeasure();
            return;
        }

        if (change.Property == FitToViewportProperty)
        {
            _hasUserView = false;
            ResetView();
            InvalidateMeasure();
            return;
        }

        if (change.Property == MinZoomProperty || change.Property == MaxZoomProperty)
        {
            if (!_hasUserView && FitToViewport)
            {
                ResetView();
            }
            else
            {
                SetView(ClampScale(Scale), Offset, markUserView: false);
            }
        }
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var measured = base.MeasureOverride(availableSize);

        if (Content is Control content)
        {
            content.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            _contentSize = SanitizeSize(content.DesiredSize);
        }
        else
        {
            _contentSize = SanitizeSize(measured);
        }

        return CalculateViewportSize(_contentSize, availableSize);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var arranged = base.ArrangeOverride(finalSize);
        _viewportSize = SanitizeSize(finalSize);

        if (Content is Control content)
        {
            content.Arrange(new Rect(0, 0, _contentSize.Width, _contentSize.Height));
        }

        if (!_hasUserView && FitToViewport)
        {
            FitCurrentView();
        }
        else
        {
            ApplyTransform();
        }

        return arranged;
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var point = e.GetCurrentPoint(this);
        if (point.Pointer.Type == PointerType.Mouse && !point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        var position = e.GetPosition(this);
        _pointers[point.Pointer.Id] = new PointerState(point.Pointer.Type, position, position);
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var point = e.GetCurrentPoint(this);
        if (!_pointers.TryGetValue(point.Pointer.Id, out var state))
        {
            return;
        }

        var position = e.GetPosition(this);
        _pointers[point.Pointer.Id] = state with
        {
            PreviousPosition = state.Position,
            Position = position
        };

        if (TryHandleTouchGesture())
        {
            e.Handled = true;
            return;
        }

        if (_pointers.Count == 1)
        {
            var updated = _pointers[point.Pointer.Id];
            var acceleration = updated.PointerType == PointerType.Mouse
                ? MousePanAcceleration
                : TouchPanAcceleration;
            PanBy((updated.Position - updated.PreviousPosition) * SanitizeAcceleration(acceleration));
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        ReleasePointer(e.Pointer);
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        ReleasePointer(e.Pointer);
    }

    /// <inheritdoc/>
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        var factor = Math.Pow(WheelZoomBase, e.Delta.Y * SanitizeAcceleration(MouseWheelZoomAcceleration));
        ZoomAt(e.GetPosition(this), Scale * factor);
        e.Handled = true;
    }

    /// <summary>
    /// Resets the view according to <see cref="FitToViewport"/>.
    /// </summary>
    /// <remarks>
    /// When fitting is enabled, reset recomputes from the latest measured content and viewport sizes.
    /// Otherwise it returns to a one-to-one scale at the origin.
    /// </remarks>
    internal void ResetView()
    {
        _hasUserView = false;
        _pointers.Clear();

        if (FitToViewport)
        {
            FitCurrentView();
        }
        else
        {
            SetView(1, default, markUserView: false);
        }
    }

    /// <summary>
    /// Applies a viewport-space pan delta and marks the current view as user-controlled.
    /// </summary>
    internal void PanBy(Vector delta)
    {
        if (delta == default)
        {
            return;
        }

        SetView(Scale, Offset + delta, markUserView: true);
    }

    /// <summary>
    /// Zooms around a viewport point so the content coordinate below that point remains stationary.
    /// </summary>
    internal void ZoomAt(Point center, double requestedScale)
    {
        var nextScale = ClampScale(requestedScale);
        var nextOffset = CalculateZoomOffset(center, Scale, nextScale, Offset);
        SetView(nextScale, nextOffset, markUserView: true);
    }

    /// <summary>
    /// Calculates the post-zoom offset that keeps the same content point under a viewport center.
    /// </summary>
    /// <remarks>
    /// The invariant is <c>(center - oldOffset) / oldScale == (center - newOffset) / newScale</c>.
    /// Rearranging gives <c>newOffset = center - (center - oldOffset) * (newScale / oldScale)</c>.
    /// </remarks>
    internal static Vector CalculateZoomOffset(Point center, double oldScale, double newScale, Vector oldOffset)
    {
        if (!IsPositiveFinite(oldScale) || !IsPositiveFinite(newScale))
        {
            return oldOffset;
        }

        var ratio = newScale / oldScale;
        return new Vector(
            center.X - (center.X - oldOffset.X) * ratio,
            center.Y - (center.Y - oldOffset.Y) * ratio);
    }

    /// <summary>
    /// Calculates the scale and offset required to show the whole content inside a viewport.
    /// </summary>
    /// <remarks>
    /// The returned offset centers the scaled content. If either size is empty or non-finite, the
    /// method falls back to scale 1 and offset 0 so layout remains deterministic.
    /// </remarks>
    internal static (double Scale, Vector Offset) CalculateFitView(
        Size contentSize,
        Size viewportSize,
        double minZoom,
        double maxZoom)
    {
        contentSize = SanitizeSize(contentSize);
        viewportSize = SanitizeSize(viewportSize);

        if (!IsPositiveFinite(contentSize.Width) ||
            !IsPositiveFinite(contentSize.Height) ||
            !IsPositiveFinite(viewportSize.Width) ||
            !IsPositiveFinite(viewportSize.Height))
        {
            return (1, default);
        }

        var scale = Math.Min(viewportSize.Width / contentSize.Width, viewportSize.Height / contentSize.Height);
        scale = ClampScale(scale, minZoom, maxZoom);

        return (
            scale,
            new Vector(
                (viewportSize.Width - contentSize.Width * scale) / 2,
                (viewportSize.Height - contentSize.Height * scale) / 2));
    }

    private void FitCurrentView()
    {
        var (scale, offset) = CalculateFitView(_contentSize, _viewportSize, MinZoom, MaxZoom);
        SetView(scale, offset, markUserView: false);
    }

    // The view state lives on the viewport, while content stays arranged at natural size.
    // This makes Reset recompute cheaply from measured sizes without forcing diagram relayout.
    private void SetView(double scale, Vector offset, bool markUserView)
    {
        Scale = ClampScale(scale);
        Offset = offset;
        _hasUserView |= markUserView;
        ApplyTransform();
    }

    private void ApplyTransform()
    {
        if (Content is not Control content)
        {
            return;
        }

        content.RenderTransformOrigin = RelativePoint.TopLeft;
        content.RenderTransform = new MatrixTransform(
            Matrix.CreateScale(Scale, Scale) * Matrix.CreateTranslation(Offset.X, Offset.Y));
    }

    // Two touch points are handled as a combined gesture: pinch distance changes scale, and midpoint
    // movement pans at the same time. This mirrors common map/image viewer interaction.
    private bool TryHandleTouchGesture()
    {
        PointerState? firstTouch = null;
        PointerState? secondTouch = null;

        foreach (var pointer in _pointers.Values)
        {
            if (pointer.PointerType != PointerType.Touch)
            {
                continue;
            }

            if (firstTouch is null)
            {
                firstTouch = pointer;
                continue;
            }

            secondTouch = pointer;
            break;
        }

        if (firstTouch is not { } first || secondTouch is not { } second)
        {
            return false;
        }

        var previousMidpoint = Midpoint(first.PreviousPosition, second.PreviousPosition);
        var midpoint = Midpoint(first.Position, second.Position);
        var previousDistance = Distance(first.PreviousPosition, second.PreviousPosition);
        var distance = Distance(first.Position, second.Position);

        if (previousDistance > 0 && distance > 0)
        {
            var factor = Math.Pow(distance / previousDistance, SanitizeAcceleration(TouchZoomAcceleration));
            var nextScale = ClampScale(Scale * factor);
            var nextOffset = CalculateZoomOffset(midpoint, Scale, nextScale, Offset);
            SetView(
                nextScale,
                nextOffset + (midpoint - previousMidpoint) * SanitizeAcceleration(TouchPanAcceleration),
                markUserView: true);
        }
        else
        {
            PanBy((midpoint - previousMidpoint) * SanitizeAcceleration(TouchPanAcceleration));
        }

        return true;
    }

    private void ReleasePointer(IPointer pointer)
    {
        _pointers.Remove(pointer.Id);

        if (ReferenceEquals(pointer.Captured, this))
        {
            pointer.Capture(null);
        }
    }

    private double ClampScale(double scale)
    {
        return ClampScale(scale, MinZoom, MaxZoom);
    }

    private static double ClampScale(double scale, double minZoom, double maxZoom)
    {
        if (!double.IsFinite(scale))
        {
            scale = 1;
        }

        if (!IsPositiveFinite(minZoom))
        {
            minZoom = 0.1;
        }

        if (!IsPositiveFinite(maxZoom) || maxZoom < minZoom)
        {
            maxZoom = minZoom;
        }

        return Math.Clamp(scale, minZoom, maxZoom);
    }

    private static Size CalculateViewportSize(Size contentSize, Size availableSize)
    {
        return new Size(
            CalculateViewportDimension(contentSize.Width, availableSize.Width),
            CalculateViewportDimension(contentSize.Height, availableSize.Height));
    }

    private static double CalculateViewportDimension(double contentDimension, double availableDimension)
    {
        return double.IsFinite(availableDimension)
            ? Math.Min(contentDimension, availableDimension)
            : contentDimension;
    }

    private static Size SanitizeSize(Size size)
    {
        return new Size(SanitizeDimension(size.Width), SanitizeDimension(size.Height));
    }

    private static double SanitizeDimension(double value)
    {
        return double.IsFinite(value) && value > 0 ? value : 0;
    }

    private static double SanitizeAcceleration(double acceleration)
    {
        return double.IsFinite(acceleration) ? Math.Max(0, acceleration) : 1;
    }

    private static bool IsPositiveFinite(double value)
    {
        return double.IsFinite(value) && value > 0;
    }

    private static Point Midpoint(Point first, Point second)
    {
        return new Point((first.X + second.X) / 2, (first.Y + second.Y) / 2);
    }

    private static double Distance(Point first, Point second)
    {
        var delta = second - first;
        return Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
    }

    private readonly record struct PointerState(
        PointerType PointerType,
        Point Position,
        Point PreviousPosition);
}