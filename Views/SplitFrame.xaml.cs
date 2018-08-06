using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class SplitFrame : UserControl
    {

        public enum SplitDirection
        {
            Left, Right, Up, Down, None
        }

        public DocumentController DocumentController => (DataContext as DocumentViewModel)?.DocumentController;

        public class SplitEventArgs
        {
            public SplitDirection Direction { get; }
            public bool Handled { get; set; }

            public SplitEventArgs(SplitDirection dir)
            {
                Direction = dir;
            }

        }

        public event Action<SplitFrame> SplitCompleted;

        public SplitFrame()
        {
            this.InitializeComponent();
        }

        private void TopLeftOnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (e.Cumulative.Translation.Y < 0 && e.Cumulative.Translation.X < 0)
            {
                e.Complete();
                return;
            }
            if (e.Cumulative.Translation.Y > e.Cumulative.Translation.X)
            {
                foreach (var splitManager in this.GetAncestorsOfType<SplitManager>())
                {
                    if (splitManager.DocViewTrySplit(this, new SplitEventArgs(SplitDirection.Down)))
                    {
                        break;
                    }
                }
                XTopLeftResizer.ManipulationMode = ManipulationModes.TranslateY;
            }
            else
            {
                foreach (var splitManager in this.GetAncestorsOfType<SplitManager>())
                {
                    if (splitManager.DocViewTrySplit(this, new SplitEventArgs(SplitDirection.Right)))
                    {
                        break;
                    }
                }
                XTopLeftResizer.ManipulationMode = ManipulationModes.TranslateX;
            }
        }

        private void TopLeftOnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var sms = this.GetAncestorsOfType<SplitManager>();
            if (XTopLeftResizer.ManipulationMode == ManipulationModes.TranslateX)
            {
                var splitManager = sms.First(sm => sm.CurSplitMode == SplitManager.SplitMode.Horizontal);
                var parent = sms.First();
                var cols = splitManager.Columns;
                var col = splitManager == parent ? Grid.GetColumn(this) : Grid.GetColumn(parent);
                double diff = e.Delta.Translation.X;
                diff = cols[col].ActualWidth - diff < 0 ? 0 : diff;
                diff = cols[col - 2].ActualWidth + diff < 0 ? 0 : diff;
                cols[col].Width = new GridLength(cols[col].ActualWidth - diff);
                cols[col - 2].Width = new GridLength(cols[col - 2].ActualWidth + diff);
            } else if (XTopLeftResizer.ManipulationMode == ManipulationModes.TranslateY)
            {
                var splitManager = sms.First(sm => sm.CurSplitMode == SplitManager.SplitMode.Vertical);
                var parent = sms.First();
                var rows = splitManager.Rows;
                var row = splitManager == parent ? Grid.GetRow(this) : Grid.GetRow(parent);
                double diff = e.Delta.Translation.Y;
                diff = rows[row].ActualHeight - diff < 0 ? rows[row].ActualHeight : diff;
                diff = rows[row - 2].ActualHeight + diff < 0 ? -rows[row - 2].ActualHeight : diff;
                rows[row].Height = new GridLength(rows[row].ActualHeight - diff);
                rows[row - 2].Height = new GridLength(rows[row - 2].ActualHeight + diff);
            }
        }

        private void TopLeftOnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            XTopLeftResizer.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            SplitCompleted?.Invoke(this);
        }

        private void BottomRightOnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            
        }

        private void BottomRightOnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            
        }

        private void BottomRightOnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            
        }
    }
}
