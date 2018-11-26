using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.System;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Annotations;
using DashShared;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Media;
using Visibility = Windows.UI.Xaml.Visibility;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash
{

    public partial class EditableImage : INotifyPropertyChanged
    {
        private StateCropControl _cropControl;
        private ImageController _imgctrl;
        public bool IsCropping;
        private DocumentView _docview;
        public DocumentController DataDocument => (DataContext as DocumentViewModel).DataDocument;
        public DocumentController LayoutDocument => (DataContext as DocumentViewModel).LayoutDocument;

        // interface-required event to communicate with the AnnotationManager about when it's okay to start annotating

        public Image Image => xImage;

        public event PropertyChangedEventHandler PropertyChanged;

        private AnnotationOverlay _annotationOverlay;

        public Stretch Stretch
        {
            get => xImage.Stretch;
            set => xImage.Stretch = value;
        }

        public KeyController DataFieldKey { get; set; }

        public EditableImage()
        {
            InitializeComponent();
            Image.Loaded += Image_Loaded;
            Image.ImageOpened += (sender, args) =>
            {
                var source = Image.Source as BitmapSource;
                XAnnotationGrid.Width = source?.PixelWidth ?? Image.ActualWidth;
                XAnnotationGrid.Height = source?.PixelHeight ?? Image.ActualHeight;

                _annotationOverlay = new AnnotationOverlay(LayoutDocument, RegionGetter);
                _annotationOverlay.CurrentAnnotationType = AnnotationType.Region;
                XAnnotationGrid.Children.Add(_annotationOverlay);
                XAnnotationGridWithEmbeddings.Children.Add(_annotationOverlay.AnnotationOverlayEmbeddings);
            };

            Loaded += EditableImage_Loaded;
            // existing annotated regions are loaded with the VisualAnnotationManager
        }

        private void EditableImage_Loaded(object sender, RoutedEventArgs e)
        {
            LayoutDocument.AddWeakFieldUpdatedListener(this, KeyStore.GoToRegionKey, (view, controller, arg3) => view.GoToUpdated(controller, arg3));
            
            _imgctrl = LayoutDocument.GetDataDocument().GetDereferencedField<ImageController>(DataFieldKey, null);
        }

        private DocumentController RegionGetter(AnnotationType type)
        {
            return new RichTextNote("Image Region").Document;
            return new ImageNote(_imgctrl.ImageSource).Document;
        }

        public async Task ReplaceImage()
        {
            _imgctrl = LayoutDocument.GetDereferencedField<ImageController>(DataFieldKey, new Context());

            // get the file from the current image controller
            var file = await GetImageFile();
            var fileProperties = await file.Properties.GetImagePropertiesAsync();

            // set image source to the new file path and fix the width
            Image.Source = new BitmapImage(new Uri(file.Path));

            // on replace image, change the original image value for revert
            var origImgCtrl = LayoutDocument.GetDataDocument().GetDereferencedField<ImageController>(DataFieldKey, new Context());
            LayoutDocument.GetDataDocument().SetField(KeyStore.OriginalImageKey, origImgCtrl, true);
            LayoutDocument.SetWidth(LayoutDocument.GetActualSize().Value.X);
            LayoutDocument.SetHeight(double.NaN);
        }

        private async Task<StorageFile> GetImageFile()
        {
            /*
			 * TODO There has to be a better way to do this. Maybe ask Bob and see if he has any ideas?
			 * try catch is literally the only way we can deal with regular
			 * local uris, absolute uris, and website uris as the same time
			 */
            try
            {
                // method of getting file from local uri
                return await StorageFile.GetFileFromPathAsync(_imgctrl.ImageSource.LocalPath);
            }
            catch (Exception)
            {
                // method of getting file from absolute uri
                return await StorageFile.GetFileFromApplicationUriAsync(_imgctrl.ImageSource);
            }
        }


        public async void Revert()
        {
            using (UndoManager.GetBatchHandle())
            {
                // make sure if we have an original image stored (which we always should)
                if (LayoutDocument.GetDataDocument().GetField(KeyStore.OriginalImageKey) is ImageController originalImage)
                {
                    _imgctrl = originalImage;

                    LayoutDocument.GetDataDocument().SetField<ImageController>(DataFieldKey, originalImage.ImageSource, true);
                    LayoutDocument.SetWidth(LayoutDocument.GetActualSize().Value.X);
                    LayoutDocument.SetHeight(double.NaN);
                }
            }
        }

        private void Image_Loaded(object sender, RoutedEventArgs e)
        {
            // initialize values that rely on the image
            _docview = this.GetFirstAncestorOfType<DocumentView>();
            Focus(FocusState.Keyboard);
            _cropControl = new StateCropControl(LayoutDocument, this);
        }

        private void GoToUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            if (args.NewValue != null)
            {
                _annotationOverlay.SelectRegion(args.NewValue as DocumentController);

               // sender.RemoveField(KeyStore.GoToRegionKey);
            }
        }

        public async Task Rotate()
        {
            await transformImage(Rect.Empty, BitmapRotation.Clockwise90Degrees, BitmapFlip.None);
        }

        public async Task MirrorHorizontal()
        {
            await MirrorImage(BitmapFlip.Horizontal);
        }

        public async Task MirrorVertical()
        {
            await MirrorImage(BitmapFlip.Vertical);
        }

        private async Task MirrorImage(BitmapFlip flip)
        {
            await transformImage(Rect.Empty, BitmapRotation.None, flip);
        }

        // called when the cropclick action is invoked in the image subtoolbar
        public void StartCrop()
        {
            // make sure that we aren't already cropping
            if (xGrid.Children.Contains(_cropControl)) return;
            Focus(FocusState.Programmatic);
            xGrid.Children.Add(_cropControl);
            IsCropping = true;
        }

        private void StopImageFromMoving(object sender, PointerRoutedEventArgs e)
        {
            // prevent the image from being moved while being cropped
            if (IsCropping) e.Handled = true;
        }

        public async Task Crop(Rect rectangleGeometry)
        {
            await transformImage(rectangleGeometry, BitmapRotation.None, BitmapFlip.None);
        }
        /// <summary>
        ///     crops the image with respect to the values of the rectangle passed in
        /// </summary>
        /// <param name="rectangleGeometry">
        ///     rectangle geometry that determines the size and starting point of the crop
        /// </param>
        private async Task transformImage(Rect rect, BitmapRotation rot, BitmapFlip flip)
        {
            var rectangleGeometry = rect != Rect.Empty ? rect : new Rect(0,0, Math.Floor(Image.ActualHeight),Math.Floor(Image.ActualWidth));
            var file = await GetImageFile();
            var fileProperties = await file.Properties.GetImagePropertiesAsync();
            var fileRot = fileProperties.Orientation;

            // retrieves data from rectangle
            var startPointX = (uint)(rectangleGeometry.X      / Image.ActualWidth  * fileProperties.Width);
            var startPointY = (uint)(rectangleGeometry.Y      / Image.ActualHeight * fileProperties.Height);
            var height      = (uint)(rectangleGeometry.Height / Image.ActualHeight * fileProperties.Height);
            var width       = (uint)(rectangleGeometry.Width  / Image.ActualWidth  * fileProperties.Width);
            switch (rot)
            {
            case BitmapRotation.None:
                if (fileRot == PhotoOrientation.Normal || fileRot == PhotoOrientation.Unspecified)
                     rot = BitmapRotation.None;
                else if (fileRot == PhotoOrientation.Rotate90)
                     rot = BitmapRotation.Clockwise270Degrees;
                else if (fileRot == PhotoOrientation.Rotate180)
                     rot = BitmapRotation.Clockwise180Degrees;
                else rot = BitmapRotation.Clockwise90Degrees;
                break;
            case BitmapRotation.Clockwise90Degrees:
                if (fileRot == PhotoOrientation.Normal || fileRot == PhotoOrientation.Unspecified)
                     rot = BitmapRotation.Clockwise90Degrees;
                else if (fileRot == PhotoOrientation.Rotate90)
                     rot = BitmapRotation.None;
                else if (fileRot == PhotoOrientation.Rotate180)
                     rot = BitmapRotation.Clockwise270Degrees;
                else rot = BitmapRotation.Clockwise180Degrees;
                break;
            case BitmapRotation.Clockwise180Degrees:
                var tmp = width; // bcz: can't quite figure out why I need to flip width/height, but I do...
                width  = height;
                height = tmp;
                if (fileRot == PhotoOrientation.Normal || fileRot == PhotoOrientation.Unspecified)
                     rot = BitmapRotation.Clockwise180Degrees;
                else if (fileRot == PhotoOrientation.Rotate90)
                     rot = BitmapRotation.Clockwise90Degrees;
                else if (fileRot == PhotoOrientation.Rotate180)
                     rot = BitmapRotation.None;
                else rot = BitmapRotation.Clockwise270Degrees;
                break;
            case BitmapRotation.Clockwise270Degrees:
                if (fileRot == PhotoOrientation.Normal || fileRot == PhotoOrientation.Unspecified)
                     rot = BitmapRotation.Clockwise270Degrees;
                else if (fileRot == PhotoOrientation.Rotate90)
                     rot = BitmapRotation.Clockwise180Degrees;
                else if (fileRot == PhotoOrientation.Rotate180)
                     rot = BitmapRotation.Clockwise90Degrees;
                else rot = BitmapRotation.None;
                break;
            }

            if (LayoutDocument.GetDataDocument().GetField<ImageController>(KeyStore.OriginalImageKey) == null)
            {
                var origImgCtrl = LayoutDocument.GetDataDocument().GetDereferencedField<ImageController>(DataFieldKey, new Context());
                LayoutDocument.GetDataDocument().SetField(KeyStore.OriginalImageKey, origImgCtrl.Copy(), true);
            }

            // opens the uri path and reads it
            using (var stream = await file.OpenReadAsync())
            {
                var decoder = await BitmapDecoder.CreateAsync(stream);

                // sets the boundaries for how we are cropping the bitmap image
                var bitmapTransform = new BitmapTransform() { Rotation = rot, Flip = flip, ScaledHeight = decoder.PixelHeight, ScaledWidth = decoder.PixelWidth };
                bitmapTransform.Bounds = new BitmapBounds {
                        X = startPointX,
                        Y = startPointY,
                        Width = width,
                        Height = height
                    };

                // creates a new bitmap image with those boundaries
                var pix = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Straight,
                    bitmapTransform,
                    ExifOrientationMode.IgnoreExifOrientation,
                    ColorManagementMode.ColorManageToSRgb
                );
                
                SaveCroppedImageAsync(width, height, decoder, pix.DetachPixelData());
            }
        }

        private async void SaveCroppedImageAsync(uint width, uint height, BitmapDecoder decoder, byte[] pixels)
        {
            using (UndoManager.GetBatchHandle())
            {
                // dis is it, the new bitmap image
                var cropBmp = new WriteableBitmap((int)width, (int)height);
                cropBmp.PixelBuffer.AsStream().Write(pixels, 0, (int)(width * height * 4));
                // update the image source, width, and positions
                Image.Source = cropBmp;
                
                var newFile = await ImageToDashUtil.CreateUniqueLocalFile();
                var bitmapEncoderGuid = BitmapEncoder.JpegEncoderId;

                // load the file with the iamge information
                using (var newStream = await newFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateAsync(bitmapEncoderGuid, newStream);

                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Straight,
                        width,
                        height,
                        decoder.DpiX,
                        decoder.DpiY,
                        pixels);
                    await encoder.FlushAsync();
                }
                
                var uri = new Uri(newFile.Path);
                LayoutDocument.GetDataDocument().SetField<ImageController>(DataFieldKey, uri, true);

                // store new image information so that multiple crops can be made
                _imgctrl = LayoutDocument.GetDataDocument().GetDereferencedField<ImageController>(DataFieldKey,null);

                var oldpoint = LayoutDocument.GetPosition() ?? new Point();
                var scale = LayoutDocument.GetField<PointController>(KeyStore.ScaleAmountFieldKey).Data;
                var oldAspect = LayoutDocument.GetActualSize().Value.X / LayoutDocument.GetActualSize().Value.Y;
                var newaspect = width / (double)height;
                if (newaspect > oldAspect)
                     LayoutDocument.SetHeight(LayoutDocument.GetActualSize().Value.X / newaspect);
                else LayoutDocument.SetWidth (LayoutDocument.GetActualSize().Value.Y * newaspect);
                var point = new Point(oldpoint.X + _cropControl.GetBounds().X * scale.X,
                                      oldpoint.Y + _cropControl.GetBounds().Y * scale.Y);

                LayoutDocument.SetPosition(point);
                _cropControl = new StateCropControl(LayoutDocument, this);
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // functionality for saving a crop and for moving the cropping boxes with directional keys
        private async void XGrid_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (IsCropping)
            {
                switch (e.Key)
                {
                case VirtualKey.Enter:
                    // crop the image!
                    IsCropping = false;
                    xGrid.Children.Remove(_cropControl);
                    await Crop(_cropControl.GetBounds());

                    break;
                case VirtualKey.Left:
                case VirtualKey.Right:
                case VirtualKey.Up:
                case VirtualKey.Down:
                    // moves the bounding box in the key's direction
                    _cropControl.OnKeyDown(e);
                    break;
                }
                e.Handled = true;
            }
        }

        // removes the cropping controls and allows image to be moved and used when focus is lost
        private void EditableImage_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (!IsCropping) return;
            IsCropping = false;
            xGrid.Children.Remove(_cropControl);
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (IsCropping) e.Handled = true;

            var point = e.GetCurrentPoint(_annotationOverlay);

            if (!IsCropping)
            {
                if (point.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
                {
                    _annotationOverlay.EndAnnotation(point.Position);
                    e.Handled = true;
                }
                else if(point.Properties.IsLeftButtonPressed && !_annotationOverlay.IsCtrlPressed())
                {
                    _annotationOverlay.UpdateAnnotation(point.Position);
                    e.Handled = true;
                }
            }
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (IsCropping) e.Handled = true;
            var point = e.GetCurrentPoint(_annotationOverlay);

            if (!IsCropping && point.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
            {
                _annotationOverlay.EndAnnotation(point.Position);
                e.Handled = true;
            }
            var curPt = e.GetCurrentPoint(this).Position;
            var delta = new Point(curPt.X - _downPt.X, curPt.Y - _downPt.Y);
            var dist = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
            if (!SelectionManager.IsSelected(this.GetFirstAncestorOfType<DocumentView>()) && dist > 10)
            {
                SelectionManager.Select(this.GetFirstAncestorOfType<DocumentView>(), this.IsShiftPressed());
            }
        }

        Point _downPt = new Point();
        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (IsCropping) e.Handled = true;
            var point = e.GetCurrentPoint(_annotationOverlay);
            if (!IsCropping && point.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
            {
                _annotationOverlay.StartAnnotation(_annotationOverlay.CurrentAnnotationType, point.Position);
            }
            _downPt = e.GetCurrentPoint(this).Position;
            e.Handled = true;
        }

        private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                _annotationOverlay.EmbedDocumentWithPin(e.GetPosition(_annotationOverlay));
            }
        }

        public async Task<DocumentController> GetRegionDocument(Point? docViewPoint)
        {
            var regionDoc = await _annotationOverlay.CreateRegionFromPreviewOrSelection();
            if (regionDoc == null)
            {
                if (docViewPoint != null)
                {
                    //else, make a new push pin region closest to given point
                    var overlayPoint = Util.PointTransformFromVisual(docViewPoint.Value, this.GetFirstAncestorOfType<DocumentView>(), _annotationOverlay);
                    var newPoint = calculateClosestPointOnImage(overlayPoint);

                    regionDoc = _annotationOverlay.CreatePinRegion(newPoint);
                }
                else
                    regionDoc = LayoutDocument;
            }
            return regionDoc;
        }
        private Point calculateClosestPointOnImage(Point p)
        {
            return new Point(p.X < 0 ? 30 : p.X > this._annotationOverlay.ActualWidth ? this._annotationOverlay.ActualWidth - 30 : p.X,
                             p.Y < 0 ? 30 : p.Y > this._annotationOverlay.ActualHeight ? this._annotationOverlay.ActualHeight - 30 : p.Y);
        }

        public void ShowRegions()
        {
            _annotationOverlay.Visibility = Visibility.Visible;
        }

        public void HideRegions()
        {
            _annotationOverlay.Visibility = Visibility.Collapsed;
        }


        public bool AreAnnotationsVisible()
        {
            return _annotationOverlay.Visibility == Visibility.Visible;
        }
    }
}
