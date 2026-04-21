namespace MPowerKit.VirtualizeListView;

public class VirtualizeListViewCell : ContentView
{
    private readonly List<View> _trackedContentViews = [];
    private bool _layoutRefreshScheduled;

    public VirtualizeListViewCell()
    {
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Fill;

        var tap = new TapGestureRecognizer();
        tap.Tapped += Tap_Tapped;
        this.GestureRecognizers.Add(tap);
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName != nameof(Content))
        {
            return;
        }

        DetachContentLayoutHandlers();

        if (Content is not View content)
        {
            return;
        }

        NormalizeContentLayout(content);
        AttachContentLayoutHandlers(content);
        InvalidateContentLayout(content);
    }

    private void NormalizeContentLayout(View content)
    {
        content.HorizontalOptions = LayoutOptions.Fill;
        content.VerticalOptions = LayoutOptions.Fill;

        if (content is ContentView { Content: View nestedContent })
        {
            nestedContent.HorizontalOptions = LayoutOptions.Fill;
            nestedContent.VerticalOptions = LayoutOptions.Fill;
        }
    }

    private void InvalidateContentLayout(View content)
    {
        (content as IView)?.InvalidateMeasure();
        (this as IView).InvalidateMeasure();
        RequestParentLayout();

        Dispatcher?.Dispatch(() =>
        {
            if (!ReferenceEquals(Content, content))
            {
                return;
            }

            NormalizeContentLayout(content);
            (content as IView)?.InvalidateMeasure();
#if ANDROID
            ArrangePlatformContent(content);
            DumpSelectionIndicatorLayout(content, "content");
#endif
            (this as IView).InvalidateMeasure();
            RequestParentLayout();
        });
    }

    private void AttachContentLayoutHandlers(View view)
    {
        foreach (var child in EnumerateSelfAndDescendants(view))
        {
            child.PropertyChanged += ContentView_PropertyChanged;
            _trackedContentViews.Add(child);
        }
    }

    private void DetachContentLayoutHandlers()
    {
        foreach (var child in _trackedContentViews)
        {
            child.PropertyChanged -= ContentView_PropertyChanged;
        }

        _trackedContentViews.Clear();
    }

    private void ContentView_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (!IsLayoutAffectingProperty(e.PropertyName))
        {
            return;
        }

        ScheduleContentLayoutRefresh();
    }

    private void ScheduleContentLayoutRefresh()
    {
        if (_layoutRefreshScheduled)
        {
            return;
        }

        _layoutRefreshScheduled = true;
        Dispatcher?.Dispatch(() =>
        {
            _layoutRefreshScheduled = false;

            if (Content is View content)
            {
                NormalizeContentLayout(content);
                (content as IView)?.InvalidateMeasure();
#if ANDROID
                ArrangePlatformContent(content);
                DumpSelectionIndicatorLayout(content, "refresh");
#endif
            }

            (this as IView).InvalidateMeasure();
            RequestParentLayout();
        });
    }

    private void RequestParentLayout()
    {
        (Parent as IView)?.InvalidateMeasure();

        if (Parent is not CellHolder holder)
        {
            return;
        }

        holder.Item?.OnCellSizeChanged();
        (holder.Parent as IView)?.InvalidateMeasure();
    }

    private static bool IsLayoutAffectingProperty(string? propertyName)
    {
        return propertyName is nameof(IsVisible)
            or nameof(WidthRequest)
            or nameof(HeightRequest)
            or nameof(MinimumWidthRequest)
            or nameof(MinimumHeightRequest)
            or nameof(MaximumWidthRequest)
            or nameof(MaximumHeightRequest)
            or nameof(Margin)
            or nameof(HorizontalOptions)
            or nameof(VerticalOptions)
            or nameof(Content);
    }

    private static IEnumerable<View> EnumerateSelfAndDescendants(View view)
    {
        yield return view;

        if (view is ContentView { Content: View content })
        {
            foreach (var child in EnumerateSelfAndDescendants(content))
            {
                yield return child;
            }
        }

        if (view is not Layout layout)
        {
            yield break;
        }

        foreach (var child in layout.Children)
        {
            if (child is not View childView)
            {
                continue;
            }

            foreach (var descendant in EnumerateSelfAndDescendants(childView))
            {
                yield return descendant;
            }
        }
    }

#if ANDROID
    private static void ArrangePlatformContent(View view)
    {
        foreach (var child in EnumerateSelfAndDescendants(view))
        {
            if (child.Handler is not IViewHandler handler)
            {
                continue;
            }

            var bounds = child.Bounds;
            if (bounds.Width < 0d || bounds.Height < 0d)
            {
                continue;
            }

            ArrangePlatformView(child, handler, bounds);
        }
    }

    private static void ArrangePlatformView(View view, IViewHandler handler, Rect bounds)
    {
        handler.PlatformArrange(bounds);

        if (handler.PlatformView is not Android.Views.View platformView)
        {
            return;
        }

        platformView.Post(() =>
        {
            if (view.Handler is not IViewHandler currentHandler)
            {
                return;
            }

            var currentBounds = view.Bounds;
            if (currentBounds.Width < 0d || currentBounds.Height < 0d)
            {
                return;
            }

            currentHandler.PlatformArrange(currentBounds);
        });
    }

    private static void DumpSelectionIndicatorLayout(View view, string source)
    {
        foreach (var child in EnumerateSelfAndDescendants(view))
        {
            if (child is not BoxView boxView || !boxView.IsVisible)
            {
                continue;
            }

            if (boxView.WidthRequest < 20d || boxView.HeightRequest < 20d)
            {
                continue;
            }

            if (boxView.Handler?.PlatformView is not Android.Views.View platformView)
            {
                continue;
            }

            Android.Util.Log.Debug(
                "VLV_DIAG",
                $"{source}: managed bounds={boxView.Bounds}, desired={boxView.DesiredSize}, " +
                $"native=({platformView.Left},{platformView.Top},{platformView.Right},{platformView.Bottom}), measured=({platformView.MeasuredWidth}x{platformView.MeasuredHeight}), " +
                $"parent={platformView.Parent?.GetType().FullName}");
        }
    }
#endif

    private void Tap_Tapped(object? sender, TappedEventArgs e)
    {
        if (this.BindingContext is null) return;

        var listview = this.FindParentOfType<VirtualizeListView>();

        if (listview is null) return;

        listview.OnItemTapped(this.BindingContext);
    }

    public void SendAppearing()
    {
        OnAppearing();
    }

    public void SendDisappearing()
    {
        OnDisappearing();
    }

    protected virtual void OnAppearing()
    {

    }

    protected virtual void OnDisappearing()
    {

    }
}
