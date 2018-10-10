using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Toolkit.Uwp.UI.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class SplitManager : UserControl
    {
        public SplitManager()
        {
            this.InitializeComponent();
        }

        public enum SplitMode
        {
            Vertical, Horizontal, Content
        }

        public SplitMode CurSplitMode { get; private set; } = SplitMode.Content;
        private SplitMode _allowedSplits = SplitMode.Content;

        public ColumnDefinitionCollection Columns => XContentGrid.ColumnDefinitions;
        public RowDefinitionCollection Rows => XContentGrid.RowDefinitions;

        public IEnumerable<SplitFrame> GetChildFrames()
        {
            foreach (var child in XContentGrid.Children)
            {
                switch (child)
                {
                    case SplitFrame frame:
                        yield return frame;
                        break;
                    case SplitManager manager:
                        foreach (var childFrame in manager.GetChildFrames())
                        {
                            yield return childFrame;
                        }

                        break;
                }
            }
        }

        public void SetContent(DocumentController document)
        {
            var viewCopy = document.GetViewCopy();
            viewCopy.SetWidth(double.NaN);
            viewCopy.SetHeight(double.NaN);
            var frame = new SplitFrame
            {
                DataContext = new DocumentViewModel(viewCopy) { Undecorated = true }
            };
            SetContent(frame);
        }

        private void SetContent(SplitFrame frame)
        {
            XContentGrid.Children.Clear();
            XContentGrid.RowDefinitions.Clear();
            XContentGrid.ColumnDefinitions.Clear();
            XContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            XContentGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            Grid.SetRow(frame, 0);
            Grid.SetColumn(frame, 0);
            XContentGrid.Children.Add(frame);
            CurSplitMode = SplitMode.Content;
        }

        public SplitFrame DocViewTrySplit(SplitFrame sender, SplitFrame.SplitEventArgs args)
        {
            SplitFrame newPane = null;
            switch (args.Direction)
            {
                case SplitFrame.SplitDirection.Down:
                case SplitFrame.SplitDirection.Up:
                    if (_allowedSplits != SplitMode.Horizontal)//Make new nested split manager and move split frame to new manager
                    {
                        if (CurSplitMode == SplitMode.Content)
                        {
                            sender.SplitCompleted += WrapFrameInManager;
                        }

                        bool up = args.Direction == SplitFrame.SplitDirection.Up;
                        var row = CurSplitMode == SplitMode.Content ? 0 : Grid.GetRow(sender.GetFirstAncestorOfType<SplitManager>());
                        CurSplitMode = SplitMode.Vertical;
                        var splitter = new GridSplitter
                        {
                            ResizeDirection = GridSplitter.GridResizeDirection.Rows,
                            ResizeBehavior = GridSplitter.GridResizeBehavior.PreviousAndNext
                        };
                        newPane = new SplitFrame()
                        {
                            DataContext = new DocumentViewModel(args.SplitDocument) { Undecorated = true }
                        };
                        var newManager = new SplitManager();
                        newManager.SetContent(newPane);
                        newManager._allowedSplits = SplitMode.Horizontal;
                        var height = 0.0;
                        if (args.AutoSize)
                        {
                            var count = 0;
                            foreach (var child in XContentGrid.Children)
                            {
                                if (child is SplitManager || child is SplitFrame)
                                {
                                    height += XContentGrid.RowDefinitions[Grid.GetRow(child as FrameworkElement)]
                                        .Height.Value;
                                    count++;
                                }
                            }

                            height /= count;
                        }
                        if (up)
                        {
                            XContentGrid.RowDefinitions.Insert(row + 1,
                                new RowDefinition { Height = new GridLength(height, GridUnitType.Star) }); //Content row
                            XContentGrid.RowDefinitions.Insert(row + 1,
                                new RowDefinition { Height = new GridLength(10) }); //Splitter row
                            Grid.SetRow(splitter, row + 1);
                            Grid.SetRow(newManager, row + 2);
                            UpdateRows(2, row + 1);
                            XContentGrid.Children.Add(splitter);
                            XContentGrid.Children.Add(newManager);
                        }
                        else
                        {
                            XContentGrid.RowDefinitions.Insert(row,
                                new RowDefinition() { Height = new GridLength(10) }); //Splitter row
                            XContentGrid.RowDefinitions.Insert(row,
                                new RowDefinition { Height = new GridLength(height, GridUnitType.Star) }); //Content row
                            Grid.SetRow(newManager, row);
                            Grid.SetRow(splitter, row + 1);
                            UpdateRows(2, row);
                            XContentGrid.Children.Add(splitter);
                            XContentGrid.Children.Add(newManager);
                        }

                    }

                    break;
                case SplitFrame.SplitDirection.Left:
                case SplitFrame.SplitDirection.Right:
                    if (_allowedSplits != SplitMode.Vertical)//Make new nested split manager and move split frame to new manager
                    {
                        if (CurSplitMode == SplitMode.Content)
                        {
                            sender.SplitCompleted += WrapFrameInManager;
                        }

                        bool left = args.Direction == SplitFrame.SplitDirection.Left;
                        var col = CurSplitMode == SplitMode.Content ? 0 : Grid.GetColumn(sender.GetFirstAncestorOfType<SplitManager>());
                        CurSplitMode = SplitMode.Horizontal;
                        var splitter = new GridSplitter
                        {
                            ResizeDirection = GridSplitter.GridResizeDirection.Columns,
                            ResizeBehavior = GridSplitter.GridResizeBehavior.PreviousAndNext
                        };
                        newPane = new SplitFrame()
                        {
                            DataContext = new DocumentViewModel(args.SplitDocument) { Undecorated = true, IsDimensionless= true }
                        };
                        var newManager = new SplitManager();
                        newManager.SetContent(newPane);
                        newManager._allowedSplits = SplitMode.Vertical;
                        var width = 0.0;
                        if (args.AutoSize)
                        {
                            var count = 0;
                            foreach (var child in XContentGrid.Children)
                            {
                                if (child is SplitManager || child is SplitFrame)
                                {
                                    width += XContentGrid.ColumnDefinitions[Grid.GetColumn(child as FrameworkElement)]
                                        .Width.Value;
                                    count++;
                                }
                            }

                            width /= count;
                        }
                        if (left)
                        {
                            XContentGrid.ColumnDefinitions.Insert(col + 1,
                                new ColumnDefinition { Width = new GridLength(width, GridUnitType.Star) }); //Content col
                            XContentGrid.ColumnDefinitions.Insert(col + 1,
                                new ColumnDefinition { Width = new GridLength(10) }); //Splitter col
                            Grid.SetColumn(splitter, col + 1);
                            Grid.SetColumn(newManager, col + 2);
                            UpdateCols(2, col + 1);
                            XContentGrid.Children.Add(splitter);
                            XContentGrid.Children.Add(newManager);
                        }
                        else
                        {
                            XContentGrid.ColumnDefinitions.Insert(col,
                                new ColumnDefinition() { Width = new GridLength(10) }); //Splitter col
                            XContentGrid.ColumnDefinitions.Insert(col,
                                new ColumnDefinition { Width = new GridLength(width, GridUnitType.Star) }); //Content col
                            Grid.SetColumn(newManager, col);
                            Grid.SetColumn(splitter, col + 1);
                            UpdateCols(2, col);
                            XContentGrid.Children.Add(splitter);
                            XContentGrid.Children.Add(newManager);
                        }
                    }

                    break;
            }

            return newPane;
        }

        private void WrapFrameInManager(SplitFrame frame)
        {
            frame.SplitCompleted -= WrapFrameInManager;
            //frame.Unloaded += Unloaded;
            XContentGrid.Children.Remove(frame);

            //async void Unloaded(object sender, RoutedEventArgs args)
            {
                //frame.Unloaded -= Unloaded;
                //await Task.Delay(5);
                var row = Grid.GetRow(frame);
                var col = Grid.GetColumn(frame);
                var nested = new SplitManager();
                nested.SetContent(frame);
                XContentGrid.Children.Add(nested);
                nested._allowedSplits =
                    CurSplitMode == SplitMode.Horizontal ? SplitMode.Vertical : SplitMode.Horizontal;
                Grid.SetRow(nested, row);
                Grid.SetColumn(nested, col);
            }
        }

        private void UpdateCols(int offset, int min)
        {
            foreach (var child in XContentGrid.Children)
            {
                var fe = child as FrameworkElement;
                var col = Grid.GetColumn(fe);
                if (col >= min)
                {
                    Grid.SetColumn(fe, col + offset);
                }
            }
        }

        private void UpdateRows(int offset, int min)
        {
            foreach (var child in XContentGrid.Children)
            {
                var fe = child as FrameworkElement;
                var row = Grid.GetRow(fe);
                if (row >= min)
                {
                    Grid.SetRow(fe, row + offset);
                }
            }
        }

        public void DeleteFrame(SplitManager childManager)
        {
            if (!XContentGrid.Children.Contains(childManager))
            {
                return;
            }

            if (CurSplitMode == SplitMode.Horizontal)
            {
                int index = Grid.GetColumn(childManager);
                DeleteFrame(index);
            }
            else if (CurSplitMode == SplitMode.Vertical)
            {
                int index = Grid.GetRow(childManager);
                DeleteFrame(index);
            }
        }

        public void DeleteFrame(int index)
        {
            bool updateActiveFrame = false;
            if (CurSplitMode == SplitMode.Horizontal)
            {
                if (index >= Columns.Count)
                {
                    return;
                }
                var splitterIndex = index == XContentGrid.ColumnDefinitions.Count - 1 ? index - 1 : index + 1;
                var eles = XContentGrid.Children.Where(ele =>
                {
                    var column = Grid.GetColumn((FrameworkElement)ele);
                    return column == index || column == splitterIndex;
                }).ToList();
                foreach (var uiElement in eles)
                {
                    updateActiveFrame |= uiElement is SplitManager sm && sm.GetChildFrames().Contains(SplitFrame.ActiveFrame);
                    XContentGrid.Children.Remove(uiElement);
                }

                if (index == XContentGrid.ColumnDefinitions.Count - 1)
                {
                    XContentGrid.ColumnDefinitions.RemoveAt(index);
                    XContentGrid.ColumnDefinitions.RemoveAt(index - 1);
                }
                else
                {
                    XContentGrid.ColumnDefinitions.RemoveAt(index + 1);
                    XContentGrid.ColumnDefinitions.RemoveAt(index);
                }
                UpdateCols(-2, index);
            }
            else if (CurSplitMode == SplitMode.Vertical)
            {
                if (index >= Rows.Count)
                {
                    return;
                }
                var splitterIndex = index == XContentGrid.RowDefinitions.Count - 1 ? index - 1 : index + 1;
                var eles = XContentGrid.Children.Where(ele =>
                {
                    var row = Grid.GetRow((FrameworkElement)ele);
                    return row == index || row == splitterIndex;
                }).ToList();
                foreach (var uiElement in eles)
                {
                    updateActiveFrame |= uiElement is SplitManager sm && sm.GetChildFrames().Contains(SplitFrame.ActiveFrame);
                    XContentGrid.Children.Remove(uiElement);
                }

                if (index == XContentGrid.RowDefinitions.Count - 1)
                {
                    XContentGrid.RowDefinitions.RemoveAt(index);
                    XContentGrid.RowDefinitions.RemoveAt(index - 1);
                }
                else
                {
                    XContentGrid.RowDefinitions.RemoveAt(index + 1);
                    XContentGrid.RowDefinitions.RemoveAt(index);
                }
                UpdateRows(-2, index);
            }

            if (updateActiveFrame)
            {
                SplitFrame.ActiveFrame = GetChildFrames().First();
            }

            //If there is only one child left after removing one, unwrap is from the SplitManager that it is in
            if (XContentGrid.Children.Count == 1)
            {
                var manager = (SplitManager)XContentGrid.Children.First();
                var children = manager.XContentGrid.Children.ToList();
                manager.XContentGrid.Children.Clear();
                XContentGrid.Children.Clear();
                foreach (var uiElement in children)
                {
                    XContentGrid.Children.Add(uiElement);
                }

                XContentGrid.ColumnDefinitions.Clear();
                foreach (var columnDefinition in manager.Columns)
                {
                    XContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = columnDefinition.Width });
                }
                XContentGrid.RowDefinitions.Clear();
                foreach (var rowDefinition in manager.Rows)
                {
                    XContentGrid.RowDefinitions.Add(new RowDefinition { Height = rowDefinition.Height });
                }
                if (manager.CurSplitMode == SplitMode.Content)
                {
                    CurSplitMode = SplitMode.Content;
                }
                else
                {
                    //TODO Splitting: Merge children with parents children because they must have the same CurSplitMode
                }
            }
        }
    }
}
