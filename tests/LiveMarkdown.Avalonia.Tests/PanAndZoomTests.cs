using Avalonia;
using Avalonia.Controls;
using NUnit.Framework;

namespace LiveMarkdown.Avalonia.Tests;

[TestFixture]
public class PanAndZoomTests
{
    [Test]
    public void Defaults_AreConservative()
    {
        var control = new PanAndZoom();

        Assert.That(control.MousePanAcceleration, Is.EqualTo(1));
        Assert.That(control.MouseWheelZoomAcceleration, Is.EqualTo(1));
        Assert.That(control.TouchPanAcceleration, Is.EqualTo(1));
        Assert.That(control.TouchZoomAcceleration, Is.EqualTo(1));
        Assert.That(control.MinZoom, Is.EqualTo(0.1));
        Assert.That(control.MaxZoom, Is.EqualTo(8));
        Assert.That(control.FitToViewport, Is.False);
        Assert.That(control.ClipToBounds, Is.True);
    }

    [Test]
    public void CalculateZoomOffset_KeepsViewportPointOnSameContentPoint()
    {
        var viewportPoint = new Point(100, 80);
        var oldScale = 1.5;
        var newScale = 3;
        var oldOffset = new Vector(10, 20);

        var newOffset = PanAndZoom.CalculateZoomOffset(viewportPoint, oldScale, newScale, oldOffset);

        var oldContentPoint = (viewportPoint - oldOffset) / oldScale;
        var newContentPoint = (viewportPoint - newOffset) / newScale;
        Assert.That(newContentPoint.X, Is.EqualTo(oldContentPoint.X).Within(0.0001));
        Assert.That(newContentPoint.Y, Is.EqualTo(oldContentPoint.Y).Within(0.0001));
    }

    [Test]
    public void CalculateFitView_FitsContentAndCentersIt()
    {
        var (scale, offset) = PanAndZoom.CalculateFitView(
            new Size(1000, 500),
            new Size(500, 500),
            minZoom: 0.1,
            maxZoom: 8);

        Assert.That(scale, Is.EqualTo(0.5).Within(0.0001));
        Assert.That(offset.X, Is.EqualTo(0).Within(0.0001));
        Assert.That(offset.Y, Is.EqualTo(125).Within(0.0001));
    }

    [Test]
    public void CalculateFitView_ClampsScale()
    {
        var (minScale, _) = PanAndZoom.CalculateFitView(
            new Size(1000, 1000),
            new Size(50, 50),
            minZoom: 0.1,
            maxZoom: 8);
        var (maxScale, _) = PanAndZoom.CalculateFitView(
            new Size(10, 10),
            new Size(500, 500),
            minZoom: 0.1,
            maxZoom: 8);

        Assert.That(minScale, Is.EqualTo(0.1).Within(0.0001));
        Assert.That(maxScale, Is.EqualTo(8).Within(0.0001));
    }

    [Test]
    public void ResetView_WhenFitToViewportIsFalse_ResetsToOneToOne()
    {
        var control = new PanAndZoom
        {
            Content = new Border { Width = 1000, Height = 500 }
        };
        control.Measure(new Size(500, 500));
        control.Arrange(new Rect(0, 0, 500, 500));

        control.ZoomAt(new Point(100, 100), 2);
        control.PanBy(new Vector(20, 10));
        control.ResetView();

        Assert.That(control.Scale, Is.EqualTo(1).Within(0.0001));
        Assert.That(control.Offset.X, Is.EqualTo(0).Within(0.0001));
        Assert.That(control.Offset.Y, Is.EqualTo(0).Within(0.0001));
    }

    [Test]
    public void ResetView_WhenFitToViewportIsTrue_UsesCurrentViewport()
    {
        var control = CreateFittedControl();

        control.ZoomAt(new Point(100, 100), 2);
        control.ResetView();

        Assert.That(control.Scale, Is.EqualTo(0.5).Within(0.0001));
        Assert.That(control.Offset.X, Is.EqualTo(0).Within(0.0001));
        Assert.That(control.Offset.Y, Is.EqualTo(125).Within(0.0001));
    }

    [Test]
    public void Arrange_WhenNotInteracted_RefitsAfterViewportChanges()
    {
        var control = CreateFittedControl();

        control.Measure(new Size(250, 500));
        control.Arrange(new Rect(0, 0, 250, 500));

        Assert.That(control.Scale, Is.EqualTo(0.25).Within(0.0001));
        Assert.That(control.Offset.X, Is.EqualTo(0).Within(0.0001));
        Assert.That(control.Offset.Y, Is.EqualTo(187.5).Within(0.0001));
    }

    [Test]
    public void Arrange_WhenInteracted_DoesNotOverwriteUserView()
    {
        var control = CreateFittedControl();
        control.PanBy(new Vector(20, 10));

        control.Measure(new Size(250, 500));
        control.Arrange(new Rect(0, 0, 250, 500));

        Assert.That(control.Scale, Is.EqualTo(0.5).Within(0.0001));
        Assert.That(control.Offset.X, Is.EqualTo(20).Within(0.0001));
        Assert.That(control.Offset.Y, Is.EqualTo(135).Within(0.0001));
    }

    private static PanAndZoom CreateFittedControl()
    {
        var control = new PanAndZoom
        {
            FitToViewport = true,
            Content = new Border { Width = 1000, Height = 500 }
        };

        control.Measure(new Size(500, 500));
        control.Arrange(new Rect(0, 0, 500, 500));
        return control;
    }
}
