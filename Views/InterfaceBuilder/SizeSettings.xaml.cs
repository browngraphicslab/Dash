using System;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class SizeSettings : UserControl
    {
        public SizeSettings()
        {
            InitializeComponent();
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
            if (sender == xWidthMovementDetectionGrid)
            {
                xWidthTextBox.Focus(FocusState.Programmatic);
                xWidthMovementDetectionGrid.IsHitTestVisible = false;
                xWidthMovementDetectionGrid.Visibility = Visibility.Collapsed;

            }
            else if (sender == xHeightMovementDetectionGrid)
            {
                xHeightTextBox.Focus(FocusState.Programmatic);
                xHeightMovementDetectionGrid.IsHitTestVisible = false;
                xHeightMovementDetectionGrid.Visibility = Visibility.Collapsed;
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
                    CreateAndRunOpacityAnimation(child, 0, 0.5);
                    if ((string)(child as Border)?.Tag == "Deduct")
                    {
                        CreateAndRunRepositionAnimation(child, 100);
                    }
                    else if ((string)(child as Border)?.Tag == "Increment")
                    {
                        CreateAndRunRepositionAnimation(child, -100);
                    }
                }
            }
            e.Handled = true;
        }


        private void XMovementDetectionGrid_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            HideDeductAndIncrement(sender);
            e.Handled = true;
        }

        private void XMovementDetectionGrid_OnPointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            // event not firing?
            HideDeductAndIncrement(sender);
            e.Handled = true;
        }

        private void XSizeTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender == xWidthTextBox)
            {
                xWidthMovementDetectionGrid.IsHitTestVisible = true;
                xWidthMovementDetectionGrid.Visibility = Visibility.Visible;
            }
            else if (sender == xHeightTextBox)
            {
                xHeightMovementDetectionGrid.IsHitTestVisible = true;
                xHeightMovementDetectionGrid.Visibility = Visibility.Visible;
            }
        }

        private void CreateAndRunOpacityAnimation(UIElement target, double from, double to)
        {
            Duration duration = new Duration(TimeSpan.FromSeconds(0.3));

            DoubleAnimation opacityAnimation = new DoubleAnimation
            {
                SpeedRatio = 2,
                From = from,
                To = to,
                Duration = duration,
                EnableDependentAnimation = true
            };
            Storyboard opacityStoryboard = new Storyboard
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

            RepositionThemeAnimation repositionAnimation = new RepositionThemeAnimation
            {
                SpeedRatio = 1.3,
                FromHorizontalOffset = horizontalOffset,
                Duration = duration
            };
            Storyboard repositionStoryboard = new Storyboard
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
                if (sender == xWidthMovementDetectionGrid)
                {
                    ChangeTextBoxText(xWidthMovementDetectionGrid, xWidthTextBox, true, false);
                }
                else if (sender == xHeightMovementDetectionGrid)
                {
                    ChangeTextBoxText(xHeightMovementDetectionGrid, xHeightTextBox, true, false);
                }
            }
            if (deltaX < 0)
            {
                if (sender == xWidthMovementDetectionGrid)
                {


                    ChangeTextBoxText(xWidthMovementDetectionGrid, xWidthTextBox, false, false);
                }
                else if (sender == xHeightMovementDetectionGrid)
                {
                    ChangeTextBoxText(xHeightMovementDetectionGrid, xHeightTextBox, false, false);
                }
            }
            e.Handled = true;
        }

        private void ChangeTextBoxText(Grid sender, TextBox textbox, bool isIncrement, bool canBeNegative)
        {
            var children = sender.Children;
            Border increment = null;
            Border deduct = null;
            foreach (var child in children)
            {
                if ((string)(child as Border).Tag == "Increment")
                {
                    increment = child as Border;
                }
                else if ((string)(child as Border).Tag == "Deduct")
                {
                    deduct = child as Border;
                }
            }


            double currentValue = 0;
            if (!textbox.Text.Equals(string.Empty))
            {
                currentValue = double.Parse(textbox.Text);
            }
            if (isIncrement)
            {
                (deduct.Child as TextBlock).FontSize = 20;
                CreateAndRunOpacityAnimation(deduct, deduct.Opacity, 0.5);
                (increment.Child as TextBlock).FontSize = 26;
                CreateAndRunOpacityAnimation(increment, increment.Opacity, 1);
                textbox.SetValue(TextBox.TextProperty,
                    (currentValue + 1).ToString());
            }
            else
            {
                (increment.Child as TextBlock).FontSize = 20;
                CreateAndRunOpacityAnimation(increment, increment.Opacity, 0.5);
                (deduct.Child as TextBlock).FontSize = 26;
                CreateAndRunOpacityAnimation(deduct, deduct.Opacity, 1);
                if (canBeNegative)
                {
                    textbox.SetValue(TextBox.TextProperty,
                        (currentValue - 1).ToString());
                }
                else
                {
                    if (currentValue != 0)
                    {
                        textbox.SetValue(TextBox.TextProperty,
                            (currentValue - 1).ToString());
                    }
                }
            }
        }

        private void XMovementDetectionGrid_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            HideDeductAndIncrement(sender);
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
                    CreateAndRunOpacityAnimation(child, child.Opacity, 0);
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
