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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class HierarchicalMenu : UserControl
    {
        private bool _isCloseAnimationPlaying;
        private bool _isOpenAnimationPlaying;
        public bool IsPaneVisible
        {
            get
            {
                if (xPaneGrid.Visibility == Visibility.Visible)
                    return true;
                return false;
                ;
            }
            set
            {
                if (value)
                    xPaneGrid.Visibility = Visibility.Visible;
                else
                    xPaneGrid.Visibility = Visibility.Collapsed;
            }
        }
        public HierarchicalMenu()
        {
            this.InitializeComponent();
        }

        private void UIElement_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var deltaX = e.Delta.Translation.X;
            var newWidth = xPaneGrid.Width + deltaX;
            if (newWidth > 100)
            {
                xPaneGrid.Width = newWidth;
            }
            e.Handled = true;
        }

        private void XPaneGrid_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (xPaneGridTranslateTransform.X < -0.3 * xPaneGrid.Width)
            {
                if (!_isCloseAnimationPlaying)
                {
                    this.CreateAndPlayClosePaneAnimation();
                }
            }
            else
            {
                var deltaX = e.Delta.Translation.X;
                if (e.Position.X < xPaneGrid.Width - 10 && deltaX < 0)
                {
                    xPaneGridTranslateTransform.X += deltaX;
                }
            }
        }

        private void XPaneGrid_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            xPaneGridTranslateTransform.X = 0;
        }

        private void CreateAndPlayClosePaneAnimation()
        {
            Duration duration = new Duration(TimeSpan.FromSeconds(0.5));

            var storyboard = new Storyboard();
            var doubleAnimation = new DoubleAnimation();
            doubleAnimation.Duration = duration;
            doubleAnimation.SpeedRatio = 2;
            doubleAnimation.EnableDependentAnimation = true;
            doubleAnimation.From = xPaneGridTranslateTransform.X;
            doubleAnimation.To = -xPaneGrid.Width;
            Storyboard.SetTargetProperty(doubleAnimation, "X");
            Storyboard.SetTarget(doubleAnimation, xPaneGridTranslateTransform);
            storyboard.Children.Add(doubleAnimation);
            storyboard.Begin();
            _isCloseAnimationPlaying = true;
            storyboard.Completed += delegate
            {
                IsPaneVisible = false;
                xPaneGridTranslateTransform.X = 0;
                _isCloseAnimationPlaying = false;
            };
        }

        private void CreateAndPlayOpenPaneAnimation()
        {
            xPaneGrid.Visibility = Visibility.Visible;

            Duration duration = new Duration(TimeSpan.FromSeconds(0.5));

            var storyboard = new Storyboard();
            var doubleAnimation = new DoubleAnimation();
            doubleAnimation.Duration = duration;
            doubleAnimation.SpeedRatio = 2;
            doubleAnimation.EnableDependentAnimation = true;
            doubleAnimation.From = -xPaneGrid.Width;
            doubleAnimation.To = 0;
            Storyboard.SetTargetProperty(doubleAnimation, "X");
            Storyboard.SetTarget(doubleAnimation, xPaneGridTranslateTransform);
            storyboard.Children.Add(doubleAnimation);
            storyboard.Begin();
            _isOpenAnimationPlaying = true;
            storyboard.Completed += delegate
            {
                _isOpenAnimationPlaying = false;
            };
        }

        public void OpenPane()
        {
            if (!_isOpenAnimationPlaying && !IsPaneVisible)
                this.CreateAndPlayOpenPaneAnimation();
        }

        private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (!_isCloseAnimationPlaying)
            {
                this.CreateAndPlayClosePaneAnimation();
            }
        }
    }
}
