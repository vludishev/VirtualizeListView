namespace MPowerKit.VirtualizeListView;

public class CellHolder : Grid
{
    private const double SizeEpsilon = 0.5d;
    private const double DefaultRequest = -1d;
    private double _lastNotifiedWidth = double.NaN;
    private double _lastNotifiedHeight = double.NaN;
    private bool _sizeRefreshScheduled;

    public VirtualizeListViewItem? Item { get; set; }
    public bool IsCached => Item is null;
    public bool WasArranged { get; protected set; }
    public bool WasMeasured { get; protected set; }
    public bool Attached { get; set; }

    public View? Content
    {
        get => this.ElementAtOrDefault(0) as View;
        set
        {
            var oldContent = Content;
            if (ReferenceEquals(value, oldContent)) return;

            DetachContentHandlers(oldContent);

            BindableLayout.ClearItems(this);
            ResetSizeTracking();

            if (value is null) return;

            AttachContentHandlers(value);
            this.Add(value);
        }
    }

    public void NotifyBound()
    {
        ResetSizeTracking();
        ScheduleSizeRefresh();
    }

    protected override Size ArrangeOverride(Rect bounds)
    {
        WasArranged = true;
        return base.ArrangeOverride(bounds);
    }

    protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
    {
        WasMeasured = true;
        return base.MeasureOverride(widthConstraint, heightConstraint);
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        NotifyItemSizeChanged(width, height, force: false);
    }

    private void Content_SizeChanged(object? sender, EventArgs e)
    {
        NotifyItemSizeChanged(this.Width, this.Height, force: false);
    }

    private void Layout_ChildAdded(object? sender, ElementEventArgs e)
    {
        ScheduleSizeRefresh();
    }

    private void Layout_ChildRemoved(object? sender, ElementEventArgs e)
    {
        ScheduleSizeRefresh();
    }

    private void AttachContentHandlers(View content)
    {
        content.SizeChanged += Content_SizeChanged;

        if (content is Layout layout)
        {
            layout.ChildAdded += Layout_ChildAdded;
            layout.ChildRemoved += Layout_ChildRemoved;
        }
    }

    private void DetachContentHandlers(View? content)
    {
        if (content is null) return;

        content.SizeChanged -= Content_SizeChanged;

        if (content is Layout layout)
        {
            layout.ChildAdded -= Layout_ChildAdded;
            layout.ChildRemoved -= Layout_ChildRemoved;
        }
    }

    private void ScheduleSizeRefresh()
    {
        if (_sizeRefreshScheduled) return;

        var dispatcher = Dispatcher;
        if (dispatcher is null)
        {
            NotifyItemSizeChanged(this.Width, this.Height, force: true);
            return;
        }

        _sizeRefreshScheduled = true;
        dispatcher.Dispatch(() =>
        {
            _sizeRefreshScheduled = false;
            NotifyItemSizeChanged(this.Width, this.Height, force: true);
        });
    }

    private void NotifyItemSizeChanged(double width, double height, bool force)
    {
        if (!Attached || Item is null)
        {
            return;
        }

        var content = Content;
        var measuredContent = TryMeasureContent(content, width);
        var measuredContentWidth = measuredContent.Width;
        var measuredContentHeight = measuredContent.Height;
        var isSupplementary = Item.AdapterItem is DataAdapter.HeaderItem or DataAdapter.FooterItem;

        if (isSupplementary && measuredContentHeight > 0d)
        {
            // iOS may allocate supplementary cells to viewport height when using translations.
            // Keep the holder height aligned with measured content so scroll range is correct.
            if (Math.Abs(HeightRequest - measuredContentHeight) > SizeEpsilon)
            {
                HeightRequest = measuredContentHeight;
                force = true;
            }
        }

        if (isSupplementary)
        {
            if (measuredContentHeight > 0d)
            {
                height = measuredContentHeight;
            }
        }
        else if (content is not null)
        {
            width = Math.Max(width, content.Width);
            height = Math.Max(height, content.Height);
        }

        if (!isSupplementary)
        {
            width = Math.Max(width, measuredContentWidth);
            height = Math.Max(height, measuredContentHeight);
        }

        if (width <= 0d || height <= 0d)
        {
            return;
        }

        if (!force
            && !double.IsNaN(_lastNotifiedWidth)
            && !double.IsNaN(_lastNotifiedHeight)
            && Math.Abs(_lastNotifiedWidth - width) < SizeEpsilon
            && Math.Abs(_lastNotifiedHeight - height) < SizeEpsilon)
        {
            return;
        }

        _lastNotifiedWidth = width;
        _lastNotifiedHeight = height;

        Item.OnCellSizeChanged();
    }

    private void ResetSizeTracking()
    {
        _lastNotifiedWidth = double.NaN;
        _lastNotifiedHeight = double.NaN;
        _sizeRefreshScheduled = false;

        if (Math.Abs(HeightRequest - DefaultRequest) > SizeEpsilon)
        {
            HeightRequest = DefaultRequest;
        }

        if (Math.Abs(WidthRequest - DefaultRequest) > SizeEpsilon)
        {
            WidthRequest = DefaultRequest;
        }
    }

    private static Size TryMeasureContent(View? content, double width)
    {
        if (content is not IView contentView)
        {
            return new();
        }

        var widthConstraint = width > 0d && !double.IsNaN(width)
            ? width
            : double.PositiveInfinity;

        var measured = contentView.Measure(widthConstraint, double.PositiveInfinity);
        var desired = contentView.DesiredSize;

        return new(
            Math.Max(measured.Width, desired.Width),
            Math.Max(measured.Height, desired.Height));
    }
}
