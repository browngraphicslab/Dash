﻿using System;
using System.Collections.Generic;
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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace DocumentMenu
{
    public sealed partial class MenuButton : UserControl
    {
        private TextBlock _descriptionText;
        private Button _button;
        private Action _buttonAction;
        private double _verticalOffset;

        public bool RotateOnTap = false;

        //public bool IsVisible
        //{
        //    get { if(_button.Visibility == Visibility.Visible) { return true; } else { return false; }}
        //    set { if (value) { _button.Visibility = Visibility.Visible; _descriptionText.Visibility = Visibility.Visible; } else { _button.Visibility = Visibility.Collapsed; _descriptionText.Visibility = Visibility.Collapsed; } }
        //}

        public MenuButton(Symbol icon, string name, Color background, Action buttonAction)
        {
            this.InitializeComponent();
            _buttonAction = buttonAction;
            this.InstantiateButton(icon, name, background);
            this.CreateAndRunInstantiationAnimation();
        }

        /// <summary>
        /// Create a circular button with an icon and a string description
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="name"></param>
        /// <param name="background"></param>
        private void InstantiateButton(Symbol icon, string name, Color background)
        {
            // create symbol for button
            var symbol = new SymbolIcon()
            {
                Symbol = icon,
                Foreground = new SolidColorBrush(Colors.White)
            };
            // create rounded(circular) border to hold the symbol
            var border = new Border()
            {
                Height = 40,
                Width = 40,
                CornerRadius = new CornerRadius(20),
                Background = new SolidColorBrush(background),
                BorderBrush = new SolidColorBrush(background),
                Child = symbol
            };
            // create button to contain the border with the symbol
            _button = new Button()
            {
                Background = new SolidColorBrush( Colors.Transparent),
                HorizontalAlignment = HorizontalAlignment.Center,
                Padding = new Thickness(-2.5),
                Content = border
            };
            // create textblock containing a description of the button
            _descriptionText = new TextBlock()
            {
                Text = name,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                FontSize = 10
            };
            // add all content to stack panel
            xButtonStackPanel.Children.Add(_button);
            xButtonStackPanel.Children.Add(_descriptionText);

            _button.Tapped += Button_Tapped;
        }

        /// <summary>
        /// Invoke specified action when button is tapped
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _buttonAction?.Invoke();
        }

        /// <summary>
        /// Rotate the button
        /// </summary>
        private void CreateAndRunRotationAnimation()
        {
            Duration duration = new Duration(TimeSpan.FromSeconds(0.5));

            var rotationTransform = new RotateTransform();
            _button.RenderTransform = rotationTransform;
            _button.RenderTransformOrigin = new Point(0.5,0.5);

            var storyboard = new Storyboard();
            var doubleAnimation = new DoubleAnimation();
            doubleAnimation.Duration = duration;
            doubleAnimation.SpeedRatio = 1.5;
            doubleAnimation.EnableDependentAnimation = true;
            doubleAnimation.From = 0;
            doubleAnimation.To = 360;
            Storyboard.SetTargetProperty(doubleAnimation, "Angle");
            Storyboard.SetTarget(doubleAnimation, rotationTransform);
            storyboard.Children.Add(doubleAnimation);
            storyboard.Begin();
        }

        public void AddAndRunCollapseAnimation(double verticalOffset)
        {
            this.CreateAndRunVerticalTranslationAnimation(verticalOffset);
            this.CreateAndRunOpacityAnimation(_button, 1, 0);
            this.CreateAndRunOpacityAnimation(_descriptionText, 1, 0);
        }

        public void AddAndRunExpandAnimation()
        {
            this.CreateAndRunOpacityAnimation(_button, 0, 1);
            this.CreateAndRunOpacityAnimation(_descriptionText, 0, 1);
            this.CreateAndRunReverseVerticalTranslationAnimation();
        }

        /// <summary>
        /// Rotate and fade out the button
        /// </summary>
        public void AddAndRunRotateOutAnimation()
        {
            this.CreateAndRunOpacityAnimation(_button, 1, 0);
            this.CreateAndRunOpacityAnimation(_descriptionText, 1, 0);
            this.CreateAndRunRotationAnimation();
        }

        /// <summary>
        /// Rotate and fade in the button
        /// </summary>
        public void AddAndRunRotateInAnimation()
        {
            this.CreateAndRunOpacityAnimation(_button, 0, 1);
            this.CreateAndRunOpacityAnimation(_descriptionText, 0, 1);
            this.CreateAndRunRotationAnimation();
        }

        public void AddAndRunDeleteAnimation()
        {
            this.CreateAndRunOpacityAnimation(_button,1,0);
            this.CreateAndRunOpacityAnimation(_descriptionText,1,0);
        }
        /// <summary>
        /// Create and run animation when button is created
        /// </summary>
        private void CreateAndRunInstantiationAnimation()
        {
            this.CreateAndRunRepositionAnimation(_button, 200,0);
            this.CreateAndRunRepositionAnimation(_descriptionText,0, 50);

            this.CreateAndRunOpacityAnimation(_button, 0, 1);
            this.CreateAndRunOpacityAnimation(_descriptionText, 0, 1);
        }

        /// <summary>
        /// Create and run vertical translation animation (for collapsing menus)
        /// </summary>
        /// <param name="verticalOffset"></param>
        private void CreateAndRunVerticalTranslationAnimation(double verticalOffset)
        {
            _verticalOffset = verticalOffset;

            Duration duration = new Duration(TimeSpan.FromSeconds(0.5));

            var translateTransform = new TranslateTransform();
            translateTransform.Y = 0;
            _button.RenderTransform = translateTransform;
            _descriptionText.RenderTransform = translateTransform;

            var storyboard = new Storyboard();
            var doubleAnimation = new DoubleAnimation();
            doubleAnimation.Duration = duration;
            doubleAnimation.SpeedRatio = 1.3;
            //doubleAnimation.EnableDependentAnimation = true;
            doubleAnimation.From = 0;
            doubleAnimation.To = -_verticalOffset;
            Storyboard.SetTargetProperty(doubleAnimation, "Y");
            Storyboard.SetTarget(doubleAnimation, translateTransform);
            storyboard.Children.Add(doubleAnimation);
            storyboard.Begin();
        }

        /// <summary>
        /// Create and run vertical translation animation (for expanding menus)
        /// </summary>
        private void CreateAndRunReverseVerticalTranslationAnimation()
        {
            Duration duration = new Duration(TimeSpan.FromSeconds(0.5));

            var translateTransform = new TranslateTransform();
            _button.RenderTransform = translateTransform;
            _descriptionText.RenderTransform = translateTransform;

            var storyboard = new Storyboard();
            var doubleAnimation = new DoubleAnimation();
            doubleAnimation.Duration = duration;
            doubleAnimation.SpeedRatio = 1.3;
            //doubleAnimation.EnableDependentAnimation = true;
            doubleAnimation.From = -_verticalOffset;
            doubleAnimation.To = 0;
            Storyboard.SetTargetProperty(doubleAnimation, "Y");
            Storyboard.SetTarget(doubleAnimation, translateTransform);
            storyboard.Children.Add(doubleAnimation);
            storyboard.Begin();

        }

        private void CreateAndRunRepositionAnimation(UIElement target, double horizontalOffset, double verticalOffset)
        {
            Duration duration = new Duration(TimeSpan.FromSeconds(0.5));

            // create and play reposition animation on button
            RepositionThemeAnimation repositionAnimation = new RepositionThemeAnimation()
            {
                SpeedRatio = 0.9,
                FromHorizontalOffset = horizontalOffset,
                FromVerticalOffset = verticalOffset,
                Duration = duration,
            };
            Storyboard repositionStoryboard = new Storyboard()
            {
                Duration = duration
            };
            repositionStoryboard.Children.Add(repositionAnimation);
            Storyboard.SetTarget(repositionAnimation, target);
            repositionStoryboard.Begin();
        }

        private void CreateAndRunOpacityAnimation(UIElement target, double from, double to)
        {
            Duration duration = new Duration(TimeSpan.FromSeconds(0.5));

            // create and play opacity animation on button
            DoubleAnimation opacityAnimation = new DoubleAnimation()
            {
                SpeedRatio = 1,
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
    }
}
