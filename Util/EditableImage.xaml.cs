using System;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Input;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using DashShared;
using Windows.UI;
using Windows.UI.Xaml;
using Flurl.Util;
using Rectangle = Windows.UI.Xaml.Shapes.Rectangle;
using Visibility = Windows.UI.Xaml.Visibility;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash
{
    public partial class EditableImage
    {
        private readonly Context _context;
        private readonly DocumentController _docCtrl;
        private ImageController _imgctrl;
        private bool _hasDragged;
        private bool _isLeft;
        private PointerPoint _p1;
        private PointerPoint _p2;
        private double _originalWidth;
        public RectangleGeometry RectGeo;
        public Image Image => xImage;

        public EditableImage(DocumentController docCtrl, Context context)
        {
            InitializeComponent();
            RectGeo = new RectangleGeometry();
            _docCtrl = docCtrl;
            _context = context;
            Image.Loaded += Image_Loaded;

            // gets datakey value (which holds an imagecontroller) and cast it as imagecontroller
            _imgctrl = docCtrl.GetDereferencedField(KeyStore.DataKey, context) as ImageController;
        }

        private void Image_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            _originalWidth = Image.Width; 
            var test = Image.RenderSize.Width;
            var docView = this.GetFirstAncestorOfType<DocumentView>();
            docView.OnCropClick += OnCropClick;

            //var transform = docView.RenderTransform;
            //transform.ToKeyValuePairs();
        }

        private void OnCropClick()
        {
            var docView = this.GetFirstAncestorOfType<DocumentView>();
            var mTransform = docView.RenderTransform as MatrixTransform;
            var xRect = new Rectangle
            {
                StrokeThickness = 4,
                Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)),
                Fill = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            xRect.Width = Image.ActualWidth;
            xRect.Height = Image.ActualHeight;
            xGrid.Children.Add(xRect);

            //transform.X = mTransform.Matrix.OffsetX;
            //transform.Y = mTransform.Matrix.OffsetX;
        }

        private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            //if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            //{
            //    _p1 = e.GetCurrentPoint(xImage);
            //    _isLeft = true;
            //    transform.X = _p1.Position.X;
            //    transform.Y = _p1.Position.Y;
            //}
        }

        private void Grid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            // TODO: Change to WhileCropClicked
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                //_p2 = e.GetCurrentPoint(xImage);
                //_hasDragged = true;
                //xRect.Visibility = Visibility.Visible;


                //xRect.Width = (int) Math.Abs(_p2.Position.X - _p1.Position.X);
                //xRect.Height = (int) Math.Abs(_p2.Position.Y - _p1.Position.Y);
            }
        }


        private async void Grid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            // TODO: Change to "WhileCropClicked && EnterKeyClicked"
            if (_isLeft && _hasDragged && !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                //_p2 = e.GetCurrentPoint(xImage);

                //xRect.Visibility = Visibility.Collapsed;


                //await Task.Delay(100);

                //RectGeo.Rect = new Rect(_p1.Position.X, _p1.Position.Y, xRect.Width, xRect.Height);


                ////xImage.Clip = rectgeo;

                ////docView.ViewModel.Width = xRect.Width;
                ////docView.ViewModel.Height = xRect.Height;

                //OnCrop(RectGeo.Rect);

                //_isLeft = false;
                //_hasDragged = false;
            }
        }

        /// <summary>
        ///     crops the image with respect to the values of the rectangle passed in
        /// </summary>
        /// <param name="rectangleGeometry"></param>
        private async void OnCrop(Rect rectangleGeometry)
        {
            var scale = (double) (_originalWidth / Image.ActualWidth);
            // retrieves data from rectangle
            var startPointX = (uint) (rectangleGeometry.X);
            var startPointY = (uint) (rectangleGeometry.Y);
            var height = (uint) (rectangleGeometry.Height);
            var width = (uint) (rectangleGeometry.Width);

            // finds local uri path of image controller's image source
            StorageFile file;
            try
            {
                file = await StorageFile.GetFileFromPathAsync(_imgctrl.ImageSource.LocalPath);
            }
            catch (Exception)
            {
                file = await StorageFile.GetFileFromApplicationUriAsync(_imgctrl.ImageSource);
            }

            Debug.Assert(file != null);
            WriteableBitmap cropBmp;

            // opens the uri path and reads it
            using (IRandomAccessStream stream = await file.OpenReadAsync())
            {
                var decoder = await BitmapDecoder.CreateAsync(stream);

                // The scaledSize of original image. 
                var scaledWidth = (uint) Math.Floor(decoder.PixelWidth / scale);
                var scaledHeight = (uint) Math.Floor(decoder.PixelHeight / scale);

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

                var pix = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Straight,
                    bitmapTransform,
                    ExifOrientationMode.IgnoreExifOrientation,
                    ColorManagementMode.ColorManageToSRgb
                );

                var pixels = pix.DetachPixelData();
                cropBmp = new WriteableBitmap((int) width, (int) height);
                var pixStream = cropBmp.PixelBuffer.AsStream();
                pixStream.Write(pixels, 0, (int) (width * height * 4));

                SaveCroppedImageAsync(cropBmp, decoder, width, height, pixels);
            }
        }

        private async void SaveCroppedImageAsync(WriteableBitmap cropBmp, BitmapDecoder decoder, uint width, uint height, byte[] pixels)
        {
            var fileName = UtilShared.GenerateNewId() + ".jpg";
            var bitmapEncoderGuid = BitmapEncoder.JpegEncoderId;
            var newFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName,
                CreationCollisionOption.ReplaceExisting);

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

            var path = "ms-appdata:///local/" + newFile.Name;
            var uri = new Uri(path);
            _docCtrl.SetField<ImageController>(KeyStore.DataKey, uri, true);
            SetupImageBinding(Image, _docCtrl, _context);
            Image.Source = cropBmp;
            Image.Width = width;
            //Image.RenderTransform
            _originalWidth = width;
            _imgctrl = _docCtrl.GetDereferencedField(KeyStore.DataKey, _context) as ImageController;

            // TODO: Test that replace button works with cropping when merged with master
        }
        
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
    }
}