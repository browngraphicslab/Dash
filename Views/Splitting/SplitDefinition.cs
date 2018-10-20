using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Dash
{
    public enum SplitMode
    {
        None,
        Horizontal,
        Vertical
    }

    public enum SplitDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    public class SplitUpdatedEventArgs
    {
        public enum UpdateType
        {
            ChildrenChanged,
            SizeChanged,
            ModeChanged,
        }

        public UpdateType Type { get; }

        public SplitUpdatedEventArgs(UpdateType type)
        {
            Type = type;
        }
    }

    public sealed class SplitDefinition : DependencyObject
    {
        public static SplitMode GetOppositeMode(SplitMode mode)
        {
            if (mode == SplitMode.Horizontal)
            {
                return SplitMode.Vertical;
            }

            if (mode == SplitMode.Vertical)
            {
                return SplitMode.Horizontal;
            }

            return SplitMode.None;
        }

        private readonly List<SplitDefinition> _childSplits = new List<SplitDefinition>();
        public ReadOnlyCollection<SplitDefinition> Children { get; }

        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
            "Mode", typeof(SplitMode), typeof(SplitDefinition), new PropertyMetadata(SplitMode.None, PropertyChangedCallback));

        public SplitMode Mode
        {
            get => (SplitMode)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

        public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
            "Size", typeof(double), typeof(SplitDefinition), new PropertyMetadata(1.0, PropertyChangedCallback));

        public double Size
        {
            get => (double)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var split = (SplitDefinition)dependencyObject;
            var type = args.Property == ModeProperty
                ? SplitUpdatedEventArgs.UpdateType.ModeChanged
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

        public void AddRow(SplitDefinition row, int? index = null)
        {
            if (Mode == SplitMode.Horizontal)
            {
                throw new ArgumentException("Can't add row to a horizontal split");
            }

            Mode = SplitMode.Vertical;
            Add(row, index);
        }

        public void AddColumn(SplitDefinition col, int? index = null)
        {
            if (Mode == SplitMode.Vertical)
            {
                throw new ArgumentException("Can't add row to a vertical split");
            }

            Mode = SplitMode.Horizontal;
            Add(col, index);
        }

        public void Add(SplitDefinition child, int? index = null)
        {
            if (child.Mode == SplitMode.None)
            {
                child.Mode = GetOppositeMode(Mode);
            }

            child.Parent = this;
            if (index is int i)
            {
                _childSplits.Insert(i, child);
            }
            else
            {
                _childSplits.Add(child);
            }

            child.Updated += OnUpdated;
            OnUpdated(this, new SplitUpdatedEventArgs(SplitUpdatedEventArgs.UpdateType.ChildrenChanged));
        }

        public enum JoinOption
        {
            JoinPrevious,
            JoinNext,
            JoinMiddle,
        }

        public bool Remove(SplitDefinition child, JoinOption option = JoinOption.JoinMiddle)
        {
            var index = _childSplits.IndexOf(child);
            if (index > 0 && option == JoinOption.JoinPrevious)
            {
                _childSplits[index - 1].Size += child.Size;
            } else if (index < _childSplits.Count - 1 && option == JoinOption.JoinNext)
            {
                _childSplits[index + 1].Size += child.Size;
            }
            if (_childSplits.Remove(child))
            {
                child.Updated -= OnUpdated;
                child.Parent = null;
                OnUpdated(this, new SplitUpdatedEventArgs(SplitUpdatedEventArgs.UpdateType.ChildrenChanged));
                return true;
            }

            return false;
        }

        public void Clear()
        {
            foreach (var child in _childSplits)
            {
                child.Updated -= OnUpdated;
                child.Parent = null;
            }
            _childSplits.Clear();
            OnUpdated(this, new SplitUpdatedEventArgs(SplitUpdatedEventArgs.UpdateType.ChildrenChanged));
        }

        public void CopyFrom(SplitDefinition other)
        {
            Mode = other.Mode;
            var children = other.Children.ToList();
            other.Clear();
            Clear();
            foreach (var splitDefinition in children)
            {
                Add(splitDefinition);
            }
        }

        public delegate void SplitDefinitionUpdatedHandler(SplitDefinition sender, SplitUpdatedEventArgs args);
        public event SplitDefinitionUpdatedHandler Updated;
        private void OnUpdated(SplitDefinition sender, SplitUpdatedEventArgs e)
        {
            Updated?.Invoke(sender, e);
        }
    }
}
