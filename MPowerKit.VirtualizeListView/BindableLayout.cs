using System.Collections;
using System.Collections.Specialized;

namespace MPowerKit.VirtualizeListView;

public partial class BindableLayout : Behavior<Layout>
{
    private static void OnPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not Layout layout) return;

        if (!layout.Behaviors.OfType<BindableLayout>().Any()) layout.Behaviors.Add(new BindableLayout());
    }

    #region ItemTemplate
    public static readonly BindableProperty ItemTemplateProperty =
        BindableProperty.CreateAttached(
            "ItemTemplate",
            typeof(DataTemplate),
            typeof(BindableLayout),
            null,
            propertyChanged: OnPropertyChanged);

    public static DataTemplate GetItemTemplate(BindableObject view) => (DataTemplate)view.GetValue(ItemTemplateProperty);

    public static void SetItemTemplate(BindableObject view, DataTemplate value) => view.SetValue(ItemTemplateProperty, value);
    #endregion

    #region ItemsSource
    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.CreateAttached(
            "ItemsSource",
            typeof(IEnumerable),
            typeof(BindableLayout),
            null,
            propertyChanged: OnPropertyChanged);

    public static IEnumerable GetItemsSource(BindableObject view) => (IEnumerable)view.GetValue(ItemsSourceProperty);

    public static void SetItemsSource(BindableObject view, IEnumerable value) => view.SetValue(ItemsSourceProperty, value);
    #endregion

    private Layout? _layout;

    protected override void OnAttachedTo(Layout bindable)
    {
        base.OnAttachedTo(bindable);

        _layout = bindable;

        Init(true);

        bindable.PropertyChanging += Layout_PropertyChanging;
        bindable.PropertyChanged += Layout_PropertyChanged;
    }

    protected override void OnDetachingFrom(Layout bindable)
    {
        bindable.PropertyChanging -= Layout_PropertyChanging;
        bindable.PropertyChanged -= Layout_PropertyChanged;

        Reset(true);

        _layout = null;

        base.OnDetachingFrom(bindable);
    }

    private void Layout_PropertyChanging(object sender, PropertyChangingEventArgs e)
    {
        if (e.PropertyName == BindableLayout.ItemsSourceProperty.PropertyName)
        {
            Reset();
        }
        else if (e.PropertyName == BindableLayout.ItemTemplateProperty.PropertyName)
        {
            if (sender is Layout layout)
            {
                ClearItems(layout);
            }
        }
    }

    private void Layout_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == BindableLayout.ItemsSourceProperty.PropertyName)
        {
            Init();
        }
        else if (e.PropertyName == BindableLayout.ItemTemplateProperty.PropertyName)
        {
            if (sender is Layout layout && GetItemsSource(layout) is { } enumerable)
            {
                AddItems(layout, enumerable, 0);
            }
        }
    }

    private void Reset(bool isDetaching = false)
    {
        if (_layout is null) return;

        var source = GetItemsSource(_layout);

        if (source is INotifyCollectionChanged collectionChanged)
        {
            collectionChanged.CollectionChanged -= CollectionChanged_CollectionChanged;
        }

        if (!isDetaching)
        {
            if (source is IEnumerable enumerable)
            {
                ClearItems(_layout);
            }
        }
    }

    private void Init(bool isAttaching = false)
    {
        if (_layout is null) return;

        if (isAttaching)
        {
            ClearItems(_layout);
        }

        var source = GetItemsSource(_layout);

        if (source is IEnumerable enumerable)
        {
            AddItems(_layout, enumerable, 0);
        }

        if (source is INotifyCollectionChanged collectionChanged)
        {
            collectionChanged.CollectionChanged += CollectionChanged_CollectionChanged;
        }
    }

    private void CollectionChanged_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_layout is not { } layout || sender is not { }) return;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                AddItems(layout, e.NewItems, e.NewStartingIndex);
                break;
            case NotifyCollectionChangedAction.Remove:
                RemoveItems(layout, e.OldItems, e.OldStartingIndex);
                break;
            case NotifyCollectionChangedAction.Replace:
                RemoveItems(layout, e.OldItems, e.OldStartingIndex);
                AddItems(layout, e.NewItems, e.NewStartingIndex);
                break;
            case NotifyCollectionChangedAction.Move:
                MoveItems(layout, e.OldItems, e.OldStartingIndex, e.NewStartingIndex);
                break;
            case NotifyCollectionChangedAction.Reset:
                ClearItems(layout);
                if (GetItemsSource(layout) is { } source)
                {
                    AddItems(layout, source, 0);
                }
                break;
        }
    }

    public static void ClearItems(Layout layout)
    {
        var items = layout.Children.OfType<VisualElement>().ToList();
        layout.Clear();
        foreach (var item in items)
        {
            DisconnectItem(item);
        }

        RequestParentRelayout(layout, triggerCellRefresh: false);
    }

    private static void AddItems(Layout layout, IEnumerable? items, int index)
    {
        if (items is null) return;

        foreach (var item in items)
        {
            var template = GetItemTemplate(layout);
            if (template is null) continue;

            while (template is DataTemplateSelector selector)
            {
                template = selector.SelectTemplate(item, layout);
            }

            if (template.CreateContent() is not View view) continue;
            view.BindingContext = item;
            layout.Insert(index++, view);
        }

        RequestParentRelayout(layout, triggerCellRefresh: true);
    }

    private static void RemoveItems(Layout layout, IEnumerable? items, int index)
    {
        if (items is null) return;

        foreach (var item in items)
        {
            var view = layout.ElementAt(index) as VisualElement;
            layout.RemoveAt(index);

            DisconnectItem(view);
        }

        RequestParentRelayout(layout, triggerCellRefresh: false);
    }

    private static void MoveItems(Layout layout, IEnumerable? items, int oldIndex, int newIndex)
    {
        if (items is null) return;

        foreach (var item in items)
        {
            layout.Move(oldIndex, newIndex);
        }

        RequestParentRelayout(layout, triggerCellRefresh: false);
    }

    public static void DisconnectItem(VisualElement? visualElement)
    {
        if (visualElement is null) return;

        visualElement.BindingContext = null;
        visualElement.Behaviors?.Clear();
        visualElement.DisconnectHandlers();
    }

    private static void RequestParentRelayout(Layout layout, bool triggerCellRefresh)
    {
#if MACIOS
        (layout as IView)?.InvalidateMeasure();
        (layout.Parent as IView)?.InvalidateMeasure();

        if (!triggerCellRefresh || layout.Parent is not CellHolder holder || !holder.Attached)
        {
            return;
        }

        holder.NotifyBound();
#endif
    }
}
