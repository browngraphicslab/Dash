using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using DashShared;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class FreeformView : UserControl
    {
        public float CanvasScale { get; set; } = 1;
        public const float MaxScale = 10;
        public const float MinScale = 0.5f;

        public Transform CanvasTransform
        {
            get { return XCanvas.RenderTransform; }
            set { XCanvas.RenderTransform = value; }
        }

        public int CanvasWidth
        {
            get { return (int)GetValue(CanvasWidthProperty); }
            set { SetValue(CanvasWidthProperty, value); }
        }
        public int CanvasHeight
        {
            get { return (int)GetValue(CanvasHeightProperty); }
            set { SetValue(CanvasHeightProperty, value); }
        }

        public static readonly DependencyProperty CanvasWidthProperty = DependencyProperty.Register(
            "CanvasWidth",
            typeof(int),
            typeof(FreeformView),
            new PropertyMetadata(null)
        );

        public static readonly DependencyProperty CanvasHeightProperty = DependencyProperty.Register(
            "CanvasHeight",
            typeof(int),
            typeof(FreeformView),
            new PropertyMetadata(null)
        );

        //public Rect CanvasClipRect
        //{
        //    get { return (Rect)GetValue(CanvasClipRectProperty); }
        //    set { SetValue(CanvasClipRectProperty, value); }
        //}

        //public static readonly DependencyProperty CanvasClipRectProperty = DependencyProperty.Register(
        //    "CanvasClipRect",
        //    typeof(Rect),
        //    typeof(FreeformView),
        //    new PropertyMetadata(null)
        //);

        public Canvas Canvas => XCanvas;

        /// <summary>
        /// Get the parent of XCanvas 
        /// </summary>
        private FrameworkElement _parentElement = null;
        private FrameworkElement ParentElement
        {
            get
            {
                if (_parentElement == null)
                {
                    _parentElement = XCanvas.Parent as FrameworkElement;
                }
                Debug.Assert(_parentElement != null);
                return _parentElement;
            }
        }

        public static FreeformView MainFreeformView { get; private set; }

        public FreeformViewModel ViewModel { get; private set; }

        public FreeformView()
        {
            this.InitializeComponent();
            XCanvas.DataContext = this;

            ViewModel = new FreeformViewModel();
            ViewModel.ElementAdded += VmElementAdded;

            XInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen;

            // set screen in middle of canvas 
            //CanvasTransform = new TranslateTransform { X = -XCanvas.Width / 2, Y = -XCanvas.Height / 2 };

            if (MainFreeformView == null)
            {
                MainFreeformView = this;
            }
        }


        private void VmElementAdded(UIElement element, float left, float top)
        {
            XCanvas.Children.Add(element);
            Canvas.SetLeft(element, left);
            Canvas.SetTop(element, top);
        }

        /// <summary>
        /// Pans and zooms upon touch manipulation 
        /// </summary>
        private void UserControl_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true;
            ManipulationDelta delta = e.Delta;

            //Create initial translate and scale transforms
            //Translate is in screen space, scale is in canvas space
            TranslateTransform translate = new TranslateTransform
            {
                X = delta.Translation.X,
                Y = delta.Translation.Y
            };

            ScaleTransform scale = new ScaleTransform
            {
                CenterX = e.Position.X,
                CenterY = e.Position.Y,
                ScaleX = delta.Scale,
                ScaleY = delta.Scale
            };
            
            //Clamp the zoom
            CanvasScale *= delta.Scale;
            if (CanvasScale > MaxScale)
            {
                CanvasScale = MaxScale;
                scale.ScaleX = 1;
                scale.ScaleY = 1;
            }
            if (CanvasScale < MinScale)
            {
                CanvasScale = MinScale;
                scale.ScaleX = 1;
                scale.ScaleY = 1;
            }

            //Create initial composite transform
            TransformGroup composite = new TransformGroup();
            composite.Children.Add(scale);
            composite.Children.Add(CanvasTransform);
            composite.Children.Add(translate);

            //Get top left and bottom right screen space points in canvas space
            GeneralTransform inverse = composite.Inverse;
            Debug.Assert(inverse != null);
            Debug.Assert(XCanvas.RenderTransform != null);
            GeneralTransform renderInverse = XCanvas.RenderTransform.Inverse;
            Debug.Assert(renderInverse != null);
            Point topLeft = inverse.TransformPoint(new Point(0, 0));
            Point bottomRight = inverse.TransformPoint(new Point(ParentElement.ActualWidth, ParentElement.ActualHeight));
            Point preTopLeft = renderInverse.TransformPoint(new Point(0, 0));
            Point preBottomRight = renderInverse.TransformPoint(new Point(ParentElement.ActualWidth, ParentElement.ActualHeight));

            //Check if the panning or zooming puts the view out of bounds of the canvas
            //Nullify scale or translate components accordingly
            bool outOfBounds = false;
            //Create a canvas space translation to correct the translation if necessary
            TranslateTransform fixTranslate = new TranslateTransform();
            if (topLeft.X < 0 && bottomRight.X > XCanvas.ActualWidth)
            {
                translate.X = 0;
                fixTranslate.X = 0;
                double scaleAmount = (bottomRight.X - topLeft.X) / CanvasWidth;
                scale.ScaleY = scaleAmount;
                scale.ScaleX = scaleAmount;
                outOfBounds = true;
            }
            else if (topLeft.X < 0)
            {
                translate.X = 0;
                fixTranslate.X = preTopLeft.X;
                scale.CenterX = 0;
                outOfBounds = true;
            }
            else if (bottomRight.X > XCanvas.ActualWidth - 1)
            {
                translate.X = 0;
                fixTranslate.X = -(XCanvas.ActualWidth - preBottomRight.X - 1);
                scale.CenterX = XCanvas.ActualWidth;
                outOfBounds = true;
            }
            if (topLeft.Y < 0 && bottomRight.Y > XCanvas.ActualHeight)
            {
                translate.Y = 0;
                fixTranslate.Y = 0;
                double scaleAmount = (bottomRight.Y - topLeft.Y) / CanvasHeight;
                scale.ScaleX = scaleAmount;
                scale.ScaleY = scaleAmount;
                outOfBounds = true;
            }
            else if (topLeft.Y < 0)
            {
                translate.Y = 0;
                fixTranslate.Y = preTopLeft.Y;
                scale.CenterY = 0;
                outOfBounds = true;
            }
            else if (bottomRight.Y > XCanvas.ActualHeight - 1)
            {
                translate.Y = 0;
                fixTranslate.Y = -(XCanvas.ActualHeight - preBottomRight.Y - 1);
                scale.CenterY = XCanvas.ActualHeight;
                outOfBounds = true;
            }

            //If the view was out of bounds recalculate the composite matrix
            if (outOfBounds)
            {
                composite = new TransformGroup();
                composite.Children.Add(fixTranslate);
                composite.Children.Add(scale);
                composite.Children.Add(CanvasTransform);
                composite.Children.Add(translate);
            }

            CanvasTransform = new MatrixTransform { Matrix = composite.Value };
        }

        /// <summary>
        /// Zooms upon mousewheel interaction 
        /// </summary>
        private void UserControl_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            //Get mousepoint in canvas space 
            PointerPoint point = e.GetCurrentPoint(XCanvas);
            double scale = Math.Pow(1 + 0.15 * Math.Sign(point.Properties.MouseWheelDelta),
                Math.Abs(point.Properties.MouseWheelDelta) / 120.0f);
            scale = Math.Max(Math.Min(scale, 1.7f), 0.4f);
            CanvasScale *= (float)scale;
            Debug.Assert(XCanvas.RenderTransform != null);
            Point canvasPos = XCanvas.RenderTransform.TransformPoint(point.Position);

            //Create initial ScaleTransform 
            ScaleTransform scaleTransform = new ScaleTransform
            {
                CenterX = canvasPos.X,
                CenterY = canvasPos.Y,
                ScaleX = scale,
                ScaleY = scale
            };

            //Clamp scale
            if (CanvasScale > MaxScale)
            {
                CanvasScale = MaxScale;
                scaleTransform.ScaleX = 1;
                scaleTransform.ScaleY = 1;
            }
            if (CanvasScale < MinScale)
            {
                CanvasScale = MinScale;
                scaleTransform.ScaleX = 1;
                scaleTransform.ScaleY = 1;
            }

            //Create initial composite transform
            TransformGroup composite = new TransformGroup();
            composite.Children.Add(CanvasTransform);
            composite.Children.Add(scaleTransform);

            GeneralTransform inverse = composite.Inverse;
            Debug.Assert(inverse != null);
            GeneralTransform renderInverse = XCanvas.RenderTransform.Inverse;
            Debug.Assert(inverse != null);
            Debug.Assert(renderInverse != null);
            Point topLeft = inverse.TransformPoint(new Point(0, 0));
            Point bottomRight = inverse.TransformPoint(new Point(ParentElement.ActualWidth, ParentElement.ActualHeight));
            Point preBottomRight = renderInverse.TransformPoint(new Point(ParentElement.ActualWidth, ParentElement.ActualHeight));

            //Create a canvas space translation to correct the translation if necessary
            TranslateTransform translate = new TranslateTransform
            {
                X = 0,
                Y = 0
            };

            //Check if the zooming puts the view out of bounds of the canvas
            //Nullify scale or translate components accordingly 
            bool outOfBounds = false;
            if (topLeft.X < 0 && bottomRight.X > XCanvas.ActualWidth)
            {
                translate.X = 0;
                double scaleAmount = (bottomRight.X - topLeft.X) / CanvasWidth;
                scaleTransform.ScaleY = scaleAmount;
                scaleTransform.ScaleX = scaleAmount;
                outOfBounds = true;
            }
            else if (topLeft.X < 0)
            {
                scaleTransform.CenterX = 0;
                outOfBounds = true;
            }
            else if (bottomRight.X >= XCanvas.ActualWidth)
            {
                translate.X = preBottomRight.X - XCanvas.ActualWidth;
                scaleTransform.CenterX = ParentElement.ActualWidth;
                outOfBounds = true;
            }
            if (topLeft.Y < 0 && bottomRight.Y > XCanvas.ActualHeight)
            {
                translate.Y = 0;
                double scaleAmount = (bottomRight.Y - topLeft.Y) / CanvasHeight;
                scaleTransform.ScaleY = scaleAmount;
                scaleTransform.ScaleX = scaleAmount;
                outOfBounds = true;
            }
            else if(topLeft.Y < 0)
            {
                scaleTransform.CenterY = 0;
                outOfBounds = true;
            }
            else if (bottomRight.Y >= XCanvas.ActualHeight)
            {
                translate.Y = preBottomRight.Y - XCanvas.ActualHeight;
                scaleTransform.CenterY = ParentElement.ActualHeight;
                outOfBounds = true;
            }

            //If the view was out of bounds recalculate the composite matrix
            if (outOfBounds)
            {
                composite = new TransformGroup();
                composite.Children.Add(translate);
                composite.Children.Add(CanvasTransform);
                composite.Children.Add(scaleTransform);
            }
            CanvasTransform = new MatrixTransform { Matrix = composite.Value };
        }

        /// <summary>
        /// Make translation inertia slow down faster
        /// </summary>
        private void UserControl_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
        {
            e.TranslationBehavior.DesiredDeceleration = 0.01;
        }

        /// <summary>
        /// Make sure the canvas is still in bounds after resize
        /// </summary>
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            TranslateTransform translate = new TranslateTransform();

            //Calculate bottomRight corner of screen in canvas space before and after resize 
            Debug.Assert(XCanvas.RenderTransform != null);
            Debug.Assert(XCanvas.RenderTransform.Inverse != null);
            Point oldBottomRight =
                XCanvas.RenderTransform.Inverse.TransformPoint(new Point(e.PreviousSize.Width, e.PreviousSize.Height));
            Point bottomRight =
                XCanvas.RenderTransform.Inverse.TransformPoint(new Point(e.NewSize.Width, e.NewSize.Height));

            //Check if new bottom right is out of bounds
            bool outOfBounds = false;
            if (bottomRight.X > XCanvas.ActualWidth - 1)
            {
                translate.X = -(oldBottomRight.X - bottomRight.X);
                outOfBounds = true;
            }
            if (bottomRight.Y > XCanvas.ActualHeight - 1)
            {
                translate.Y = -(oldBottomRight.Y - bottomRight.Y);
                outOfBounds = true;
            }
            //If it is out of bounds, translate so that is is in bounds
            if (outOfBounds)
            {
                TransformGroup composite = new TransformGroup();
                composite.Children.Add(translate);
                composite.Children.Add(XCanvas.RenderTransform);
                XCanvas.RenderTransform = new MatrixTransform { Matrix = composite.Value };
            }

            Clip = new RectangleGeometry {Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height)};
        }

        /// <summary>
        /// Handles drop events onto the canvas, usually by creating a copy document of the original and
        /// placing it into the canvas.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">drag event arguments</param>
        private async void XCanvas_Drop(object sender, DragEventArgs e) {
            Image dragged = new Image();
            string type = "";

            // load items dragged from solution explorer
            if (e.DataView.Contains(StandardDataFormats.StorageItems)) {
                var items = await e.DataView.GetStorageItemsAsync();

                if (items.Any()) {
                    var storageFile = items[0] as StorageFile;
                    var contentType = storageFile.ContentType;

                    StorageFolder folder = ApplicationData.Current.LocalFolder;

                    // parse images dropped in
                    if (contentType == "image/jpg" || contentType == "image/png" || contentType == "image/jpeg") {
                        StorageFile newFile = await storageFile.CopyAsync(folder, storageFile.Name, NameCollisionOption.GenerateUniqueName);
                        var bitmapImg = new BitmapImage();
                        bitmapImg.SetSource(await storageFile.OpenAsync(FileAccessMode.Read));
                        dragged.Source = bitmapImg;
                        type = "image";
                    }

                    // parse text files dropped in
                    if (contentType == "text/plain") {
                        StorageFile newFile = await storageFile.CopyAsync(folder, storageFile.Name, NameCollisionOption.GenerateUniqueName);
                        string text = await FileIO.ReadTextAsync(newFile);
                        type = "text";


                        var docControllr = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
                        var keyControllr = App.Instance.Container.GetRequiredService<KeyEndpoint>();

                        // generate single-image document model
                        var fields = new Dictionary<Key, FieldModel>();
                        var contentKey = keyControllr.CreateKeyAsync("content");
                        fields[contentKey] = new TextFieldModel(text);
                        var dm = docControllr.CreateDocumentAsync("textfile");
                        dm.SetFields(fields);
                        DocumentViewModel model = new DocumentViewModel(dm);
                        DocumentView view = new DocumentView(model);

                        // position relative to mouse
                        Point dPos = e.GetPosition(XCanvas);
                        view.Margin = new Thickness(dPos.X, dPos.Y, 0, 0);
                        XCanvas.Children.Add(view);

                        return;
                    }

                }
            }

            if (e.DataView.Properties["image"] != null)
                dragged = e.DataView.Properties["image"] as Image; // fetches stored drag object
            
            // make document
            var docController = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            
                // generate single-image document model
                DocumentModel image = DocumentModel.OneImage();
                image.SetField(DocumentModel.GetFieldKeyByName("content"), new ImageFieldModel(dragged), true);
                DocumentViewModel model3 = new DocumentViewModel(image);
                DocumentView view3 = new DocumentView(model3);

                // position relative to mouse
                Point dropPos = e.GetPosition(XCanvas);
                view3.Margin = new Thickness(dropPos.X, dropPos.Y, 0, 0);

                XCanvas.Children.Add(view3);

            
        }
        
        private void XCanvas_DragOver_1(object sender, DragEventArgs e) {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }
    }
}
