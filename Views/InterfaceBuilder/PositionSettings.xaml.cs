using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using DashShared;
using Visibility = Windows.UI.Xaml.Visibility;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class PositionSettings : UserControl
    {
        public PositionSettings()
        {
            this.InitializeComponent();
        }


        public PositionSettings(DocumentController editedLayoutDocument, Context context): this()
        {
            BindPosition(editedLayoutDocument, context);
        }

        private void BindPosition(DocumentController docController, Context context)
        {
            var fmc = docController.GetField(KeyStore.PositionFieldKey);
            var positionController = docController.GetPositionField(context);
            Debug.Assert(positionController != null);

            var converter = new StringCoordinateToPointConverter(positionController.Data);

            var xPositionBinding = new Binding
            {
                Source = positionController,
                Path = new PropertyPath(nameof(positionController.Data)),
                Mode = BindingMode.TwoWay,
                Converter = converter,
                ConverterParameter = Coordinate.X,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            xHorizontalPositionTextBox.SetBinding(TextBox.TextProperty,xPositionBinding);

            var yPositionBinding = new Binding
            {
                Source = positionController,
                Path = new PropertyPath(nameof(positionController.Data)),
                Mode = BindingMode.TwoWay,
                Converter = converter,
                ConverterParameter = Coordinate.Y,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            xVerticalPositionTextBox.SetBinding(TextBox.TextProperty, yPositionBinding);
        }

        #region ValueSlider

        private void XMovementDetectionGrid_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender == xXMovementDetectionGrid)
            {
                xHorizontalPositionTextBox.Focus(FocusState.Programmatic);
                xXMovementDetectionGrid.IsHitTestVisible = false;
                xXMovementDetectionGrid.Visibility = Visibility.Collapsed;

            }
            else if (sender == xYMovementDetectionGrid)
            {
                xVerticalPositionTextBox.Focus(FocusState.Programmatic);
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

        private void XPositionTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender == xHorizontalPositionTextBox)
            {
                xXMovementDetectionGrid.IsHitTestVisible = true;
                xXMovementDetectionGrid.Visibility = Visibility.Visible;
            }
            else if (sender == xVerticalPositionTextBox)
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
                    (xHorizontalDeduct.Child as TextBlock).FontSize = 20;
                    this.CreateAndRunOpacityAnimation(xHorizontalDeduct, xHorizontalDeduct.Opacity, 0.5);
                    (xHorizontalIncrement.Child as TextBlock).FontSize = 26;
                    this.CreateAndRunOpacityAnimation(xHorizontalIncrement, xHorizontalIncrement.Opacity, 1);
                    double currentValue = 0;
                    if (!xHorizontalPositionTextBox.Text.Equals(string.Empty))
                    {
                        currentValue = double.Parse(xHorizontalPositionTextBox.Text);
                    }
                    xHorizontalPositionTextBox.SetValue(TextBox.TextProperty,
                        (currentValue + 1).ToString());
                }
                else if (sender == xYMovementDetectionGrid)
                {
                    (xVerticalDeduct.Child as TextBlock).FontSize = 20;
                    this.CreateAndRunOpacityAnimation(xVerticalDeduct, xVerticalDeduct.Opacity, 0.5);
                    (xVerticalIncrement.Child as TextBlock).FontSize = 26;
                    this.CreateAndRunOpacityAnimation(xVerticalIncrement, xVerticalIncrement.Opacity, 1);
                    double currentValue = 0;
                    if (!xVerticalPositionTextBox.Text.Equals(string.Empty))
                    {
                        currentValue = double.Parse(xVerticalPositionTextBox.Text);
                    }
                    xVerticalPositionTextBox.SetValue(TextBox.TextProperty,
                        (currentValue + 1).ToString());
                }
            }
            if (deltaX < 0)
            {
                if (sender == xXMovementDetectionGrid)
                {
                    (xHorizontalIncrement.Child as TextBlock).FontSize = 20;
                    this.CreateAndRunOpacityAnimation(xHorizontalIncrement, xHorizontalIncrement.Opacity, 0.5);
                    (xHorizontalDeduct.Child as TextBlock).FontSize = 26;
                    this.CreateAndRunOpacityAnimation(xHorizontalDeduct, xHorizontalDeduct.Opacity, 1);
                    double currentValue = 0;
                    if (!xHorizontalPositionTextBox.Text.Equals(string.Empty))
                    {
                        currentValue = double.Parse(xHorizontalPositionTextBox.Text);
                    }
                    xHorizontalPositionTextBox.SetValue(TextBox.TextProperty,
                        (currentValue - 1).ToString());
                }
                else if (sender == xYMovementDetectionGrid)
                {
                    (xVerticalIncrement.Child as TextBlock).FontSize = 20;
                    this.CreateAndRunOpacityAnimation(xVerticalIncrement, xVerticalIncrement.Opacity, 0.5);
                    (xVerticalDeduct.Child as TextBlock).FontSize = 26;
                    this.CreateAndRunOpacityAnimation(xVerticalDeduct, xVerticalDeduct.Opacity, 1);
                    double currentValue = 0;
                    if (!xVerticalPositionTextBox.Text.Equals(string.Empty))
                    {
                        currentValue = double.Parse(xVerticalPositionTextBox.Text);
                    }
                    xVerticalPositionTextBox.SetValue(TextBox.TextProperty,
                        (currentValue - 1).ToString());
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
            (xHorizontalIncrement.Child as TextBlock).FontSize = 20;
            (xVerticalIncrement.Child as TextBlock).FontSize = 20;
            (xHorizontalDeduct.Child as TextBlock).FontSize = 20;
            (xVerticalDeduct.Child as TextBlock).FontSize = 20;
        }

        #endregion
    }
}
