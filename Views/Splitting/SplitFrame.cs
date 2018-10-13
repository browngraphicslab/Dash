using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Dash.Views.Splitting
{
    public enum SplitDirection
    {
        None,
        Horizontal,
        Vertical
    }

    public sealed class SplitDefinition : DependencyObject
    {
        public static SplitDirection GetOppositeDirection(SplitDirection dir)
        {
            if (dir == SplitDirection.Horizontal)
            {
                return SplitDirection.Vertical;
            }

            if (dir == SplitDirection.Vertical)
            {
                return SplitDirection.Horizontal;
            }

            return SplitDirection.None;
        }

        private readonly List<SplitDefinition> _childSplits = new List<SplitDefinition>();
        public IReadOnlyList<SplitDefinition> Children { get; }

        public static readonly DependencyProperty DirectionProperty = DependencyProperty.Register(
            "Direction", typeof(SplitDirection), typeof(SplitDefinition), new PropertyMetadata(SplitDirection.None, PropertyChangedCallback));

        public SplitDirection Direction
        {
            get => (SplitDirection)GetValue(DirectionProperty);
            set => SetValue(DirectionProperty, value);
        }

        public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
            "Size", typeof(double), typeof(SplitDefinition), new PropertyMetadata(1, PropertyChangedCallback));

        public double Size
        {
            get => (double)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var split = (SplitDefinition)dependencyObject;
            var type = args.Property == DirectionProperty
                ? SplitUpdatedEventArgs.UpdateType.DirectionChanged
                : SplitUpdatedEventArgs.UpdateType.SizeChanged;
            split.OnUpdated(split, new SplitUpdatedEventArgs(type));
        }

        public double ActualWidth { get; set; }
        public double ActualHeight { get; set; }
        public Point ActualPosition { get; set; }

        public SplitDefinition Parent { get; private set; }

        public SplitDefinition()
        {
            Children = new ReadOnlyCollection<SplitDefinition>(_childSplits);
        }

        public void AddRow(SplitDefinition row)
        {
            if (Direction == SplitDirection.Horizontal)
            {
                throw new ArgumentException("Can't add row to a horizontal split");
            }

            Direction = SplitDirection.Vertical;
            Add(row);
        }

        public void AddColumn(SplitDefinition col)
        {
            if (Direction == SplitDirection.Vertical)
            {
                throw new ArgumentException("Can't add row to a vertical split");
            }

            Direction = SplitDirection.Horizontal;
            Add(col);
        }

        public void Add(SplitDefinition child)
        {
            if (child.Direction == SplitDirection.None)
            {
                child.Direction = GetOppositeDirection(Direction);
            }

            child.Parent = this;
            _childSplits.Add(child);
            child.Updated += OnUpdated;
            OnUpdated(this, new SplitUpdatedEventArgs(SplitUpdatedEventArgs.UpdateType.ChildrenChanged));
        }

        public bool Remove(SplitDefinition child)
        {
            if (_childSplits.Remove(child))
            {
                child.Updated -= OnUpdated;
                child.Parent = null;
                OnUpdated(this, new SplitUpdatedEventArgs(SplitUpdatedEventArgs.UpdateType.ChildrenChanged));
                return true;
            }

            return false;
        }

        public delegate void SplitDefinitionUpdatedHandler(SplitDefinition sender, SplitUpdatedEventArgs args);
        public event SplitDefinitionUpdatedHandler Updated;
        private void OnUpdated(SplitDefinition sender, SplitUpdatedEventArgs e)
        {
            Updated?.Invoke(sender, e);
        }
    }

    public class SplitUpdatedEventArgs
    {
        public enum UpdateType
        {
            ChildrenChanged,
            SizeChanged,
            DirectionChanged,
        }

        public UpdateType Type { get; }

        public SplitUpdatedEventArgs(UpdateType type)
        {
            Type = type;
        }
    }

    public class SplitFrame : Panel
    {
        private Dictionary<SplitDefinition, List<FrameworkElement>> _resizerMap = new Dictionary<SplitDefinition, List<FrameworkElement>>();

        public IEnumerable<UIElement> ItemSource { get; set; }

        public static readonly DependencyProperty SplitDefinitionProperty = DependencyProperty.Register(
            "SplitDefinition", typeof(SplitDefinition), typeof(SplitFrame), new PropertyMetadata(default(SplitDefinition), PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            ((SplitFrame)obj).SplitDefinitionChanged(args);
        }

        private void SplitDefinitionChanged(DependencyPropertyChangedEventArgs args)
        {
            if (args.OldValue is SplitDefinition oldSplit)
            {
                oldSplit.Updated -= SplitOnUpdated;
            }
            if (args.NewValue is SplitDefinition newSplit)
            {
                newSplit.Updated += SplitOnUpdated;
            }
        }

        private SolidColorBrush _draggerBrush = new SolidColorBrush(Colors.Red);

        private void SplitOnUpdated(SplitDefinition sender, SplitUpdatedEventArgs args)
        {
            if (args.Type == SplitUpdatedEventArgs.UpdateType.ChildrenChanged ||
                args.Type == SplitUpdatedEventArgs.UpdateType.DirectionChanged)
            {
                void UpdateList(SplitDefinition split, List<FrameworkElement> draggers)
                {
                    var manipulationMode = split.Direction == SplitDirection.Horizontal
                        ? ManipulationModes.TranslateX
                        : ManipulationModes.TranslateY;

                    void ManipDelta(object manipSender, ManipulationDeltaRoutedEventArgs deltaArgs)
                    {
                        var delta = deltaArgs.Delta.Translation;
                        var dragger = (FrameworkElement) manipSender;
                        var (child1, child2) = ((SplitDefinition, SplitDefinition))dragger.DataContext;
                        double d = 0;
                        switch (dragger.ManipulationMode)
                        {
                            case ManipulationModes.TranslateX:
                                foreach (var splitDefinition in child1.Parent.Children)
                                {
                                    splitDefinition.Size = splitDefinition.ActualWidth;
                                }
                                d = delta.X;
                                break;
                            case ManipulationModes.TranslateY:
                                foreach (var splitDefinition in child1.Parent.Children)
                                {
                                    splitDefinition.Size = splitDefinition.ActualHeight;
                                }
                                d = delta.Y;
                                break;
                        }

                        d = d > child2.Size ? child2.Size : d;
                        d = d < -child1.Size ? -child1.Size : d;
                        child1.Size += d;
                        child2.Size -= d;
                        child1.Size = Math.Max(child1.Size, 0);
                        child2.Size = Math.Max(child2.Size, 0);
                    }
                    for (int i = 0; i < split.Children.Count - 1; ++i)
                    {
                        var child1 = split.Children[i];
                        var child2 = split.Children[i + 1];
                        var dragger = new Rectangle
                        {
                            Fill = _draggerBrush,
                            ManipulationMode = manipulationMode,
                            DataContext = (child1, child2)
                        };
                        dragger.ManipulationDelta += ManipDelta;
                        Children.Add(dragger);
                        draggers.Add(dragger);
                    }
                }
                if (_resizerMap.TryGetValue(sender, out var val))
                {
                    foreach (var frameworkElement in val)
                    {
                        Children.Remove(frameworkElement);
                    }
                    val.Clear();
                    UpdateList(sender, val);
                }
                else
                {
                    val = new List<FrameworkElement>();
                    UpdateList(sender, val);
                    _resizerMap[sender] = val;
                }
            }

            InvalidateMeasure();
        }

        public SplitDefinition SplitDefinition
        {
            get => (SplitDefinition)GetValue(SplitDefinitionProperty);
            set => SetValue(SplitDefinitionProperty, value);
        }

        public static readonly DependencyProperty SplitLocationProperty = DependencyProperty.RegisterAttached(
            "SplitLocation", typeof(SplitDefinition), typeof(SplitFrame), new PropertyMetadata(default(SplitDefinition)));

        public static void SetSplitLocation(DependencyObject element, SplitDefinition value)
        {
            element.SetValue(SplitLocationProperty, value);
        }

        public static SplitDefinition GetSplitLocation(DependencyObject element)
        {
            return (SplitDefinition)element.GetValue(SplitLocationProperty);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var split = SplitDefinition;
            if (split == null)
            {
                return new Size();
            }

            MeasureSplitDefinition(split, availableSize, new Point(0, 0));

            foreach (var child in Children)
            {
                if (child is FrameworkElement fe && fe.DataContext is SplitDefinition)
                {
                    continue;
                }

                var splitLoc = GetSplitLocation(child) ?? split;
                child.Measure(new Size(split.ActualWidth, splitLoc.ActualHeight));
            }

            foreach (var kvp in _resizerMap)
            {
                var sd = kvp.Key;
                var draggers = kvp.Value;
                Size size;
                switch (sd.Direction)
                {
                    case SplitDirection.Horizontal:
                        size = new Size(_draggerSize, sd.ActualHeight);
                        break;
                    case SplitDirection.Vertical:
                        size = new Size(sd.ActualWidth, _draggerSize);
                        break;
                    default:
                        size = new Size(sd.ActualWidth, sd.ActualHeight);
                        break;
                }
                foreach (var dragger in draggers)
                {
                    dragger.Measure(size);
                }
            }

            return availableSize;
        }

        private double _draggerSize = 10;

        private void MeasureSplitDefinition(SplitDefinition d, Size availableSize, Point position)
        {
            double total = 0;
            foreach (var splitDefinition in d.Children)
            {
                total += splitDefinition.Size;
            }

            d.ActualWidth = availableSize.Width;
            d.ActualHeight = availableSize.Height;
            d.ActualPosition = position;

            if (d.Direction == SplitDirection.Horizontal)
            {
                availableSize.Width -= _draggerSize * (d.Children.Count - 1);
            }
            else if (d.Direction == SplitDirection.Vertical)
            {
                availableSize.Height -= _draggerSize * (d.Children.Count - 1);
            }

            total = total == 0 ? 1 : total;
            foreach (var splitDefinition in d.Children)
            {
                Size size;
                Point pos = position;
                if (d.Direction == SplitDirection.Horizontal)
                {
                    size = new Size(availableSize.Width * splitDefinition.Size / total, availableSize.Height);
                    position.X += size.Width + _draggerSize;
                }
                else if (d.Direction == SplitDirection.Vertical)
                {
                    size = new Size(availableSize.Width, availableSize.Height * splitDefinition.Size / total);
                    position.Y += size.Height + _draggerSize;
                }
                else
                {
                    size = availableSize;
                }
                MeasureSplitDefinition(splitDefinition, size, pos);
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var split = SplitDefinition;
            if (SplitDefinition == null)
            {
                return finalSize;
            }
            foreach (var child in Children)
            {
                if (child is FrameworkElement fe && fe.DataContext is SplitDefinition)
                {
                    continue;
                }
                var splitLoc = GetSplitLocation(child) ?? split;
                child.Arrange(new Rect(splitLoc.ActualPosition, new Size(splitLoc.ActualWidth, splitLoc.ActualHeight)));
            }

            foreach (var kvp in _resizerMap)
            {
                var splitChild = kvp.Key;
                var eles = kvp.Value;
                var pos = splitChild.ActualPosition;

                for (int i = 0; i < eles.Count; ++i)
                {
                    var s = splitChild.Children[i];
                    var dragger = eles[i];
                    switch (splitChild.Direction)
                    {
                        case SplitDirection.Horizontal:
                            pos.X += s.ActualWidth;
                            dragger.Arrange(new Rect(pos, new Size(_draggerSize, splitChild.ActualHeight)));
                            pos.X += _draggerSize;
                            break;
                        case SplitDirection.Vertical:
                            pos.Y += s.ActualHeight;
                            dragger.Arrange(new Rect(pos, new Size(splitChild.ActualWidth, _draggerSize)));
                            pos.Y += _draggerSize;
                            break;
                    }
                }
            }

            return finalSize;
        }
    }
}
