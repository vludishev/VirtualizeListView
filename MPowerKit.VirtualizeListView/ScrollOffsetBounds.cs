namespace MPowerKit.VirtualizeListView;

internal static class ScrollOffsetBounds
{
    internal static double ResolveContentExtent(
        double? layoutExtent,
        double padding,
        double nativeExtent)
    {
        if (layoutExtent is { } extent
            && double.IsFinite(extent)
            && extent >= 0d)
        {
            var finitePadding = double.IsFinite(padding) ? padding : 0d;
            return Math.Max(0d, extent + finitePadding);
        }

        return NormalizeNonNegative(nativeExtent);
    }

    internal static double Clamp(
        double requestedOffset,
        double contentExtent,
        double viewportExtent,
        double leadingInset,
        double trailingInset)
    {
        var normalizedLeadingInset = NormalizeFinite(leadingInset);
        var normalizedTrailingInset = NormalizeFinite(trailingInset);
        var normalizedContentExtent = NormalizeNonNegative(contentExtent);
        var normalizedViewportExtent = NormalizeNonNegative(viewportExtent);

        var minimum = -normalizedLeadingInset;
        var maximum = Math.Max(
            minimum,
            normalizedContentExtent - normalizedViewportExtent + normalizedTrailingInset);

        if (double.IsNaN(requestedOffset)) return minimum;

        return Math.Clamp(requestedOffset, minimum, maximum);
    }

    internal static bool RequiresUpdate(
        double currentX,
        double currentY,
        double targetX,
        double targetY)
        => currentX != targetX || currentY != targetY;

    private static double NormalizeNonNegative(double value)
        => double.IsFinite(value) && value > 0d ? value : 0d;

    private static double NormalizeFinite(double value)
        => double.IsFinite(value) ? value : 0d;
}
