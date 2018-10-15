using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Dash
{
    public class SplitPane : Panel
    {
        private Dictionary<SplitDefinition, List<FrameworkElement>> _resizerMap = new Dictionary<SplitDefinition, List<FrameworkElement>>();

        public IEnumerable<UIElement> ItemSource { get; set; }

        public static readonly DependencyProperty SplitDefinitionProperty = DependencyProperty.Register(
            "SplitDefinition", typeof(SplitDefinition), typeof(SplitPane), new PropertyMetadata(default(SplitDefinition), PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            ((SplitPane)obj).SplitDefinitionChanged(args);
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

        private SolidColorBrush _draggerBrush = new SolidColorBrush(Colors.DarkGray);

        public void ResizeSplits(SplitDefinition split1, SplitDefinition split2, Point delta)
        {
            Debug.Assert(split1.Parent != null && split1.Parent == split2.Parent);
            double d = 0;
            switch (split1.Parent.Mode)
            {
            case SplitMode.Horizontal:
                foreach (var splitDefinition in split1.Parent.Children)
                {
                    splitDefinition.Size = splitDefinition.ActualWidth;
                }

                d = delta.X;
                break;
            case SplitMode.Vertical:
                foreach (var splitDefinition in split1.Parent.Children)
                {
                    splitDefinition.Size = splitDefinition.ActualHeight;
                }

                d = delta.Y;
                break;
            }

            d = d > split2.Size ? split2.Size : d;
            d = d < -split1.Size ? -split1.Size : d;
            split1.Size += d;
            split2.Size -= d;
            split1.Size = Math.Max(split1.Size, 0);
            split2.Size = Math.Max(split2.Size, 0);
        }

        private void SplitOnUpdated(SplitDefinition sender, SplitUpdatedEventArgs args)
        {
            if (args.Type == SplitUpdatedEventArgs.UpdateType.ChildrenChanged ||
                args.Type == SplitUpdatedEventArgs.UpdateType.ModeChanged)
            {
                void UpdateList(SplitDefinition split, List<FrameworkElement> draggers)
                {
                    var manipulationMode = split.Mode == SplitMode.Horizontal
                        ? ManipulationModes.TranslateX
                        : ManipulationModes.TranslateY;

                    void ManipDelta(object manipSender, ManipulationDeltaRoutedEventArgs deltaArgs)
                    {
                        var delta = deltaArgs.Delta.Translation;
                        var dragger = (FrameworkElement) manipSender;
                        var (child1, child2) = ((SplitDefinition, SplitDefinition))dragger.DataContext;
                        ResizeSplits(child1, child2, delta);
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
                        SetIsSplitPaneResizer(dragger, true);
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
            "SplitLocation", typeof(SplitDefinition), typeof(SplitPane), new PropertyMetadata(default(SplitDefinition)));

        public static void SetSplitLocation(DependencyObject element, SplitDefinition value)
        {
            element.SetValue(SplitLocationProperty, value);
        }

        public static SplitDefinition GetSplitLocation(DependencyObject element)
        {
            return (SplitDefinition)element.GetValue(SplitLocationProperty);
        }

        private static readonly DependencyProperty IsSplitPaneResizerProperty = DependencyProperty.RegisterAttached(
            "IsSplitPaneResizer", typeof(bool), typeof(SplitPane), new PropertyMetadata(default(bool)));

        private static void SetIsSplitPaneResizer(DependencyObject element, bool value)
        {
            element.SetValue(IsSplitPaneResizerProperty, value);
        }

        private static bool GetIsSplitPaneResizer(DependencyObject element)
        {
            return (bool)element.GetValue(IsSplitPaneResizerProperty);
        }

        public void RemoveSplit(SplitDefinition split, SplitDefinition.JoinOption joinOption = SplitDefinition.JoinOption.JoinMiddle)
        {
            var parent = split.Parent;
            if (parent == null)
            {
                return;
            }
            parent.Remove(split, joinOption);
            if (parent.Children.Count == 1)
            {
                var childSplit = parent.Children[0];
                parent.CopyFrom(childSplit);

                foreach (var ele in Children.Where(child => GetSplitLocation(child) == childSplit))
                {
                    SetSplitLocation(ele, parent);
                }
            }

            var removedSplits = new HashSet<SplitDefinition>();
            var toVisit = new Stack<SplitDefinition>();
            toVisit.Push(split);
            while (toVisit.Any())
            {
                var s = toVisit.Pop();
                if (_resizerMap.TryGetValue(s, out var l))
                {
                    foreach (var frameworkElement in l)
                    {
                        Children.Remove(frameworkElement);
                    }
                    _resizerMap.Remove(s);
                }
                removedSplits.Add(s);
                foreach (var splitDefinition in s.Children)
                {
                    toVisit.Push(splitDefinition);
                }
            }

            Children.Where(child => removedSplits.Contains(GetSplitLocation(child))).ToList().ForEach(ele => Children.Remove(ele));
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
                switch (sd.Mode)
                {
                case SplitMode.Horizontal:
                    size = new Size(_draggerSize, sd.ActualHeight);
                    break;
                case SplitMode.Vertical:
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

            if (d.Mode == SplitMode.Horizontal)
            {
                availableSize.Width -= _draggerSize * (d.Children.Count - 1);
            }
            else if (d.Mode == SplitMode.Vertical)
            {
                availableSize.Height -= _draggerSize * (d.Children.Count - 1);
            }

            total = total == 0 ? 1 : total;
            foreach (var splitDefinition in d.Children)
            {
                Size size;
                Point pos = position;
                if (d.Mode == SplitMode.Horizontal)
                {
                    size = new Size(availableSize.Width * splitDefinition.Size / total, availableSize.Height);
                    position.X += size.Width + _draggerSize;
                }
                else if (d.Mode == SplitMode.Vertical)
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
                if (GetIsSplitPaneResizer(child))
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
                    switch (splitChild.Mode)
                    {
                    case SplitMode.Horizontal:
                        pos.X += s.ActualWidth;
                        dragger.Arrange(new Rect(pos, new Size(_draggerSize, splitChild.ActualHeight)));
                        pos.X += _draggerSize;
                        break;
                    case SplitMode.Vertical:
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
