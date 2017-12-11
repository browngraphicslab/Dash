using System;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using DashShared;
using System.Collections.Generic;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Visibility = Windows.UI.Xaml.Visibility;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class ImageSettings : UserControl
    {
        public ImageSettings()
        {
            this.InitializeComponent();
        }

        public ImageSettings(DocumentController docController, Context context) : this()
        {
            Debug.Assert(docController.DocumentType.Equals(ImageBox.DocumentType), "You can only create image settings for an ImageBox");

            xSizeRow.Children.Add(new SizeSettings(docController, context));
            xCropRow.Children.Add(new ClipSettings(docController, context));
            xPositionRow.Children.Add(new PositionSettings(docController, context));
            xAlignmentRow.Children.Add(new AlignmentSettings(docController, context));
            BindOpacity(docController, context);
        }

        private void BindOpacity(DocumentController docController, Context context)
        {
            var opacityController =
                    docController.GetDereferencedField(ImageBox.OpacityKey, context) as NumberController;
            Debug.Assert(opacityController != null);

            var opacityBinding = new Binding
            {
                Source = opacityController,
                Path = new PropertyPath(nameof(opacityController.Data)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            xOpacityTextbox.SetBinding(TextBox.TextProperty, opacityBinding);
        }

        #region ValueSlider

        private void XMovementDetectionGrid_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender == xOpacityMovementDetectionGrid)
            {
                xOpacityTextbox.Focus(FocusState.Programmatic);
                xOpacityMovementDetectionGrid.IsHitTestVisible = false;
                xOpacityMovementDetectionGrid.Visibility = Visibility.Collapsed;

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

        private void XOpacityTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender == xOpacityTextbox)
            {
                xOpacityMovementDetectionGrid.IsHitTestVisible = true;
                xOpacityMovementDetectionGrid.Visibility = Visibility.Visible;
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
                if (sender == xOpacityMovementDetectionGrid)
                {
                    (xDeduct.Child as TextBlock).FontSize = 20;
                    this.CreateAndRunOpacityAnimation(xDeduct, xDeduct.Opacity, 0.5);
                    (xIncrement.Child as TextBlock).FontSize = 26;
                    this.CreateAndRunOpacityAnimation(xIncrement, xIncrement.Opacity, 1);
                    double currentValue = 0;
                    if (!xOpacityTextbox.Text.Equals(string.Empty))
                    {
                        currentValue = double.Parse(xOpacityTextbox.Text);
                    }
                    if (currentValue < 1)
                    {
                        xOpacityTextbox.SetValue(TextBox.TextProperty,
                            (currentValue + 0.005).ToString());
                    }
                }
            }
            if (deltaX < 0)
            {
                if (sender == xOpacityMovementDetectionGrid)
                {
                    (xIncrement.Child as TextBlock).FontSize = 20;
                    this.CreateAndRunOpacityAnimation(xIncrement, xIncrement.Opacity, 0.5);
                    (xDeduct.Child as TextBlock).FontSize = 26;
                    this.CreateAndRunOpacityAnimation(xDeduct, xDeduct.Opacity, 1);
                    double currentValue = 0;
                    if (!xOpacityTextbox.Text.Equals(string.Empty))
                    {
                        currentValue = double.Parse(xOpacityTextbox.Text);
                    }
                    if (currentValue > 0)
                    {
                        xOpacityTextbox.SetValue(TextBox.TextProperty,
                            (currentValue - 0.005).ToString());
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
