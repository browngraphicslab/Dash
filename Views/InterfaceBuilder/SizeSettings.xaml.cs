using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
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
    public sealed partial class SizeSettings : UserControl
    {
        public SizeSettings()
        {
            this.InitializeComponent();
        }


        public SizeSettings(DocumentController editedLayoutDocument, Context context) : this()
        {
            BindWidth(editedLayoutDocument, context);
            BindHeight(editedLayoutDocument, context);
        }

        private void BindHeight(DocumentController docController, Context context)
        {
            var heightController =
                docController.GetHeightField(context);
            Debug.Assert(heightController != null);

            var heightBinding = new Binding
            {
                Source = heightController,
                Path = new PropertyPath(nameof(heightController.Data)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            xHeightTextBox.SetBinding(TextBox.TextProperty, heightBinding);

        }


        private void BindWidth(DocumentController docController, Context context)
        {
            var widthController = docController.GetWidthField(context);
            Debug.Assert(widthController != null);

            var widthBinding = new Binding
            {
                Source = widthController,
                Path = new PropertyPath(nameof(widthController.Data)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            xWidthTextBox.SetBinding(TextBox.TextProperty, widthBinding);
        }

        #region ValueSlider

        private void XMovementDetectionGrid_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender == xXMovementDetectionGrid)
            {
                xWidthTextBox.Focus(FocusState.Programmatic);
                xXMovementDetectionGrid.IsHitTestVisible = false;
                xXMovementDetectionGrid.Visibility = Visibility.Collapsed;

            }
            else if (sender == xYMovementDetectionGrid)
            {
                xHeightTextBox.Focus(FocusState.Programmatic);
                xYMovementDetectionGrid.IsHitTestVisible = false;
                xYMovementDetectionGrid.Visibility = Visibility.Collapsed;
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

        private void XSizeTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender == xWidthTextBox)
            {
                xXMovementDetectionGrid.IsHitTestVisible = true;
                xXMovementDetectionGrid.Visibility = Visibility.Visible;
            }
            else if (sender == xHeightTextBox)
            {
                xYMovementDetectionGrid.IsHitTestVisible = true;
                xYMovementDetectionGrid.Visibility = Visibility.Visible;
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
                if (sender == xXMovementDetectionGrid)
                {
                    (xWidthDeduct.Child as TextBlock).FontSize = 20;
                    this.CreateAndRunOpacityAnimation(xWidthDeduct, xWidthDeduct.Opacity, 0.5);
                    (xWidthIncrement.Child as TextBlock).FontSize = 26;
                    this.CreateAndRunOpacityAnimation(xWidthIncrement, xWidthIncrement.Opacity, 1);
                    double currentValue = 0;
                    if (!xWidthTextBox.Text.Equals(string.Empty))
                    {
                        currentValue = double.Parse(xWidthTextBox.Text);
                    }
                    xWidthTextBox.SetValue(TextBox.TextProperty,
                        (currentValue + 1).ToString());
                }
                else if (sender == xYMovementDetectionGrid)
                {
                    (xHeightDeduct.Child as TextBlock).FontSize = 20;
                    this.CreateAndRunOpacityAnimation(xHeightDeduct, xHeightDeduct.Opacity, 0.5);
                    (xHeightIncrement.Child as TextBlock).FontSize = 26;
                    this.CreateAndRunOpacityAnimation(xHeightIncrement, xHeightIncrement.Opacity, 1);
                    double currentValue = 0;
                    if (!xHeightTextBox.Text.Equals(string.Empty))
                    {
                        currentValue = double.Parse(xHeightTextBox.Text);
                    }
                    xHeightTextBox.SetValue(TextBox.TextProperty,
                        (currentValue + 1).ToString());
                }
            }
            if (deltaX < 0)
            {
                if (sender == xXMovementDetectionGrid)
                {
                    (xWidthIncrement.Child as TextBlock).FontSize = 20;
                    this.CreateAndRunOpacityAnimation(xWidthIncrement, xWidthIncrement.Opacity, 0.5);
                    (xWidthDeduct.Child as TextBlock).FontSize = 26;
                    this.CreateAndRunOpacityAnimation(xWidthDeduct, xWidthDeduct.Opacity, 1);
                    double currentValue = 0;
                    if (!xWidthTextBox.Text.Equals(string.Empty))
                    {
                        double.TryParse(xWidthTextBox.Text, out currentValue);
                    }
                    if (currentValue != 0)
                    {
                        xWidthTextBox.SetValue(TextBox.TextProperty,
                            (currentValue - 1).ToString());
                    }
                }
                else if (sender == xYMovementDetectionGrid)
                {
                    (xHeightIncrement.Child as TextBlock).FontSize = 20;
                    this.CreateAndRunOpacityAnimation(xHeightIncrement, xHeightIncrement.Opacity, 0.5);
                    (xHeightDeduct.Child as TextBlock).FontSize = 26;
                    this.CreateAndRunOpacityAnimation(xHeightDeduct, xHeightDeduct.Opacity, 1);
                    double currentValue = 0;
                    if (!xHeightTextBox.Text.Equals(string.Empty))
                    {
                        double.TryParse(xHeightTextBox.Text, out currentValue);
                    }
                    if (currentValue != 0)
                    {
                        xHeightTextBox.SetValue(TextBox.TextProperty,
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
            (xWidthIncrement.Child as TextBlock).FontSize = 20;
            (xHeightIncrement.Child as TextBlock).FontSize = 20;
            (xWidthDeduct.Child as TextBlock).FontSize = 20;
            (xHeightDeduct.Child as TextBlock).FontSize = 20;
        }
        #endregion

    }
}
