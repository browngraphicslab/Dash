using System;
using System.Collections.Generic;
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
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using DashShared;
using Visibility = Windows.UI.Xaml.Visibility;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class FreeformSettings : UserControl
    {
        private readonly DocumentController _dataDocument;
        private readonly Context _context;

        public DocumentController SelectedDocument { get; set; }

        public FreeformSettings()
        {
            this.InitializeComponent();
        }

        public FreeformSettings(DocumentController layoutDocument, DocumentController dataDocument, Context context): this()
        {
            SelectedDocument = layoutDocument;

            if (dataDocument == null)
            {
                xCollapsableDocRow.Height = new GridLength(0);
                TypeBlock.Text = layoutDocument.DocumentType.Type;
            }
            else
            {
                xDocRow.Children.Add(new DocumentSettings(layoutDocument, dataDocument, context));
                TypeBlock.Text =  "Document (" + layoutDocument.DocumentType.Type + ")";
            }

            if (layoutDocument.DocumentType.Type == "Spacing Layout")
            {
                xSpacingGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
                GridViewSettings.BindSpacing(layoutDocument, context, xSpacingTextbox); 
            } else if (layoutDocument.DocumentType.Type == "ListView Layout")
            {
                xSpacingGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
                ListViewSettings.BindSpacing(layoutDocument, context, xSpacingTextbox); 
            }

            xSizeRow.Children.Add(new SizeSettings(layoutDocument, context));
            xPositionRow.Children.Add(new PositionSettings(layoutDocument, context));
            xAlignmentRow.Children.Add(new AlignmentSettings(layoutDocument,context));
        }

        private void FreeformSettings_OnLoaded(object sender, RoutedEventArgs e)
        {
            ManipulationMode = ManipulationModes.All;
            var window = this.GetFirstAncestorOfType<WindowTemplate>();
            if (window != null) ManipulationDelta += window.HeaderOnManipulationDelta;
        }

        #region ValueSlider

        private void XMovementDetectionGrid_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender == xSpacingMovementDetectionGrid)
            {
                xSpacingTextbox.Focus(FocusState.Programmatic);
                xSpacingMovementDetectionGrid.IsHitTestVisible = false;
                xSpacingMovementDetectionGrid.Visibility = Visibility.Collapsed;

            }
        }

        private void XMovementDetectionGrid_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var grid = sender as Panel;
            var children = grid?.Children;
            if (children != null)
            {
                foreach (var child in children)
                {
                    this.CreateAndRunOpacityAnimation(child, 0, 0.5);
                    if ((string)(child as Border)?.Tag == "Deduct")
                    {
                        this.CreateAndRunRepositionAnimation(child, 100);
                    }
                    else if ((string)(child as Border)?.Tag == "Increment")
                    {
                        this.CreateAndRunRepositionAnimation(child, -100);
                    }
                }
            }
            e.Handled = true;
        }


        private void XMovementDetectionGrid_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            this.HideDeductAndIncrement(sender);
            e.Handled = true;
        }

        private void XMovementDetectionGrid_OnPointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            // event not firing?
            this.HideDeductAndIncrement(sender);
            e.Handled = true;
        }

        private void xSpacingTextbox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender == xSpacingTextbox)
            {
                xSpacingMovementDetectionGrid.IsHitTestVisible = true;
                xSpacingMovementDetectionGrid.Visibility = Visibility.Visible;
            }
        }

        private void CreateAndRunOpacityAnimation(UIElement target, double from, double to)
        {
            Duration duration = new Duration(TimeSpan.FromSeconds(0.3));

            DoubleAnimation opacityAnimation = new DoubleAnimation()
            {
                SpeedRatio = 2,
                From = from,
                To = to,
                Duration = duration,
                EnableDependentAnimation = true
            };
            Storyboard opacityStoryboard = new Storyboard()
            {
                Duration = duration
            };
            opacityStoryboard.Children.Add(opacityAnimation);
            Storyboard.SetTarget(opacityAnimation, target);
            Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
            opacityStoryboard.Begin();
        }

        private void CreateAndRunRepositionAnimation(UIElement target, double horizontalOffset)
        {
            Duration duration = new Duration(TimeSpan.FromSeconds(0.5));

            RepositionThemeAnimation repositionAnimation = new RepositionThemeAnimation()
            {
                SpeedRatio = 1.3,
                FromHorizontalOffset = horizontalOffset,
                Duration = duration
            };
            Storyboard repositionStoryboard = new Storyboard()
            {
                Duration = duration
            };
            repositionStoryboard.Children.Add(repositionAnimation);
            Storyboard.SetTarget(repositionAnimation, target);
            repositionStoryboard.Begin();
        }

        private void XMovementDetectionGrid_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var deltaX = e.Delta.Translation.X;
            if (deltaX > 0)
            {
                if (sender == xSpacingMovementDetectionGrid)
                {
                    (xDeduct.Child as TextBlock).FontSize = 20;
                    this.CreateAndRunOpacityAnimation(xDeduct, xDeduct.Opacity, 0.5);
                    (xIncrement.Child as TextBlock).FontSize = 26;
                    this.CreateAndRunOpacityAnimation(xIncrement, xIncrement.Opacity, 1);
                    double currentValue = 0;
                    if (!xSpacingTextbox.Text.Equals(string.Empty))
                    {
                        currentValue = double.Parse(xSpacingTextbox.Text);
                    }
                    if (currentValue < 50)
                    {
                        xSpacingTextbox.SetValue(TextBox.TextProperty,
                            (currentValue + 1).ToString());
                    }
                }
            }
            if (deltaX < 0)
            {
                if (sender == xSpacingMovementDetectionGrid)
                {
                    (xIncrement.Child as TextBlock).FontSize = 20;
                    this.CreateAndRunOpacityAnimation(xIncrement, xIncrement.Opacity, 0.5);
                    (xDeduct.Child as TextBlock).FontSize = 26;
                    this.CreateAndRunOpacityAnimation(xDeduct, xDeduct.Opacity, 1);
                    double currentValue = 0;
                    if (!xSpacingTextbox.Text.Equals(string.Empty))
                    {
                        currentValue = double.Parse(xSpacingTextbox.Text);
                    }
                    if (currentValue > 0)
                    {
                        xSpacingTextbox.SetValue(TextBox.TextProperty,
                            (currentValue - 1).ToString());
                    }
                }
            }
            e.Handled = true;
        }

        private void XMovementDetectionGrid_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            this.HideDeductAndIncrement(sender);
            e.Handled = true;
        }

        private void HideDeductAndIncrement(object sender)
        {
            var grid = sender as Panel;
            var children = grid?.Children;
            if (children != null)
            {
                foreach (var child in children)
                {
                    this.CreateAndRunOpacityAnimation(child, child.Opacity, 0);
                }
            }
            (xIncrement.Child as TextBlock).FontSize = 20;
            (xDeduct.Child as TextBlock).FontSize = 20;
        }

        #endregion
    }
}
