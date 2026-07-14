namespace MPowerKit.VirtualizeListView.Tests;

[TestFixture]
public class ScrollOffsetBoundsTests
{
    [TestCase(0d, 0d, 833d, 0d)]
    [TestCase(0d, 20d, 833d, 20d)]
    [TestCase(812d, 20d, 200d, 832d)]
    public void ResolveContentExtent_UsesAvailableLayoutGeometry(
        double layoutExtent,
        double padding,
        double nativeExtent,
        double expected)
    {
        var actual = ScrollOffsetBounds.ResolveContentExtent(
            layoutExtent,
            padding,
            nativeExtent);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase(null, 20d, 833d, 833d)]
    [TestCase(double.NaN, 20d, 833d, 833d)]
    public void ResolveContentExtent_FallsBackToNativeWithoutLayoutGeometry(
        double? layoutExtent,
        double padding,
        double nativeExtent,
        double expected)
    {
        var actual = ScrollOffsetBounds.ResolveContentExtent(
            layoutExtent,
            padding,
            nativeExtent);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase(-483d, 833d, 704d, 0d)]
    [TestCase(0d, 833d, 704d, 0d)]
    [TestCase(129d, 833d, 704d, 129d)]
    [TestCase(317d, 833d, 704d, 129d)]
    [TestCase(100d, 200d, 704d, 0d)]
    public void Clamp_UsesScrollableRange(
        double requestedOffset,
        double contentExtent,
        double viewportExtent,
        double expected)
    {
        var actual = ScrollOffsetBounds.Clamp(
            requestedOffset,
            contentExtent,
            viewportExtent,
            leadingInset: 0d,
            trailingInset: 0d);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase(-20d, -10d)]
    [TestCase(-10d, -10d)]
    [TestCase(149d, 149d)]
    [TestCase(200d, 149d)]
    public void Clamp_IncludesAdjustedContentInsets(double requestedOffset, double expected)
    {
        var actual = ScrollOffsetBounds.Clamp(
            requestedOffset,
            contentExtent: 833d,
            viewportExtent: 704d,
            leadingInset: 10d,
            trailingInset: 20d);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase(0d, 10d)]
    [TestCase(10d, 10d)]
    [TestCase(109d, 109d)]
    [TestCase(129d, 109d)]
    public void Clamp_PreservesNegativeAdjustedContentInsets(double requestedOffset, double expected)
    {
        var actual = ScrollOffsetBounds.Clamp(
            requestedOffset,
            contentExtent: 833d,
            viewportExtent: 704d,
            leadingInset: -10d,
            trailingInset: -20d);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Clamp_NaNRequest_UsesMinimumOffset()
    {
        var actual = ScrollOffsetBounds.Clamp(
            double.NaN,
            contentExtent: 833d,
            viewportExtent: 704d,
            leadingInset: 10d,
            trailingInset: 20d);

        Assert.That(actual, Is.EqualTo(-10d));
    }

    [TestCase(10d, 20d, 10d, 20d, false)]
    [TestCase(10d, 20d, 11d, 20d, true)]
    [TestCase(10d, 20d, 10d, 21d, true)]
    public void RequiresUpdate_OnlyWhenTargetOffsetChanged(
        double currentX,
        double currentY,
        double targetX,
        double targetY,
        bool expected)
    {
        var actual = ScrollOffsetBounds.RequiresUpdate(
            currentX,
            currentY,
            targetX,
            targetY);

        Assert.That(actual, Is.EqualTo(expected));
    }
}
