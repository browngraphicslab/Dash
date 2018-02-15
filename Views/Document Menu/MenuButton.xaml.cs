using Dash.Views.Document_Menu;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class MenuButton : UserControl, IDisposable
    {
        private TextBlock _descriptionText;
        private Action _buttonAction;
        private double _verticalOffset;
        private Storyboard OpacityAnimation;
        private Storyboard TranslationAnimation;
        private MenuButtonContainer content;
        private PointerPoint _lastPointerPoint;
        private List<Button> _buttons = new List<Button>();
        private Border _border;
        private Border Border {
            get => _border;
            set { _border = value;
                _border.PointerPressed += (s, e) => _lastPointerPoint = e.GetCurrentPoint(s as UIElement);
                _border.DragStarting += (s,e) =>  StartDragAsync(_lastPointerPoint);
            }
        }

        public new Brush Background
        {
            get => _border.Background;
            set => _border.Background = value;
        }

        public MenuButtonContainer Contents { get { return content; } }
        public TextBlock ButtonText { get { return _descriptionText; } }

        public bool RotateOnTap = false;
        public bool IsComposite;

        public Border View { get { return _border; } }

        #region singleDraggableButton
        /// <summary>
        /// Creates a single button with a given action
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="name"></param>
        /// <param name="buttonAction"></param>
        public MenuButton(Symbol icon, string name, Action buttonAction)
        {
            this.InitializeComponent();
            _buttonAction = buttonAction;
            this.InstantiateButton(icon, name);
            this.CreateAndRunInstantiationAnimation(false);
            IsComposite = false;
            PointerPressed += MenuButton_PointerPressed;
        }

        private void MenuButton_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e != null && (e.GetCurrentPoint(this).Properties.IsRightButtonPressed || e.Pointer.PointerDeviceType == PointerDeviceType.Touch))
            {
                StartDragAsync(e.GetCurrentPoint(sender as UIElement));
            }
        }


        /// <summary>
        /// Create a circular button with an icon with a string description
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="name"></param>
        /// <param name="background"></param>
        private void InstantiateButton(Symbol icon, string name)
        {
            // create button to contain the border with the symbol
            content = new MenuButtonContainer(icon, name);
            _descriptionText = content.Label;
            Border = content.Border;
            // makes buttons appear circular 
            content.Border.Background = Resources["MenuBackground"] as SolidColorBrush;
            ;

            // add all content to stack panel
            xButtonStackPanel.Children.Add(content);

            content.Tapped += Button_Tapped;
            content.DoubleTapped += Button_DoubleTapped;
        }

        #endregion



        #region togglable buttons

        /// <summary>
        /// Creates a toggle-able merged set of buttons ... 
        /// </summary>
        public MenuButton(List<Symbol> icons, List<Action> buttonActions)
        {
            this.InitializeComponent();
            Debug.Assert(icons.Count == buttonActions.Count);
            
            this.InstantiateButtons(icons, buttonActions);
            this.CreateAndRunInstantiationAnimation(true);
            IsComposite = true;
        }
        /// <summary>
        /// Create a set of related toggle-able buttons with edges rounded at the top and buttom 
        /// </summary>
        private void InstantiateButtons(List<Symbol> icons, List<Action> buttonActions)
        {
            var buttonBackground = Resources["MenuBackground"] as SolidColorBrush;
            int i = 0;
            foreach (Symbol icon in icons)
            {
                // create symbol for button
                var symbol = new SymbolIcon()
                {
                    Symbol = icon,
                    //Foreground = Resources["TitleColor"] as SolidColorBrush // TODO move this to static resources
                    Foreground = new SolidColorBrush(Colors.Black)
                };
                // create rounded(circular) border to hold the symbol
                Border = new Border()
                {
                    Height = 40,
                    Width = 40,
                    Background = buttonBackground,
                    BorderBrush = buttonBackground,
                    Child = symbol
                };
                // if it's the first button, round the top 
                if (i == 0) _border.CornerRadius = new CornerRadius(20, 20, 0, 0);
                // if last button, round the buttom  
                else if (i == icons.Count - 1)
                {
                    _border.CornerRadius = new CornerRadius(0, 0, 20, 20);
                }

                // create button to contain the border with the symbol
                var button = new Button()
                {
                    Background = new SolidColorBrush(Colors.Transparent),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Padding = new Thickness(-2.5),
                    Content = _border,
                    Foreground = new SolidColorBrush(Colors.Black),
                };

                // add all content to stack panel
                xButtonStackPanel.Children.Add(button);
                _buttons.Add(button);

                //Capture the right value for i
                int j = i;
                button.Tag = buttonActions[j];

                //events 
                button.Tapped += (s, e) =>
                {
                    e.Handled = true;
                    buttonActions[j]?.Invoke();
                    HighlightAction(buttonActions[j].ToString());
                };
                button.DoubleTapped += (s, e) => e.Handled = true;
                i++;
            }
        }

        public void HighlightAction(string text)
        {
            var buttonBackground = Resources["MenuBackground"] as SolidColorBrush;
            foreach (var b in _buttons)
                (b.Content as Border).Background = buttonBackground;
            foreach (var menubutton in _buttons)
            {
                if (menubutton.ToString()  == text)
                    (menubutton.Content as Border).Background = new SolidColorBrush(Colors.Gray);
            }
        }


        public void Dispose()
        {
            if (content == null) return;
            content.Tapped -= Button_Tapped;
            content.DoubleTapped -= Button_DoubleTapped;
        }

        #endregion

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

        #region Animation
        /// <summary>
        /// Rotate the button
        /// </summary>
        private void CreateAndRunRotationAnimation()
        {
            Duration duration = new Duration(TimeSpan.FromSeconds(0.2));

            var rotationTransform = new RotateTransform();
            if (content != null)
            {
                content.RenderTransform = rotationTransform;
                content.RenderTransformOrigin = new Point(0.5, 0.5);
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
            doubleAnimation.SpeedRatio = 2;
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
            if (content != null)
            {
                this.CreateAndRunOpacityAnimation(content, from, to);
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
        /// Runs the instantiation animations again.
        /// </summary>
        public void AnimateAppearance() {
            OpacityAnimation.Begin();
            TranslationAnimation.Begin();
        }

        /// <summary>
        /// Create and run animation when button is created
        /// </summary>
        public void CreateAndRunInstantiationAnimation(bool isComposite)
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

            TranslationAnimation = CreateAndRunRepositionAnimation(content, 100, 0);
            OpacityAnimation = CreateAndRunOpacityAnimation(content, 0, 1);
            /*
            CreateAndRunRepositionAnimation(_button, 200, 0);
            CreateAndRunRepositionAnimation(_descriptionText, 0, 50);

            CreateAndRunOpacityAnimation(_button, 0, 1);
            CreateAndRunOpacityAnimation(_descriptionText, 0, 1);
            */
        }

        /// <summary>
        /// Create and run vertical translation animation (for collapsing menus)
        /// </summary>
        /// <param name="verticalOffset"></param>
        private void CreateAndRunVerticalTranslationAnimation(double verticalOffset)
        {
            _verticalOffset = verticalOffset;

            Duration duration = new Duration(TimeSpan.FromSeconds(0.2));

            var translateTransform = new TranslateTransform();
            translateTransform.Y = 0;
            if (content != null)
            {
                content.RenderTransform = translateTransform;
                _descriptionText.RenderTransform = translateTransform;
            }
            else
                foreach (var b in _buttons)
                    b.RenderTransform = translateTransform;

            var storyboard = new Storyboard();
            var doubleAnimation = new DoubleAnimation();
            doubleAnimation.Duration = duration;
            doubleAnimation.SpeedRatio = 2;
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
            Duration duration = new Duration(TimeSpan.FromSeconds(0.2));

            var translateTransform = new TranslateTransform();
            if (content != null)
            {
                content.RenderTransform = translateTransform;
                _descriptionText.RenderTransform = translateTransform;
            }
            else
                foreach (var b in _buttons)
                    b.RenderTransform = translateTransform;

            var storyboard = new Storyboard();
            var doubleAnimation = new DoubleAnimation();
            doubleAnimation.Duration = duration;
            doubleAnimation.SpeedRatio = 2;
            doubleAnimation.From = -_verticalOffset;
            doubleAnimation.To = 0;
            Storyboard.SetTargetProperty(doubleAnimation, "Y");
            Storyboard.SetTarget(doubleAnimation, translateTransform);
            storyboard.Children.Add(doubleAnimation);
            storyboard.Begin();

        }

        private Storyboard CreateAndRunRepositionAnimation(UIElement target, double horizontalOffset, double verticalOffset)
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
            return repositionStoryboard;
        }

        private Storyboard CreateAndRunOpacityAnimation(UIElement target, double from, double to)
        {
            Duration duration = new Duration(TimeSpan.FromSeconds(0.2));

            // create and play opacity animation on button
            DoubleAnimation opacityAnimation = new DoubleAnimation()
            {
                SpeedRatio = 1.3,
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
            return opacityStoryboard;
        }
    }

    #endregion
}
