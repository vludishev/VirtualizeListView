namespace MPowerKit.VirtualizeListView;

public class CellHolder : Grid
{
#if MACIOS
    private const double SizeEpsilon = 0.5d;
    private const double DefaultRequest = -1d;
    private double _lastNotifiedWidth = double.NaN;
    private double _lastNotifiedHeight = double.NaN;
    private bool _sizeRefreshScheduled;
#endif

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
#if MACIOS
            var oldContent = Content;
            if (ReferenceEquals(value, oldContent)) return;

            DetachContentHandlers(oldContent);

            BindableLayout.ClearItems(this);
            ResetSizeTracking();

            if (value is null) return;

            PrepareContent(value);
            AttachContentHandlers(value);
            this.Add(value);
            InvalidateContentLayout(value);
#else
            if (value == Content) return;

            BindableLayout.ClearItems(this);

            if (value is null) return;

            PrepareContent(value);
            this.Add(value);
            InvalidateContentLayout(value);
#endif
        }
    }

    public CellHolder()
    {
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Fill;
        RowDefinitions.Add(new RowDefinition(GridLength.Star));
        ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
    }

    private static void PrepareContent(View content)
    {
        content.HorizontalOptions = LayoutOptions.Fill;
        content.VerticalOptions = LayoutOptions.Fill;
        SetRow(content, 0);
        SetColumn(content, 0);
    }

    private void InvalidateContentLayout(View content)
    {
        (content as IView)?.InvalidateMeasure();
        (this as IView).InvalidateMeasure();
        RequestParentLayout();

        Dispatcher?.Dispatch(() =>
        {
            PrepareContent(content);
            (content as IView)?.InvalidateMeasure();
            (this as IView).InvalidateMeasure();
            RequestParentLayout();
        });
    }

    private void RequestParentLayout()
    {
        Item?.OnCellSizeChanged();
        (Parent as IView)?.InvalidateMeasure();
    }

    public void NotifyBound()
    {
#if MACIOS
        ResetSizeTracking();
        ScheduleSizeRefresh();
#endif
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

#if MACIOS
        NotifyItemSizeChanged(width, height, force: false);
        return;
#else
        Item?.OnCellSizeChanged();
#endif
    }

#if MACIOS
    private void Content_SizeChanged(object? sender, EventArgs e)
    {
        NotifyItemSizeChanged(Width, Height, force: false);
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
            NotifyItemSizeChanged(Width, Height, force: true);
            return;
        }

        _sizeRefreshScheduled = true;
        dispatcher.Dispatch(() =>
        {
            _sizeRefreshScheduled = false;
            NotifyItemSizeChanged(Width, Height, force: true);
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
        var measuredContentHeight = measuredContent.Height;
        var isSupplementary = Item.AdapterItem is DataAdapter.HeaderItem or DataAdapter.FooterItem;

        if (isSupplementary && measuredContentHeight > 0d)
        {
            // iOS may allocate supplementary cells to viewport height when using translations.
            // Keep holder height aligned with measured content to keep scroll range stable.
            if (Math.Abs(HeightRequest - measuredContentHeight) > SizeEpsilon)
            {
                HeightRequest = measuredContentHeight;
                force = true;
            }

            height = measuredContentHeight;
        }
        else if (content is not null)
        {
            width = Math.Max(width, content.Width);
            height = Math.Max(height, content.Height);
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
#endif
}
