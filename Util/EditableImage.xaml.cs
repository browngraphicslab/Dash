
using Windows.UI.Xaml.Controls;
using Windows.UI.Input;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using System.Diagnostics;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.Storage;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml.Data;
using DashShared;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash
{

    public partial class EditableImage
    {

        public Image Image => xImage;
        private PointerPoint p1;
        private PointerPoint p2;
        private bool isLeft;
        private bool hasDragged;
        public RectangleGeometry rectgeo;
        private ImageController _imgctrl;
        private DocumentController _docCtrl;
        private Context _context;
      
       

        public EditableImage(DocumentController docCtrl, Context context)
        {
            InitializeComponent();
            rectgeo = new RectangleGeometry();
            _docCtrl = docCtrl;
            _context = context;

            // gets datakey value (which holds an imagecontroller) and cast it as imagecontroller
            _imgctrl = docCtrl.GetDereferencedField(KeyStore.DataKey, context) as ImageController;
        }



        private void Grid_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            // TODO: Change to OnCropClick
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                p1 = e.GetCurrentPoint(xImage);
                isLeft = true;
                transform.X = p1.Position.X;
                transform.Y = p1.Position.Y;
            }

        }

        private void Grid_PointerMoved(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            // TODO: Change to WhileCropClicked
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                p2 = e.GetCurrentPoint(xImage);
                hasDragged = true;
                xRect.Visibility = Windows.UI.Xaml.Visibility.Visible;
               

                xRect.Width = (int)Math.Abs(p2.Position.X - p1.Position.X);
                xRect.Height = (int)Math.Abs(p2.Position.Y - p1.Position.Y);

            }

        }

        

        private async void Grid_PointerReleased(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            // TODO: Change to "WhileCropClicked && EnterKeyClicked"
            if (isLeft && hasDragged && !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                
                p2 = e.GetCurrentPoint(xImage);

                xRect.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            
                await Task.Delay(100);
                
                rectgeo.Rect = new Rect(p1.Position.X, p1.Position.Y, xRect.Width, xRect.Height);


                //xImage.Clip = rectgeo;

                //var docView = this.GetFirstAncestorOfType<DocumentView>();

                //Point point = new Point(rectgeo.Rect.X, rectgeo.Rect.Y);


                //docView.RenderTransform = transform;

                //docView.ViewModel.Width = xRect.Width;
                //docView.ViewModel.Height = xRect.Height;

                OnCrop(rectgeo.Rect);

                isLeft = false;
                hasDragged = false;

            }

        }

        /// <summary>
        ///     crops the image with respect to the values of the rectangle passed in
        /// </summary>
        /// <param name="rectangleGeometry"></param>
        private async void OnCrop(Rect rectangleGeometry)
        {
            var scale = .5;
            // retrieves data from rectangle
            var startPointX = (uint) (rectangleGeometry.X * scale);
            var startPointY = (uint) (rectangleGeometry.Y * scale);
            var height = (uint) (rectangleGeometry.Height * scale);
            var width = (uint) (rectangleGeometry.Width * scale);

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
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                // The scaledSize of original image. 
                uint scaledWidth = (uint)Math.Floor(decoder.PixelWidth * scale);
                uint scaledHeight = (uint)Math.Floor(decoder.PixelHeight * scale);

                BitmapTransform transform = new BitmapTransform();
                BitmapBounds bounds = new BitmapBounds()
                {
                    X = startPointX,
                    Y = startPointY,
                    Width = width,
                    Height = height
                };
                transform.Bounds = bounds;

                transform.ScaledWidth = scaledWidth;
                transform.ScaledHeight = scaledHeight;

                PixelDataProvider pix = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Straight,
                    transform,
                    ExifOrientationMode.IgnoreExifOrientation,
                    ColorManagementMode.ColorManageToSRgb
                );

                byte[] pixels = pix.DetachPixelData();

                cropBmp = new WriteableBitmap((int) width, (int) height);
                Stream pixStream = cropBmp.PixelBuffer.AsStream();
                pixStream.Write(pixels, 0, (int)(width * height * 4));
            }
            
            WriteableBitmapToStorageFile(cropBmp);
        }

        private async void WriteableBitmapToStorageFile(WriteableBitmap WB)
        {
            string FileName = UtilShared.GenerateNewId() + ".jpg";
            Guid BitmapEncoderGuid = BitmapEncoder.JpegEncoderId;

            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(FileName, CreationCollisionOption.ReplaceExisting);
            using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoderGuid, stream);
                Stream pixelStream = WB.PixelBuffer.AsStream();
                byte[] pixels = new byte[pixelStream.Length];
                await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                    (uint)WB.PixelWidth,
                    (uint)WB.PixelHeight,
                    96.0,
                    96.0,
                    pixels);
                await encoder.FlushAsync();
            }

            string path = "ms-appdata:///local/" + file.Name;
            Uri uri = new Uri(path);
            _docCtrl.SetField<ImageController>(KeyStore.DataKey, uri, true);
            SetupImageBinding(Image, _docCtrl, _context);
            Image.Source = new BitmapImage(uri);
            _imgctrl = _docCtrl.GetDereferencedField(KeyStore.DataKey, _context) as ImageController;
        }


        private static void SetupImageBinding(Image image, DocumentController controller,
           Context context)
        {
            var data = controller.GetField(KeyStore.DataKey);
            if (data is ReferenceController)
            {
                var reference = data as ReferenceController;
                var dataDoc = reference.GetDocumentController(context);
                dataDoc.AddFieldUpdatedListener(reference.FieldKey,
                    delegate (FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
                    {
                        var doc = (DocumentController) sender;
                        var dargs =
                            (DocumentController.DocumentFieldUpdatedEventArgs)args;
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
