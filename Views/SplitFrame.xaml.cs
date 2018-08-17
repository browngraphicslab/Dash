using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class SplitFrame : UserControl
    {

        public DocumentView Document => XDocView;

        public static SplitFrame ActiveFrame { get; set; }

        public static void OpenInActiveFrame(DocumentController doc)
        {
            if (ActiveFrame.ViewModel.DataDocument.Equals(doc.GetDataDocument()))
            {
                return;
            }
            if (!double.IsNaN(doc.GetWidth()) || !double.IsNaN(doc.GetHeight()))
            {
                doc = doc.GetViewCopy();
                doc.SetWidth(double.NaN);
                doc.SetHeight(double.NaN);
            }

            ActiveFrame.DataContext = new DocumentViewModel(doc) { Undecorated = true };
        }

        public static void OpenInInactiveFrame(DocumentController doc, SplitFrame frame = null)
        {
            throw new NotImplementedException();
        }

        public DocumentViewModel ViewModel => DataContext as DocumentViewModel;

        public enum SplitDirection
        {
            Left, Right, Up, Down, None
        }

        public enum SplitMode
        {
            VerticalSplit, HorizontalSplit,
            VerticalCollapsePrevious, VerticalCollapseNext,
            HorizontalCollapsePrevious, HorizontalCollapseNext,
            None
        }

        public SplitMode CurrentSplitMode { get; private set; } = SplitMode.None;

        public DocumentController DocumentController => (DataContext as DocumentViewModel)?.DocumentController;

        public class SplitEventArgs
        {
            public DocumentController SplitDocument { get; }
            public SplitDirection Direction { get; }
            public bool Handled { get; set; }

            public SplitEventArgs(SplitDirection dir, DocumentController splitDocument)
            {
                Direction = dir;
                SplitDocument = splitDocument;
            }

        }

        public event Action<SplitFrame> SplitCompleted;

        public SplitFrame()
        {
            this.InitializeComponent();
            //XDocView.RemoveResizeHandlers();
            XTopRightResizer.Tapped += (sender, args) => SplitCompleted?.Invoke(this);
        }

        private void TrySplit(SplitDirection direction, DocumentController splitDoc)
        {
            foreach (var splitManager in this.GetAncestorsOfType<SplitManager>())
            {
                if (splitManager.DocViewTrySplit(this, new SplitEventArgs(direction, splitDoc)))
                {
                    break;
                }
            }

            CurrentSplitMode = (direction == SplitDirection.Left || direction == SplitDirection.Right) ? SplitMode.HorizontalSplit : SplitMode.VerticalSplit;
        }

        private void TopRightOnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            var x = e.Cumulative.Translation.X;
            var y = e.Cumulative.Translation.Y;
            var angle = Math.Atan2(y, x);
            angle = angle * 180 / Math.PI;
            if (angle > 135 || angle < -150)
            {
                TrySplit(SplitDirection.Left, DocumentController);
            }
            else if (angle <= 135 && angle > 60)
            {
                TrySplit(SplitDirection.Down, DocumentController);
            }
            else if (angle <= 60 && angle > -45)
            {
                CurrentSplitMode = SplitMode.HorizontalCollapseNext;
            }
            else
            {
                CurrentSplitMode = SplitMode.VerticalCollapsePrevious;
            }
        }

        private void BottomLeftOnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            var x = e.Cumulative.Translation.X;
            var y = e.Cumulative.Translation.Y;
            var angle = Math.Atan2(y, x);
            angle = angle * 180 / Math.PI;
            if (angle < 30 && angle > -45)
            {
                TrySplit(SplitDirection.Right, DocumentController);
            }
            else if (angle <= -45 && angle > -120)
            {
                TrySplit(SplitDirection.Up, DocumentController);
            }
            else if (angle <= -120 || angle > 135)
            {
                CurrentSplitMode = SplitMode.HorizontalCollapsePrevious;
            }
            else
            {
                CurrentSplitMode = SplitMode.VerticalCollapseNext;
            }
        }

        private void ResizeColumns(int offset, double diff)
        {
            var sms = this.GetAncestorsOfType<SplitManager>();
            var splitManager = sms.First(sm => sm.CurSplitMode == SplitManager.SplitMode.Horizontal);
            var parent = sms.First();
            var cols = splitManager.Columns;
            var col = splitManager == parent ? Grid.GetColumn(this) : Grid.GetColumn(parent);
            diff = cols[col].ActualWidth - diff < 0 ? cols[col].ActualWidth : diff;
            diff = cols[col + offset].ActualWidth + diff < 0 ? -cols[col + offset].ActualWidth : diff;
            cols[col].Width = new GridLength(cols[col].ActualWidth - diff, GridUnitType.Star);
            cols[col + offset].Width = new GridLength(cols[col + offset].ActualWidth + diff, GridUnitType.Star);
        }

        private void ResizeRows(int offset, double diff)
        {
            var sms = this.GetAncestorsOfType<SplitManager>();
            var splitManager = sms.First(sm => sm.CurSplitMode == SplitManager.SplitMode.Vertical);
            var parent = sms.First();
            var rows = splitManager.Rows;
            var row = splitManager == parent ? Grid.GetRow(this) : Grid.GetRow(parent);
            diff = rows[row].ActualHeight - diff < 0 ? rows[row].ActualHeight : diff;
            diff = rows[row + offset].ActualHeight + diff < 0 ? -rows[row + offset].ActualHeight : diff;
            rows[row].Height = new GridLength(rows[row].ActualHeight - diff, GridUnitType.Star);
            rows[row + offset].Height = new GridLength(rows[row + offset].ActualHeight + diff, GridUnitType.Star);
        }

        private void TopRightOnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (CurrentSplitMode == SplitMode.HorizontalSplit)
            {
                ResizeColumns(2, -e.Delta.Translation.X);
            }
            else if (CurrentSplitMode == SplitMode.VerticalSplit)
            {
                ResizeRows(-2, e.Delta.Translation.Y);
            }
        }

        private void BottomLeftOnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (CurrentSplitMode == SplitMode.HorizontalSplit)
            {
                ResizeColumns(-2, e.Delta.Translation.X);
            }
            else if (CurrentSplitMode == SplitMode.VerticalSplit)
            {
                ResizeRows(2, -e.Delta.Translation.Y);
            }
        }

        private void OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            switch (CurrentSplitMode)
            {
                case SplitMode.VerticalSplit:
                case SplitMode.HorizontalSplit:
                    SplitCompleted?.Invoke(this);
                    break;
                case SplitMode.VerticalCollapsePrevious:
                    {
                        var parent = this.GetFirstAncestorOfType<SplitManager>();
                        var splitManager = parent?.GetFirstAncestorOfType<SplitManager>();
                        if (splitManager?.CurSplitMode == SplitManager.SplitMode.Vertical)
                        {
                            int index = Grid.GetRow(parent);
                            splitManager.DeleteFrame(e.Cumulative.Translation.Y < 0 ? index - 2 : index);
                        }

                        break;
                    }
                case SplitMode.HorizontalCollapsePrevious:
                    {
                        var parent = this.GetFirstAncestorOfType<SplitManager>();
                        var splitManager = parent?.GetFirstAncestorOfType<SplitManager>();
                        if (splitManager?.CurSplitMode == SplitManager.SplitMode.Horizontal)
                        {
                            int index = Grid.GetColumn(parent);
                            splitManager.DeleteFrame(e.Cumulative.Translation.X < 0 ? index - 2 : index);
                        }

                        break;
                    }
                case SplitMode.VerticalCollapseNext:
                    {
                        var parent = this.GetFirstAncestorOfType<SplitManager>();
                        var splitManager = parent?.GetFirstAncestorOfType<SplitManager>();
                        if (splitManager?.CurSplitMode == SplitManager.SplitMode.Vertical)
                        {
                            int index = Grid.GetRow(parent);
                            splitManager.DeleteFrame(e.Cumulative.Translation.Y < 0 ? index : index + 2);
                        }

                        break;
                    }
                case SplitMode.HorizontalCollapseNext:
                    {
                        var parent = this.GetFirstAncestorOfType<SplitManager>();
                        var splitManager = parent?.GetFirstAncestorOfType<SplitManager>();
                        if (splitManager?.CurSplitMode == SplitManager.SplitMode.Horizontal)
                        {
                            int index = Grid.GetColumn(parent);
                            splitManager.DeleteFrame(e.Cumulative.Translation.Y < 0 ? index : index + 2);
                        }

                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
            CurrentSplitMode = SplitMode.None;
        }

        private void XDocView_DocumentSelected(DocumentView obj)
        {
            ActiveFrame = this;
        }

        private void XTopRightResizer_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            Grid.Children.Remove(XDocView);
            Grid.Children.Add(XDocView);
        }

        private SolidColorBrush Yellow = new SolidColorBrush(Color.FromArgb(127, 255, 215, 0));
        private SolidColorBrush Transparent = new SolidColorBrush(Colors.Transparent);

        private void DropTarget_OnDragEnter(object sender, DragEventArgs e)
        {
            (sender as Rectangle).Fill = Yellow;
        }

        private void DropTarget_OnDragLeave(object sender, DragEventArgs e)
        {
            (sender as Rectangle).Fill = Transparent;
        }

        private void XRightDropTarget_OnDrop(object sender, DragEventArgs e)
        {
            (sender as Rectangle).Fill = Transparent;
        }

        private void XLeftDropTarget_OnDrop(object sender, DragEventArgs e)
        {
            (sender as Rectangle).Fill = Transparent;
        }

        private void XBottomDropTarget_OnDrop(object sender, DragEventArgs e)
        {
            (sender as Rectangle).Fill = Transparent;
        }

        private void XTopDropTarget_OnDrop(object sender, DragEventArgs e)
        {
            (sender as Rectangle).Fill = Transparent;
        }
    }
}
