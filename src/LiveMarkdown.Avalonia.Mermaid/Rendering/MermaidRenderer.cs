using Avalonia;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Base class for Mermaid native renderer parts that can be targeted by Avalonia styles.
/// </summary>
/// <remarks>
/// Renderer parts are logical style objects rather than controls. A <see cref="MermaidPresenter"/>
/// owns them as logical children so selectors such as
/// <c>MermaidPresenter ClassRenderer</c> can tune diagram-specific geometry without putting every
/// small renderer token on the presenter itself. They do not participate in the visual tree, hit
/// testing, layout, or templating.
/// </remarks>
public abstract class MermaidRenderer : StyledElement
{
    /// <summary>
    /// Gets the presenter that owns this renderer part.
    /// </summary>
    protected MermaidPresenter? Owner { get; private set; }

    /// <summary>
    /// Assigns the presenter that should be invalidated when renderer styling changes.
    /// </summary>
    /// <remarks>
    /// This method is called by <see cref="MermaidPresenter"/> while it wires renderer parts into
    /// its logical tree. Application code normally styles renderer parts through selectors instead
    /// of setting the owner manually.
    /// </remarks>
    internal void AttachOwner(MermaidPresenter owner)
    {
        Owner = owner;
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (Owner is not { } owner || !ShouldInvalidatePresenter(change.Property))
        {
            return;
        }

        if (ShouldInvalidatePresenterMeasure(change.Property))
        {
            owner.InvalidateMeasure();
        }
        else
        {
            owner.InvalidateVisual();
        }
    }

    /// <summary>
    /// Determines whether a property change should invalidate the owning presenter.
    /// </summary>
    /// <remarks>
    /// Derived renderers can ignore purely diagnostic or cached properties by returning
    /// <see langword="false"/>. The default invalidates for properties registered on the concrete
    /// renderer type or one of its base types.
    /// </remarks>
    protected virtual bool ShouldInvalidatePresenter(AvaloniaProperty property) => property.OwnerType.IsInstanceOfType(this);

    /// <summary>
    /// Determines whether a renderer property change affects measurement rather than drawing only.
    /// </summary>
    /// <remarks>
    /// Most renderer tokens are drawing geometry and only need a visual invalidation. Override this
    /// for future properties that change the presenter's reported desired size.
    /// </remarks>
    protected virtual bool ShouldInvalidatePresenterMeasure(AvaloniaProperty property) => false;
}