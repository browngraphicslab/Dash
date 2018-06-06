namespace Dash
{
    using Dash;
    using System;
    using Windows.Foundation;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;
	using Windows.UI.Xaml.Media;
	using Windows.UI.Xaml.Media.Animation;

	/// <summary>
	/// A Content Control that can be dragged around. Huge thanks to Diederik Kols for the smartness behind this.
	/// </summary>
	[TemplatePart(Name = BorderPartName, Type = typeof(Border))]
    public class Floating : ContentControl
    {
        private const string BorderPartName = "DraggingBorder";

        public static readonly DependencyProperty IsBoundByParentProperty =
            DependencyProperty.Register("IsBoundByParent", typeof(bool), typeof(Floating), new PropertyMetadata(false));

        public static readonly DependencyProperty IsBoundByScreenProperty =
            DependencyProperty.Register("IsBoundByScreen", typeof(bool), typeof(Floating), new PropertyMetadata(false));

        public static readonly DependencyProperty ShouldManiuplateChildProperty =
            DependencyProperty.Register("ShouldManipulateChild", typeof(bool), typeof(Floating), new PropertyMetadata(false));
        private Border _border;
		private bool _expanding;

		private double CurrCanvasTop
		{
			get { return (double)_border.GetValue(Canvas.TopProperty); }
			set { _border.SetValue(Canvas.TopProperty, value); }
		}

        /// <summary>
        /// Prevents the Floating control from manipulating it's content
        /// </summary>
        public bool ShouldManipulateChild
        {
            get { return (bool)GetValue(ShouldManiuplateChildProperty); }
            set { SetValue(ShouldManiuplateChildProperty, value); }
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Floating"/> class.
        /// </summary>
        public Floating()
        {
            DefaultStyleKey = typeof(Floating);
            ShouldManipulateChild = true;
			_expanding = false;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the control is bound by its parent size.
        /// </summary>
        public bool IsBoundByParent
        {
            get { return (bool)GetValue(IsBoundByParentProperty); }
            set { SetValue(IsBoundByParentProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the control is bound by the screen size.
        /// </summary>
        public bool IsBoundByScreen
        {
            get { return (bool)GetValue(IsBoundByScreenProperty); }
            set { SetValue(IsBoundByScreenProperty, value); }
        }

        /// <summary>
        /// Invoked whenever application code or internal processes (such as a rebuilding layout pass) call ApplyTemplate.
        /// In simplest terms, this means the method is called just before a UI element displays in your app.
        /// Override this method to influence the default post-template logic of a class.
        /// </summary>
        protected override void OnApplyTemplate()
        {
            // Border
            _border = GetTemplateChild(BorderPartName) as Border;
            if (_border != null)
            {
                //this.border.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY | ManipulationModes.TranslateInertia;
                _border.ManipulationDelta += Border_ManipulationDelta;

                // Move Canvas properties from control to border.
                Canvas.SetLeft(_border, Canvas.GetLeft(this));
                Canvas.SetLeft(this, 0);
                Canvas.SetTop(_border, Canvas.GetTop(this));
                Canvas.SetTop(this, 0);

                // Move Margin to border.
                _border.Padding = Margin;
                Margin = new Thickness(0);
            }
            else
            {
                // Exception
                throw new Exception("Floating Control Style has no Border.");
            }

            Loaded += Floating_Loaded;
        }

        /// <summary>
        /// Loaded handler which registers for the SizeChanged event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Floating_Loaded(object sender, RoutedEventArgs e)
        {
            FrameworkElement el = GetClosestParentWithSize(this);
            if (el == null)
            {
                return;
            }

            el.SizeChanged += Floating_SizeChanged;
        }

        /// <summary>
        /// Size Changed Handler for Control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Floating_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var left = Canvas.GetLeft(_border);
            var top = Canvas.GetTop(_border);
            var rect = new Rect(left, top, _border.ActualWidth, _border.ActualHeight);

            AdjustCanvasPosition(rect);
        }

        /// <summary>
        /// Handler for ManuplationDelta event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Border_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (ShouldManipulateChild)
            {
                ManipulateControlPosition(e.Delta.Translation.X, e.Delta.Translation.Y);
            }
        }

        /// <summary>
        /// Manipulate the control's positon!
        /// </summary>
        /// <param name="x">Delta on the X axis</param>
        /// <param name="y">Delta on the Y axis</param>
        public void ManipulateControlPosition(double x, double y)
        {
            ManipulateControlPosition(x, y, _border.ActualHeight, _border.ActualWidth);
        }

        /// <summary>
        /// Manipulate the control's positon!
        /// </summary>
        /// <param name="x">Delta on the X axis</param>
        /// <param name="y">Delta on the Y axis</param>
        /// <param name="expectedHeight">Expected heigth of the floating control</param>
        /// <param name="expectedWidth">Expected width of the floating control</param>
        public void ManipulateControlPosition(double x, double y, double expectedHeight, double expectedWidth)
        {
            var left = Canvas.GetLeft(_border) + x;
            var top = Canvas.GetTop(_border) + y;

            Rect rect = new Rect(left, top, expectedWidth, expectedHeight);
            AdjustCanvasPosition(rect);
        }

        public void SetControlPosition(double x, double y)
        {
            Rect rect = new Rect(x, y, _border.ActualWidth, _border.ActualHeight);
            AdjustCanvasPosition(rect);
        }

        /// <summary>
        /// Adjusts the canvas position according to the IsBoundBy* properties.
        /// </summary>
        private void AdjustCanvasPosition(Rect rect)
        {
            // No boundaries
            if (!IsBoundByParent && !IsBoundByScreen)
            {
                Canvas.SetLeft(_border, rect.Left);
                Canvas.SetTop(_border, rect.Top);

                return;
            }

            FrameworkElement el = GetClosestParentWithSize(this);

            // No parent
            if (el == null)
            {
                // We probably never get here.
                return;
            }

            var position = new Point(rect.Left, rect.Top);

            if (IsBoundByParent)
            {
                Rect parentRect = new Rect(0, 0, el.ActualWidth, el.ActualHeight);
                position = AdjustedPosition(rect, parentRect);
            }

            if (IsBoundByScreen)
            {
                //var ttv = el.TransformToVisual(Window.Current.Content);
                //var topLeft = ttv.TransformPoint(new Point(0, 0));
                var topLeft = Util.PointTransformFromVisual(new Point(0, 0), el);
                Rect parentRect = new Rect(topLeft.X, topLeft.Y, Window.Current.Bounds.Width - topLeft.X, Window.Current.Bounds.Height - topLeft.Y);
                position = AdjustedPosition(rect, parentRect);
            }

			if (_expanding && (Math.Abs(Canvas.GetTop(_border) - position.Y) > 1 || Math.Abs(Canvas.GetLeft(_border) - position.X) > 1))
			{
				this.MoveAnimation(-(Canvas.GetLeft(_border) - position.X), -(Canvas.GetTop(_border) - position.Y), position.Y);
				// Set new position
				//Canvas.SetLeft(_border, position.X);
				//Canvas.SetTop(_border, position.Y);
			}
			else
			{
				// Set new position
				Canvas.SetLeft(_border, position.X);
				Canvas.SetTop(_border, position.Y);
			}
			

		}

        /// <summary>
        /// Returns the adjusted the topleft position of a rectangle so that is stays within a parent rectangle.
        /// </summary>
        /// <param name="rect">The rectangle.</param>
        /// <param name="parentRect">The parent rectangle.</param>
        /// <returns></returns>
        private Point AdjustedPosition(Rect rect, Rect parentRect)
        {
            var left = rect.Left;
            var top = rect.Top;

            if (left < -parentRect.Left)
            {
                left = -parentRect.Left;
            }
            else if (left + rect.Width > parentRect.Width)
            {
                left = parentRect.Width - rect.Width;

            }

            if (top < -parentRect.Top)
            {
                top = -parentRect.Top;
            }
            else if (top + rect.Height > parentRect.Height)
            {
                top = parentRect.Height - rect.Height;
            }

            return new Point(left, top);
        }

        /// <summary>
        /// Gets the closest parent with a real size.
        /// </summary>
        private FrameworkElement GetClosestParentWithSize(FrameworkElement element)
        {
            while (element != null && (element.ActualHeight == 0 || element.ActualWidth == 0))
            {
                element = element.Parent as FrameworkElement;
            }

            return element;
        }

	    /// <summary>
	    /// Adjusts position of floating object to account for an expansion
	    /// </summary>
		public void AdjustPositionForExpansion(double height, double width)
	    {
		    if (IsBoundByScreen)
		    {
				_expanding = true;
				FrameworkElement el = GetClosestParentWithSize(this);

				var topLeft = Util.PointTransformFromVisual(new Point(0, 0), el);

				double elSize = _border.ActualHeight;
				double topY = Canvas.GetTop(_border);

				double newLowY =  topY + elSize + height;

				//if expansion would cause the object to extend past the bottom of the screen, adjust y position by the difference
				if (newLowY >= Window.Current.Bounds.Height)
				{
					this.ManipulateControlPosition(0, Window.Current.Bounds.Height - newLowY);
				}
				_expanding = false;
			}
	   
	    }

		public void MoveAnimation(double xDist, double yDist, double yPos)
		{
			System.Diagnostics.Debug.WriteLine("CANVAS GET TOP Before" + Canvas.GetLeft(_border));
			// Create the transform
			TranslateTransform moveTransform = new TranslateTransform();
			moveTransform.X = 0;
			moveTransform.Y = 0;
			this.RenderTransform = moveTransform;

			// Create a duration of .5 seconds.
			Duration duration = new Duration(TimeSpan.FromSeconds(.5));
			// Create two DoubleAnimations and set their properties.
			DoubleAnimation myDoubleAnimationX = new DoubleAnimation();
			DoubleAnimation myDoubleAnimationY = new DoubleAnimation();
			myDoubleAnimationX.Duration = duration;
			myDoubleAnimationY.Duration = duration;
			Storyboard justintimeStoryboard = new Storyboard();
			justintimeStoryboard.Duration = duration;
			justintimeStoryboard.Children.Add(myDoubleAnimationX);
			justintimeStoryboard.Children.Add(myDoubleAnimationY);
			Storyboard.SetTarget(myDoubleAnimationX, moveTransform);
			Storyboard.SetTarget(myDoubleAnimationY, moveTransform);

			// Set the X and Y properties of the Transform to be the target properties
			// of the two respective DoubleAnimations.
			Storyboard.SetTargetProperty(myDoubleAnimationX, "X");
			Storyboard.SetTargetProperty(myDoubleAnimationY, "Y");
			myDoubleAnimationX.To = xDist;
			myDoubleAnimationY.To = yDist - 15;

			// Make the Storyboard a resource.
			//LayoutRoot.Resources.Add("justintimeStoryboard", justintimeStoryboard);
			// Begin the animation.
			justintimeStoryboard.Begin();

			//SOMEHOW SET CANVAS BACK TO ORIGINAL POSITIONING
			
			
			justintimeStoryboard.Completed += (s, e) =>
			{
				//FrameworkElement el = GetClosestParentWithSize(this);

				//var topLeft = Util.PointTransformFromVisual(new Point(0, 0), el);
				//this.SetControlPosition(Canvas.GetLeft(_border), Window.Current.Bounds.Height - topLeft.Y);
				
				//_expanding = false;
				moveTransform.X = moveTransform.X - xDist;
				moveTransform.Y = moveTransform.Y - yDist + 15;
				Canvas.SetTop(_border, yPos -15);
				
				/*
				var topLeft = Util.PointTransformFromVisual(new Point(0, 0), el);

				double elSize = _border.ActualHeight;
				double topY = Canvas.GetTop(_border);

				double newLowY = topY + elSize + 50;

				//if expansion would cause the object to extend past the bottom of the screen, adjust y position by the difference
				if (newLowY >= Window.Current.Bounds.Height)
				{
					this.SetControlPosition(0, Window.Current.Bounds.Height - newLowY);
				}
				*/
			};
			
		}

		public void MoveAnimation2(double xDist, double yDist)
		{
			// Create the transform
			//TranslateTransform moveTransform = new TranslateTransform();
			//moveTransform.X = 0;
			//moveTransform.Y = 0;
			//_border.RenderTransform = moveTransform;

			// Create a duration of .5 seconds.
			Duration duration = new Duration(TimeSpan.FromSeconds(.5));
			// Create two DoubleAnimations and set their properties.
			//DoubleAnimation myDoubleAnimationX = new DoubleAnimation();
			DoubleAnimation myDoubleAnimationY = new DoubleAnimation();
			//myDoubleAnimationX.Duration = duration;
			myDoubleAnimationY.Duration = duration;
			Storyboard fadeAnimation = new Storyboard();
			fadeAnimation.Duration = duration;
			//fadeAnimation.Children.Add(myDoubleAnimationX);
			fadeAnimation.Children.Add(myDoubleAnimationY);
			//Storyboard.SetTarget(myDoubleAnimationX, _border);
			Storyboard.SetTarget(myDoubleAnimationY, this);
			//_border.SetValue(Canvas.LeftProperty, 4);
			// Set the X and Y properties of the Transform to be the target properties
			// of the two respective DoubleAnimations.
			//Storyboard.SetTargetProperty((Canvas.Left), "X");
			Storyboard.SetTargetProperty(myDoubleAnimationY, "Top");
			//myDoubleAnimationX.To = xDist;
			myDoubleAnimationY.To = yDist;

			// Make the Storyboard a resource.
			//LayoutRoot.Resources.Add("justintimeStoryboard", justintimeStoryboard);
			// Begin the animation.
			fadeAnimation.Begin();
		}
	}
}