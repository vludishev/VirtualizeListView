using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using AndroidX.Core.Widget;

using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace MPowerKit.VirtualizeListView;

public partial class VirtualizeListViewHandler : ScrollViewHandler
{
    protected override MauiScrollView CreatePlatformView()
    {
        var scrollView = new SmoothScrollView(
            new ContextThemeWrapper(MauiContext!.Context, Resource.Style.scrollViewTheme),
            null!,
            Resource.Attribute.scrollViewStyle)
        {
            ClipToOutline = true,
            FillViewport = true
        };

        scrollView.Attach((VirtualView as VirtualizeListView)!);

        return scrollView;
    }
}

public class SmoothScrollView : MauiScrollView
{
    private OverScroller? _scroller;
    private VirtualizeListView? _listView;

    public SmoothScrollView(Context context) : base(context)
    {
        InitScroller();
    }

    public SmoothScrollView(Context context, Android.Util.IAttributeSet attrs) : base(context, attrs)
    {
        InitScroller();
    }

    public SmoothScrollView(Context context, Android.Util.IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
    {
        InitScroller();
    }

    protected SmoothScrollView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
    }

    public void Attach(VirtualizeListView listView)
    {
        _listView = listView;
    }

    private void InitScroller()
    {
        var field = Java.Lang.Class.FromType(typeof(NestedScrollView)).GetDeclaredField("mScroller");
        field.Accessible = true;

        _scroller = field.Get(this) as OverScroller;
    }

    public virtual void AdjustScroll(double dxdp, double dydp)
    {
        if (_scroller is null) return;

        var dx = (int)this.Context.ToPixels(dxdp);
        var dy = (int)this.Context.ToPixels(dydp);

        if (!_scroller.IsFinished)
        {
            var velocity = _scroller.CurrVelocity + dy;

            var direction = _scroller.FinalY < _scroller.CurrY ? -velocity : velocity;

            this.ScrollBy(dx, dy);

            _scroller.ForceFinished(true);
            base.Fling((int)direction);
        }
        else
        {
            ScrollBy(dx, dy);
        }
    }

    public override void Fling(int velocityY)
    {
        velocityY /= (int)(_listView?.ScrollSpeed ?? ScrollSpeed.Normal);

        base.Fling(velocityY);
    }

    protected override void OnScrollChanged(int l, int t, int oldl, int oldt)
    {
        base.OnScrollChanged(l, t, oldl, oldt);

        try
        {
            if (_scroller is not null && (!CanScrollVertically(1) || !CanScrollVertically(-1)) && !_scroller.IsFinished)
            {
                _scroller.AbortAnimation();
            }
        }
        catch { }
    }

    public override void ComputeScroll()
    {
        if (_scroller is null || !_scroller.ComputeScrollOffset() || _scroller.IsFinished) return;

        int oldY = ScrollY;
        int newY = _scroller.CurrY;
        if (oldY != newY)
        {
            ScrollTo(0, newY);
        }
        PostInvalidateOnAnimation();
    }
}
