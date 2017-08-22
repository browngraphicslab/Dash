using Dash.Views.Document_Menu;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    /// <summary>
    /// Represents an entire menu of buttons for the Document Menu. Includes code to create new buttons and
    /// add them to a menu.
    /// </summary>
    public sealed partial class MenuButton : UserControl, IDisposable
    {
        private TextBlock _descriptionText;
        private Button _button;
        private Action _buttonAction;
        private double _verticalOffset;

        public bool RotateOnTap = false;

        public MenuButton()
        {
            this.InitializeComponent();
        }

        public MenuButton(Symbol icon, string name, Color background, Action buttonAction)
        {
            this.InitializeComponent();
            _buttonAction = buttonAction;
            this.InstantiateButton(icon, name, background);
            this.CreateAndRunInstantiationAnimation(true);
        }

        private int _selectedInd; 
        private List<Button> _buttons = new List<Button>();
        private Border _border;

        public new Brush Background
        {
            get => _border.Background;
            set => _border.Background = value;
        }


        /// <summary>
        /// Creates a toggle-able merged set of buttons ... 
        /// </summary>
        public MenuButton(List<Symbol> icons, Color background, List<Action> buttonActions)
        {
            this.InitializeComponent();
            Debug.Assert(icons.Count == buttonActions.Count);

            _selectedInd = icons.Count - 1; 

            this.InstantiateButtons(icons, background, buttonActions);
            this.CreateAndRunInstantiationAnimation(true);
        }

        /// <summary>
        /// Create a set of related toggle-able buttons with edges rounded at the top and buttom 
        /// </summary>
        private void InstantiateButtons(List<Symbol> icons, Color background, List<Action> buttonActions)
        {
            foreach (Symbol icon in icons)
            {
                var i = icons.IndexOf(icon); // have to do this for eventhandling 

                // create symbol for button
                var symbol = new SymbolIcon()
                {
                    Symbol = icon,
                    Foreground = new SolidColorBrush(Colors.White)
                };
                // create rounded(circular) border to hold the symbol
                Border border = new Border()
                {
                    Height = 40,
                    Width = 40,
                    Background = new SolidColorBrush(background),
                    BorderBrush = new SolidColorBrush(background),
                    Child = symbol
                };
                // if it's the first button, round the top 
                if (i == 0) border.CornerRadius = new CornerRadius(20, 20, 0, 0);
                // if last button, round the buttom  
                else if (i == icons.Count - 1)
                {
                    border.CornerRadius = new CornerRadius(0, 0, 20, 20);
                    //border.Background = new SolidColorBrush(Colors.Gray);
                }

                if (i == _selectedInd) border.Background = new SolidColorBrush(Colors.Gray);

                // create button to contain the border with the symbol
                var button = new Button()
                {
                    Background = new SolidColorBrush(Colors.Transparent),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Padding = new Thickness(-2.5),
                    Content = border
                };

                // add all content to stack panel
                xButtonStackPanel.Children.Add(button);
                _buttons.Add(button);

                //events 
                button.Tapped += (s, e) =>
                {
                    e.Handled = true;
                    foreach (var b in _buttons) (b.Content as Border).Background = new SolidColorBrush(background);
                    (button.Content as Border).Background = new SolidColorBrush(Colors.Gray);
                    buttonActions[i]?.Invoke();

                    _selectedInd = i; 
                };
                button.DoubleTapped += (s, e) => e.Handled = true;
            }
        }

        /// <summary>
        /// Create a circular button with an icon with a string description
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="name"></param>
        /// <param name="background"></param>
        private void InstantiateButton(Symbol icon, string name, Color background)
        {
            // create button to contain the border with the symbol
            MenuButtonContainer content = new MenuButtonContainer(icon, name);
            _descriptionText = content.Label;
            _button = content.Button;
            _border = content.Border;
            content.Border.Background = new SolidColorBrush(background);

            // add all content to stack panel
            xButtonStackPanel.Children.Add(content);

            _button.Tapped += Button_Tapped;
            _button.DoubleTapped += Button_DoubleTapped;
        }

        private void Button_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// Invoke specified action when button is tapped
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            _buttonAction?.Invoke();
            e.Handled = true;
        }

        public void Dispose()
        {
            if (_button == null) return; 
            _button.Tapped -= Button_Tapped;
            _button.DoubleTapped -= Button_DoubleTapped;
        }

        /// <summary>
        /// Rotate the button
        /// </summary>
        private void CreateAndRunRotationAnimation()
        {
            Duration duration = new Duration(TimeSpan.FromSeconds(0.5));

            var rotationTransform = new RotateTransform();
            if (_button != null)
            {
                _button.RenderTransform = rotationTransform;
                _button.RenderTransformOrigin = new Point(0.5, 0.5);
            }
            else
            {
                foreach (var b in _buttons)
                {
                    b.RenderTransform = rotationTransform;
                    b.RenderTransformOrigin = new Point(0.5, 0.5);
                }
            }
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

        private void OpactiyAnimationHelper(int from, int to)
        {
            if (_button != null)
            {
                this.CreateAndRunOpacityAnimation(_button, from, to);
                this.CreateAndRunOpacityAnimation(_descriptionText, from, to);
            }
            else
                foreach (var b in _buttons)
                    CreateAndRunOpacityAnimation(b, from, to);
        }

        public void AddAndRunCollapseAnimation(double verticalOffset)
        {
            this.CreateAndRunVerticalTranslationAnimation(verticalOffset);
            OpactiyAnimationHelper(1, 0); 
        }

        public void AddAndRunExpandAnimation()
        {
            OpactiyAnimationHelper(0,1);
            this.CreateAndRunReverseVerticalTranslationAnimation();
        }

        /// <summary>
        /// Rotate and fade out the button
        /// </summary>
        public void AddAndRunRotateOutAnimation()
        {
            OpactiyAnimationHelper(1, 0);
            this.CreateAndRunRotationAnimation();
        }

        /// <summary>
        /// Rotate and fade in the button
        /// </summary>
        public void AddAndRunRotateInAnimation()
        {
            OpactiyAnimationHelper(0, 1);
            this.CreateAndRunRotationAnimation();
        }

        public void AddAndRunDeleteAnimation()
        {
            OpactiyAnimationHelper(1,0);
        }
        /// <summary>
        /// Create and run animation when button is created
        /// </summary>
        private void CreateAndRunInstantiationAnimation(bool isComposite)
        {
            if (isComposite)
            {
                foreach (var button in _buttons)
                {
                    this.CreateAndRunRepositionAnimation(button, 200, 0);
                    this.CreateAndRunOpacityAnimation(button, 0, 1);
                }
                return;
            }
            this.CreateAndRunRepositionAnimation(_button, 200, 0);
            this.CreateAndRunRepositionAnimation(_descriptionText, 0, 50);

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
            if (_button != null)
            {
                _button.RenderTransform = translateTransform;
                _descriptionText.RenderTransform = translateTransform;
            }
            else
                foreach (var b in _buttons)
                    b.RenderTransform = translateTransform;

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
            if (_button != null)
            {
                _button.RenderTransform = translateTransform;
                _descriptionText.RenderTransform = translateTransform;
            }
            else
                foreach (var b in _buttons)
                    b.RenderTransform = translateTransform;

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
