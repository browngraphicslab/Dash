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

    public partial class EditableImage 
    {
        private StateCropControl  _cropControl;
        private AnnotationOverlay _annotationOverlay;
        private ImageController   _imgctrl;

        public DocumentController DataDocument   => (DataContext as DocumentViewModel).DataDocument;
        public DocumentController LayoutDocument => (DataContext as DocumentViewModel).LayoutDocument;

        public bool               AreAnnotationsVisible =>  _annotationOverlay?.Visibility == Visibility.Visible;
        public Image              Image                 => xImage;
        public Stretch            Stretch               => xImage.Stretch;
        public Viewbox            Viewbox               => xViewbox;
        public bool               IsCropping   
        {
            get => _cropControl != null;
            set
            {
                if (value != IsCropping)
                {
                    if (value)
                    {
                        _cropControl = new StateCropControl(LayoutDocument, this);
                        Focus(FocusState.Programmatic);
                        xGrid.Children.Add(_cropControl);
                    }
                    else
                    {
                        xGrid.Children.Remove(_cropControl);
                        _cropControl = null;

                    }
                }
            }
        }
        public KeyController      DataFieldKey { get; set; }

        public EditableImage()
        {
            InitializeComponent();
            LostFocus += (s, e) => IsCropping = false;
            Image.ImageOpened += (sender, args) =>
            {
                var source = Image.Source as BitmapSource;
                if (source != null)
                {
                    var pw = source.PixelWidth;
                    var scaling = pw > 800 ? 800.0 / pw : 1;
                    XAnnotationGrid.Width = source.PixelWidth * scaling;
                    XAnnotationGrid.Height = source.PixelHeight * scaling;
                }
                else
                {
                    XAnnotationGrid.Width = Image.ActualWidth;
                    XAnnotationGrid.Height = Image.ActualHeight;
                }

                //_annotationOverlay = new AnnotationOverlay(LayoutDocument);
                //_annotationOverlay.CurrentAnnotationType = AnnotationType.Region;
                //XAnnotationGrid.Children.Add(_annotationOverlay);
                //XAnnotationGridWithEmbeddings.Children.Add(_annotationOverlay.AnnotationOverlayEmbeddings);
            };
        }

        public async Task<DocumentController> GetRegionDocument(Point? docViewPoint)
        {
            Point calculateClosestPointOnImage(Point p)
            {
                return new Point(p.X < 0 ? 30 : p.X > _annotationOverlay.ActualWidth ? _annotationOverlay.ActualWidth - 30 : p.X,
                                 p.Y < 0 ? 30 : p.Y > _annotationOverlay.ActualHeight ? _annotationOverlay.ActualHeight - 30 : p.Y);
            }
            var regionDoc = await _annotationOverlay.CreateRegionFromPreviewOrSelection();
            if (regionDoc == null && docViewPoint != null)
            {
                var pointInAnnotationOverlayCoords = Util.PointTransformFromVisual(docViewPoint.Value, this.GetDocumentView(), _annotationOverlay);
                regionDoc = _annotationOverlay.CreatePinRegion(calculateClosestPointOnImage(pointInAnnotationOverlayCoords));
            }
            return regionDoc ?? LayoutDocument;
        }
        public async Task Rotate()           { await transformImage(Rect.Empty, BitmapRotation.Clockwise90Degrees, BitmapFlip.None); }
        public async Task MirrorHorizontal() { await MirrorImage(BitmapFlip.Horizontal); }
        public async Task MirrorVertical()   { await MirrorImage(BitmapFlip.Vertical); }
        public async Task ReplaceImage()
        {
            // get the file from the current image controller
            var file = await GetImageFile();
            var fileProperties = await file.Properties.GetImagePropertiesAsync();

            // set image source to the new file path and fix the width
            Image.Source = new BitmapImage(new Uri(file.Path));

            // on replace image, change the original image value for revert
            var origImgCtrl = LayoutDocument.GetDataDocument().GetDereferencedField<ImageController>(DataFieldKey, null);
            LayoutDocument.GetDataDocument().SetField(KeyStore.OriginalImageKey, origImgCtrl, true);
            LayoutDocument.SetWidth(LayoutDocument.GetActualSize().X);
            LayoutDocument.SetHeight(double.NaN);
        }
        public async void Revert()
        {
            using (UndoManager.GetBatchHandle())
            {
                // make sure if we have an original image stored (which we always should)
                if (LayoutDocument.GetDataDocument().GetField(KeyStore.OriginalImageKey) is ImageController originalImage)
                {
                    LayoutDocument.GetDataDocument().SetField<ImageController>(DataFieldKey, originalImage.ImageSource, true);
                    var file = await GetImageFile();
                    var fileProperties = await file.Properties.GetImagePropertiesAsync();
                    if (Math.Sign(-1+1.0*fileProperties.Width / fileProperties.Height) != 
                        Math.Sign(-1+1.0*LayoutDocument.GetActualSize().X / LayoutDocument.GetActualSize().Y))
                    {
                        LayoutDocument.SetWidth(LayoutDocument.GetWidth() / (LayoutDocument.GetActualSize().X / LayoutDocument.GetActualSize().Y));
                    }
                    LayoutDocument.SetHeight(double.NaN);
                }
            }
        }
        public void       SetRegionVisibility(Visibility state)
        {
            _annotationOverlay.Visibility = state;
            XAnnotationGridWithEmbeddings.Visibility = state;
        }

        private void      GoToUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            if (args.NewValue != null)
            {
                _annotationOverlay.SelectRegion(args.NewValue as DocumentController);

               // sender.RemoveField(KeyStore.GoToRegionKey);
            }
        }

        private async Task  MirrorImage(BitmapFlip flip)
        {
            await transformImage(Rect.Empty, BitmapRotation.None, flip);
        }
        private async Task  Crop(Rect rectangleGeometry)
        {
            var scaling = 1.0 * Image.ActualWidth / ActualWidth;
            var scaledRect = new Rect(rectangleGeometry.X * scaling, rectangleGeometry.Y * scaling, rectangleGeometry.Width * scaling, rectangleGeometry.Height * scaling);
            await transformImage(scaledRect, BitmapRotation.None, BitmapFlip.None);
        }
        /// <summary>
        ///     crops the image with respect to the values of the rectangle passed in
        /// </summary>
        /// <param name="rectangleGeometry">
        ///     rectangle geometry that determines the size and starting point of the crop
        /// </param>
        private async Task  transformImage(Rect rect, BitmapRotation rot, BitmapFlip flip)
        {
            var rectGeometry = rect != Rect.Empty ? rect : new Rect(0,0, Math.Floor(Image.ActualWidth),Math.Floor(Image.ActualHeight));
            var file         = await GetImageFile();
            var fileProps    = await file.Properties.GetImagePropertiesAsync();
            var fileRot      = fileProps.Orientation;  

            // retrieves data from rectangle
            var startPointX = (uint)(rectGeometry.X      / Image.ActualWidth  * fileProps.Width);
            var startPointY = (uint)(rectGeometry.Y      / Image.ActualHeight * fileProps.Height);
            var height      = (uint)(rectGeometry.Height / Image.ActualHeight * fileProps.Height);
            var width       = (uint)(rectGeometry.Width  / Image.ActualWidth  * fileProps.Width);
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
                var origImgCtrl = LayoutDocument.GetDataDocument().GetDereferencedField<ImageController>(DataFieldKey, null);
                LayoutDocument.GetDataDocument().SetField(KeyStore.OriginalImageKey, origImgCtrl.Copy(), true);
            }

            // opens the uri path and reads it
            using (var stream = await file.OpenReadAsync())
            {
                var decoder = await BitmapDecoder.CreateAsync(stream);
                var rotated = rot == BitmapRotation.Clockwise270Degrees || rot == BitmapRotation.Clockwise90Degrees;
                // sets the boundaries for how we are cropping the bitmap image
                var bmpXform = new BitmapTransform() { Rotation = rot, Flip = flip, ScaledHeight = decoder.PixelHeight, ScaledWidth = decoder.PixelWidth,
                                                       Bounds = new BitmapBounds { X = startPointX, Y = startPointY, Width = rotated ? height: width, Height = rotated? width:height } };

                // creates a new bitmap image with those boundaries
                var pix = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Straight,
                    bmpXform,
                    ExifOrientationMode.IgnoreExifOrientation,
                    ColorManagementMode.ColorManageToSRgb
                );
                
                SaveCroppedImageAsync(bmpXform.Bounds.Width, bmpXform.Bounds.Height, decoder, pix.DetachPixelData(), rot);
            }
        }
        private async void  SaveCroppedImageAsync(uint width, uint height, BitmapDecoder decoder, byte[] pixels, BitmapRotation rot)
        {
            using (UndoManager.GetBatchHandle())
            {
                // dis is it, the new bitmap image
                var cropBmp = new WriteableBitmap((int)width, (int)height);
                cropBmp.PixelBuffer.AsStream().Write(pixels, 0, (int)(width * height * 4));
                // update the image source, width, and positions
                Image.Source = cropBmp;
                
                var newFile = await ImageToDashUtil.CreateUniqueLocalFile();
                // load the file with the iamge information
                using (var newStream = await newFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, newStream);

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
                
                LayoutDocument.GetDataDocument().SetField<ImageController>(DataFieldKey, new Uri(newFile.Path), true);

                var oldpoint   = LayoutDocument.GetPosition();
                var actualSize = LayoutDocument.GetActualSize();
                if (rot == BitmapRotation.Clockwise90Degrees || rot == BitmapRotation.Clockwise270Degrees)
                {
                    LayoutDocument.SetWidth(actualSize.Y);
                    if (!double.IsNaN(LayoutDocument.GetHeight()))
                    {
                        LayoutDocument.SetHeight(actualSize.X);
                    }
                }
                else if (rot == BitmapRotation.Clockwise180Degrees)
                { }
                else
                {
                    var oldAspect = LayoutDocument.GetActualSize().X / LayoutDocument.GetActualSize().Y;
                    var newaspect = width / (double)height;
                    if (newaspect > oldAspect)
                         LayoutDocument.SetHeight(LayoutDocument.GetActualSize().X / newaspect);
                    else LayoutDocument.SetWidth(LayoutDocument.GetActualSize().Y * newaspect);
                }
                
                LayoutDocument.SetPosition(new Point(oldpoint.X + (_cropControl?.GetBounds().X ?? 0),
                                                     oldpoint.Y + (_cropControl?.GetBounds().Y ?? 0)));
            }
        }

        // functionality for saving a crop and for moving the cropping boxes with directional keys
        private async void XGrid_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (IsCropping)
            {
                switch (e.Key)
                {
                    case VirtualKey.Enter: // crop the image!
                        await Crop(_cropControl.GetBounds());
                        IsCropping = false;
                        break;
                    case VirtualKey.Left:
                    case VirtualKey.Right:
                    case VirtualKey.Up: // moves the bounding box in the key's direction
                        _cropControl.OnKeyDown(e);
                        break;
                }
                e.Handled = true;
            }
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (IsCropping)
            {
                e.Handled = true;
            }
            else if (!IsCropping && this.GetDocumentView().AreContentsActive && e.IsLeftPressed() && _annotationOverlay != null)
            {
                var point = e.GetCurrentPoint(_annotationOverlay);
                _annotationOverlay.StartAnnotation(_annotationOverlay.CurrentAnnotationType, point.Position);
                e.Handled = true;
            }
        }
        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_annotationOverlay == null || IsCropping)
            {
                e.Handled = true;
                return;
            }
            var point = e.GetCurrentPoint(_annotationOverlay);
            if (point.Properties.IsLeftButtonPressed && !_annotationOverlay.IsCtrlPressed())
            {
                _annotationOverlay.UpdateAnnotation(point.Position);
                e.Handled = true;
            }
        }
        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(_annotationOverlay);
            if (_annotationOverlay == null || IsCropping)
            {
                e.Handled = true;
            }
            else if (!IsCropping && point.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
            {
                _annotationOverlay.EndAnnotation(point.Position);
                e.Handled = true;
            }
        }
        private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            _annotationOverlay.AnnotationOverlayDoubleTapped(sender, e);
        }

        private async Task<StorageFile> GetImageFile()
        {
            _imgctrl = LayoutDocument.GetDereferencedField<ImageController>(DataFieldKey, null);
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
    }
}
