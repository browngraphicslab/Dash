using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Annotations;
using DashShared;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash
{
    public partial class EditableImage : INotifyPropertyChanged
    {
        private readonly Context _context;
        private readonly DocumentController _docCtrl;
        private StateCropControl _cropControl;
        private ImageController _imgctrl;
        private ImageSource _imgSource;
        private bool _isCropping;
        private double _originalWidth;
        private DocumentView _docview;
        private Image _originalImage;

        public RectangleGeometry RectGeo;

        public Image Image => xImage;

        public ImageSource ImageSource
        {
            get => _imgSource;
            set
            {
                _imgSource = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public EditableImage(DocumentController docCtrl, Context context)
        {
            InitializeComponent();
            RectGeo = new RectangleGeometry();
            _docCtrl = docCtrl;
            _context = context;
            Image.Loaded += Image_Loaded;
            Image.SizeChanged += Image_SizeChanged;

            // gets datakey value (which holds an imagecontroller) and cast it as imagecontroller
            _imgctrl = docCtrl.GetDereferencedField(KeyStore.DataKey, context) as ImageController;
        }

        private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _docview = this.GetFirstAncestorOfType<DocumentView>();
            _docview.OnCropClick += OnCropClick;
            _docview.OnRevert += OnRevert;
            Focus(FocusState.Keyboard);
            _cropControl = new StateCropControl(_docCtrl, this);
        }

        private void OnRevert()
        {
            Image.Source = _originalImage.Source;
            Image.Height = _originalImage.Height;
            Image.Width = _originalImage.Width;
        }

        private void Image_Loaded(object sender, RoutedEventArgs e)
        {
            // initialize values that rely on the image
            _originalImage = Image;
            _originalWidth = Image.Width;
            _docview = this.GetFirstAncestorOfType<DocumentView>();
            _docview.OnCropClick += OnCropClick;
            Focus(FocusState.Keyboard);
            _cropControl = new StateCropControl(_docCtrl, this);
        }

        // called when the cropclick action is invoked in the image subtoolbar
        private void OnCropClick()
        {
            // make sure that we aren't already cropping
            if (xGrid.Children.Contains(_cropControl)) return;
            Focus(FocusState.Programmatic);
            xGrid.Children.Add(_cropControl);
            _docview.hideControls();
            _isCropping = true;
        }

        private void StopImageFromMoving(object sender, PointerRoutedEventArgs e)
        {
            // prevent the image from being moved while being cropped
            if (_isCropping) e.Handled = true;
        }

        /// <summary>
        ///     crops the image with respect to the values of the rectangle passed in
        /// </summary>
        /// <param name="rectangleGeometry">
        ///     rectangle geometry that determines the size and starting point of the crop
        /// </param>
        private async void OnCrop(Rect rectangleGeometry)
        {

            var scale = _originalWidth / Image.ActualWidth;

            // retrieves data from rectangle
            var startPointX = (uint) rectangleGeometry.X;
            var startPointY = (uint) rectangleGeometry.Y;
            var height = (uint) rectangleGeometry.Height;
            var width = (uint) rectangleGeometry.Width;

            // finds local uri path of image controller's image source
            StorageFile file;

            /*
             * try catch is literally the only way we can deal with regular
             * local uris, absolute uris, and website uris as the same time
             */
            try
            {
                // method of getting file from local uri
                file = await StorageFile.GetFileFromPathAsync(_imgctrl.ImageSource.LocalPath);
            }
            catch (Exception)
            {
                // method of getting file from absolute uri
                file = await StorageFile.GetFileFromApplicationUriAsync(_imgctrl.ImageSource);
            }

            Debug.Assert(file != null); // if neither works, something's hecked up
            WriteableBitmap cropBmp;

            // opens the uri path and reads it
            using (IRandomAccessStream stream = await file.OpenReadAsync())
            {
                var decoder = await BitmapDecoder.CreateAsync(stream);

                // finds scaled size of the new bitmap image
                var scaledWidth = (uint) Math.Floor(decoder.PixelWidth / scale);
                var scaledHeight = (uint) Math.Floor(decoder.PixelHeight / scale);

                // sets the boundaries for how we are cropping the bitmap image
                var bitmapTransform = new BitmapTransform();
                var bounds = new BitmapBounds
                {
                    X = startPointX,
                    Y = startPointY,
                    Width = width,
                    Height = height
                };
                bitmapTransform.Bounds = bounds;
                bitmapTransform.ScaledWidth = scaledWidth;
                bitmapTransform.ScaledHeight = scaledHeight;

                // creates a new bitmap image with those boundaries
                var pix = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Straight,
                    bitmapTransform,
                    ExifOrientationMode.IgnoreExifOrientation,
                    ColorManagementMode.ColorManageToSRgb
                );

                var pixels = pix.DetachPixelData();

                // dis is it, the new bitmap image
                cropBmp = new WriteableBitmap((int) width, (int) height);
                var pixStream = cropBmp.PixelBuffer.AsStream();
                pixStream.Write(pixels, 0, (int) (width * height * 4));

                SaveCroppedImageAsync(cropBmp, decoder, rectangleGeometry, pixels);
            }
        }

        private async void SaveCroppedImageAsync(WriteableBitmap cropBmp, BitmapDecoder decoder, Rect rectgeo,
            byte[] pixels)
        {
            var width = (uint) rectgeo.Width;
            var height = (uint) rectgeo.Height;

            // randomly generate a new guid for the filename
            var fileName = UtilShared.GenerateNewId() + ".jpg"; // .jpg works for all images
            var bitmapEncoderGuid = BitmapEncoder.JpegEncoderId;
            // create the file
            var newFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName,
                CreationCollisionOption.ReplaceExisting);

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

            // retrieve the uri from the file to update the image controller
            var path = "ms-appdata:///local/" + newFile.Name;
            var uri = new Uri(path);
            _docCtrl.SetField<ImageController>(KeyStore.DataKey, uri, true);
            SetupImageBinding(Image, _docCtrl, _context);

            // update the image source, width, and positions
            Image.Source = cropBmp;
            Image.Width = width;

            // store new image information so that multiple crops can be made
            _originalWidth = width;
            _imgctrl = _docCtrl.GetDereferencedField(KeyStore.DataKey, _context) as ImageController;

            var oldpoint = _docCtrl.GetField<PointController>(KeyStore.PositionFieldKey).Data;
            Point point = new Point(oldpoint.X + _cropControl.GetBounds().X, oldpoint.Y + _cropControl.GetBounds().Y);

            _docCtrl.SetField<PointController>(KeyStore.PositionFieldKey, point, true);
            _cropControl = new StateCropControl(_docCtrl, this);


            // TODO: Test that replace button works with cropping when merged with master
        }

        // stolen directly from Util/Courtesy Documents/ImageBox. tbh don't really know what it does
        private static void SetupImageBinding(Image image, DocumentController controller,
            Context context)
        {
            var data = controller.GetField(KeyStore.DataKey);
            if (data is ReferenceController reference)
            {
                var dataDoc = reference.GetDocumentController(context);
                dataDoc.AddFieldUpdatedListener(reference.FieldKey,
                    delegate(FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
                    {
                        var doc = (DocumentController) sender;
                        var dargs =
                            (DocumentController.DocumentFieldUpdatedEventArgs) args;
                        if (args.Action == DocumentController.FieldUpdatedAction.Update || dargs.FromDelegate)
                            return;
                        BindImageSource(image, doc, c, reference.FieldKey);
                    });
            }

            BindImageSource(image, controller, context, KeyStore.DataKey);
        }

        // stolen directly from Util/Courtesy Documents/ImageBox. tbh don't really know what it does
        protected static void BindImageSource(Image image, DocumentController docController, Context context,
            KeyController key)
        {
            var data = docController.GetDereferencedField(key, context) as ImageController;
            if (data == null)
                return;
            var binding = new FieldBinding<ImageController>
            {
                Document = docController,
                Key = KeyStore.DataKey,
                Mode = BindingMode.OneWay,
                Context = context,
                Converter = UriToBitmapImageConverter.Instance
            };
            image.AddFieldBinding(Image.SourceProperty, binding);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // functionality for saving a crop and for moving the cropping boxes with directional keys
        private void XGrid_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (_isCropping)
                switch (e.Key)
                {
                    case VirtualKey.Enter:
                        // crop the image!
                        _isCropping = false;
                        xGrid.Children.Remove(_cropControl);
                        OnCrop(_cropControl.GetBounds());
                        _docview.showControls();
                      



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

        // removes the cropping controls and allows image to be moved and used when focus is lost
        private void EditableImage_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (!_isCropping) return;
            _isCropping = false;
            _docview.showControls();
            xGrid.Children.Remove(_cropControl);
        }
    }
}