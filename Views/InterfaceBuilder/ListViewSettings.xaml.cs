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
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash
{
    /// <summary>
    /// Settings pane that shows up in interfacebuilder when listview is selected (as composite view) 
    /// </summary>
    public sealed partial class ListViewSettings : UserControl
    {
        public ListViewSettings()
        {
            this.InitializeComponent();
        }

        public ListViewSettings(DocumentController docController, Context context) : this()
        {
            Debug.Assert(docController.DocumentType.Equals(ListViewLayout.DocumentType));

            xSizeRow.Children.Add(new SizeSettings(docController, context));
            xPositionRow.Children.Add(new PositionSettings(docController, context));
            xAlignmentRow.Children.Add(new AlignmentSettings(docController, context));
            BindSpacing(docController, context, xSpacingTextbox);
        }

        /// <summary>
        /// Bind the value of spacingSlider to the spacing between the items in listview  
        /// </summary>
        public static void BindSpacing(DocumentController docController, Context context, TextBox tb)
        {
            var spacingController =
                    docController.GetDereferencedField(ListViewLayout.SpacingKey, context) as NumberController;
            Debug.Assert(spacingController != null);

            var spacingBinding = new Binding()
            {
                Source = spacingController,
                Path = new PropertyPath(nameof(spacingController.Data)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            tb.SetBinding(TextBox.TextProperty, spacingBinding);
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
