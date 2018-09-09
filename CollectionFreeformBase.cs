using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Dash.Views.Collection;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using NewControls.Geometry;
using Point = Windows.Foundation.Point;
using Rectangle = Windows.UI.Xaml.Shapes.Rectangle;
using Task = System.Threading.Tasks.Task;
using Window = Windows.UI.Xaml.Window;
using DashShared;
using System.Threading;
using Windows.Storage.Streams;
using Windows.Storage;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Windows.UI.Input.Inking;

namespace Dash
{
	public abstract class CollectionFreeformBase : UserControl, ICollectionView
	{
		MatrixTransform _transformBeingAnimated;// Transform being updated during animation
		Panel _itemsPanelCanvas => GetCanvas();
		CollectionViewModel _lastViewModel = null;
		public UserControl UserControl => this;
		public abstract DocumentView ParentDocument { get; }
		//TODO: instantiate in derived class and define OnManipulatorTranslatedOrScaled
		public abstract ViewManipulationControls ViewManipulationControls { get; set; }
		public bool TagMode { get; set; }
		public KeyController TagKey { get; set; }
		public abstract CollectionViewModel ViewModel { get; }
		public abstract CollectionView.CollectionViewType Type { get; }
		private Mutex _mutex = new Mutex();

		//SET BACKGROUND IMAGE SOURCE
		public delegate void SetBackground(object backgroundImagePath);
		private static event SetBackground setBackground;

		//SET BACKGROUND IMAGE OPACITY
		public delegate void SetBackgroundOpacity(float opacity);
		private static event SetBackgroundOpacity setBackgroundOpacity;

        public abstract Panel GetCanvas();

        public abstract ItemsControl GetItemsControl();

        // This uses a content presenter mainly because of this http://microsoft.github.io/Win2D/html/RefCycles.htm
        // If that link dies, google win2d canvascontrol refcycle
        // If nothing comes up, maybe the issue is fixed.
        // Currently, the issue is that CanvasControls make it extremely easy to create memory leaks and need to be dealt with carefully
        // As the link states, adding handlers to events on a CanvasControl creates reference cycles that the GC can't detect, so the events need to be handled manually
        // What the link doesn't say is that apparently having events on any siblings of the CanvasControl without having events on the CanvasControl still somehow prevents it from
        // being GC-ed, so it is easier to just create and destroy it on load and unload rather than try to manage all of the events and references... 
        // Because we create it in this class, we need a content presenter to put it in, otherwise the subclass can't decide where to put it
        public abstract ContentPresenter GetBackgroundContentPresenter();
        private CanvasControl _backgroundCanvas;

        public abstract Grid GetOuterGrid();

        public abstract AutoSuggestBox GetTagBox();

        public abstract Canvas GetSelectionCanvas();

        public abstract Rectangle GetDropIndicationRectangle();

        public abstract Canvas GetInkHostCanvas();

        protected CollectionFreeformBase()
        {
            Loaded += OnBaseLoaded;
            Unloaded += OnBaseUnload;
            KeyDown += _marquee_KeyDown;
        }

        private void OnBaseLoaded(object sender, RoutedEventArgs e)
        {
            _backgroundCanvas = new CanvasControl();
            _backgroundCanvas.CreateResources += CanvasControl_OnCreateResources;
            _backgroundCanvas.Draw += CanvasControl_OnDraw;
            GetBackgroundContentPresenter().Content = _backgroundCanvas;

            MakePreviewTextbox();

			//make and add selectioncanvas 
			SelectionCanvas = new Canvas();
			Canvas.SetLeft(SelectionCanvas, -30000);
			Canvas.SetTop(SelectionCanvas, -30000);
			//Canvas.SetZIndex(GetInkHostCanvas(), 2);//Uncomment this to get the Marquee on top, but it causes issues with regions
			GetInkHostCanvas().Children.Add(SelectionCanvas);

            if (ViewModel.InkController == null)
                ViewModel.ContainerDocument.SetField<InkController>(KeyStore.InkDataKey, new List<InkStroke>(), true);
            //MakeInkCanvas();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            setBackground += ChangeBackground;
            setBackgroundOpacity += ChangeOpacity;

			var settingsView = MainPage.Instance.GetSettingsView;
			if (settingsView.ImageState == SettingsView.BackgroundImageState.Custom)
			{
				var storedPath = settingsView.CustomImagePath;
				if (storedPath != null) _background = storedPath; 
			}
			else
			{
				_background = settingsView.EnumToPathDict[settingsView.ImageState];
			}

			BackgroundOpacity = settingsView.BackgroundImageOpacity;
		}

        private void OnBaseUnload(object sender, RoutedEventArgs e)
        {
            _backgroundCanvas?.RemoveFromVisualTree();
            GetBackgroundContentPresenter().Content = null;
            _backgroundCanvas = null;
            if (_lastViewModel != null)
            {
                _lastViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

			_lastViewModel = null;
			setBackground -= ChangeBackground;
			setBackgroundOpacity -= ChangeOpacity;
		}

        protected void OnDataContextChanged(object sender, DataContextChangedEventArgs e)
        {
            _lastViewModel = ViewModel;
        }

        protected void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            //Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.IBeam, 1);
        }

		protected void OnPointerExited(object sender, PointerRoutedEventArgs e)
		{
			//Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 1);
		}

		protected void OnSizeChanged(object sender, SizeChangedEventArgs e)
		{
			var grid = sender as Grid;
			if (grid?.Clip != null)
			{
				grid.Clip.Rect = new Rect(0, 0, grid.ActualWidth, grid.ActualHeight);
			}
		}

        public DocumentController Snapshot(bool copyData = false)
        {
            var controllers = new List<DocumentController>();
            foreach (var dvm in ViewModel.DocumentViewModels)
                controllers.Add(copyData ? dvm.DocumentController.GetDataCopy() : dvm.DocumentController.GetViewCopy());
            var snap = new CollectionNote(new Point(), CollectionView.CollectionViewType.Freeform, double.NaN, double.NaN, controllers).Document;
            snap.GetDataDocument().SetTitle(ParentDocument.ViewModel.DocumentController.Title + "_copy");
            snap.SetFitToParent(true);
            return snap;
        }


		#region Manipulation
		/// <summary>
		/// Animation storyboard for first half. Unfortunately, we can't use the super useful AutoReverse boolean of animations to do this with one storyboard
		/// </summary>
		Storyboard _storyboard1, _storyboard2;

		public void Move(TranslateTransform translate)
		{
			var composite = new TransformGroup();
			composite.Children.Add((GetItemsControl()?.ItemsPanelRoot as Canvas).RenderTransform);
			composite.Children.Add(translate);

			var matrix = composite.Value;
			ViewModel.TransformGroup = new TransformGroupData(new Point(matrix.OffsetX, matrix.OffsetY), new Point(matrix.M11, matrix.M22));
		}

		public void MoveAnimated(TranslateTransform translate)
		{
			var old = (_itemsPanelCanvas?.RenderTransform as MatrixTransform)?.Matrix;
			if (old == null)
			{
				return;
			}
			_transformBeingAnimated = new MatrixTransform()  { Matrix = (Matrix)old };

			Debug.Assert(_transformBeingAnimated != null);
			var milliseconds = 1000;
			var duration = new Duration(TimeSpan.FromMilliseconds(milliseconds));

			//Clear storyboard
			_storyboard1?.Stop();
			_storyboard1?.Children.Clear();
			_storyboard1 = new Storyboard { Duration = duration };

			_storyboard2?.Stop();
			_storyboard2?.Children.Clear();
			_storyboard2 = new Storyboard { Duration = duration };


			var startX = _transformBeingAnimated.Matrix.OffsetX;
			var startY = _transformBeingAnimated.Matrix.OffsetY;

			// Create a DoubleAnimation for translating
			var translateAnimationX = MakeAnimationElement(_transformBeingAnimated, startX, startX + translate.X, "MatrixTransform.Matrix.OffsetX", duration);
			var translateAnimationY = MakeAnimationElement(_transformBeingAnimated, startY, startY + translate.Y, "MatrixTransform.Matrix.OffsetY", duration);
			translateAnimationX.AutoReverse = false;
			translateAnimationY.AutoReverse = false;

			_storyboard1.Children.Add(translateAnimationX);
			_storyboard1.Children.Add(translateAnimationY);

			CompositionTarget.Rendering -= CompositionTargetOnRendering;
			CompositionTarget.Rendering += CompositionTargetOnRendering;

			// Begin the animation.
			_storyboard1.Begin();
			_storyboard1.Completed -= Storyboard1OnCompleted;
			_storyboard1.Completed += Storyboard1OnCompleted;
		}

		public void SetTransform(TranslateTransform translate, ScaleTransform scale)
		{
			var composite = new TransformGroup();
			//composite.Children.Add((GetItemsControl()?.ItemsPanelRoot as Canvas).RenderTransform);
			if (scale != null)
			{
				composite.Children.Add(scale);
			}
			composite.Children.Add(translate);

			var matrix = composite.Value;
			ViewModel.TransformGroup = new TransformGroupData(new Point(matrix.OffsetX, matrix.OffsetY), new Point(matrix.M11, matrix.M22));
			ViewManipulationControls.ElementScale = matrix.M11;
		}

		public void SetTransformAnimated(TranslateTransform translate, ScaleTransform scale)
		{
			UndoManager.StartBatch();
			//get rendering postion of _itemsPanelCanvas, 2x3 matrix
			var old = (_itemsPanelCanvas?.RenderTransform as MatrixTransform)?.Matrix;
			if (old == null)
			{
				return;
			}
			//set transformBeingAnimated to matrix of old
			_transformBeingAnimated = new MatrixTransform() { Matrix = (Matrix)old };

			Debug.Assert(_transformBeingAnimated != null);
			var milliseconds = 1000;
			var duration = new Duration(TimeSpan.FromMilliseconds(milliseconds));

			//Clear storyboard
			_storyboard1?.Stop();
			_storyboard1?.Children.Clear();
			_storyboard1 = new Storyboard { Duration = duration };

			_storyboard2?.Stop();
			_storyboard2?.Children.Clear();
			_storyboard2 = new Storyboard { Duration = duration };

			var startMatrix = _transformBeingAnimated.Matrix;

			var scaleMatrix = scale.GetMatrix();

			//Create a Double Animation for zooming in and out. Unfortunately, the AutoReverse bool does not work as expected.
			//the higher number, the more it xooms, but doesn't actually change final view 
			var zoomAnimationX = MakeAnimationElement(_transformBeingAnimated, startMatrix.M11, scaleMatrix.M11, "MatrixTransform.Matrix.M11", duration);
			var zoomAnimationY = MakeAnimationElement(_transformBeingAnimated, startMatrix.M22, scaleMatrix.M22, "MatrixTransform.Matrix.M22", duration);

			_storyboard1.Children.Add(zoomAnimationX);
			_storyboard1.Children.Add(zoomAnimationY);

			// Create a DoubleAnimation for translating
			var translateAnimationX = MakeAnimationElement(_transformBeingAnimated, startMatrix.OffsetX, translate.X + scaleMatrix.OffsetX, "MatrixTransform.Matrix.OffsetX", duration);
			var translateAnimationY = MakeAnimationElement(_transformBeingAnimated, startMatrix.OffsetY, translate.Y + scaleMatrix.OffsetY, "MatrixTransform.Matrix.OffsetY", duration);

			_storyboard1.Children.Add(translateAnimationX);
			_storyboard1.Children.Add(translateAnimationY);


			CompositionTarget.Rendering -= CompositionTargetOnRendering;
			CompositionTarget.Rendering += CompositionTargetOnRendering;

			// Begin the animation.
			_storyboard1.Begin();
			_storyboard1.Completed -= Storyboard1OnCompleted;
			_storyboard1.Completed += Storyboard1OnCompleted;


		}

		protected void Storyboard1OnCompleted(object sender, object e)
		{
			CompositionTarget.Rendering -= CompositionTargetOnRendering;
			_storyboard1.Completed -= Storyboard1OnCompleted;
			UndoManager.EndBatch();
		}

		protected void CompositionTargetOnRendering(object sender, object e)
		{
			var matrix = _transformBeingAnimated.Matrix;
			ViewModel.TransformGroup = new TransformGroupData(new Point(matrix.OffsetX, matrix.OffsetY), new Point(matrix.M11, matrix.M22));
			ViewManipulationControls.ElementScale = matrix.M11; // bcz: don't update elementscale to have no zoom bounds on jumping between things (not scroll zooming)
		}

		protected DoubleAnimation MakeAnimationElement(MatrixTransform matrix, double from, double to, String name, Duration duration)
		{

			var toReturn = new DoubleAnimation();
			toReturn.EnableDependentAnimation = true;
			toReturn.Duration = duration;
			//Storyboard.TargetProperty targets a particular property of the element as named by Storyboard.TargetName
			Storyboard.SetTarget(toReturn, matrix);
			Storyboard.SetTargetProperty(toReturn, name);

			//The animation progresses from the value specified by the From property to the value specified by the To property
			toReturn.From = from;
			toReturn.To = to;

			toReturn.EasingFunction = new QuadraticEase();
			return toReturn;

		}

		/// <summary>
		/// Pans and zooms upon touch manipulation 
		/// </summary>   
		protected virtual void ManipulationControls_OnManipulatorTranslated(TransformGroupData transformation, bool abs)
		{
			// calculate the translate delta
			var translateDelta = new TranslateTransform
			{
				X = transformation.Translate.X,
				Y = transformation.Translate.Y
			};

			// calculate the scale delta
			var scaleDelta = new ScaleTransform
			{
				CenterX = transformation.ScaleCenter.X,
				CenterY = transformation.ScaleCenter.Y,
				ScaleX = transformation.ScaleAmount.X,
				ScaleY = transformation.ScaleAmount.Y
			};

			//Create initial composite transform
			var composite = new TransformGroup();
			if (!abs)
				composite.Children.Add(_itemsPanelCanvas.RenderTransform); // get the current transform            
			composite.Children.Add(translateDelta); // add the new translate
			composite.Children.Add(scaleDelta); // add the new scaling
			var matrix = composite.Value;
			ViewModel.TransformGroup = new TransformGroupData(new Point(matrix.OffsetX, matrix.OffsetY), new Point(matrix.M11, matrix.M22));
            MainPage.Instance.XDocumentDecorations.SetPositionAndSize(); // bcz: hack ... The Decorations should update automatically when the view zooms -- need a mechanism to bind/listen to view changing globally?
		}

		#endregion

		#region BackgroundTiling
		bool _resourcesLoaded;
		CanvasImageBrush _bgBrush;
		const double NumberOfBackgroundRows = 2; // THIS IS A MAGIC NUMBER AND SHOULD CHANGE IF YOU CHANGE THE BACKGROUND IMAGE
		private float _bgOpacity = 1.0f;

		/// <summary>
		/// Collection background tiling image opacity
		/// </summary>
		public static float BackgroundOpacity { set => setBackgroundOpacity?.Invoke(value); }
		private static object _background = "ms-appx:///Assets/transparent_grid_tilable.png";
		private CanvasBitmap _bgImage;

		/// <summary>
		/// Collection background tiling image
		/// </summary>
		public static object BackgroundImage { set => setBackground?.Invoke(value); }

        /// <summary>
        /// Called when background opacity is set and the background tiling must be redrawn to reflect the change
        /// </summary>
        /// <param name="opacity"></param>
        private void ChangeOpacity(float opacity)
        {
            _bgOpacity = opacity;
            _backgroundCanvas?.Invalidate();
        }
        #endregion

		/// <summary>
		/// All of the following background image updating logic was sourced from this article --> https://microsoft.github.io/Win2D/html/LoadingResourcesOutsideCreateResources.htm
		/// </summary>
		#region LOADING AND REDRAWING BACKUP ASYNC

		private Task _backgroundTask;

		// 1
		protected void CanvasControl_OnCreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
		{
			_bgBrush = new CanvasImageBrush(sender);

			// Set the brush's edge behaviour to wrap, so the image repeats if the drawn region is too big
			_bgBrush.ExtendX = _bgBrush.ExtendY = CanvasEdgeBehavior.Wrap;
			_resourcesLoaded = true;

			args.TrackAsyncAction(CreateResourcesAsync(sender).AsAsyncAction());
		}

		// 2
		protected async Task CreateResourcesAsync(CanvasControl sender)
		{
			if (_backgroundTask != null)
			{
				_backgroundTask.AsAsyncAction().Cancel();
				try { await _backgroundTask; } catch (Exception e) { Debug.WriteLine(e); }
				_backgroundTask = null;
			}

			//Internally null checks _background
			//NOTE *** Invalid or null input will end the entire update chain and, to the user, nothing will visibily change. ***
			ChangeBackground(_background);
		}

		// 3
		protected async void ChangeBackground(object backgroundImagePath)
		{
			// Null-checking. WARNING - if null, Dash throws an Unhandled Exception
			if (backgroundImagePath == null) return;

            // Now, backgroundImagePath is either <string> or <IRandomAccessStream> - while local ms-appx (assets folder) paths <string> don't need conversion, 
            // external file system paths <string> need to be converted into <IRandomAccessStream>
            if (backgroundImagePath is string path && !path.Contains("ms-appx:"))
            {
                backgroundImagePath = await FileRandomAccessStream.OpenAsync(path, FileAccessMode.Read);
            }
            // Update the path/stream instance var to be used next in LoadBackgroundAsync
            _background = backgroundImagePath;
            // Now, register and perform the new loading
            _backgroundTask = LoadBackgroundAsync(_backgroundCanvas);
        }

		// 4
		protected async Task LoadBackgroundAsync(CanvasControl canvas)
		{
			// Convert the <IRandomAccessStream> and update the <CanvasBitmap> instance var to be used later by the <CanvasImageBrush> in CanvasControl_OnDraw
			if (_background is string s) // i.e. A rightfully unconverted ms-appx path
				_bgImage = await CanvasBitmap.LoadAsync(canvas, new Uri(s));
			else
				_bgImage = await CanvasBitmap.LoadAsync(canvas, (IRandomAccessStream)_background);
			// NOTE *** At this point, _backgroundTask will be marked completed. This has bearing on the IsLoadInProgress bool and how that dictates the rendered drawing (see immediately below).
			// Indicates that the contents of the CanvasControl need to be redrawn. Calling Invalidate results in the Draw event being raised shortly afterward (see immediately below).
			canvas.Invalidate();
		}

		// 5
		protected void CanvasControl_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
		{
			if (IsLoadInProgress())
			{
				// If the image failed to load in time, simply display a blank white background
				args.DrawingSession.FillRectangle(0, 0, (float)sender.Width, (float)sender.Height, Colors.White);
			}
			else
			{
				// If it successfully loaded, set the desired image and the opacity of the <CanvasImageBrush>
				_bgBrush.Image = _bgImage;
				_bgBrush.Opacity = _bgOpacity;

				// Lastly, fill a rectangle with the tiling image brush, covering the entire bounds of the canvas control
				var session = args.DrawingSession;
				session.FillRectangle(new Rect(new Point(), sender.Size), _bgBrush);
			}
		}

		protected bool IsLoadInProgress()
		{
			// Not gonna happen, see above sequence of events
			if (_backgroundTask == null) return false;

			// Unless the draw event from Invalidate() outpaces the actual async loading, this won't ever get hit as the LoadBackgroundAsync should have already returned a Task.Completed
			if (!_backgroundTask.IsCompleted) return true;

			try
			{
				// As _background task was set to LoadBackgroundAsync, should have already completed. Wait will be moot. 
				_backgroundTask.Wait();
			}
			catch (AggregateException ae)
			{
				// Catch any task-related errors along the way
				ae.Handle(ex => throw ex);
			}
			finally
			{
				// _backgroundTask will be set to null, so that CreateResourcesAsync won't be concerned with phantom existing tasks
				_backgroundTask = null;
			}
			// Permits the <CanvasControl> to render the safely loaded image 
			return false;
		}

		/// <summary>
		/// When the ViewModel's TransformGroup changes, this needs to update its background canvas
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (ViewModel == null) return;

			if (e.PropertyName == nameof(CollectionViewModel.TransformGroup))
			{

				if (_resourcesLoaded)
				{
					double clampBackgroundScaleForAliasing(double currentScale, double numberOfBackgroundRows)
					{
						while (currentScale / numberOfBackgroundRows > numberOfBackgroundRows)
						{
							currentScale /= numberOfBackgroundRows;
						}

						while (currentScale > 0 && currentScale * numberOfBackgroundRows < numberOfBackgroundRows)
						{
							currentScale *= numberOfBackgroundRows;
						}
						return currentScale;
					}

					var transformation = ViewModel.TransformGroup;
					// calculate the translate delta
					var translateDelta = new TranslateTransform
					{
						X = transformation.Translate.X,
						Y = transformation.Translate.Y
					};

					// calculate the scale delta
					var scaleDelta = new ScaleTransform
					{
						CenterX = transformation.ScaleCenter.X,
						CenterY = transformation.ScaleCenter.Y,
						ScaleX = transformation.ScaleAmount.X,
						ScaleY = transformation.ScaleAmount.Y
					};

					//Create initial composite transform
					var composite = new TransformGroup();
					composite.Children.Add(scaleDelta); // add the new scaling
					composite.Children.Add(translateDelta); // add the new translate

					var matrix = composite.Value;

                    var aliasSafeScale = clampBackgroundScaleForAliasing(matrix.M11, NumberOfBackgroundRows);
                    _bgBrush.Transform = new Matrix3x2((float)aliasSafeScale,
                        (float)matrix.M12,
                        (float)matrix.M21,
                        (float)aliasSafeScale,
                        (float)matrix.OffsetX,
                        (float)matrix.OffsetY);
                    _backgroundCanvas.Invalidate();
                }
            }
        }
        #endregion

		#region Tagging

		public bool TagNote(string tagValue, DocumentView docView)
		{
			return false;
			if (!TagMode)
			{
				return false;
			}
			//DocumentController image = null;
			//foreach (var documentController in group.TypedData)
			//{
			//    if (documentController.DocumentType.Equals(ImageBox.DocumentType))
			//    {
			//        image = documentController.GetDataDocument(null);
			//        break;
			//    }
			//}

			//if (image != null)
			//{
			//    image.SetField(TagKey, new TextController(tagValue), true);
			//    return true;
			//}
			return false;
		}

		public void ShowTagKeyBox()
		{
			GetTagBox().Visibility = Windows.UI.Xaml.Visibility.Visible;
			var mousePos = Util.PointTransformFromVisual(this.RootPointerPos(), Window.Current.Content, GetOuterGrid());
			GetTagBox().RenderTransform = new TranslateTransform { X = mousePos.X, Y = mousePos.Y };
		}

		public void HideTagKeyBox()
		{
			GetTagBox().Visibility = Windows.UI.Xaml.Visibility.Collapsed;
		}

		protected async void TagKeyBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
		{
			if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
			{
				var keys = await RESTClient.Instance.Fields.GetControllersByQueryAsync<KeyController>(new EverythingQuery<FieldModel>());
				var names = keys.Where(k => !k.Name.StartsWith("_"));
				GetTagBox().ItemsSource = names;
			}
		}

		protected void TagKeyBox_OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
		{
			sender.Text = ((KeyController)args.SelectedItem).Name;
		}

		protected async void TagKeyBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
		{
			if (args.ChosenSuggestion != null)
			{
				TagKey = (KeyController)args.ChosenSuggestion;
			}
			else
			{
			    var keys = await RESTClient.Instance.Fields.GetControllersByQueryAsync<KeyController>(new EverythingQuery<FieldModel>());
				var key = keys.FirstOrDefault(k => k.Name == args.QueryText);

				if (key == null)
				{
					TagKey = new KeyController(args.QueryText, Guid.NewGuid().ToString());
				}
				else
				{
					TagKey = key;
				}
			}
			TagMode = true;

			HideTagKeyBox();
		}

		#endregion

		#region Marquee Select

		Rectangle _marquee;
		Point _marqueeAnchor;
		bool _isMarqueeActive;
		private MarqueeInfo mInfo;

		protected virtual void OnPointerReleased(object sender, PointerRoutedEventArgs e)
		{
			if (_marquee != null)
			{
				var pos = Util.PointTransformFromVisual(new Point(Canvas.GetLeft(_marquee), Canvas.GetTop(_marquee)),
					GetSelectionCanvas(), GetItemsControl().ItemsPanelRoot);
				SelectionManager.SelectDocuments(DocsInMarquee(new Rect(pos, new Size(_marquee.Width, _marquee.Height))), this.IsShiftPressed());
				GetSelectionCanvas().Children.Remove(_marquee);
				_marquee = null;
				_isMarqueeActive = false;
				if (e != null) e.Handled = true;
			}

			SelectionCanvas?.Children.Clear();
			GetOuterGrid().PointerMoved -= OnPointerMoved;
			if (e != null) GetOuterGrid().ReleasePointerCapture(e.Pointer);
		}

		/// <summary>
		/// Handles mouse movement.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected virtual void OnPointerMoved(object sender, PointerRoutedEventArgs args)
		{
			if (_isMarqueeActive)
			{
				var pos = args.GetCurrentPoint(SelectionCanvas).Position;
				var dX = pos.X - _marqueeAnchor.X;
				var dY = pos.Y - _marqueeAnchor.Y;

				//Height and width depend on the difference in position of the current point and the anchor (initial point)
				double newWidth = (dX > 0) ? dX : -dX;
				double newHeight = (dY > 0) ? dY : -dY;

				//Anchor point should also be moved if dX or dY are moved
				var newAnchor = _marqueeAnchor;
				if (dX < 0) newAnchor.X += dX;
				if (dY < 0) newAnchor.Y += dY;

				if (newWidth > 5 && newHeight > 5 && _marquee == null)
				{
					this.Focus(FocusState.Programmatic);
					_marquee = new Rectangle()
					{
						Stroke = new SolidColorBrush(Color.FromArgb(200, 66, 66, 66)),
						StrokeThickness = 1.5 / Zoom,
						StrokeDashArray = new DoubleCollection { 4, 1 },
						CompositeMode = ElementCompositeMode.SourceOver
					};
                    this.IsTabStop = true;
                    this.Focus(FocusState.Pointer);
					_marquee.AllowFocusOnInteraction = true;
					SelectionCanvas?.Children.Add(_marquee);

					mInfo = new MarqueeInfo();
					SelectionCanvas?.Children.Add(mInfo);
				}

				if (_marquee != null) //Adjust the marquee rectangle
				{
					Canvas.SetLeft(_marquee, newAnchor.X);
					Canvas.SetTop(_marquee, newAnchor.Y);
					_marquee.Width = newWidth;
					_marquee.Height = newHeight;
					args.Handled = true;

					Canvas.SetLeft(mInfo, newAnchor.X);
					Canvas.SetTop(mInfo, newAnchor.Y - 32);
				}
			}
		}

		/// <summary>
		/// Handles mouse movement. Starts drawing Marquee selection.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected virtual void OnPointerPressed(object sender, PointerRoutedEventArgs args)
		{
			// marquee on left click by default
			if (MenuToolbar.Instance.GetMouseMode() == MenuToolbar.MouseMode.TakeNote)// bcz:  || args.IsRightPressed())
			{
				if (
					(args.KeyModifiers & VirtualKeyModifiers.Control) == 0 &&
					( // bcz: the next line makes right-drag pan within nested collections instead of moving them -- that doesn't seem right to me since MouseMode feels like it applies to left-button dragging only
					  // MenuToolbar.Instance.GetMouseMode() == MenuToolbar.MouseMode.PanFast || 
						((!args.GetCurrentPoint(GetOuterGrid()).Properties.IsRightButtonPressed)) && MenuToolbar.Instance.GetMouseMode() != MenuToolbar.MouseMode.PanFast))
				{
					if ((args.KeyModifiers & VirtualKeyModifiers.Shift) == 0)
						SelectionManager.DeselectAll();

					GetOuterGrid().CapturePointer(args.Pointer);
					_marqueeAnchor = args.GetCurrentPoint(GetSelectionCanvas()).Position;
					_isMarqueeActive = true;
					PreviewTextbox_LostFocus(null, null);
					ParentDocument.ManipulationMode = ManipulationModes.None;
					args.Handled = true;
					GetOuterGrid().PointerMoved -= OnPointerMoved;
					GetOuterGrid().PointerMoved += OnPointerMoved;
				}
			}
		}

        private static readonly List<VirtualKey> MarqueeKeys = new List<VirtualKey>
        {
            VirtualKey.A,
            VirtualKey.Back,
            VirtualKey.C,
            VirtualKey.Delete,
            VirtualKey.G,
            VirtualKey.R,
            VirtualKey.T
        };

        private void _marquee_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (_marquee != null && MarqueeKeys.Contains(e.Key) && _isMarqueeActive)
            {
                TriggerActionFromSelection(e.Key, true);
                e.Handled = true;
            }
        }

		public bool IsMarqueeActive => _isMarqueeActive;

		// called by SelectionManager to reset this collection's internal selection-based logic
		public void ResetMarquee()
		{
			GetSelectionCanvas()?.Children?.Clear();
			_marquee = null;
			_isMarqueeActive = false;
		}

		public List<DocumentView> DocsInMarquee(Rect marquee)
		{
			var selectedDocs = new List<DocumentView>();
			if (GetItemsControl().ItemsPanelRoot != null)
			{
				var docs = GetItemsControl().ItemsPanelRoot.Children;
				foreach (var documentView in docs.Select((d) => d.GetFirstDescendantOfType<DocumentView>()).Where(d => d != null && d.IsHitTestVisible))
				{
					var rect = documentView.TransformToVisual(GetCanvas()).TransformBounds(
						new Rect(new Point(), new Point(documentView.ActualWidth, documentView.ActualHeight)));
					if (marquee.IntersectsWith(rect))
					{
						selectedDocs.Add(documentView);
					}
				}
			}
			return selectedDocs;
		}

		public Rect GetBoundingRectFromSelection()
		{
			Point topLeftMostPoint = new Point(Double.PositiveInfinity, Double.PositiveInfinity);
			Point bottomRightMostPoint = new Point(Double.NegativeInfinity, Double.NegativeInfinity);

			bool isEmpty = true;

			foreach (DocumentView doc in SelectionManager.GetSelectedDocs())
			{
				isEmpty = false;
				topLeftMostPoint.X = doc.ViewModel.Position.X < topLeftMostPoint.X ? doc.ViewModel.Position.X : topLeftMostPoint.X;
				topLeftMostPoint.Y = doc.ViewModel.Position.Y < topLeftMostPoint.Y ? doc.ViewModel.Position.Y : topLeftMostPoint.Y;
				bottomRightMostPoint.X = doc.ViewModel.Position.X + doc.ViewModel.ActualSize.X > bottomRightMostPoint.X
					? doc.ViewModel.Position.X + doc.ViewModel.ActualSize.X
					: bottomRightMostPoint.X;
				bottomRightMostPoint.Y = doc.ViewModel.Position.Y + doc.ViewModel.ActualSize.Y > bottomRightMostPoint.Y
					? doc.ViewModel.Position.Y + doc.ViewModel.ActualSize.Y
					: bottomRightMostPoint.Y;
			}

			if (isEmpty) return Rect.Empty;

			return new Rect(topLeftMostPoint, bottomRightMostPoint);
		}

		/// <summary>
		/// Triggers one of the actions that you can do with selected documents, whether it's by dragging through a marquee or from currently selected ones.
		/// </summary>
		/// <param name="modifier"></param>
		/// <param name="fromMarquee">True if we select from the marquee, false if from currently selecte documents</param>
		public void TriggerActionFromSelection(VirtualKey modifier, bool fromMarquee)
		{
			void DoAction(Action<List<DocumentView>, Point, Size> action)
			{
				Point where;
				Rectangle marquee;
				IEnumerable<DocumentView> viewsToSelectFrom;

				if (fromMarquee)
				{
					where = Util.PointTransformFromVisual(new Point(Canvas.GetLeft(_marquee), Canvas.GetTop(_marquee)),
						SelectionCanvas, GetItemsControl().ItemsPanelRoot);
					marquee = _marquee;
					viewsToSelectFrom = DocsInMarquee(new Rect(where, new Size(_marquee.Width, _marquee.Height)));
					OnPointerReleased(null, null);
				}
				else
				{
					var bounds = GetBoundingRectFromSelection();

					// hack to escape when CoreWindow fires the event a second time when it's actually from the marquee
					if (bounds == Rect.Empty) return;

					where = new Point(bounds.X, bounds.Y);
					marquee = new Rectangle
					{
						Height = bounds.Height,
						Width = bounds.Width
					};
					viewsToSelectFrom = SelectionManager.GetSelectedDocs();
				}

				var toSelectFrom = viewsToSelectFrom.ToList();
				using (UndoManager.GetBatchHandle())
					action(toSelectFrom, where, new Size(marquee.Width, marquee.Height));
			}

			var type = CollectionView.CollectionViewType.Freeform;

			var deselect = false;
			if (!(this.IsCtrlPressed() || this.IsShiftPressed() || this.IsAltPressed()))
			{
				switch (modifier)
				{
					//create a viewcopy of everything selected
					case VirtualKey.A:
						DoAction((dvs, where, size) =>
						{
							var docs = dvs.Select(dv => dv.ViewModel.DocumentController.GetViewCopy()).ToList();
							ViewModel.AddDocument(new CollectionNote(where, type, size.Width, size.Height, docs).Document);
						});
						deselect = true;
						break;
					case VirtualKey.T:
						type = CollectionView.CollectionViewType.Schema;
						goto case VirtualKey.C;
					case VirtualKey.C:
						DoAction((views, where, size) =>
							{
								var docss = views.Select(dvm => dvm.ViewModel.DocumentController).ToList();
								DocumentController newCollection = new CollectionNote(where, type, size.Width, size.Height, docss).Document;
								ViewModel.AddDocument(newCollection);

								foreach (DocumentView v in views)
								{
									v.ViewModel.LayoutDocument.IsMovingCollections = true;
									v.DeleteDocument();
								}
							});
						deselect = true;
						break;
					case VirtualKey.Back:
					case VirtualKey.Delete:
						DoAction((views, where, size) =>
						{
							foreach (DocumentView v in views)
							{
								v.DeleteDocument();
							}
						});

                        deselect = true;
                        break;
                    case VirtualKey.G:
                        DoAction((views, where, size) =>
                        {
                            ViewModel.AddDocument(Util.AdornmentWithPosition(BackgroundShape.AdornmentShape.Rectangular, where, size.Width, size.Height));
                        });
                        deselect = true;
                        break;
                    case VirtualKey.R:
                        DoAction((views, where, size) =>
                        {
                            if (size.Width >= 215 && size.Height >= 200)
                            {
                                ViewModel.AddDocument(new DishReplBox(where.X, where.Y, size.Width, size.Height).Document);
                            }
                        });
                        deselect = true;
                        break;
                }
            }
            else if (this.IsShiftPressed())
            {
                switch (modifier)
                {
                    case VirtualKey.R:
                        DoAction((views, where, size) =>
                        {
                            if (size.Width >= 215 && size.Height >= 200)
                            {
                                ViewModel.AddDocument(new DishScriptBox(where.X, where.Y, size.Width, size.Height).Document);
                            }
                        });
                        deselect = true;
                        break;
                }
            }

			if (deselect)
				SelectionManager.DeselectAll();
		}

		#endregion

		#region DragAndDrop

		public void SetDropIndicationFill(Brush fill)
		{
			GetDropIndicationRectangle().Fill = fill;
		}

		#endregion

		#region Activation

		protected void OnTapped(object sender, TappedRoutedEventArgs e)
		{
			//if (XInkCanvas.IsTopmost())
			{
				_isMarqueeActive = false;
				if (!this.IsShiftPressed())
					RenderPreviewTextbox(e.GetPosition(_itemsPanelCanvas));
			}
			foreach (var rtv in Content.GetDescendantsOfType<RichTextView>())
				rtv.xRichEditBox.Document.Selection.EndPosition = rtv.xRichEditBox.Document.Selection.StartPosition;
		}
        
		public void RenderPreviewTextbox(Point where)
        {
            previewTextBuffer = "";
            if (previewTextbox != null)
			{
				Canvas.SetLeft(previewTextbox, where.X);
				Canvas.SetTop(previewTextbox, where.Y);
				previewTextbox.Visibility = Visibility.Visible;
				AddHandler(KeyDownEvent, previewTextHandler, false);
				previewTextbox.Text = "";
				previewTextbox.SelectAll();
				previewTextbox.LostFocus -= PreviewTextbox_LostFocus;
				previewTextbox.LostFocus += PreviewTextbox_LostFocus;
				previewTextbox.Focus(FocusState.Pointer);
			}
		}
		#endregion

		#region TextInputBox

		string previewTextBuffer = "";
		private bool previewSelectText = false;
		public FreeformInkControl InkControl;
		public InkCanvas XInkCanvas;
		public Canvas SelectionCanvas;
		public double Zoom => ViewManipulationControls.ElementScale;

		void MakeInkCanvas()
		{
			XInkCanvas = new InkCanvas() { Width = 60000, Height = 60000 };

			InkControl = new FreeformInkControl(this, XInkCanvas, SelectionCanvas);
			Canvas.SetLeft(XInkCanvas, -30000);
			Canvas.SetTop(XInkCanvas, -30000);
			GetInkHostCanvas().Children.Add(XInkCanvas);
		}

		bool loadingPermanentTextbox;

		/// <summary>
		/// THIS IS KIND OF A HACK, DON'T USE THIS
		/// </summary>
		public void MarkLoadingNewTextBox(string text = "", bool selectText = false)
		{
			previewTextBuffer = text;
			previewSelectText = selectText;
			if (!loadingPermanentTextbox)
			{
				loadingPermanentTextbox = true;
			}
		}

		TextBox previewTextbox { get; set; }

		object previewTextHandler = null;
		void MakePreviewTextbox()
		{
			if (previewTextHandler == null)
				previewTextHandler = new KeyEventHandler(PreviewTextbox_KeyDown);

			previewTextbox = new TextBox
			{
				Width = 200,
				Height = 50,
				Background = new SolidColorBrush(Colors.Transparent),
				Visibility = Visibility.Collapsed
			};
			previewTextbox.Paste += previewTextbox_Paste;
			previewTextbox.Unloaded += (s, e) => RemoveHandler(KeyDownEvent, previewTextHandler);
			GetInkHostCanvas().Children.Add(previewTextbox);
			previewTextbox.LostFocus -= PreviewTextbox_LostFocus;
			previewTextbox.LostFocus += PreviewTextbox_LostFocus;
		}

		void PreviewTextbox_LostFocus(object sender, RoutedEventArgs e)
		{
			if (previewTextHandler != null)
			{
				RemoveHandler(KeyDownEvent, previewTextHandler);
				previewTextbox.Visibility = Visibility.Collapsed;
				previewTextbox.LostFocus -= PreviewTextbox_LostFocus;
			}
		}

		protected void previewTextbox_Paste(object sender, TextControlPasteEventArgs e)
		{
			var text = previewTextbox.Text;
			if (previewTextbox.Visibility != Visibility.Collapsed)
			{
				var where = new Point(Canvas.GetLeft(previewTextbox), Canvas.GetTop(previewTextbox));
			}
		}

		async void PreviewTextbox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key.Equals(VirtualKey.Escape))
			{
				e.Handled = true;
				PreviewTextbox_LostFocus(null, null);
				return;
			}
			previewTextbox.LostFocus -= PreviewTextbox_LostFocus;
			var text = KeyCodeToUnicode(e.Key);
			if (string.IsNullOrEmpty(text))
				return;
			if (previewTextbox.Visibility != Visibility.Collapsed)
			{
				e.Handled = true;
				var where = new Point(Canvas.GetLeft(previewTextbox), Canvas.GetTop(previewTextbox));
                Debug.WriteLine("Where = " + where);
				if (this.IsCtrlPressed())
				{
					//deals with control V pasting
					if (text == "v")
					{
						using (UndoManager.GetBatchHandle())
						{
							var postitNote = await ViewModel.Paste(Clipboard.GetContent(), where);

							//check if a doc is currently in link activation mode
							if (LinkActivationManager.ActivatedDocs.Count >= 1)
							{
								foreach (DocumentView activated in LinkActivationManager.ActivatedDocs)
								{
									//make this rich text an annotation for activated  doc
									if (KeyStore.RegionCreator.ContainsKey(activated.ViewModel.DocumentController.DocumentType))
									{
										var region = KeyStore.RegionCreator[activated.ViewModel.DocumentController.DocumentType](activated,
											postitNote.GetPosition());

                                    //link region to this text 
                                    region.Link(postitNote, LinkBehavior.Overlay);
                                }
                            }
                        }

							previewTextbox.Visibility = Visibility.Collapsed;
						}
					}
					else
					{
						LoadNewActiveTextBox("", where);
					}
				}
				//else if (this.IsCtrlPressed())
				//{
				//    //if we can access rich text view here, we can actually respond to these events
				//    //either call the key down event in richtextbox or handle diff control cases here
				//    LoadNewActiveTextBox("", where);
				//}
				else
				{
					previewTextBuffer += text;
					if (text.Length > 0)
						LoadNewActiveTextBox(text, where);
				}
			}
		}

		public void LoadNewActiveTextBox(string text, Point where, bool resetBuffer = false)
		{
			if (!loadingPermanentTextbox)
			{
				using (UndoManager.GetBatchHandle())
				{

					if (resetBuffer)
						previewTextBuffer = "";
					loadingPermanentTextbox = true;

					if (SettingsView.Instance.MarkdownEditOn)
					{
						var postitNote = new MarkdownNote(text: text).Document;
						Actions.DisplayDocument(ViewModel, postitNote, where);
					}
					else
					{
						var postitNote = new RichTextNote(text: text).Document;
						Actions.DisplayDocument(ViewModel, postitNote, where);

						//move link activation stuff here
						//check if a doc is currently in link activation mode
						if (LinkActivationManager.ActivatedDocs.Count >= 1)
						{
							foreach (var activated in LinkActivationManager.ActivatedDocs.Where((dv) => dv.ViewModel != null))
							{
                                KeyStore.RegionCreator.TryGetValue(activated.ViewModel.DocumentController.DocumentType, out KeyStore.MakeRegionFunc func);
                                //make this rich text an annotation for activated  doc
                                if (func != null)
								{
									var region = func( activated,
											           Util.PointTransformFromVisual(postitNote.GetPosition() ?? new Point(), _itemsPanelCanvas, activated));

									//link region to this text  
									region.Link(postitNote, LinkBehavior.Annotate);
								}
							}
						}
					}
				}
			}
		}

		public void LoadNewDataBox(string keyname, Point where, bool resetBuffer = false)
        {
            if (!loadingPermanentTextbox)
            {
                if (resetBuffer)
                    previewTextBuffer = "";
                loadingPermanentTextbox = true;
                var containerData = ViewModel.ContainerDocument.GetDataDocument();
                var keycontroller = new KeyController(keyname);
                if (containerData.GetField(keycontroller, true) == null)
                    containerData.SetField(keycontroller, containerData.GetField(keycontroller) ?? new TextController("<default>"), true);
                var dbox = new DataBox(new DocumentReferenceController(containerData, keycontroller), where.X, where.Y).Document;
                dbox.Tag = "Auto DataBox " + DateTime.Now.Second + "." + DateTime.Now.Millisecond;
                dbox.SetField(KeyStore.DocumentContextKey, containerData, true);
                Actions.DisplayDocument(ViewModel, dbox, where);
            }
        }

		string KeyCodeToUnicode(VirtualKey key)
		{

			var shiftState = this.IsShiftPressed();
			var capState = this.IsCapsPressed();
			var virtualKeyCode = (uint)key;

			string character = null;

			// take care of symbols
			if (key == VirtualKey.Space)
			{
				character = " ";
			}
			if (key == VirtualKey.Multiply)
			{
				character = "*";
			}
			// TODO take care of more symbols

			//Take care of letters
			if (virtualKeyCode >= 65 && virtualKeyCode <= 90)
			{
				if ((!shiftState && !capState) || (shiftState && capState))
				{
					character = key.ToString().ToLower();
				}
				else
				{
					character = key.ToString();
				}
			}

			//Take care of numbers
			if (virtualKeyCode >= 48 && virtualKeyCode <= 57)
			{
				character = (virtualKeyCode - 48).ToString();
				if ((shiftState != false || capState != false) &&
					(!shiftState || !capState))
				{
					switch ((virtualKeyCode - 48))
					{
						case 1: character = "!"; break;
						case 2: character = "@"; break;
						case 3: character = "#"; break;
						case 4: character = "$"; break;
						case 5: character = "%"; break;
						case 6: character = "^"; break;
						case 7: character = "&"; break;
						case 8: character = "*"; break;
						case 9: character = "("; break;
						case 0: character = ")"; break;
						default: break;
					}
				}
			}

			if (virtualKeyCode >= 186 && virtualKeyCode <= 222)
			{
				var shifted = ((shiftState != false || capState != false) &&
					(!shiftState || !capState));
				switch (virtualKeyCode)
				{
					case 186: character = shifted ? ":" : ";"; break;
					case 187: character = shifted ? "=" : "+"; break;
					case 188: character = shifted ? "<" : ","; break;
					case 189: character = shifted ? "_" : "-"; break;
					case 190: character = shifted ? ">" : "."; break;
					case 191: character = shifted ? "?" : "/"; break;
					case 192: character = shifted ? "~" : "`"; break;
					case 219: character = shifted ? "{" : "["; break;
					case 220: character = shifted ? "|" : "\\"; break;
					case 221: character = shifted ? "}" : "]"; break;
					case 222: character = shifted ? "\"" : "'"; break;
				}

			}
			//Take care of numpad numbers
			if (virtualKeyCode >= 96 && virtualKeyCode <= 105)
			{
				character = (virtualKeyCode - 96).ToString();
			}

			return character;
		}

		/// <summary>
		/// OnLoad handler. Interfaces with DocumentView to call corresponding functions.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void DocumentViewOnLoaded(object sender, RoutedEventArgs e)
		{
			if (sender is DocumentView documentView)
			{
				if (loadingPermanentTextbox)
				{
					var richEditBox = documentView.GetDescendantsOfType<RichEditBox>().FirstOrDefault();
					if (richEditBox != null)
					{
						richEditBox.GotFocus -= RichEditBox_GotFocus;
						richEditBox.GotFocus += RichEditBox_GotFocus;
						richEditBox.Focus(FocusState.Programmatic);
						//if (previewSelectText)
						//{
						// RoutedEventHandler loaded = null;
						// loaded = (o, args) =>
						// {
						//  richEditBox.Loaded -= loaded;
						//  richEditBox.Document.GetText(TextGetOptions.None, out var str);
						//  richEditBox.Document.Selection.SetRange(0, str.Length);
						// };
						// richEditBox.Loaded += loaded;

						// previewSelectText = false;
						//}
					}
					var textBox = documentView.GetDescendantsOfType<EditableTextBlock>().FirstOrDefault();
					if (textBox != null)
					{
						textBox.Loaded -= TextBox_Loaded;
						textBox.Loaded += TextBox_Loaded;
					}
					var editableScriptBox = documentView.GetDescendantsOfType<EditableScriptView>().FirstOrDefault();
					if (editableScriptBox != null)
					{
						editableScriptBox.Loaded -= EditableScriptView_Loaded;
						editableScriptBox.Loaded += EditableScriptView_Loaded;
					}
					var a = documentView.GetDescendantsOfType<EditableMarkdownBlock>();
					var editableMarkdownBox = documentView.GetDescendantsOfType<EditableMarkdownBlock>().FirstOrDefault();
					if (editableMarkdownBox != null)
					{
						editableMarkdownBox.Loaded -= EditableMarkdownBlock_Loaded;
						editableMarkdownBox.Loaded += EditableMarkdownBlock_Loaded;
					}
                }
			}

		}

		protected void EditableScriptView_Loaded(object sender, RoutedEventArgs e)
		{
			var textBox = sender as EditableScriptView;
			textBox.Loaded -= EditableScriptView_Loaded;
			textBox.MakeEditable();
			textBox.XTextBox.GotFocus -= TextBox_GotFocus;
			textBox.XTextBox.GotFocus += TextBox_GotFocus;
			textBox.XTextBox.Focus(FocusState.Programmatic);
		}

		protected void TextBox_Loaded(object sender, RoutedEventArgs e)
		{
			var textBox = sender as EditableTextBlock;
			textBox.Loaded -= TextBox_Loaded;
			textBox.MakeEditable();
			textBox.XTextBox.GotFocus -= TextBox_GotFocus;
			textBox.XTextBox.GotFocus += TextBox_GotFocus;
			textBox.XTextBox.Focus(FocusState.Programmatic);
		}
		private void EditableMarkdownBlock_Loaded(object sender, RoutedEventArgs e)
		{
			var textBox = sender as EditableMarkdownBlock;
			textBox.Loaded -= EditableMarkdownBlock_Loaded;
			textBox.MakeEditable();
			textBox.XMarkdownBox.GotFocus -= TextBox_GotFocus;
			textBox.XMarkdownBox.GotFocus += TextBox_GotFocus;
			textBox.XMarkdownBox.Focus(FocusState.Programmatic);
		}

		void RichEditBox_GotFocus(object sender, RoutedEventArgs e)
		{
			RemoveHandler(KeyDownEvent, previewTextHandler);
			previewTextbox.Visibility = Visibility.Collapsed;
			loadingPermanentTextbox = false;
			var text = previewTextBuffer;
			var richEditBox = sender as RichEditBox;
			richEditBox.GotFocus -= RichEditBox_GotFocus;
			previewTextbox.Text = string.Empty;
			richEditBox.Document.Selection.SetRange(0, 0);
			richEditBox.Document.SetText(TextSetOptions.None, text);
			richEditBox.Document.Selection.SetRange(text.Length, text.Length);
		}

		void TextBox_GotFocus(object sender, RoutedEventArgs e)
		{
			var textBox = sender as TextBox;

			RemoveHandler(KeyDownEvent, previewTextHandler);
			previewTextbox.Visibility = Visibility.Collapsed;
			loadingPermanentTextbox = false;
			var text = previewTextBuffer;
			textBox.GotFocus -= TextBox_GotFocus;
			previewTextbox.Text = string.Empty;
		}

		#endregion

	}

}
