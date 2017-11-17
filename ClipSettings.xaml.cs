using Dash.Converters;
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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class ClipSettings : UserControl
    {
        private DocumentController _documentController;
        private Context _context;

        public ClipSettings()
        {
            this.InitializeComponent();
        }

        public ClipSettings(DocumentController editedLayoutDocument, Context context) : this()
        {
            _documentController = editedLayoutDocument;
            _context = context;
            BindClip(context);
        }

        private void BindClip(Context context)
        {
            var clipController =
                _documentController.GetDereferencedField(ImageBox.ClipKey, context) as RectController;
            Debug.Assert(clipController != null);
            InitializeClip(clipController);
        }

        private void InitializeClip(RectController clipController)
        {
            ClipBindingHelper(xClipXTextBox, "X", clipController.Data);
            ClipBindingHelper(xClipYTextBox, "Y", clipController.Data);
            ClipBindingHelper(xClipWidthTextBox, "Width", clipController.Data);
            ClipBindingHelper(xClipHeightTextBox, "Height", clipController.Data);
        }

        private void ClipBindingHelper(TextBox tb, string path, Rect clipRect)
        {
            var binding = new Binding
            {
                Source = clipRect,
                Path = new PropertyPath(path),
                Converter = new StringToDoubleConverter()
            };
            tb.SetBinding(TextBox.TextProperty, binding);
        }
        
        private RectController ClipController()
        {
            return _documentController.GetDereferencedField(ImageBox.ClipKey, _context) as RectController;
        }

        private void XClipXTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {

            var clipController = ClipController();
            Debug.Assert(clipController != null);
            double clipX;
            if (!double.TryParse(xClipXTextBox.Text, out clipX)) return;
            clipController.Data = new Rect(clipX, clipController.Data.Y, clipController.Data.Width,
                clipController.Data.Height);
        }

        private void XClipYTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            double clipY;
            if (!double.TryParse(xClipYTextBox.Text, out clipY)) return;
            var clipController = ClipController();
            Debug.Assert(clipController != null);
            clipController.Data = new Rect(clipController.Data.X, clipY, clipController.Data.Width, clipController.Data.Height);
        }

        private void XClipWidthTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            double clipWidth;
            if (!double.TryParse(xClipWidthTextBox.Text, out clipWidth)) return;
            var clipController = ClipController();
            Debug.Assert(clipController != null);
            clipController.Data = new Rect(clipController.Data.X, clipController.Data.Y, clipWidth, clipController.Data.Height);
        }

        private void XClipHeightTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            double clipHeight;
            if (!double.TryParse(xClipHeightTextBox.Text, out clipHeight)) return;
            var clipController = ClipController();
            Debug.Assert(clipController != null);
            clipController.Data = new Rect(clipController.Data.X, clipController.Data.Y, clipController.Data.Width, clipHeight);
        }

        #region ValueSlider

        private void XMovementDetectionGrid_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender == xXMovementDetectionGrid)
            {
                xClipXTextBox.Focus(FocusState.Programmatic);
                xXMovementDetectionGrid.IsHitTestVisible = false;
                xXMovementDetectionGrid.Visibility = Visibility.Collapsed;

            }
            else if (sender == xYMovementDetectionGrid)
            {
                xClipYTextBox.Focus(FocusState.Programmatic);
                xYMovementDetectionGrid.IsHitTestVisible = false;
                xYMovementDetectionGrid.Visibility = Visibility.Collapsed;
            }
            else if (sender == xWidthMovementDetectionGrid)
            {
                xClipWidthTextBox.Focus(FocusState.Programmatic);
                xWidthMovementDetectionGrid.IsHitTestVisible = false;
                xWidthMovementDetectionGrid.Visibility = Visibility.Collapsed;

            }
            else if (sender == xHeightMovementDetectionGrid)
            {
                xClipHeightTextBox.Focus(FocusState.Programmatic);
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

        private void XTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender == xClipXTextBox)
            {
                xXMovementDetectionGrid.IsHitTestVisible = true;
                xXMovementDetectionGrid.Visibility = Visibility.Visible;
            }
            else if (sender == xClipYTextBox)
            {
                xYMovementDetectionGrid.IsHitTestVisible = true;
                xYMovementDetectionGrid.Visibility = Visibility.Visible;
            }
            else if (sender == xClipWidthTextBox)
            {
                xWidthMovementDetectionGrid.IsHitTestVisible = true;
                xWidthMovementDetectionGrid.Visibility = Visibility.Visible;
            }
            else if (sender == xClipHeightTextBox)
            {
                xHeightMovementDetectionGrid.IsHitTestVisible = true;
                xHeightMovementDetectionGrid.Visibility = Visibility.Visible;
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
                    this.ChangeTextBoxText(xXMovementDetectionGrid, xClipXTextBox, true, false);
                }
                else if (sender == xYMovementDetectionGrid)
                {
                    this.ChangeTextBoxText(xYMovementDetectionGrid, xClipYTextBox, true, false);
                }
                else if (sender == xWidthMovementDetectionGrid)
                {
                    this.ChangeTextBoxText(xWidthMovementDetectionGrid, xClipWidthTextBox, true, false);
                }
                else if (sender == xHeightMovementDetectionGrid)
                {
                    this.ChangeTextBoxText(xHeightMovementDetectionGrid, xClipHeightTextBox, true, false);
                }
            }
            if (deltaX < 0)
            {
                if (sender == xXMovementDetectionGrid)
                {
                    this.ChangeTextBoxText(xXMovementDetectionGrid, xClipXTextBox, false, true);
                }
                else if (sender == xYMovementDetectionGrid)
                {
                    this.ChangeTextBoxText(xYMovementDetectionGrid, xClipYTextBox, false, true);
                }
                else if (sender == xWidthMovementDetectionGrid)
                {
                    this.ChangeTextBoxText(xWidthMovementDetectionGrid, xClipWidthTextBox, false, false);
                }
                else if (sender == xHeightMovementDetectionGrid)
                {
                    this.ChangeTextBoxText(xHeightMovementDetectionGrid, xClipHeightTextBox, false, false);
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
                if ((string) (child as Border).Tag == "Increment")
                {
                    increment = child as Border;
                } else if ((string) (child as Border).Tag == "Deduct")
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
                this.CreateAndRunOpacityAnimation(deduct, deduct.Opacity, 0.5);
                (increment.Child as TextBlock).FontSize = 26;
                this.CreateAndRunOpacityAnimation(increment, increment.Opacity, 1);
                textbox.SetValue(TextBox.TextProperty,
                    (currentValue + 1).ToString());
            }
            else
            {
                (increment.Child as TextBlock).FontSize = 20;
                this.CreateAndRunOpacityAnimation(increment, increment.Opacity, 0.5);
                (deduct.Child as TextBlock).FontSize = 26;
                this.CreateAndRunOpacityAnimation(deduct, deduct.Opacity, 1);
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
            (xWidthIncrement.Child as TextBlock).FontSize = 20;
            (xHeightIncrement.Child as TextBlock).FontSize = 20;
            (xWidthDeduct.Child as TextBlock).FontSize = 20;
            (xHeightDeduct.Child as TextBlock).FontSize = 20;
        }

        #endregion
    }
}
