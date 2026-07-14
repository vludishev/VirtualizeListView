using Microsoft.Maui;
using Microsoft.Maui.Controls;

using static MPowerKit.VirtualizeListView.DataAdapter;

namespace MPowerKit.VirtualizeListView.Tests;

[TestFixture]
public class RangeReplacementAnchorTests
{
    [TestCase(812d, 812d, 129d, 0d)]
    [TestCase(812d, 1000d, 129d, 188d)]
    public void VisibleReplacement_IsMeasuredBeforeAnchorAdjustment(
        double previousExtent,
        double replacementExtent,
        double scrollY,
        double expectedDelta)
    {
        var fixture = CreateFixture(previousExtent, replacementExtent, scrollY, anchorExtent: 20d);

        fixture.Manager.Replace(startingIndex: 0, oldCount: 1, newCount: 1);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Manager.Calls, Is.EqualTo(new[] { "update", "adjust" }));
            Assert.That(fixture.Manager.RequestedScrollDelta, Is.EqualTo(expectedDelta));
            Assert.That(fixture.Manager.GetMainExtent(fixture.Manager.Items[0]), Is.EqualTo(replacementExtent));
        });
    }

    [Test]
    public void HorizontalReplacement_IsMeasuredBeforeAnchorAdjustment()
    {
        var fixture = CreateFixture(
            previousExtent: 812d,
            replacementExtent: 1000d,
            scrollY: 129d,
            anchorExtent: 20d,
            orientation: ScrollOrientation.Horizontal);

        fixture.Manager.Replace(startingIndex: 0, oldCount: 1, newCount: 1);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Manager.Calls, Is.EqualTo(new[] { "update", "adjust" }));
            Assert.That(fixture.Manager.RequestedScrollDelta, Is.EqualTo(188d));
            Assert.That(fixture.Manager.GetMainExtent(fixture.Manager.Items[0]), Is.EqualTo(1000d));
        });
    }

    [Test]
    public void OffscreenSameTemplateReplacement_RetainsPreviousExtentWithoutEagerMeasurement()
    {
        var fixture = CreateFixture(
            previousExtent: 812d,
            replacementExtent: 1000d,
            scrollY: 1000d,
            anchorExtent: 1000d);

        fixture.Manager.Replace(startingIndex: 0, oldCount: 1, newCount: 1);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Manager.Calls, Is.EqualTo(new[] { "update", "adjust" }));
            Assert.That(fixture.Manager.MeasuredPositions, Is.Empty);
            Assert.That(fixture.Manager.GetMainExtent(fixture.Manager.Items[0]), Is.EqualTo(812d));
            Assert.That(fixture.Manager.RequestedScrollDelta, Is.Zero);
        });
    }

    [Test]
    public void RemovalBeforeAnchor_UpdatesLayoutBeforeAnchorAdjustment()
    {
        var fixture = CreateFixture(
            previousExtent: 812d,
            replacementExtent: 812d,
            scrollY: 129d,
            anchorExtent: 20d);

        fixture.Manager.Remove(startingIndex: 0, count: 1);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Manager.Calls, Is.EqualTo(new[] { "update", "adjust" }));
            Assert.That(fixture.Manager.RequestedScrollDelta, Is.EqualTo(-812d));
        });
    }

    [Test]
    public void RemovalFromStartWithoutVisibleAnchor_DoesNotReadBeforeCollectionStart()
    {
        var fixture = CreateFixture(
            previousExtent: 812d,
            replacementExtent: 812d,
            scrollY: 129d,
            anchorExtent: 20d);
        fixture.Manager.Items[1].Cell!.Attached = false;

        Assert.DoesNotThrow(() => fixture.Manager.Remove(startingIndex: 0, count: 1));
        Assert.Multiple(() =>
        {
            Assert.That(fixture.Manager.Calls, Is.EqualTo(new[] { "update" }));
            Assert.That(fixture.Manager.Items, Has.Count.EqualTo(1));
        });
    }

    private static Fixture CreateFixture(
        double previousExtent,
        double replacementExtent,
        double scrollY,
        double anchorExtent,
        ScrollOrientation orientation = ScrollOrientation.Vertical)
    {
        var template = new DataTemplate(() => new BoxView());
        var listView = new RecordingListView
        {
            Orientation = orientation
        };
        ((IScrollViewController)listView).SetScrolledPosition(
            orientation == ScrollOrientation.Horizontal ? scrollY : 0d,
            orientation == ScrollOrientation.Vertical ? scrollY : 0d);

        var replacementData = new ItemData(replacementExtent);
        var anchorData = new ItemData(anchorExtent);
        var adapter = new TestDataAdapter(listView, template);
        var replacementAdapterItem = new HeaderItem(replacementData);
        var anchorAdapterItem = new AdapterItem(anchorData);
        adapter.SetItems(replacementAdapterItem, anchorAdapterItem);

        var manager = new TestLayoutManager();
        manager.Initialize(
            listView,
            adapter,
            template,
            previousExtent,
            anchorAdapterItem,
            anchorExtent,
            orientation);

        return new(manager);
    }

    private sealed record Fixture(TestLayoutManager Manager);

    private sealed record ItemData(double Extent);

    private sealed class RecordingListView : VirtualizeListView
    {
        public override void AdjustScroll(double dx, double dy)
        {
        }
    }

    private sealed class TestDataAdapter(
        VirtualizeListView listView,
        DataTemplate template) : DataAdapter(listView)
    {
        public void SetItems(params AdapterItem[] items)
            => InternalItems = [.. items];

        public override DataTemplate GetTemplate(int position) => template;
    }

    private sealed class TestLayoutManager : VirtualizeItemsLayoutManger
    {
        public IReadOnlyList<VirtualizeListViewItem> Items => ReadOnlyLaidOutItems;
        public List<string> Calls { get; } = [];
        public List<int> MeasuredPositions { get; } = [];
        public double RequestedScrollDelta { get; private set; }

        public void Initialize(
            RecordingListView listView,
            TestDataAdapter adapter,
            DataTemplate template,
            double previousExtent,
            AdapterItem anchorAdapterItem,
            double anchorExtent,
            ScrollOrientation orientation)
        {
            ListView = listView;
            Adapter = adapter;
            AvailableSpace = orientation == ScrollOrientation.Vertical
                ? new(402d, 704d)
                : new(704d, 402d);

            LaidOutItems.Add(new(this)
            {
                AdapterItem = new HeaderItem(new ItemData(previousExtent)),
                Template = template,
                Position = 0,
                Size = CreateSize(previousExtent),
                LeftTopWithMargin = new(0d, 0d)
            });

            LaidOutItems.Add(new(this)
            {
                AdapterItem = anchorAdapterItem,
                Template = template,
                Position = 1,
                Size = CreateSize(anchorExtent),
                LeftTopWithMargin = CreatePoint(previousExtent),
                Cell = new CellHolder { Attached = true }
            });
        }

        public double GetMainExtent(VirtualizeListViewItem item)
            => IsVertical ? item.Size.Height : item.Size.Width;

        public void Replace(int startingIndex, int oldCount, int newCount)
            => AdapterItemRangeChanged(this, (startingIndex, oldCount, newCount));

        public void Remove(int startingIndex, int count)
            => AdapterItemRangeRemoved(this, (startingIndex, count));

        protected override bool DoesListViewHaveSize() => true;

        protected override void UpdateItemsLayout(int fromPosition, bool shouldAdjustScroll)
        {
            Calls.Add("update");

            for (var position = fromPosition; position < LaidOutItems.Count; position++)
            {
                var item = LaidOutItems[position];
                if (!item.IsOnScreen || item.IsAttached) continue;

                MeasuredPositions.Add(position);
                item.Size = CreateSize(((ItemData)item.AdapterItem!.Data).Extent);
                ShiftItemsChunk(LaidOutItems, position + 1, LaidOutItems.Count);
            }
        }

        public override VirtualizeListViewItem CreateItemForPosition(int position)
            => new(this)
            {
                AdapterItem = Adapter!.Items[position],
                Template = Adapter.GetTemplate(position),
                Position = position,
                Size = GetEstimatedItemSize(null!, AvailableSpace)
            };

        protected override void RepositionItemsFromIndex(
            IReadOnlyList<VirtualizeListViewItem> items,
            int index)
        {
            for (var position = index; position < items.Count; position++)
            {
                items[position].Position = position;
            }
        }

        protected override Thickness GetItemMargin(
            IReadOnlyList<VirtualizeListViewItem> items,
            VirtualizeListViewItem item) => new();

        protected override Size GetEstimatedItemSize(
            VirtualizeListViewItem item,
            Size availableSize) => IsVertical
                ? new(availableSize.Width, 200d)
                : new(200d, availableSize.Height);

        protected override Size MeasureItem(
            IReadOnlyList<VirtualizeListViewItem> items,
            VirtualizeListViewItem item,
            Size availableSpace) => item.Size;

        protected override void ArrangeItem(
            IReadOnlyList<VirtualizeListViewItem> items,
            VirtualizeListViewItem item,
            Size availableSpace)
        {
        }

        protected override void ShiftItemsChunk(
            IReadOnlyList<VirtualizeListViewItem> items,
            int start,
            int exclusiveEnd)
        {
            if (start < 0 || start >= exclusiveEnd || exclusiveEnd > items.Count) return;

            var coordinate = start == 0
                ? 0d
                : IsVertical
                    ? items[start - 1].RightBottomWithMargin.Y
                    : items[start - 1].RightBottomWithMargin.X;
            for (var position = start; position < exclusiveEnd; position++)
            {
                items[position].LeftTopWithMargin = CreatePoint(coordinate);
                coordinate = IsVertical
                    ? items[position].RightBottomWithMargin.Y
                    : items[position].RightBottomWithMargin.X;
            }
        }

        protected override void ShiftItemsConsecutively(
            IReadOnlyList<VirtualizeListViewItem> items,
            int start,
            int exclusiveEnd) => ShiftItemsChunk(items, start, exclusiveEnd);

        protected override void AdjustScrollForItemBoundsChange(
            IReadOnlyList<VirtualizeListViewItem> items,
            VirtualizeListViewItem item,
            Rect prevBoundsOfItem)
        {
        }

        protected override bool AdjustScrollIfNeeded(
            IReadOnlyList<VirtualizeListViewItem> items,
            VirtualizeListViewItem item,
            Rect prevBoundsOfItem)
        {
            Calls.Add("adjust");
            RequestedScrollDelta = IsVertical
                ? item.Bounds.Bottom - prevBoundsOfItem.Bottom
                : item.Bounds.Right - prevBoundsOfItem.Right;
            return RequestedScrollDelta != 0d;
        }

        private bool IsVertical => ListView!.IsOrientation(ScrollOrientation.Vertical);

        private Size CreateSize(double extent)
            => IsVertical
                ? new(AvailableSpace.Width, extent)
                : new(extent, AvailableSpace.Height);

        private Point CreatePoint(double coordinate)
            => IsVertical ? new(0d, coordinate) : new(coordinate, 0d);
    }
}
